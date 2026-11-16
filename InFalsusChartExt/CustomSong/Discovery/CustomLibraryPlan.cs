using System;
using System.Collections.Generic;
using InFalsusChartExt.CustomSong.Config;
#if MELONLOADER
using Il2Cppifapp.Game.Data;
#elif BEPINEX
using ifapp.Game.Data;
#endif

namespace InFalsusChartExt.CustomSong.Discovery
{
    internal static class CustomSongSchema
    {
        internal const int DifficultyCount = 4;
        internal const int LanguageCount = 5;
        internal const int FallbackLanguage = 2;

        internal static readonly string[] LanguageKeys =
        {
            "sc", "tc", "en", "ja", "ko",
        };

        internal static int LanguageIndex(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return -1;
            switch (key.Trim().ToLowerInvariant())
            {
                case "sc": return 0;
                case "tc": return 1;
                case "en": return 2;
                case "ja": return 3;
                case "ko": return 4;
                default: return -1;
            }
        }

        internal static string Language(
            string[] values,
            int language)
        {
            if (values is null || values.Length == 0)
                return string.Empty;
            int index = language;
            if (index < 0 || index >= values.Length)
                index = FallbackLanguage;
            if (index < values.Length &&
                !string.IsNullOrEmpty(values[index]))
                return values[index];
            for (int i = 0; i < values.Length; i++)
                if (!string.IsNullOrEmpty(values[i]))
                    return values[i];
            return string.Empty;
        }
    }

    internal sealed class CustomLibraryPlan
    {
        internal IReadOnlyList<CustomPackPlan> Packs { get; }

        internal CustomLibraryPlan(IReadOnlyList<CustomPackPlan> packs)
        {
            Packs = packs ?? Array.Empty<CustomPackPlan>();
        }
    }

    internal sealed class CustomPackPlan
    {
        internal string Directory { get; }
        internal PackConfig Config { get; }
        internal string[] Titles { get; }
        internal IReadOnlyDictionary<string, string[]> Visuals { get; }
        internal IReadOnlyList<CustomSongPlan> Songs { get; }
        internal ushort RuntimePackId { get; set; }

        internal CustomPackPlan(
            string directory,
            PackConfig config,
            string[] titles,
            IReadOnlyDictionary<string, string[]> visuals,
            IReadOnlyList<CustomSongPlan> songs)
        {
            Directory = directory;
            Config = config;
            Titles = titles ?? new string[CustomSongSchema.LanguageCount];
            Visuals = visuals ??
                new Dictionary<string, string[]>(StringComparer.Ordinal);
            Songs = songs ?? Array.Empty<CustomSongPlan>();
        }

        internal string Visual(string key, int language)
        {
            if (key is null || !Visuals.TryGetValue(key, out string[] slots))
                return null;
            return CustomSongSchema.Language(slots, language);
        }
    }

    internal sealed class CustomSongPlan
    {
        internal CustomPackPlan Pack { get; set; }
        internal string Directory { get; }
        internal SongConfig Config { get; }
        internal string BaseName { get; }
        internal string[] Titles { get; }
        internal string[] Composers { get; }
        internal string[] Illustrators { get; }
        internal string[] AudioPaths { get; }
        internal string[] PreviewPaths { get; }
        internal float[] PreviewStarts { get; }
        internal float[] PreviewEnds { get; }
        internal string[] BackgroundPaths { get; }
        internal string[] JacketLargePaths { get; }
        internal string[] JacketSmallPaths { get; }
        internal IReadOnlyList<CustomChartPlan> Charts { get; }
        internal ushort RuntimeSongId { get; set; }
        internal ushort TemplateSongId { get; set; }
        internal string TemplateAudioName { get; set; }

        internal CustomSongPlan(
            string directory,
            SongConfig config,
            string baseName,
            string[] titles,
            string[] composers,
            string[] illustrators,
            string[] audioPaths,
            string[] previewPaths,
            float[] previewStarts,
            float[] previewEnds,
            string[] backgroundPaths,
            string[] jacketLargePaths,
            string[] jacketSmallPaths,
            IReadOnlyList<CustomChartPlan> charts)
        {
            Directory = directory;
            Config = config;
            BaseName = baseName;
            Titles = titles;
            Composers = composers;
            Illustrators = illustrators;
            AudioPaths = audioPaths;
            PreviewPaths = previewPaths;
            PreviewStarts = previewStarts;
            PreviewEnds = previewEnds;
            BackgroundPaths = backgroundPaths;
            JacketLargePaths = jacketLargePaths;
            JacketSmallPaths = jacketSmallPaths;
            Charts = charts ?? Array.Empty<CustomChartPlan>();
        }

        internal string Title(int language) =>
            CustomSongSchema.Language(Titles, language);

        internal string Composer(int language) =>
            CustomSongSchema.Language(Composers, language);

        internal string Illustrator(int language) =>
            CustomSongSchema.Language(Illustrators, language);

        internal string AudioFor(int slot) =>
            At(AudioPaths, slot);

        internal string PreviewFor(int slot) =>
            At(PreviewPaths, slot);

        internal float PreviewStartFor(int slot) =>
            At(PreviewStarts, slot);

        internal float PreviewEndFor(int slot) =>
            At(PreviewEnds, slot);

        internal string BackgroundFor(int slot) =>
            At(BackgroundPaths, slot);

        internal string JacketLargeFor(int slot) =>
            At(JacketLargePaths, slot);

        internal string JacketSmallFor(int slot) =>
            At(JacketSmallPaths, slot);

        private static T At<T>(T[] values, int slot)
        {
            if (values is null || values.Length == 0)
                return default;
            if (slot >= 0 && slot < values.Length)
                return values[slot];
            return values[0];
        }
    }

    internal sealed class CustomChartPlan
    {
        internal CustomSongPlan Song { get; set; }
        internal string TemplateParserKey { get; set; }
        internal string TemplateChartId { get; set; }
        internal string LogicalChartKey { get; }
        internal string ParserKey { get; }
        internal string SourcePath { get; }
        internal string BackgroundPath { get; }
        internal ChartDifficultyFlag Difficulty { get; }
        internal int Slot { get; }
        internal int Rating { get; }
        internal string LevelSectionIndicator { get; }
        internal string[] ChartDesigners { get; }
        internal string[] JacketDesigners { get; }
        internal string JacketLargePath { get; }
        internal string JacketSmallPath { get; }

        internal CustomChartPlan(
            string logicalChartKey,
            string parserKey,
            string sourcePath,
            string backgroundPath,
            ChartDifficultyFlag difficulty,
            int slot,
            int rating,
            string levelSectionIndicator,
            string[] chartDesigners,
            string[] jacketDesigners,
            string jacketLargePath,
            string jacketSmallPath)
        {
            LogicalChartKey = logicalChartKey;
            ParserKey = parserKey;
            SourcePath = sourcePath;
            BackgroundPath = backgroundPath;
            Difficulty = difficulty;
            Slot = slot;
            Rating = rating;
            LevelSectionIndicator = levelSectionIndicator;
            ChartDesigners = chartDesigners ??
                new string[CustomSongSchema.LanguageCount];
            JacketDesigners = jacketDesigners ??
                new string[CustomSongSchema.LanguageCount];
            JacketLargePath = jacketLargePath;
            JacketSmallPath = jacketSmallPath;
        }

        internal string ChartDesigner(int language) =>
            CustomSongSchema.Language(ChartDesigners, language);

        internal string JacketDesigner(int language) =>
            CustomSongSchema.Language(JacketDesigners, language);
    }
}
