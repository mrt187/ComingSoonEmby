using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ComingSoonPlugin.Models;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace ComingSoonPlugin.Services
{
    /// <summary>
    /// Pulls Overview/Poster from Emby's own registered metadata providers via
    /// IProviderManager.GetRemoteSearchResults - the same "Identify" search Emby's dashboard uses
    /// when matching an item by provider id, invoked here with only ProviderIds set so an
    /// id-capable provider returns an exact match. IProviderManager has no method that returns
    /// cast/crew for an item outside the library, so Cast stays empty here - Emby's own scan
    /// fetches cast once the item is a real library entry (see ComingSoonFileLibrarySync), with
    /// TmdbService as a further fallback.
    /// </summary>
    public class EmbyProviderMetadataService
    {
        private readonly IProviderManager _providerManager;
        private readonly ILogger _logger;
        private readonly string _metadataLanguage;

        public EmbyProviderMetadataService(IProviderManager providerManager, ILogger logger, ComingSoonLanguage language = ComingSoonLanguage.English)
        {
            _providerManager = providerManager;
            _logger = logger;
            _metadataLanguage = language == ComingSoonLanguage.German ? "de" : "en";
        }

        public async Task<MetadataResult?> TryGetMovieMetadataAsync(int tmdbId, CancellationToken cancellationToken)
        {
            var lookupInfo = new MovieInfo
            {
                MetadataLanguage = _metadataLanguage,
                ProviderIds = new ProviderIdDictionary { ["Tmdb"] = tmdbId.ToString() }
            };

            var query = new RemoteSearchQuery<MovieInfo> { SearchInfo = lookupInfo };

            try
            {
                var results = await _providerManager
                    .GetRemoteSearchResults<Movie, MovieInfo>(query, cancellationToken)
                    .ConfigureAwait(false);

                var best = results?.FirstOrDefault();
                if (best is null)
                {
                    _logger.LogDebug("Emby provider search returned no match for movie tmdbId {TmdbId}", tmdbId);
                    return null;
                }

                return new MetadataResult
                {
                    Overview = best.Overview,
                    PosterUrl = best.ImageUrl
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Emby provider search failed for movie tmdbId {TmdbId}, falling back to TMDB", tmdbId);
                return null;
            }
        }

        public async Task<MetadataResult?> TryGetSeriesMetadataAsync(int tvdbId, CancellationToken cancellationToken)
        {
            var lookupInfo = new SeriesInfo
            {
                MetadataLanguage = _metadataLanguage,
                ProviderIds = new ProviderIdDictionary { ["Tvdb"] = tvdbId.ToString() }
            };

            var query = new RemoteSearchQuery<SeriesInfo> { SearchInfo = lookupInfo };

            try
            {
                var results = await _providerManager
                    .GetRemoteSearchResults<Series, SeriesInfo>(query, cancellationToken)
                    .ConfigureAwait(false);

                var best = results?.FirstOrDefault();
                if (best is null)
                {
                    _logger.LogDebug("Emby provider search returned no match for series tvdbId {TvdbId}", tvdbId);
                    return null;
                }

                return new MetadataResult
                {
                    Overview = best.Overview,
                    PosterUrl = best.ImageUrl
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Emby provider search failed for series tvdbId {TvdbId}, falling back to TMDB", tvdbId);
                return null;
            }
        }
    }
}
