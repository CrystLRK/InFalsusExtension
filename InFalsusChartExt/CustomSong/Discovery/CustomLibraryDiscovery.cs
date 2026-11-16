using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using InFalsusChartExt.CustomSong.Config;
#if MELONLOADER
using Il2Cppifapp.Game.Data;
#elif BEPINEX
using ifapp.Game.Data;
#endif

namespace InFalsusChartExt.CustomSong.Discovery
{
    internal static class CustomLibraryDiscovery
    {
        private static readonly ChartDifficultyFlag[] SlotDifficulties =
        {
            ChartDifficultyFlag.Minimal,
            ChartDifficultyFlag.Evolved,
            ChartDifficultyFlag.Ultimate,
            ChartDifficultyFlag.Forbidden,
        };

        internal static CustomLibraryPlan Scan(out string report)
            => Scan(CustomSongPaths.ChartsRoot, out report);

        internal static CustomLibraryPlan Scan(
            string root,
            out string report)
        {
            var packs = new List<CustomPackPlan>();
            var lines = new List<string>();
            var packIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var songIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var baseNames = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(root))
            {
                report = "Charts directory not found: " + root;
                return new CustomLibraryPlan(packs);
            }

            int order = 0;
            foreach (string directory in Directory
                         .EnumerateDirectories(root)
                         .OrderBy(
                             path => path,
                             StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(directory);
                if (name.StartsWith(
                        ".",
                        StringComparison.Ordinal))
                    continue;
                if (!File.Exists(Path.Combine(
                        directory,
                        IfConfigReader.PackFileName)))
                {
                    lines.Add(
                        $"pack ignored '{name}': " +
                        IfConfigReader.PackFileName + " is missing");
                    continue;
                }

                try
                {
                    CustomPackPlan pack = ScanPack(
                        directory,
                        order++,
                        songIds,
                        baseNames);
                    if (!packIds.Add(pack.Config.Id))
                    {
                        lines.Add(
                            $"pack rejected '{name}': duplicate pack id " +
                            $"'{pack.Config.Id}'");
                        continue;
                    }
                    packs.Add(pack);
                    lines.Add(
                        $"pack '{pack.Config.Id}': " +
                        $"{pack.Songs.Count} song(s)");
                }
                catch (Exception exception)
                {
                    lines.Add(
                        $"pack rejected '{name}': " +
                        exception.Message);
                }
            }

            packs.Sort(ComparePacks);
            report = string.Join(Environment.NewLine, lines);
            return new CustomLibraryPlan(packs);
        }

        private static int ComparePacks(
            CustomPackPlan left,
            CustomPackPlan right)
        {
            int byOrder = left.Config.SortOrder.CompareTo(
                right.Config.SortOrder);
            if (byOrder != 0) return byOrder;
            int byTitle = string.Compare(
                left.Config.Title,
                right.Config.Title,
                StringComparison.OrdinalIgnoreCase);
            if (byTitle != 0) return byTitle;
            return string.Compare(
                left.Config.Id,
                right.Config.Id,
                StringComparison.OrdinalIgnoreCase);
        }

