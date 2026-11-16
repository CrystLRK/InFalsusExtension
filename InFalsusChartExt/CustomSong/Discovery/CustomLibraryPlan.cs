using System;
using System.Collections.Generic;
using InFalsusChartExt.CustomSong.Config;
using Il2Cppifapp.Game.Data;

namespace InFalsusChartExt.CustomSong.Discovery
{
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
        internal string JacketPath { get; }
        internal IReadOnlyList<CustomSongPlan> Songs { get; }
        internal ushort RuntimePackId { get; set; }

        internal CustomPackPlan(
            string directory,
            PackConfig config,
            string jacketPath,
            IReadOnlyList<CustomSongPlan> songs)
        {
            Directory = directory;
            Config = config;
            JacketPath = jacketPath;
            Songs = songs ?? Array.Empty<CustomSongPlan>();
        }
    }

    internal sealed class CustomSongPlan
    {
        internal CustomPackPlan Pack { get; set; }
        internal string Directory { get; }
        internal SongConfig Config { get; }
        internal string BaseName { get; }
        internal string AudioPath { get; }
        internal string PreviewPath { get; }
        internal float PreviewStart { get; }
        internal float PreviewEnd { get; }
        internal string BackgroundPath { get; }
        internal string JacketLargePath { get; }
        internal string JacketSmallPath { get; }
        internal IReadOnlyList<CustomChartPlan> Charts { get; }
        internal ushort RuntimeSongId { get; set; }
        internal ushort TemplateSongId { get; set; }
        internal string TemplateAudioName { get; set; }

        internal CustomSongPlan(
            string directory,
            SongConfig config,
            string baseName,
            string audioPath,
            string previewPath,
            float previewStart,
            float previewEnd,
            string backgroundPath,
            string jacketLargePath,
            string jacketSmallPath,
            IReadOnlyList<CustomChartPlan> charts)
        {
            Directory = directory;
            Config = config;
            BaseName = baseName;
            AudioPath = audioPath;
            PreviewPath = previewPath;
            PreviewStart = previewStart;
            PreviewEnd = previewEnd;
            BackgroundPath = backgroundPath;
            JacketLargePath = jacketLargePath;
            JacketSmallPath = jacketSmallPath;
            Charts = charts ?? Array.Empty<CustomChartPlan>();
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
        internal int Rating { get; }
        internal string LevelSectionIndicator { get; }

        internal CustomChartPlan(
            string logicalChartKey,
            string parserKey,
            string sourcePath,
            string backgroundPath,
            ChartDifficultyFlag difficulty,
            int rating,
            string levelSectionIndicator)
        {
            LogicalChartKey = logicalChartKey;
            ParserKey = parserKey;
            SourcePath = sourcePath;
            BackgroundPath = backgroundPath;
            Difficulty = difficulty;
            Rating = rating;
            LevelSectionIndicator = levelSectionIndicator;
        }
    }
}
