using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ComingSoonPlugin.Models;
using Microsoft.Extensions.Logging;

namespace ComingSoonPlugin.Services
{
    /// <summary>
    /// Direct TMDB v3 API client - the trailer-key source for both movies and series, and a
    /// fallback for Overview/Cast/Poster. Optional: PluginConfiguration.TmdbApiKey may be empty.
    /// </summary>
    public class TmdbService
    {
        private const string BaseUrl = "https://api.themoviedb.org/3";

        private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w500";
        private const string BackdropImageBaseUrl = "https://image.tmdb.org/t/p/w1280";

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _apiKey;
        private readonly string _language;

        public TmdbService(HttpClient httpClient, ILogger logger, string apiKey, ComingSoonLanguage language = ComingSoonLanguage.English)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = apiKey;
            _language = language == ComingSoonLanguage.German ? "de-DE" : "en-US";
        }

        private bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<MetadataResult?> GetMovieMetadataAsync(int tmdbId, CancellationToken cancellationToken)
        {
            if (!HasApiKey)
            {
                return null;
            }

            var url = $"{BaseUrl}/movie/{tmdbId}?api_key={_apiKey}&language={_language}&append_to_response=credits,videos";
            return await FetchAndMapAsync(url, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MetadataResult?> GetSeriesMetadataAsync(int tmdbTvId, CancellationToken cancellationToken)
        {
            if (!HasApiKey)
            {
                return null;
            }

            var url = $"{BaseUrl}/tv/{tmdbTvId}?api_key={_apiKey}&language={_language}&append_to_response=credits,videos";
            return await FetchAndMapAsync(url, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sonarr only gives us a tvdbId, but TMDB's /tv endpoints are keyed by TMDB's own id.
        /// This bridges the gap via TMDB's /find endpoint. Returns null if TMDB has no match.
        /// </summary>
        public async Task<int?> FindTmdbTvIdByTvdbIdAsync(int tvdbId, CancellationToken cancellationToken)
        {
            if (!HasApiKey)
            {
                return null;
            }

            var url = $"{BaseUrl}/find/{tvdbId}?api_key={_apiKey}&external_source=tvdb_id";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB /find lookup failed for tvdbId {TvdbId}: {Status}", tvdbId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<TmdbFindResponse>(json, JsonOptions);

            return result?.TvResults?.FirstOrDefault()?.Id;
        }

        private async Task<MetadataResult?> FetchAndMapAsync(string url, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB lookup failed ({Status}): {Url}", response.StatusCode, RedactApiKey(url));
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var dto = JsonSerializer.Deserialize<TmdbDetailsDto>(json, JsonOptions);
            if (dto is null)
            {
                return null;
            }

            var trailer = dto.Videos?.Results?
                .Where(v => string.Equals(v.Site, "YouTube", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(v => string.Equals(v.Type, "Trailer", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(v => v.Official)
                .FirstOrDefault();

            return new MetadataResult
            {
                Overview = dto.Overview,
                PosterUrl = string.IsNullOrEmpty(dto.PosterPath) ? null : ImageBaseUrl + dto.PosterPath,
                BackdropUrl = string.IsNullOrEmpty(dto.BackdropPath) ? null : BackdropImageBaseUrl + dto.BackdropPath,
                TrailerYoutubeKey = trailer?.Key,
                Cast = dto.Credits?.Cast?
                    .OrderBy(c => c.Order)
                    .Take(10)
                    .Select(c => new CastMember
                    {
                        Name = c.Name ?? string.Empty,
                        Character = c.Character ?? string.Empty,
                        ProfileImageUrl = string.IsNullOrEmpty(c.ProfilePath) ? null : ImageBaseUrl + c.ProfilePath
                    })
                    .ToList() ?? new List<CastMember>()
            };
        }

        private static string RedactApiKey(string url) =>
            System.Text.RegularExpressions.Regex.Replace(url, @"api_key=[^&]+", "api_key=***");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // --- TMDB API DTOs (subset of fields we actually use) ---

        private class TmdbDetailsDto
        {
            public string? Overview { get; set; }

            [JsonPropertyName("poster_path")]
            public string? PosterPath { get; set; }

            [JsonPropertyName("backdrop_path")]
            public string? BackdropPath { get; set; }

            public TmdbCreditsDto? Credits { get; set; }
            public TmdbVideosDto? Videos { get; set; }
        }

        private class TmdbCreditsDto
        {
            public List<TmdbCastDto>? Cast { get; set; }
        }

        private class TmdbCastDto
        {
            public string? Name { get; set; }
            public string? Character { get; set; }

            [JsonPropertyName("profile_path")]
            public string? ProfilePath { get; set; }

            public int Order { get; set; }
        }

        private class TmdbVideosDto
        {
            public List<TmdbVideoDto>? Results { get; set; }
        }

        private class TmdbVideoDto
        {
            public string? Key { get; set; }
            public string? Site { get; set; }
            public string? Type { get; set; }
            public bool Official { get; set; }
        }

        private class TmdbFindResponse
        {
            [JsonPropertyName("tv_results")]
            public List<TmdbTvResultDto>? TvResults { get; set; }
        }

        private class TmdbTvResultDto
        {
            public int Id { get; set; }
        }
    }
}
