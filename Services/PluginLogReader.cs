using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ComingSoonPlugin.Services
{
    /// <summary>
    /// Backs the config page's log viewer: tails Emby's current server log file and keeps only
    /// lines tagged with our logger name ("ComingSoon"), since the file otherwise mixes in every
    /// other plugin/component's output.
    /// </summary>
    public static class PluginLogReader
    {
        private const string LoggerTag = "ComingSoon";

        public static string ReadRecentLines(string logDirectoryPath, int maxLines)
        {
            if (string.IsNullOrWhiteSpace(logDirectoryPath) || !Directory.Exists(logDirectoryPath))
            {
                return string.Empty;
            }

            var latestLog = new DirectoryInfo(logDirectoryPath)
                .GetFiles()
                .Where(f => f.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                         || f.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestLog is null)
            {
                return string.Empty;
            }

            var matches = new List<string>();

            // Emby keeps the log file open for writing, so share read/write access rather than
            // taking an exclusive lock.
            using var stream = new FileStream(latestLog.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains(LoggerTag, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(line);
                }
            }

            var tail = matches.Count > maxLines ? matches.Skip(matches.Count - maxLines) : matches;
            return string.Join("\n", tail);
        }
    }
}
