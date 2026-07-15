using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ComingSoonPlugin.Models;
using ComingSoonPlugin.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace ComingSoonPlugin.ScheduledTasks
{
    /// <summary>
    /// Pulls Sonarr/Radarr calendars once, then for each admin-configured list resolves/filters
    /// the relevant items, enriches them, and syncs them into that list's own library folder via
    /// ComingSoonFileLibrarySync.
    /// </summary>
    public class RefreshComingSoonTask : IScheduledTask
    {
        // Bump when the burned-in badge appearance changes, so the next refresh forces Emby to
        // re-read the re-rendered poster/backdrop images for items already in the library.
        private const int BadgeRenderVersion = 2;

        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        private readonly ILibraryManager _libraryManager;
        private readonly MediaBrowser.Model.IO.IFileSystem _fileSystem;
        private static readonly HttpClient SharedHttpClient = new();

        // Emby's DI container only registers the legacy ILogManager/ILogger, not the generic
        // ILogger<T> - see LegacyLoggerAdapter.
        public RefreshComingSoonTask(
            MediaBrowser.Model.Logging.ILogManager logManager, IProviderManager providerManager,
            ILibraryManager libraryManager, MediaBrowser.Model.IO.IFileSystem fileSystem)
        {
            _logger = new LegacyLoggerAdapter(logManager.GetLogger("ComingSoon"));
            _providerManager = providerManager;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
        }

        public string Name => "Refresh Coming Soon";
        public string Description => "Pulls Sonarr/Radarr calendars and refreshes the Coming Soon home screen lists.";
        public string Category => "Coming Soon";
        public string Key => "ComingSoonRefresh";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = new TimeSpan(0, 0, 15, 0).Ticks
            };
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            // Deduplicate by name - two lists with the same name resolve to the same library
            // folder anyway, and it cleans up any duplicates left by the old config-growth bug.
            var lists = config.Lists is { Count: > 0 }
                ? config.Lists
                    .GroupBy(l => (l.Name ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList()
                : new List<ComingSoonListConfig> { new() };

            if (string.IsNullOrWhiteSpace(config.TmdbApiKey))
            {
                _logger.LogInformation(
                    "Coming Soon: no TMDB API key configured - regional movie dates and local trailer download unavailable; Radarr dates will be used.");
            }

            var radarrService = new RadarrService(SharedHttpClient, _logger);
            var sonarrService = new SonarrService(SharedHttpClient, _logger);
            var tmdbService = new TmdbService(SharedHttpClient, _logger, config.TmdbApiKey, config.Language);
            var embyProviderService = new EmbyProviderMetadataService(_providerManager, _logger, config.Language);
            var aggregator = new MetadataAggregator(embyProviderService, tmdbService, _logger);
            var imageOverlayService = new ImageOverlayService(
                SharedHttpClient, _logger, Plugin.Instance!.FontsPath, config.Language, config.BadgePosition);

            // With no explicit ffmpeg path, TrailerDownloader downloads a progressive (already
            // muxed) stream that needs no merge, so ffmpeg isn't required at all. Emby's own
            // bundled ffmpeg is deliberately NOT auto-detected: it can be years out of date on
            // some images, and using it for merging would fail on every refresh. Users who want
            // higher-resolution merged trailers can point FfmpegPath at a modern ffmpeg.
            var ffmpegPath = config.FfmpegPath;

            var ytDlpPath = config.DownloadTrailersLocally
                ? await YtDlpProvisioner.ResolveAsync(
                    config.YtDlpPath, Plugin.Instance!.DataFolderPath, SharedHttpClient, _logger, cancellationToken)
                    .ConfigureAwait(false)
                : config.YtDlpPath;

            var trailerDownloader = new TrailerDownloader(_logger, ytDlpPath, ffmpegPath);

            var rawMovies = new List<CalendarItem>();
            var radarrFetchSucceeded = true;
            if (config.RadarrEnabled && !string.IsNullOrWhiteSpace(config.RadarrUrl) && lists.Any(l => l.IncludeMovies))
            {
                try
                {
                    rawMovies = await radarrService.GetUpcomingRawAsync(
                        config.RadarrUrl, config.RadarrApiKey, config.DaysAhead, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    radarrFetchSucceeded = false;
                    _logger.LogError(ex, "Coming Soon: Radarr calendar fetch failed");
                }
            }

            var episodes = new List<CalendarItem>();
            var sonarrFetchSucceeded = true;
            if (config.SonarrEnabled && !string.IsNullOrWhiteSpace(config.SonarrUrl) && lists.Any(l => l.IncludeTvShows))
            {
                try
                {
                    episodes = await sonarrService.GetUpcomingAsync(
                        config.SonarrUrl, config.SonarrApiKey, config.DaysAhead, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    sonarrFetchSucceeded = false;
                    _logger.LogError(ex, "Coming Soon: Sonarr calendar fetch failed");
                }
            }

            // Metadata (overview/cast/poster URL/trailer key) doesn't depend on which date a list
            // resolved, so it's fetched once per unique item and reused across every list that
            // includes it - avoids redundant TMDB calls for a movie appearing in multiple lists.
            var metadataCache = new Dictionary<string, MetadataResult>();

            // One lookup per unique movie+region per run. The same movie can appear in several
            // lists, and lists can deliberately use different countries.
            var regionalReleaseDateCache = new Dictionary<string, RegionalReleaseDates?>(StringComparer.OrdinalIgnoreCase);

            // Maps a YouTube trailer key to the first local file downloaded for it this run, so
            // the same trailer is fetched once and copied to every other folder that needs it.
            var trailerPathCache = new Dictionary<string, string>();

            // A language switch or a badge-design change alters the burned-in poster/backdrop, so
            // existing items need Emby to re-read their re-rendered local images - a plain file
            // scan won't pick that up. Bump BadgeRenderVersion whenever the badge appearance
            // changes so a one-time forced refresh runs after that update.
            var needsImageRefresh = config.LastSyncedLanguage != config.Language
                || config.LastBadgeRenderVersion != BadgeRenderVersion;

            var now = DateTime.UtcNow.Date;
            var windowEnd = now.AddDays(config.DaysAhead);
            var listIndex = 0;

            foreach (var list in lists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var itemsForList = new List<CalendarItem>();

                if (list.IncludeMovies)
                {
                    foreach (var movie in rawMovies)
                    {
                        RegionalReleaseDates? regionalDates = null;
                        if (movie.TmdbId.HasValue && !string.IsNullOrWhiteSpace(config.TmdbApiKey))
                        {
                            var region = NormalizeRegion(list.ReleaseDateRegion);
                            var cacheKey = $"{movie.TmdbId.Value}:{region}";
                            if (!regionalReleaseDateCache.TryGetValue(cacheKey, out regionalDates))
                            {
                                regionalDates = await tmdbService.GetMovieReleaseDatesAsync(
                                    movie.TmdbId.Value, region, cancellationToken).ConfigureAwait(false);
                                regionalReleaseDateCache[cacheKey] = regionalDates;
                            }
                        }

                        var resolvedDate = RadarrService.ResolveDate(movie, list.MovieDateType, regionalDates);
                        if (resolvedDate is null && regionalDates is not null)
                        {
                            _logger.LogDebug(
                                "Coming Soon: no {DateType} date for '{Title}' in region {Region}; omitted from list '{List}'",
                                list.MovieDateType, movie.Title, NormalizeRegion(list.ReleaseDateRegion), list.Name);
                        }

                        if (resolvedDate is null || resolvedDate.Value.Date < now || resolvedDate.Value.Date > windowEnd)
                        {
                            continue;
                        }

                        var clone = movie.Clone();
                        clone.Date = resolvedDate.Value;
                        itemsForList.Add(clone);
                    }
                }

                if (list.IncludeTvShows)
                {
                    itemsForList.AddRange(episodes);
                }

                var results = new List<CachedMediaItem>();
                var libraryDataPath = Plugin.Instance!.GetLibraryDataPath(list.Name);

                foreach (var calendarItem in itemsForList.OrderBy(i => i.Date))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var badgeKind = BadgeKindFor(calendarItem.Type, list.MovieDateType);
                        var cached = await EnrichAsync(
                            calendarItem, aggregator, imageOverlayService, trailerDownloader, config,
                            libraryDataPath, metadataCache, trailerPathCache, badgeKind, cancellationToken).ConfigureAwait(false);

                        if (cached != null)
                        {
                            results.Add(cached);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Coming Soon: failed to enrich '{Title}' for list '{List}'", calendarItem.Title, list.Name);
                    }
                }

                try
                {
                    new ComingSoonFileLibrarySync(
                        _libraryManager, _providerManager, _fileSystem, _logger, list.Name, libraryDataPath, config.Language)
                        .Sync(
                            results,
                            needsImageRefresh,
                            removeStaleMovies: !list.IncludeMovies || radarrFetchSucceeded,
                            removeStaleEpisodes: !list.IncludeTvShows || sonarrFetchSucceeded);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Coming Soon: library sync failed for list '{List}'", list.Name);
                }

                _logger.LogInformation("Coming Soon: list '{List}' complete - {Count} items", list.Name, results.Count);

                listIndex++;
                progress.Report(listIndex / (double)lists.Count * 100);
            }

            if (needsImageRefresh)
            {
                config.LastSyncedLanguage = config.Language;
                config.LastBadgeRenderVersion = BadgeRenderVersion;
                Plugin.Instance?.SaveConfiguration();
            }
        }

        private static string NormalizeRegion(string? value)
        {
            var region = (value ?? string.Empty).Trim().ToUpperInvariant();
            return region.Length == 2 && region.All(char.IsLetter) ? region : "DE";
        }

        // Episodes are always "Upcoming"; movies get a badge matching the list's release-date type.
        private static ReleaseBadgeKind BadgeKindFor(CalendarItemType type, RadarrReleaseDateType dateType)
        {
            if (type != CalendarItemType.Movie)
            {
                return ReleaseBadgeKind.Upcoming;
            }

            return dateType switch
            {
                RadarrReleaseDateType.InCinemas => ReleaseBadgeKind.InCinemas,
                RadarrReleaseDateType.Physical => ReleaseBadgeKind.OnDisc,
                RadarrReleaseDateType.Digital => ReleaseBadgeKind.Digital,
                _ => ReleaseBadgeKind.Upcoming
            };
        }

        private static async Task<CachedMediaItem?> EnrichAsync(
            CalendarItem calendarItem,
            MetadataAggregator aggregator,
            ImageOverlayService imageOverlayService,
            TrailerDownloader trailerDownloader,
            PluginConfiguration config,
            string libraryDataPath,
            Dictionary<string, MetadataResult> metadataCache,
            Dictionary<string, string> trailerPathCache,
            ReleaseBadgeKind badgeKind,
            CancellationToken cancellationToken)
        {
            if (calendarItem.Type == CalendarItemType.Movie && !calendarItem.TmdbId.HasValue)
            {
                return null; // can't enrich or badge without an id
            }

            if (calendarItem.Type == CalendarItemType.Episode && !calendarItem.TvdbId.HasValue)
            {
                return null;
            }

            if (!metadataCache.TryGetValue(calendarItem.MetadataCacheKey, out var metadata))
            {
                metadata = calendarItem.Type == CalendarItemType.Movie
                    ? await aggregator.GetForMovieAsync(calendarItem.TmdbId!.Value, cancellationToken).ConfigureAwait(false)
                    : await aggregator.GetForSeriesAsync(calendarItem.TvdbId!.Value, cancellationToken).ConfigureAwait(false);
                metadataCache[calendarItem.MetadataCacheKey] = metadata;
            }

            var folderName = ComingSoonEntryPaths.BuildFolderName(
                calendarItem.Title, calendarItem.Type, calendarItem.SeasonNumber, calendarItem.EpisodeNumber,
                calendarItem.TmdbId, calendarItem.TvdbId);
            var entryDir = Path.Combine(libraryDataPath, folderName);

            var posterUrl = metadata.PosterUrl ?? calendarItem.PosterUrl;
            if (!string.IsNullOrWhiteSpace(posterUrl))
            {
                await imageOverlayService.DownloadAndBadgeAsync(
                    posterUrl, calendarItem.Date, entryDir, cancellationToken,
                    calendarItem.SeasonNumber, calendarItem.EpisodeNumber, badgeKind)
                    .ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(metadata.BackdropUrl))
            {
                await imageOverlayService.DownloadBackdropAsync(metadata.BackdropUrl, entryDir, cancellationToken, badgeKind)
                    .ConfigureAwait(false);
            }

            if (config.DownloadTrailersLocally && !string.IsNullOrWhiteSpace(metadata.TrailerYoutubeKey))
            {
                var trailerKey = metadata.TrailerYoutubeKey!;
                var trailerDir = Path.Combine(entryDir, "trailers");

                // Reuse a trailer already downloaded this run for the same YouTube video (every
                // episode of a series shares one trailer, and a movie can appear in several lists)
                // instead of fetching it from YouTube again per folder.
                if (trailerPathCache.TryGetValue(trailerKey, out var existingTrailer) && File.Exists(existingTrailer))
                {
                    trailerDownloader.ReuseExisting(existingTrailer, trailerDir);
                }
                else
                {
                    var downloaded = await trailerDownloader.DownloadAsync(trailerKey, trailerDir, cancellationToken)
                        .ConfigureAwait(false);
                    if (downloaded != null)
                    {
                        trailerPathCache[trailerKey] = downloaded;
                    }
                }
            }

            return new CachedMediaItem
            {
                Type = calendarItem.Type,
                Title = calendarItem.Title,
                Date = calendarItem.Date,
                Overview = metadata.Overview ?? calendarItem.Overview,
                TmdbId = calendarItem.TmdbId,
                TvdbId = calendarItem.TvdbId,
                SeasonNumber = calendarItem.SeasonNumber,
                EpisodeNumber = calendarItem.EpisodeNumber,
                EpisodeTitle = calendarItem.EpisodeTitle,
                Cast = metadata.Cast
            };
        }
    }
}
