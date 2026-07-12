using System.IO;
using ComingSoonPlugin.Models;

namespace ComingSoonPlugin.Services
{
    /// <summary>Single source of truth for the per-item library folder name.</summary>
    public static class ComingSoonEntryPaths
    {
        public static string BuildFolderName(
            string title, CalendarItemType type, int? seasonNumber, int? episodeNumber, int? tmdbId, int? tvdbId)
        {
            var displayTitle = type == CalendarItemType.Episode
                ? $"{title} - S{seasonNumber:D2}E{episodeNumber:D2}"
                : title;

            var stableId = tmdbId.HasValue
                ? $"tmdb-{tmdbId}"
                : $"tvdb-{tvdbId}-s{seasonNumber}e{episodeNumber}";

            return SafeFileName($"{displayTitle} [{stableId}]");
        }

        public static string SafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, ' ');
            }

            name = name.Trim();

            // GetInvalidFileNameChars() strips '/' but not '.', so a name that ends up as just
            // dots (e.g. an empty/all-stripped title) would otherwise resolve as "." or ".." when
            // combined into a path. Guard against that explicitly rather than relying on every
            // caller always appending a non-dot suffix.
            return name.Length == 0 || name.Trim('.').Length == 0 ? "_" : name;
        }
    }
}
