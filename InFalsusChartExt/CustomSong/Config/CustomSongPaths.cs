using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InFalsusChartExt.CustomSong.Config
{
    internal static class CustomSongPaths
    {
        private static readonly HashSet<string> AudioExtensions =
            new HashSet<string>(
                new[] { ".ogg", ".wav", ".mp3" },
                StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> BackgroundExtensions =
            new HashSet<string>(
                new[] { ".png", ".jpg", ".jpeg", ".webp", ".mp4" },
                StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> JacketExtensions =
            new HashSet<string>(
                new[] { ".png" },
                StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> ChartExtensions =
            new HashSet<string>(
                new[] { ".spc", ".sam" },
                StringComparer.OrdinalIgnoreCase);

        internal static string GameRoot
        {
            get
            {
                string mods = Path.GetDirectoryName(
                    typeof(ChartExtMod).Assembly.Location);
                return Path.GetFullPath(Path.Combine(mods, ".."));
            }
        }

        internal static string ChartsRoot =>
            Path.Combine(GameRoot, "CustomCharts");

        internal static bool TryResolveFile(
            string root,
            string configuredPath,
            IReadOnlyCollection<string> allowedExtensions,
            out string resolved,
            out string error)
        {
            resolved = null;
            error = null;

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                error = "path is empty";
                return false;
            }

            string normalized = configuredPath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(normalized) ||
                normalized.IndexOf(':') >= 0)
            {
                error = "absolute paths are not allowed";
                return false;
            }

            string[] segments = normalized.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 ||
                Array.Exists(segments, segment => segment == ".."))
            {
                error = "parent traversal is not allowed";
                return false;
            }

            string fullRoot = EnsureTrailingSeparator(
                Path.GetFullPath(root));
            string candidate = Path.GetFullPath(
                Path.Combine(fullRoot, normalized));
            if (!candidate.StartsWith(
                    fullRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                error = "path escapes the song directory";
                return false;
            }

            if (allowedExtensions != null &&
                !allowedExtensions.Any(
                    extension => string.Equals(
                        extension,
                        Path.GetExtension(candidate),
                        StringComparison.OrdinalIgnoreCase)))
            {
                error = "unsupported extension '" +
                        Path.GetExtension(candidate) + "'";
                return false;
            }

            if (!File.Exists(candidate))
            {
                error = "file does not exist";
                return false;
            }
            if ((File.GetAttributes(candidate) &
                 FileAttributes.ReparsePoint) != 0)
            {
                error = "symbolic-link files are not allowed";
                return false;
            }

            string existingParent = Path.GetDirectoryName(candidate);
            while (!string.IsNullOrEmpty(existingParent) &&
                   existingParent.StartsWith(
                       fullRoot,
                       StringComparison.OrdinalIgnoreCase))
            {
                FileAttributes attributes = File.GetAttributes(existingParent);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    error = "symbolic-link traversal is not allowed";
                    return false;
                }
                existingParent = Path.GetDirectoryName(existingParent);
            }

            resolved = candidate;
            return true;
        }

        internal static bool IsAudioPathValid(string path) =>
            AudioExtensions.Contains(Path.GetExtension(path));

        internal static bool IsBackgroundPathValid(string path) =>
            BackgroundExtensions.Contains(Path.GetExtension(path));

        internal static bool IsJacketPathValid(string path) =>
            JacketExtensions.Contains(Path.GetExtension(path));

        internal static bool IsChartPathValid(string path) =>
            ChartExtensions.Contains(Path.GetExtension(path));

        internal static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(
                    Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal))
                return path;
            return path + Path.DirectorySeparatorChar;
        }
    }
}
