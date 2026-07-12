using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ComingSoonPlugin.Services
{
    /// <summary>
    /// Shells out to yt-dlp to pull a trailer down into a "trailers" subfolder, so Emby indexes it
    /// as a local trailer extra (its native "Trailer" button) that plays through Emby's own player
    /// instead of opening YouTube - which kicks tvOS/Apple TV users into a separate app.
    /// </summary>
    public class TrailerDownloader
    {
        public const string TrailerFileName = "trailer.mp4";

        private readonly ILogger _logger;
        private readonly string _ytDlpPath;
        private readonly string _ffmpegPath;
        private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(5);

        public TrailerDownloader(ILogger logger, string ytDlpPath, string ffmpegPath)
        {
            _logger = logger;
            _ytDlpPath = string.IsNullOrWhiteSpace(ytDlpPath) ? "yt-dlp" : ytDlpPath;
            _ffmpegPath = string.IsNullOrWhiteSpace(ffmpegPath) ? "ffmpeg" : ffmpegPath;
        }

        /// <summary>
        /// Copies an already-downloaded trailer into trailerDirectory instead of fetching it again,
        /// used when several episodes of the same series (or the same movie in several lists) share
        /// one trailer. Returns the new path, or null if the copy failed.
        /// </summary>
        public string? ReuseExisting(string existingTrailerPath, string trailerDirectory)
        {
            Directory.CreateDirectory(trailerDirectory);
            var outputPath = Path.Combine(trailerDirectory, TrailerFileName);

            if (File.Exists(outputPath))
            {
                return outputPath;
            }

            try
            {
                File.Copy(existingTrailerPath, outputPath, overwrite: true);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not reuse trailer from {Source} into {Dir}", existingTrailerPath, trailerDirectory);
                return null;
            }
        }

        /// <summary>
        /// Downloads the given YouTube video into trailerDirectory/trailer.mp4. Returns the path on
        /// success, or null if the download failed or yt-dlp isn't available - a missing file just
        /// means Emby shows the entry without a trailer, and the next refresh retries.
        /// </summary>
        public async Task<string?> DownloadAsync(
            string youtubeKey,
            string trailerDirectory,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(trailerDirectory);
            var outputPath = Path.Combine(trailerDirectory, TrailerFileName);

            if (File.Exists(outputPath))
            {
                return outputPath;
            }

            var youtubeUrl = $"https://www.youtube.com/watch?v={youtubeKey}";

            var startInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            PreferSystemDynamicLinker(startInfo);

            var haveFfmpeg = HasUsableFfmpeg(_ffmpegPath);

            // ArgumentList passes each argument atomically (no shell involved), avoiding any
            // injection risk from youtubeKey containing quotes/special characters.
            startInfo.ArgumentList.Add("--no-progress");
            startInfo.ArgumentList.Add("-f");
            // Prefer a progressive (already-muxed, single-file) stream so no ffmpeg merge is
            // needed - YouTube serves progressive mp4 up to 720p, plenty for a trailer, and it
            // works even when ffmpeg is missing or too old. Only fall back to separate streams
            // (which need ffmpeg to merge) when a usable ffmpeg is available.
            startInfo.ArgumentList.Add(haveFfmpeg
                ? "best[ext=mp4][vcodec!=none][acodec!=none]/bestvideo[height<=1080][ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best"
                : "best[ext=mp4][vcodec!=none][acodec!=none]/best[vcodec!=none][acodec!=none]/best");

            if (haveFfmpeg)
            {
                startInfo.ArgumentList.Add("--ffmpeg-location");
                startInfo.ArgumentList.Add(_ffmpegPath);
                startInfo.ArgumentList.Add("--merge-output-format");
                startInfo.ArgumentList.Add("mp4");
            }

            startInfo.ArgumentList.Add("-o");
            startInfo.ArgumentList.Add(outputPath);
            startInfo.ArgumentList.Add(youtubeUrl);

            try
            {
                using var process = new Process { StartInfo = startInfo };
                var stdErr = new StringBuilder();
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) stdErr.AppendLine(e.Data); };

                process.Start();
                process.BeginErrorReadLine();

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(ProcessTimeout);

                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning(
                        "yt-dlp exited with code {Code} for {Url}: {Error}",
                        process.ExitCode, youtubeUrl, stdErr.ToString());
                    CleanupOrphanFragments(trailerDirectory);
                    return null;
                }

                if (!File.Exists(outputPath))
                {
                    _logger.LogWarning("yt-dlp reported success but output file is missing: {Path}", outputPath);
                    CleanupOrphanFragments(trailerDirectory);
                    return null;
                }

                CleanupOrphanFragments(trailerDirectory);
                return outputPath;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("yt-dlp download timed out after {Timeout} for {Url}", ProcessTimeout, youtubeUrl);
                TryDelete(outputPath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to run yt-dlp (path: {Path}) - is it installed and reachable?", _ytDlpPath);
                return null;
            }
        }

        /// <summary>
        /// Self-contained Emby builds (e.g. linuxserver/emby) run with LD_LIBRARY_PATH pointing at
        /// their own bundled lib directory, whose libc is often older than the host's, so a binary
        /// spawned from the Emby process (yt-dlp, or /usr/bin/env via its shebang) can fail with a
        /// "GLIBC_x.xx not found" error. Prepending the host's standard library dirs makes the
        /// dynamic linker find the newer system glibc first - safe because glibc is backwards
        /// compatible.
        /// </summary>
        private static void PreferSystemDynamicLinker(ProcessStartInfo startInfo)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            const string systemLibPaths =
                "/lib:/usr/lib:/lib/x86_64-linux-gnu:/usr/lib/x86_64-linux-gnu:/lib/aarch64-linux-gnu:/usr/lib/aarch64-linux-gnu";

            var existing = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
            startInfo.Environment["LD_LIBRARY_PATH"] = string.IsNullOrEmpty(existing)
                ? systemLibPaths
                : systemLibPaths + ":" + existing;
        }

        private static bool HasUsableFfmpeg(string ffmpegPath)
        {
            if (string.IsNullOrWhiteSpace(ffmpegPath))
            {
                return false;
            }

            if (Path.IsPathRooted(ffmpegPath))
            {
                return File.Exists(ffmpegPath);
            }

            var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ffmpegPath + ".exe" : ffmpegPath;
            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            return pathVar.Split(Path.PathSeparator)
                .Where(dir => !string.IsNullOrWhiteSpace(dir))
                .Any(dir => File.Exists(Path.Combine(dir, exe)));
        }

        // A failed merge leaves the separate streams behind as trailer.fNNN.mp4 / trailer.fNNN.m4a
        // - remove them so a later refresh retries cleanly and they don't pile up on disk.
        private void CleanupOrphanFragments(string trailerDirectory)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(trailerDirectory, "trailer.f*"))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not clean up orphan yt-dlp fragments in {Dir}", trailerDirectory);
            }
        }

        private void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not clean up partial download at {Path}", path);
            }
        }
    }
}