        private static CustomPackPlan ScanPack(
            string directory,
            int defaultOrder,
            HashSet<string> songIds,
            HashSet<string> baseNames)
        {
            if (!IfConfigReader.TryReadPack(
                    directory,
                    defaultOrder,
                    out PackJsonDocument pack,
                    out string packError))
            {
                throw new InvalidDataException(packError);
            }

            var songs = new List<CustomSongPlan>();
            var lines = new List<string>();
            foreach (string songDirectory in Directory
                         .EnumerateDirectories(directory)
                         .OrderBy(
                             path => path,
                             StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(songDirectory);
                if (name.StartsWith(
                        ".",
                        StringComparison.Ordinal))
                    continue;
                try
                {
                    songs.Add(ScanSong(
                        songDirectory,
                        songIds,
                        baseNames));
                }
                catch (Exception exception)
                {
                    lines.Add(
                        $"song ignored '{name}': " +
                        exception.Message);
                    ChartExtMod.Log.Warning(
                        $"[CustomSong] song '{name}' in pack " +
                        $"'{pack.Id}' was ignored: {exception.Message}");
                }
            }

            if (songs.Count == 0)
            {
                throw new InvalidDataException(
                    "pack contains no valid song directories" +
                    (lines.Count > 0
                        ? " (" + string.Join("; ", lines) + ")"
                        : string.Empty));
            }

            var config = new PackConfig
            {
                SchemaVersion = 1,
                Id = pack.Id,
                Title = CustomSongSchema.Language(pack.Titles, 0),
                Artist = pack.Artist ?? string.Empty,
                Description = pack.Description ?? string.Empty,
                StylePackSlug = string.IsNullOrWhiteSpace(
                    pack.StylePackSlug)
                    ? "act-4"
                    : pack.StylePackSlug,
                SortOrder = pack.SortOrder,
            };

            var result = new CustomPackPlan(
                directory,
                config,
                pack.Titles,
                pack.Visuals,
                songs);
            for (int i = 0; i < songs.Count; i++)
                songs[i].Pack = result;
            return result;
        }

        private static CustomSongPlan ScanSong(
            string directory,
            HashSet<string> songIds,
            HashSet<string> baseNames)
        {
            if (!IfConfigReader.TryReadSong(
                    directory,
                    out IfSongDocument song,
                    out string error))
            {
                throw new InvalidDataException(error);
            }
            if (!songIds.Add(song.Id))
            {
                throw new InvalidDataException(
                    $"duplicate song id '{song.Id}'");
            }

            string baseName = BuildBaseName(song.Id);
            if (!baseNames.Add(baseName))
            {
                throw new InvalidDataException(
                    $"song id '{song.Id}' produces duplicate " +
                    $"base name '{baseName}'");
            }

            ValidateAudio(song.AudioPaths, "audio");
            ValidateAudio(song.PreviewPaths, "preview audio");

            var charts = new List<CustomChartPlan>();
            for (int slot = 0;
                 slot < CustomSongSchema.DifficultyCount;
                 slot++)
            {
                string chartPath = song.ChartPaths[slot];
                if (string.IsNullOrWhiteSpace(chartPath))
                    continue;

                string parserKey =
                    Path.GetFileNameWithoutExtension(chartPath) +
                    ".spc";
                int rating = song.Ratings[slot];
                if (rating < 0)
                    rating = 0;

                var plan = new CustomChartPlan(
                    baseName + slot,
                    parserKey,
                    chartPath,
                    song.BackgroundPaths?[slot],
                    SlotDifficulties[slot],
                    slot,
                    rating,
                    song.LevelSections?[slot] ?? "?",
                    song.ChartDesigners?[slot],
                    song.JacketDesigners?[slot],
                    song.JacketLargePaths?[slot],
                    song.JacketSmallPaths?[slot]);
                charts.Add(plan);
            }

            charts.Sort(
                (left, right) =>
                    left.Slot.CompareTo(right.Slot));
            if (charts.Count == 0)
                throw new InvalidDataException(
                    "song contains no available difficulty");

            var config = new SongConfig
            {
                SchemaVersion = 1,
                Id = song.Id,
                Title = CustomSongSchema.Language(song.Titles, 0),
                Artist = CustomSongSchema.Language(
                    song.Composers, 0),
                ChartDesigner = charts[0].ChartDesigner(0),
                JacketDesigner =
                    CustomSongSchema.Language(song.Illustrators, 0),
                Audio = song.AudioPaths[0],
                PreviewStart = song.PreviewStarts[0],
                PreviewEnd = song.PreviewEnds[0],
                Background = song.BackgroundPaths?[0],
                JacketLarge = song.JacketLargePaths?[0],
                JacketSmall = song.JacketSmallPaths?[0],
            };

            var result = new CustomSongPlan(
                directory,
                config,
                baseName,
                song.Titles,
                song.Composers,
                song.Illustrators,
                song.AudioPaths,
                song.PreviewPaths,
                song.PreviewStarts,
                song.PreviewEnds,
                song.BackgroundPaths,
                song.JacketLargePaths,
                song.JacketSmallPaths,
                charts);
            foreach (CustomChartPlan chart in charts)
                chart.Song = result;
            return result;
        }

        private static void ValidateAudio(
            string[] paths,
            string label)
        {
            if (paths is null)
                return;
            var seen = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrWhiteSpace(path) ||
                    !seen.Add(path))
                    continue;
                ValidateAudioSource(path, label + "[" + i + "]");
            }
        }

        private static void ValidateAudioSource(
            string path,
            string label)
        {
            if (DetectAudioHeader(path, out _))
                return;
            if (CustomSongPaths.IsAudioPathValid(path))
                return;
            throw new InvalidDataException(
                label + " is not a recognized OGG/WAV/MP3 file " +
                "by content or extension: " + path);
        }

        private static bool DetectAudioHeader(
            string path,
            out string format)
        {
            using FileStream stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[12];
            int read = stream.Read(header);
            if (read >= 12 &&
                header[0] == (byte)'R' &&
                header[1] == (byte)'I' &&
                header[2] == (byte)'F' &&
                header[3] == (byte)'F' &&
                header[8] == (byte)'W' &&
                header[9] == (byte)'A' &&
                header[10] == (byte)'V' &&
                header[11] == (byte)'E')
            {
                format = "WAV";
                return true;
            }
            if (read >= 4 &&
                header[0] == (byte)'O' &&
                header[1] == (byte)'g' &&
                header[2] == (byte)'g' &&
                header[3] == (byte)'S')
            {
                format = "OGG";
                return true;
            }
            if (read >= 3 &&
                header[0] == (byte)'I' &&
                header[1] == (byte)'D' &&
                header[2] == (byte)'3')
            {
                format = "MP3";
                return true;
            }
            if (read >= 2 &&
                header[0] == 0xFF &&
                (header[1] & 0xE0) == 0xE0)
            {
                format = "MP3";
                return true;
            }
            format = null;
            return false;
        }

        private static string BuildBaseName(string id)
        {
            var builder = new StringBuilder("IFCE_");
            foreach (char character in id)
            {
                builder.Append(
                    (character <= 0x7F &&
                     char.IsLetterOrDigit(character)) ||
                    character == '_' || character == '-'
                        ? character
                        : '_');
            }
            return builder.ToString();
        }
    }
}
