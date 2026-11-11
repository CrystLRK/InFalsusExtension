namespace InFalsusChartExt.CustomSong.Scores
{
    internal static class CustomScoreRecordComparer
    {
        internal static bool Equals(
            CustomScoreRecord left,
            CustomScoreRecord right) =>
            left.Lamp == right.Lamp &&
            left.Clear == right.Clear &&
            left.MaxCombo == right.MaxCombo &&
            left.Pace == right.Pace &&
            left.Score == right.Score &&
            left.SecondsSinceEpochUtc ==
                right.SecondsSinceEpochUtc &&
            CountsEqual(left.Tap, right.Tap) &&
            CountsEqual(left.Hold, right.Hold) &&
            CountsEqual(left.SkyArea, right.SkyArea) &&
            CountsEqual(left.Flick, right.Flick);

        private static bool CountsEqual(
            JudgementCounts left,
            JudgementCounts right) =>
            left.None == right.None &&
            left.Miss == right.Miss &&
            left.NearEarly == right.NearEarly &&
            left.NearLate == right.NearLate &&
            left.PerfectEarly == right.PerfectEarly &&
            left.PerfectLate == right.PerfectLate &&
            left.Shiny == right.Shiny;
    }
}
