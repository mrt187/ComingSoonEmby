using System.Threading;
using System.Threading.Tasks;
using ComingSoonPlugin.Models;
using Microsoft.Extensions.Logging;

namespace ComingSoonPlugin.Services
{
    /// <summary>
    /// Combines EmbyProviderMetadataService (primary Overview) and TmdbService (fallback Overview
    /// + Cast + Poster + trailer key) into one MetadataResult per calendar item.
    /// </summary>
    public class MetadataAggregator
    {
        private readonly EmbyProviderMetadataService _embyProvider;
        private readonly TmdbService _tmdb;
        private readonly ILogger _logger;

        public MetadataAggregator(EmbyProviderMetadataService embyProvider, TmdbService tmdb, ILogger logger)
        {
            _embyProvider = embyProvider;
            _tmdb = tmdb;
            _logger = logger;
        }

        public async Task<MetadataResult> GetForMovieAsync(int tmdbId, CancellationToken cancellationToken)
        {
            var embyResult = await _embyProvider.TryGetMovieMetadataAsync(tmdbId, cancellationToken).ConfigureAwait(false);
            var tmdbResult = await _tmdb.GetMovieMetadataAsync(tmdbId, cancellationToken).ConfigureAwait(false);

            return Merge(embyResult, tmdbResult);
        }

        public async Task<MetadataResult> GetForSeriesAsync(int tvdbId, CancellationToken cancellationToken)
        {
            var embyResult = await _embyProvider.TryGetSeriesMetadataAsync(tvdbId, cancellationToken).ConfigureAwait(false);

            // TMDB needs its own tv id, not tvdbId directly - bridge via /find.
            var tmdbTvId = await _tmdb.FindTmdbTvIdByTvdbIdAsync(tvdbId, cancellationToken).ConfigureAwait(false);
            var tmdbResult = tmdbTvId.HasValue
                ? await _tmdb.GetSeriesMetadataAsync(tmdbTvId.Value, cancellationToken).ConfigureAwait(false)
                : null;

            if (!tmdbTvId.HasValue)
            {
                _logger.LogDebug("No TMDB tv id found for tvdbId {TvdbId} - trailer lookup will be skipped", tvdbId);
            }

            return Merge(embyResult, tmdbResult);
        }

        private static MetadataResult Merge(MetadataResult? primary, MetadataResult? fallback)
        {
            return new MetadataResult
            {
                Overview = FirstNonEmpty(primary?.Overview, fallback?.Overview),
                PosterUrl = FirstNonEmpty(fallback?.PosterUrl, primary?.PosterUrl), // TMDB poster preferred
                BackdropUrl = fallback?.BackdropUrl, // TMDB only
                Cast = (primary?.Cast?.Count ?? 0) > 0 ? primary!.Cast : (fallback?.Cast ?? new()),
                TrailerYoutubeKey = fallback?.TrailerYoutubeKey // TMDB only
            };
        }

        private static string? FirstNonEmpty(string? a, string? b) =>
            !string.IsNullOrWhiteSpace(a) ? a : (!string.IsNullOrWhiteSpace(b) ? b : null);
    }
}
