using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using InFalsusChartExt.CustomSong.Config;
using Il2Cppifapp.Game.Data;

namespace InFalsusChartExt.CustomSong.Discovery
{
    internal static class CustomLibraryDiscovery
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

        private static readonly Dictionary<string, ChartDifficultyFlag>
            DifficultyByName =
                new Dictionary<string, ChartDifficultyFlag>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["MIN"] = ChartDifficultyFlag.Minimal,
                    ["MINIMAL"] = ChartDifficultyFlag.Minimal,
                    ["EVO"] = ChartDifficultyFlag.Evolved,
                    ["EVOLVED"] = ChartDifficultyFlag.Evolved,
                    ["ULT"] = ChartDifficultyFlag.Ultimate,
                    ["ULTIMATE"] = ChartDifficultyFlag.Ultimate,
                    ["FBD"] = ChartDifficultyFlag.Forbidden,
                    ["FORBIDDEN"] = ChartDifficultyFlag.Forbidden,
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
            var parserKeys = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var baseNames = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(root))
            {
                report = "CustomCharts directory not found: " + root;
                return new CustomLibraryPlan(packs);
            }

            foreach (string directory in Directory
                         .EnumerateDirectories(root)
                         .OrderBy(
                             path => path,
                             StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var candidatePackIds = new HashSet<string>(
                        packIds, StringComparer.OrdinalIgnoreCase);
                    var candidateSongIds = new HashSet<string>(
                        songIds, StringComparer.OrdinalIgnoreCase);
                    var candidateParserKeys = new HashSet<string>(
                        parserKeys, StringComparer.OrdinalIgnoreCase);
                    var candidateBaseNames = new HashSet<string>(
                        baseNames, StringComparer.OrdinalIgnoreCase);
                    CustomPackPlan pack = ScanPack(
                        directory,
                        candidatePackIds,
                        candidateSongIds,
                        candidateParserKeys,
                        candidateBaseNames);
                    packIds = candidatePackIds;
                    songIds = candidateSongIds;
                    parserKeys = candidateParserKeys;
                    baseNames = candidateBaseNames;
                    packs.Add(pack);
                    lines.Add(
                        $"pack '{pack.Config.Id}': " +
                        $"{pack.Songs.Count} song(s)");
                }
                catch (Exception exception)
                {
                    lines.Add(
                        $"pack rejected '{Path.GetFileName(directory)}': " +
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
            HashSet<string> packIds,
            HashSet<string> songIds,
            HashSet<string> parserKeys,
            HashSet<string> baseNames)
        {
            PackConfig config = ReadJson<PackConfig>(
                Path.Combine(directory, "pack_config.json"),
                "pack_config.json");
            ValidatePackConfig(config);
            if (!packIds.Add(config.Id))
                throw new InvalidDataException(
                    $"duplicate pack id '{config.Id}'");

            string jacket = Path.Combine(directory, "pack_jacket.png");
            if (!File.Exists(jacket))
                jacket = null;

            var songs = new List<CustomSongPlan>();
            foreach (string songDirectory in Directory
                         .EnumerateDirectories(directory)
                         .OrderBy(
                             path => path,
                             StringComparer.OrdinalIgnoreCase))
            {
                songs.Add(ScanSong(
                    songDirectory,
                    songIds,
                    parserKeys,
                    baseNames));
            }

            if (songs.Count == 0)
                throw new InvalidDataException(
                    "pack contains no valid song directories");

            var pack = new CustomPackPlan(
                directory, config, jacket, songs);
            for (int i = 0; i < songs.Count; i++)
                songs[i].Pack = pack;
            return pack;
        }

        private static CustomSongPlan ScanSong(
            string directory,
            HashSet<string> songIds,
            HashSet<string> parserKeys,
            HashSet<string> baseNames)
        {
            SongConfig config = ReadJson<SongConfig>(
                Path.Combine(directory, "config.json"),
                "song config.json");
            ValidateSongConfig(config);
            if (!songIds.Add(config.Id))
                throw new InvalidDataException(
                    $"duplicate song id '{config.Id}'");

            string audio = ResolveRequired(
                directory,
                config.Audio,
                null,
                "audio");
            ValidateAudioSource(audio, "audio");

            string preview = audio;
            float previewStart = config.PreviewStart ?? 0f;
            float previewEnd = config.PreviewEnd ?? previewStart;
            if (config.Preview is not null)
            {
                if (!string.IsNullOrWhiteSpace(config.Preview.Audio))
                {
                    preview = ResolveRequired(
                        directory,
                        config.Preview.Audio,
                        null,
                        "preview.audio");
                    ValidateAudioSource(preview, "preview.audio");
                }
                previewStart = config.Preview.Start ?? previewStart;
                previewEnd = config.Preview.End ?? previewEnd;
            }
            if (!float.IsFinite(previewStart) ||
                !float.IsFinite(previewEnd) ||
                previewEnd < previewStart)
            {
                throw new InvalidDataException(
                    "preview time range is invalid");
            }

            string background = ResolveOptional(
                directory,
                config.Background,
                CustomSongPaths.IsBackgroundPathValid,
                "background");
            string jacketLarge = ResolveOptional(
                directory,
                config.JacketLarge,
                CustomSongPaths.IsJacketPathValid,
                "jacketLarge");
            string jacketSmall = ResolveOptional(
                directory,
                config.JacketSmall,
                CustomSongPaths.IsJacketPathValid,
                "jacketSmall");

            string baseName = BuildBaseName(config.Id);
            if (!baseNames.Add(baseName))
            {
                throw new InvalidDataException(
                    $"song id '{config.Id}' produces duplicate " +
                    $"base name '{baseName}'");
            }
            var charts = new List<CustomChartPlan>();
            foreach (KeyValuePair<string, DifficultyConfig> item
                     in config.Difficulties)
            {
                if (!DifficultyByName.TryGetValue(
                        item.Key,
                        out ChartDifficultyFlag difficulty))
                {
                    throw new InvalidDataException(
                        $"unknown difficulty '{item.Key}'");
                }
                if (item.Value is null ||
                    string.IsNullOrWhiteSpace(item.Value.Chart))
                {
                    throw new InvalidDataException(
                        $"difficulty '{item.Key}' has no chart");
                }

                string chartPath = ResolveRequired(
                    directory,
                    item.Value.Chart,
                    CustomSongPaths.IsChartPathValid,
                    "difficulty " + item.Key);
                string parserKey =
                    Path.GetFileNameWithoutExtension(chartPath) + ".spc";
                if (!parserKeys.Add(parserKey))
                {
                    throw new InvalidDataException(
                        $"duplicate chart parser key '{parserKey}'");
                }

                int slot = DifficultySlot(difficulty);
                string difficultyBackground = ResolveOptional(
                    directory,
                    item.Value.Background,
                    CustomSongPaths.IsBackgroundPathValid,
                    "difficulty " + item.Key + " background");
                charts.Add(new CustomChartPlan(
                    baseName + slot,
                    parserKey,
                    chartPath,
                    difficultyBackground ?? background,
                    difficulty,
                    item.Value.Rating,
                    string.IsNullOrWhiteSpace(
                        item.Value.LevelSectionIndicator)
                        ? "?"
                        : item.Value.LevelSectionIndicator.Trim()));
            }

            charts.Sort(
                (left, right) =>
                    DifficultySlot(left.Difficulty).CompareTo(
                        DifficultySlot(right.Difficulty)));
            if (charts.Count == 0)
                throw new InvalidDataException(
                    "song contains no available difficulty");

            return new CustomSongPlan(
                directory,
                config,
                baseName,
                audio,
                preview,
                previewStart,
                previewEnd,
                background,
                jacketLarge,
                jacketSmall,
                charts);
        }

        private static T ReadJson<T>(string path, string label)
            where T : class
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    label + " was not found", path);
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                T value = JsonSerializer.Deserialize<T>(
                    new ReadOnlySpan<byte>(bytes), JsonOptions);
                return value ?? throw new InvalidDataException(
                    label + " is empty");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException(
                    label + " is invalid JSON: " + exception.Message,
                    exception);
            }
        }

        private static void ValidatePackConfig(PackConfig config)
        {
            if (config.SchemaVersion != 1)
                throw new InvalidDataException(
                    "pack schemaVersion must be 1");
            RequireText(config.Id, "pack id");
            RequireText(config.Title, "pack title");
        }

        private static void ValidateSongConfig(SongConfig config)
        {
            if (config.SchemaVersion != 1)
                throw new InvalidDataException(
                    "song schemaVersion must be 1");
            RequireText(config.Id, "song id");
            RequireText(config.Title, "song title");
            RequireText(config.Audio, "song audio");
            if (config.Difficulties is null ||
                config.Difficulties.Count == 0)
                throw new InvalidDataException(
                    "song difficulties are missing");
        }

        private static void RequireText(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException(label + " is required");
        }

        private static string ResolveRequired(
            string root,
            string configured,
            Func<string, bool> extensionAllowed,
            string label)
        {
            if (!CustomSongPaths.TryResolveFile(
                    root,
                    configured,
                    null,
                    out string resolved,
                    out string error))
            {
                throw new InvalidDataException(
                    label + ": " + error);
            }
            if (extensionAllowed is not null &&
                !extensionAllowed(resolved))
            {
                throw new InvalidDataException(
                    label + ": unsupported extension '" +
                    Path.GetExtension(resolved) + "'");
            }
            return resolved;
        }

        private static void ValidateAudioSource(
            string path,
            string label)
        {
            if (DetectAudioHeader(path, out string format))
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

        private static string ResolveOptional(
            string root,
            string configured,
            Func<string, bool> extensionAllowed,
            string label)
        {
            if (string.IsNullOrWhiteSpace(configured))
                return null;
            return ResolveRequired(
                root, configured, extensionAllowed, label);
        }

        private static int DifficultySlot(
            ChartDifficultyFlag difficulty) =>
            difficulty switch
            {
                ChartDifficultyFlag.Minimal => 0,
                ChartDifficultyFlag.Evolved => 1,
                ChartDifficultyFlag.Ultimate => 2,
                ChartDifficultyFlag.Forbidden => 3,
                _ => throw new InvalidDataException(
                    $"unsupported difficulty value {(byte)difficulty}"),
            };

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
