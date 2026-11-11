using System;
using InFalsusChartExt.CustomSong.Discovery;
using Il2Cppifapp.Game.Data;

namespace InFalsusChartExt.CustomSong.Scores
{
    /// <summary>
    /// Converts the Mod-owned score DTO to and from the original
    /// GameResultV4 value type. This type never writes the save file.
    /// </summary>
    internal static class CustomScoreNativeBridge
    {
        internal static GameResultsV4 FindGameResults()
        {
            try
            {
                Il2Cpp_K._NH save = Il2Cpp_K._NH._CEb;
                return save?._vNA();
            }
            catch (Exception exception)
            {
                ChartExtMod.Log.Error(
                    "[CustomScore] GameResultsV4 lookup failed: " +
                    exception.Message);
                return null;
            }
        }

        internal static GameResultV4 ToNative(
            CustomScoreRecord record,
            SongInfo songInfo,
            CustomChartPlan chart)
        {
            return new GameResultV4
            {
                SongId = songInfo.Id,
                Difficulty = chart.Difficulty,
                Lamp = (GameResultLamp)record.Lamp,
                CalculatedResultClear =
                    (GameResultClear)record.Clear,
                MaxCombo = (ushort)record.MaxCombo,
                CalculatedPaceValue = (short)record.Pace,
                TapCounts = ToNative(record.Tap),
                HoldCounts = ToNative(record.Hold),
                SkyAreaCounts = ToNative(record.SkyArea),
                FlickCounts = ToNative(record.Flick),
                PlayerScore = record.Score,
                SecondsSinceEpochUtc =
                    record.SecondsSinceEpochUtc,
            };
        }

        internal static CustomScoreRecord ToRecord(
            GameResultV4 result) =>
            new CustomScoreRecord
            {
                Lamp = (int)result.Lamp,
                Clear = (int)result.CalculatedResultClear,
                MaxCombo = result.MaxCombo,
                Pace = result.CalculatedPaceValue,
                Tap = ToRecord(result.TapCounts),
                Hold = ToRecord(result.HoldCounts),
                SkyArea = ToRecord(result.SkyAreaCounts),
                Flick = ToRecord(result.FlickCounts),
                Score = result.PlayerScore,
                SecondsSinceEpochUtc =
                    result.SecondsSinceEpochUtc,
            };

        private static JudgementTypeCount ToNative(
            JudgementCounts counts) =>
            new JudgementTypeCount
            {
                None = (ushort)counts.None,
                Miss = (ushort)counts.Miss,
                NearEarly = (ushort)counts.NearEarly,
                NearLate = (ushort)counts.NearLate,
                PerfectEarly = (ushort)counts.PerfectEarly,
                PerfectLate = (ushort)counts.PerfectLate,
                Shiny = (ushort)counts.Shiny,
            };

        private static JudgementCounts ToRecord(
            JudgementTypeCount counts) =>
            new JudgementCounts
            {
                None = counts.None,
                Miss = counts.Miss,
                NearEarly = counts.NearEarly,
                NearLate = counts.NearLate,
                PerfectEarly = counts.PerfectEarly,
                PerfectLate = counts.PerfectLate,
                Shiny = counts.Shiny,
            };
    }
}
