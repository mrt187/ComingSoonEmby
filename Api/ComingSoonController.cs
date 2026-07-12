using System;
using System.Net.Http;
using System.Threading;
using ComingSoonPlugin.Services;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace ComingSoonPlugin.Api
{
    /// <summary>
    /// Backs the Dashboard config page's "Test Connection" buttons. Uses Emby's ServiceStack-
    /// style plugin routing.
    /// </summary>
    [Route("/ComingSoon/TestSonarr", "POST", Summary = "Tests connectivity to the given Sonarr server")]
    public class TestSonarrConnection : IReturn<TestConnectionResult>
    {
        public string Url { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    [Route("/ComingSoon/TestRadarr", "POST", Summary = "Tests connectivity to the given Radarr server")]
    public class TestRadarrConnection : IReturn<TestConnectionResult>
    {
        public string Url { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public class TestConnectionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    [Route("/ComingSoon/Logs", "GET", Summary = "Returns recent ComingSoon-tagged lines from Emby's server log")]
    public class GetComingSoonLogs : IReturn<ComingSoonLogsResult>
    {
        public int MaxLines { get; set; } = 300;
    }

    public class ComingSoonLogsResult
    {
        public string Text { get; set; } = string.Empty;
    }

    [Route("/ComingSoon/Changelog", "GET", Summary = "Returns the plugin changelog")]
    public class GetComingSoonChangelog : IReturn<ChangelogEntry[]>
    {
    }

    public class ChangelogEntry
    {
        public string Version { get; set; } = string.Empty;
        public string[] Notes { get; set; } = System.Array.Empty<string>();
    }

    [Authenticated]
    public class ComingSoonService : IService, IRequiresRequest
    {
        // Dedicated short-timeout client for connection tests from the Dashboard config page -
        // separate from RefreshComingSoonTask's SharedHttpClient since a "Test Connection" click
        // should fail fast rather than hang for the same timeout as a real calendar fetch.
        private static readonly HttpClient TestHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

        public IRequest Request { get; set; } = null!;

        public object Post(TestSonarrConnection request)
        {
            if (!IsValidServerUrl(request.Url))
            {
                return new TestConnectionResult { Success = false, Message = "Only valid HTTP or HTTPS server URLs are allowed." };
            }

            var sonarr = new SonarrService(TestHttpClient, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
            var (success, message) = sonarr.TestConnectionAsync(request.Url, request.ApiKey, Request.CancellationToken)
                .GetAwaiter().GetResult();
            return new TestConnectionResult { Success = success, Message = message };
        }

        public object Post(TestRadarrConnection request)
        {
            if (!IsValidServerUrl(request.Url))
            {
                return new TestConnectionResult { Success = false, Message = "Only valid HTTP or HTTPS server URLs are allowed." };
            }

            var radarr = new RadarrService(TestHttpClient, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
            var (success, message) = radarr.TestConnectionAsync(request.Url, request.ApiKey, Request.CancellationToken)
                .GetAwaiter().GetResult();
            return new TestConnectionResult { Success = success, Message = message };
        }

        public object Get(GetComingSoonLogs request)
        {
            var maxLines = request.MaxLines > 0 ? request.MaxLines : 300;
            var text = PluginLogReader.ReadRecentLines(Plugin.Instance!.LogDirectoryPath, maxLines);
            return new ComingSoonLogsResult { Text = text };
        }

        // Serves the embedded changelog.json so the config page can render an always-current
        // changelog even when the browser has the (cached) configPage.js from an older build.
        public object Get(GetComingSoonChangelog request)
        {
            using var stream = typeof(ComingSoonService).Assembly
                .GetManifestResourceStream("ComingSoonPlugin.Configuration.changelog.json");
            if (stream is null)
            {
                return System.Array.Empty<ChangelogEntry>();
            }

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return System.Text.Json.JsonSerializer.Deserialize<ChangelogEntry[]>(stream, options)
                ?? System.Array.Empty<ChangelogEntry>();
        }

        private static bool IsValidServerUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }
}
