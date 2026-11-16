using System;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Dumps the raw bytes of the game's FastText font assets and can rebuild
    /// a custom FastTextFontFamily from files placed in the plugin fonts
    /// directory. The font bytes are the same TTFs the game itself feeds to
    /// FreeType/HarfBuzz, so no external font asset is required.
    /// </summary>
    internal static class FastTextFontSupport
    {
        private const string FontDirectory = "fonts";
        private static int _baseFontBytes;

        internal static string FontsDirectory =>
            Path.Combine(
                PluginEnvironment.PluginDataDirectory,
                FontDirectory);

        internal static void DumpFrom(
            Il2CppFastText.Data.FastTextFontFamily family,
            string directory)
        {
            if (family == null) return;
            try
            {
                Directory.CreateDirectory(directory);
                var assets = family.fontAssets;
                if (assets == null) return;
                for (int i = 0; i < assets.Length; i++)
                {
                    Il2CppFastText.Data.FastTextAsset asset = assets[i];
                    if (asset == null) continue;
                    var raw = asset.RawBytes;
                    if (raw == null || raw.Length == 0) continue;
                    string identifier = asset.Identifier;
                    if (string.IsNullOrWhiteSpace(identifier))
                        identifier = "font_" + i;
                    identifier = Sanitize(identifier);
                    string path = Path.Combine(
                        directory, identifier + ".ttf");
                    if (File.Exists(path)) continue;
                    var bytes = new byte[raw.Length];
                    for (int j = 0; j < bytes.Length; j++)
                        bytes[j] = raw[j];
                    File.WriteAllBytes(path, bytes);
                    string basePath = Path.Combine(directory, "base.ttf");
                    if (bytes.Length > _baseFontBytes)
                    {
                        _baseFontBytes = bytes.Length;
                        File.WriteAllBytes(basePath, bytes);
                    }
                    ChartExtMod.Log.Msg(
                        $"[ChartExt] font dumped: {path} " +
                        $"({bytes.Length} bytes)");
                }
            }
            catch (Exception e)
            {
                ChartExtMod.Log.Warning(
                    "[ChartExt] font dump failed: " + e.Message);
            }
        }

        internal static void ExportBaseFont(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var objects = Resources.FindObjectsOfTypeAll(
                    Il2CppType.Of<Il2CppFastText.Data.FastTextFontFamily>());
                if (objects == null) return;
                for (int i = 0; i < objects.Length; i++)
                {
                    var family = objects[i]?.TryCast<
                        Il2CppFastText.Data.FastTextFontFamily>();
                    if (family == null) continue;
                    DumpFrom(family, directory);
                }
            }
            catch (Exception e)
            {
                ChartExtMod.Log.Warning(
                    "[ChartExt] base font export failed: " + e.Message);
            }
        }

        internal static Il2CppFastText.Data.FastTextFontFamily
            TryLoadCustomFamily(
                Il2CppFastText.Data.FastTextFontFamily template,
                string directory)
        {
            if (template == null || !Directory.Exists(directory))
                return null;
            try
            {
                var files = new List<string>();
                foreach (string path in Directory.GetFiles(directory))
                {
                    string ext = Path.GetExtension(path);
                    if (ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".otf", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".ttc", StringComparison.OrdinalIgnoreCase))
                        files.Add(path);
                }
                if (files.Count == 0) return null;

                var assets =
                    new Il2CppReferenceArray<
                        Il2CppFastText.Data.FastTextAsset>(files.Count);
                float multiplier = 1f;
                var templateAssets = template.fontAssets;
                if (templateAssets != null && templateAssets.Length > 0 &&
                    templateAssets[0] != null)
                    multiplier = templateAssets[0].PointSizeMultiplier;
                for (int i = 0; i < files.Count; i++)
                {
                    byte[] bytes = File.ReadAllBytes(files[i]);
                    var asset = ScriptableObject.CreateInstance<
                        Il2CppFastText.Data.FastTextAsset>();
                    asset.Identifier = Path.GetFileNameWithoutExtension(files[i]);
                    asset.RawBytes = new Il2CppStructArray<byte>(bytes);
                    asset.PointSizeMultiplier = multiplier;
                    assets[i] = asset;
                }

                var family = ScriptableObject.CreateInstance<
                    Il2CppFastText.Data.FastTextFontFamily>();
                family.settings = template.settings;
                family.fontAssets = assets;
                family.supportsLocalization = template.supportsLocalization;
                family.behaviours = template.behaviours;
                family.nonEnglishLocalizationToFontSet =
                    template.nonEnglishLocalizationToFontSet;
                family.EnsureLookups();
                ChartExtMod.Log.Msg(
                    $"[ChartExt] custom font family loaded: " +
                    $"{files.Count} file(s) from {directory}");
                return family;
            }
            catch (Exception e)
            {
                ChartExtMod.Log.Warning(
                    "[ChartExt] custom font family failed: " + e.Message);
                return null;
            }
        }

        internal static void Apply(
            Il2CppFastText.FastText text,
            Il2CppFastText.Data.FastTextFontFamily family)
        {
            if (text == null || family == null) return;
            try
            {
                text.SetFontFamilyAsset(family);
            }
            catch
            {
                try { text.fontFamilyAsset = family; } catch { }
            }
        }

        private static string Sanitize(string value)
        {
            var builder = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                builder.Append(
                    char.IsLetterOrDigit(c) || c == '_' || c == '-'
                        ? c : '_');
            }
            return builder.Length == 0 ? "font" : builder.ToString();
        }
    }
}
