using System.Collections.Generic;

namespace InFalsusChartExt.CustomSong.Scores
{
    internal sealed class CustomScoreDocument
    {
        internal const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public List<CustomScoreEntry> Entries { get; set; } =
            new List<CustomScoreEntry>();
    }

    internal sealed class CustomScoreEntry
    {
        public string PackId { get; set; } = string.Empty;
        public string SongId { get; set; } = string.Empty;
        public string BaseName { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public CustomScoreRecord Best { get; set; }
    }

    internal sealed class CustomScoreRecord
    {
        public int Lamp { get; set; }
        public int Clear { get; set; }
        public int MaxCombo { get; set; }
        public int Pace { get; set; }
        public JudgementCounts Tap { get; set; } =
            new JudgementCounts();
        public JudgementCounts Hold { get; set; } =
            new JudgementCounts();
        public JudgementCounts SkyArea { get; set; } =
            new JudgementCounts();
        public JudgementCounts Flick { get; set; } =
            new JudgementCounts();
        public ulong Score { get; set; }
        public ulong SecondsSinceEpochUtc { get; set; }
    }

    internal sealed class JudgementCounts
    {
        public int None { get; set; }
        public int Miss { get; set; }
        public int NearEarly { get; set; }
        public int NearLate { get; set; }
        public int PerfectEarly { get; set; }
        public int PerfectLate { get; set; }
        public int Shiny { get; set; }
    }
}
