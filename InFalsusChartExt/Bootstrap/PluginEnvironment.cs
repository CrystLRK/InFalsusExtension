using System;
using System.IO;

namespace InFalsusChartExt
{
    internal enum PluginLoaderKind
    {
        MelonLoader,
        BepInEx,
    }

    /// <summary>
    /// Loader-independent paths for the active plugin host.
    /// </summary>
    internal static class PluginEnvironment
    {
        private static string _gameRoot;
        private static string _pluginDirectory;
        private static string _fallbackGameRoot;

        internal static PluginLoaderKind Loader { get; private set; }

        /// <summary>
        /// Resolved game installation root. Falls back to the directory of the
        /// running process because MelonLoader applies Harmony patches (and
        /// therefore runs patch type initializers) before OnInitializeMelon.
        /// </summary>
        internal static string GameRoot => _gameRoot ?? FallbackGameRoot;
        internal static string PluginDirectory =>
            _pluginDirectory ?? Path.Combine(GameRoot, "Mods");

        internal static bool IsInitialized =>
            !string.IsNullOrEmpty(_gameRoot);

        /// <summary>
        /// Loader-independent shared resource/data directory. Both MelonLoader
        /// and BepInEx use the same game-root folder so profiles, fonts,
        /// bomb skin and custom scores survive a loader switch.
        /// </summary>
        internal static string SharedDataDirectory =>
            Path.Combine(GameRoot, "InFalsusChartExt");

        internal static string PluginDataDirectory =>
            SharedDataDirectory;

        private static string FallbackGameRoot
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_fallbackGameRoot))
                    return _fallbackGameRoot;
                _fallbackGameRoot = ResolveFallbackGameRoot();
                return _fallbackGameRoot;
            }
        }

        internal static void Initialize(
            PluginLoaderKind loader,
            string gameRoot,
            string pluginDirectory)
        {
            Loader = loader;
            _gameRoot = NormalizePath(gameRoot) ?? FallbackGameRoot;
            _pluginDirectory = NormalizePath(pluginDirectory) ??
                Path.Combine(_gameRoot, "Mods");
        }

        internal static string ResolveGameRelative(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative))
                return GameRoot;
            string normalized = relative
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(GameRoot, normalized));
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            try
            {
                string full = Path.GetFullPath(path);
                return string.IsNullOrWhiteSpace(full) ? null : full;
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveFallbackGameRoot()
        {
            try
            {
                string processPath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(processPath))
                {
                    string processDirectory =
                        Path.GetDirectoryName(processPath);
                    if (!string.IsNullOrWhiteSpace(processDirectory))
                        return Path.GetFullPath(processDirectory);
                }
            }
            catch
            {
            }

            if (!string.IsNullOrWhiteSpace(_pluginDirectory))
            {
                try
                {
                    string parent = Path.GetDirectoryName(
                        _pluginDirectory.TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar));
                    if (!string.IsNullOrWhiteSpace(parent))
                        return Path.GetFullPath(parent);
                }
                catch
                {
                }
            }

            return Environment.CurrentDirectory;
        }
    }
}
