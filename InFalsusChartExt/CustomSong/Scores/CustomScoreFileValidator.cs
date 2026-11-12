using System;
using System.Collections.Generic;

namespace InFalsusChartExt.CustomSong.Scores
{
    internal static class CustomScoreFileValidator
    {
        internal static bool Validate(
            CustomScoreDocument document,
            out string error)
        {
            error = null;
            if (document is null)
            {
                error = "document is null";
                return false;
            }
            if (document.SchemaVersion !=
                CustomScoreDocument.CurrentSchemaVersion)
            {
                error =
                    $"unsupported schemaVersion {document.SchemaVersion}";
                return false;
            }

            document.Entries ??= new List<CustomScoreEntry>();
            var keys = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < document.Entries.Count; i++)
            {
                CustomScoreEntry entry = document.Entries[i];
                if (!ValidateEntry(entry, i, out error) ||
                    !keys.Add(CustomScoreFile.Key(
                        entry.PackId,
                        entry.SongId,
                        entry.Difficulty)))
                {
                    if (error is null)
                        error = $"entries[{i}] duplicates key";
                    return false;
                }
            }
            return true;
        }

        private static bool ValidateEntry(
            CustomScoreEntry entry,
            int index,
            out string error)
        {
            error = null;
            if (entry is null ||
                string.IsNullOrWhiteSpace(entry.PackId) ||
                string.IsNullOrWhiteSpace(entry.SongId) ||
                string.IsNullOrWhiteSpace(entry.BaseName) ||
                string.IsNullOrWhiteSpace(entry.Difficulty) ||
                entry.Best is null)
            {
                error = $"entries[{index}] is incomplete";
                return false;
            }
            if (!ValidateCounts(entry.Best.Tap, out error) ||
                !ValidateCounts(entry.Best.Hold, out error) ||
                !ValidateCounts(entry.Best.SkyArea, out error) ||
                !ValidateCounts(entry.Best.Flick, out error))
            {
                error = $"entries[{index}]: {error}";
                return false;
            }
            if (entry.Best.MaxCombo < 0 ||
                entry.Best.MaxCombo > ushort.MaxValue)
            {
                error = $"entries[{index}] maxCombo is out of range";
                return false;
            }
            if (entry.Best.Pace < short.MinValue ||
                entry.Best.Pace > short.MaxValue)
            {
                error = $"entries[{index}] pace is out of range";
                return false;
            }
            if ((uint)entry.Best.Lamp > byte.MaxValue ||
                (uint)entry.Best.Clear > byte.MaxValue)
            {
                error =
                    $"entries[{index}] lamp/clear is out of range";
                return false;
            }
            if (!IsDifficulty(entry.Difficulty))
            {
                error =
                    $"entries[{index}] difficulty is invalid";
                return false;
            }
            return true;
        }

        private static bool ValidateCounts(
            JudgementCounts counts,
            out string error)
        {
            if (counts is null)
            {
                error = "judgement counts are missing";
                return false;
            }
            int[] values =
            {
                counts.None,
                counts.Miss,
                counts.NearEarly,
                counts.NearLate,
                counts.PerfectEarly,
                counts.PerfectLate,
                counts.Shiny,
            };
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < 0 ||
                    values[i] > ushort.MaxValue)
                {
                    error = "judgement count is out of range";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private static bool IsDifficulty(string value) =>
            string.Equals(value, "MIN", StringComparison.Ordinal) ||
            string.Equals(value, "EVO", StringComparison.Ordinal) ||
            string.Equals(value, "ULT", StringComparison.Ordinal) ||
            string.Equals(value, "FBD", StringComparison.Ordinal);
    }
}
