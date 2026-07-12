using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace ComingSoonPlugin
{
    /// <summary>Which Radarr release-date field to treat as "the" date for a movie.</summary>
    public enum RadarrReleaseDateType
    {
        Digital,
        Physical,
        InCinemas,
        Earliest
    }

    /// <summary>Language for poster badge text and fetched Overview/plot text.</summary>
    public enum ComingSoonLanguage
    {
        English,
        German
    }

    /// <summary>What the top status badge says, derived from a list's release-date type.</summary>
    public enum ReleaseBadgeKind
    {
        Upcoming,
        InCinemas,
        OnDisc,
        Digital
    }

    /// <summary>Which corner of the poster/backdrop the status badge sits in.</summary>
    public enum BadgeCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// One admin-configured home-screen row: its own Emby library, synced independently every
    /// refresh. A movie can appear in more than one list if it matches more than one.
    /// </summary>
    public class ComingSoonListConfig
    {
        public string Name { get; set; } = "Coming Soon";
        public bool IncludeMovies { get; set; } = true;
        public RadarrReleaseDateType MovieDateType { get; set; } = RadarrReleaseDateType.Digital;
        public bool IncludeTvShows { get; set; } = true;
    }

    /// <summary>Persisted plugin settings, edited via the Dashboard config page.</summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool SonarrEnabled { get; set; } = false;
        public string SonarrUrl { get; set; } = "http://localhost:8989";
        public string SonarrApiKey { get; set; } = string.Empty;

        public bool RadarrEnabled { get; set; } = false;
        public string RadarrUrl { get; set; } = "http://localhost:7878";
        public string RadarrApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Initialised empty on purpose: the XmlSerializer Emby uses *appends* deserialised
        /// elements to whatever the getter already holds (it ignores the setter for collections),
        /// so a non-empty default would grow the list by its default entries on every plugin load.
        /// A missing/empty list is treated as one default row by the task and the config page.
        /// </summary>
        public List<ComingSoonListConfig> Lists { get; set; } = new();

        /// <summary>Optional - only used for local trailer download.</summary>
        public string TmdbApiKey { get; set; } = string.Empty;

        /// <summary>Badge text and fetched Overview/plot language.</summary>
        public ComingSoonLanguage Language { get; set; } = ComingSoonLanguage.English;

        /// <summary>Which corner the status badge (UPCOMING / IN CINEMAS / ...) is drawn in.</summary>
        public BadgeCorner BadgePosition { get; set; } = BadgeCorner.TopLeft;

        /// <summary>
        /// The Language value the last refresh actually rendered with, set by the plugin itself
        /// (not the config page). Lets the next refresh detect a language change and force Emby to
        /// re-read the re-badged local images, so a language switch takes effect without deleting
        /// and recreating the libraries.
        /// </summary>
        public ComingSoonLanguage LastSyncedLanguage { get; set; } = ComingSoonLanguage.English;

        /// <summary>
        /// Badge-appearance version the last refresh rendered with, set by the plugin. When the
        /// badge design changes (see RefreshComingSoonTask.BadgeRenderVersion) this differs from
        /// the current version, so the next refresh forces Emby to re-read the re-rendered images.
        /// </summary>
        public int LastBadgeRenderVersion { get; set; } = 0;

        /// <summary>How many days ahead to pull from the Sonarr/Radarr calendars.</summary>
        public int DaysAhead { get; set; } = 30;

        /// <summary>Download trailers locally via yt-dlp instead of only caching the YouTube key.</summary>
        public bool DownloadTrailersLocally { get; set; } = true;

        /// <summary>
        /// Path to a yt-dlp executable/binary. Default "yt-dlp" tries PATH first, then falls back
        /// to auto-downloading a standalone copy into the plugin's own data folder if not found.
        /// </summary>
        public string YtDlpPath { get; set; } = "yt-dlp";

        /// <summary>
        /// Path to an ffmpeg executable, used by yt-dlp for remuxing. Empty (default) uses
        /// whatever ffmpeg Emby itself is already configured to use.
        /// </summary>
        public string FfmpegPath { get; set; } = string.Empty;
    }
}
