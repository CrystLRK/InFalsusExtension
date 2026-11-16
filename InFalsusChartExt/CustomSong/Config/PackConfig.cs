using System.Collections.Generic;

namespace InFalsusChartExt.CustomSong.Config
{
    internal sealed class PackConfig
    {
        public int SchemaVersion { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Description { get; set; }
        public string StylePackSlug { get; set; }
        public int SortOrder { get; set; }
    }

    internal sealed class SongConfig
    {
        public int SchemaVersion { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string ChartDesigner { get; set; }
        public string JacketDesigner { get; set; }
        public string Audio { get; set; }
        public float? PreviewStart { get; set; }
        public float? PreviewEnd { get; set; }
        public PreviewConfig Preview { get; set; }
        public string Background { get; set; }
        public string JacketLarge { get; set; }
        public string JacketSmall { get; set; }
        public Dictionary<string, DifficultyConfig> Difficulties { get; set; }
    }

    internal sealed class PreviewConfig
    {
        public string Audio { get; set; }
        public float? Start { get; set; }
        public float? End { get; set; }
    }

    internal sealed class DifficultyConfig
    {
        public string Chart { get; set; }
        public int Rating { get; set; }
        public string LevelSectionIndicator { get; set; }
        public string Background { get; set; }
    }
}
