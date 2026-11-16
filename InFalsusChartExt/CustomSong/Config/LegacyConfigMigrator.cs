using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace InFalsusChartExt.CustomSong.Config
{
    internal static class LegacyConfigMigrator
    {
        private const int MigrationSchema = 1;
        private const int DifficultyCount = 4;
        private const int LanguageCount = 5;

        private static readonly Dictionary<string, int> DifficultySlots =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["MIN"] = 0,
                ["MINIMAL"] = 0,
                ["EVO"] = 1,
                ["EVOLVED"] = 1,
                ["ULT"] = 2,
                ["ULTIMATE"] = 2,
                ["FBD"] = 3,
                ["FORBIDDEN"] = 3,
            };

        private static readonly JsonDocumentOptions JsonOptions =
            new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

        private static readonly object Gate = new object();
        private static bool _completed;

        internal static void EnsureMigrated(out string report)
            => EnsureMigrated(out report, null);

        internal static void EnsureMigrated(
            out string report,
            Action<string> progress)
        {
            lock (Gate)
            {
                EnsureMigratedCore(out report, progress);
            }
        }

        private static void EnsureMigratedCore(
            out string report,
            Action<string> progress)
        {
            report = null;
            if (_completed)
                return;
            _completed = true;

            var lines = new List<string>();
            string root = CustomSongPaths.ChartsRoot;
            string legacy = CustomSongPaths.LegacyChartsRoot;
            try
            {
                Directory.CreateDirectory(root);
                ArchiveDirectSongDirectories(root, lines);

                if (Directory.Exists(legacy))
                {
                    MigrateLegacyPacks(
                        legacy, root, lines, progress);
                }
                else if (File.Exists(
                             CustomSongPaths.MigrationMarkerPath))
                {
                    lines.Add(
                        "migration marker present; legacy conversion skipped");
                }
                else
                {
                    lines.Add("legacy CustomCharts directory not found");
                }
            }
            catch (Exception exception)
            {
                lines.Add("migration failed: " + exception);
                ChartExtMod.Log.Error(
                    "[CustomSong] legacy config migration failed: " +
                    exception);
            }

            if (lines.Count > 0)
            {
                report = string.Join(Environment.NewLine, lines);
                ChartExtMod.Log.Msg(
                    "[CustomSong] migration:\n" + report);
            }
        }

        private static void ArchiveDirectSongDirectories(
            string root,
            List<string> lines)
        {
            foreach (string directory in Directory
                         .EnumerateDirectories(root)
                         .OrderByOrdinal())
            {
                string name = Path.GetFileName(directory);
                if (name.StartsWith(".", StringComparison.Ordinal))
                    continue;
                if (File.Exists(Path.Combine(
                        directory,
                        IfConfigReader.PackFileName)))
                    continue;

                string target = UniqueArchivePath(name);
                Directory.CreateDirectory(
                    CustomSongPaths.LegacyArchiveRoot);
                Directory.Move(directory, target);
                lines.Add(
                    $"archived legacy chart folder '{name}' -> " +
                    Path.GetFileName(target));
            }
        }

        private static string UniqueArchivePath(string name)
        {
            string root = CustomSongPaths.LegacyArchiveRoot;
            string candidate = Path.Combine(root, name);
            int suffix = 2;
            while (Directory.Exists(candidate) ||
                   File.Exists(candidate))
            {
                candidate = Path.Combine(
                    root,
                    name + "-" + suffix++);
            }
            return candidate;
        }

        private static void MigrateLegacyPacks(
            string legacyRoot,
            string chartsRoot,
            List<string> lines,
            Action<string> progress)
        {
            var mapping = new List<PackMapping>();
            foreach (string packDirectory in Directory
                         .EnumerateDirectories(legacyRoot)
                         .OrderByOrdinal())
            {
                string packName = Path.GetFileName(packDirectory);
                string configPath = Path.Combine(
                    packDirectory,
                    "pack_config.json");
                if (!File.Exists(configPath))
                {
                    lines.Add(
                        $"legacy pack skipped '{packName}': " +
                        "pack_config.json is missing");
                    continue;
                }
                progress?.Invoke(
                    "MIGRATING CHARTS: " + packName);

                try
                {
                    PackConfig pack = ReadLegacyJson<PackConfig>(
                        configPath, "pack_config.json");
                    if (pack.SchemaVersion != 1 ||
                        string.IsNullOrWhiteSpace(pack.Id) ||
                        string.IsNullOrWhiteSpace(pack.Title))
                    {
                        throw new InvalidDataException(
                            "pack_config.json is invalid");
                    }

                    string targetDirectory = Path.Combine(
                        chartsRoot, packName);
                    if (Directory.Exists(targetDirectory))
                    {
                        if (File.Exists(Path.Combine(
                                targetDirectory,
                                "pack_config.json")))
                        {
                            PackMapping pending =
                                ConvertPackInPlace(
                                    targetDirectory,
                                    pack,
                                    lines);
                            if (pending is not null)
                            {
                                mapping.Add(pending);
                                lines.Add(
                                    $"legacy pack converted in place " +
                                    $"'{packName}': " +
                                    $"{pending.Songs.Count} song(s)");
                            }
                            continue;
                        }
                        if (File.Exists(Path.Combine(
                                targetDirectory,
                                IfConfigReader.PackFileName)))
                        {
                            if (CanDropLegacyDuplicate(
                                    packDirectory,
                                    targetDirectory))
                            {
                                Directory.Delete(
                                    packDirectory, true);
                                lines.Add(
                                    $"legacy duplicate removed " +
                                    $"'{packName}' (target already converted)");
                                PackMapping existing =
                                    ReadPackMapping(
                                        targetDirectory, pack.Id);
                                if (existing is not null)
                                    mapping.Add(existing);
                            }
                            else
                            {
                                lines.Add(
                                    $"legacy pack conflict '{packName}': " +
                                    "target already converted but source " +
                                    "has more songs; source kept");
                            }
                            continue;
                        }
                        lines.Add(
                            $"legacy pack conflict '{packName}': " +
                            "target exists without pack.json or " +
                            "pack_config.json; skipped");
                        continue;
                    }

                    Directory.Move(packDirectory, targetDirectory);
                    PackMapping moved = ConvertPackInPlace(
                        targetDirectory, pack, lines);
                    if (moved is not null)
                    {
                        mapping.Add(moved);
                        lines.Add(
                            $"legacy pack moved '{packName}' -> " +
                            $"Charts: {moved.Songs.Count} song(s)");
                    }
                }
                catch (Exception exception)
                {
                    lines.Add(
                        $"legacy pack failed '{packName}': " +
                        exception.Message);
                }
            }

            RetryPendingPacks(
                chartsRoot, mapping, lines, progress);
            CleanupLegacyRoot(legacyRoot, chartsRoot, lines);
            if (mapping.Count == 0)
            {
                lines.Add(
                    "no legacy pack converted; marker withheld " +
                    "so the next start can retry");
                return;
            }
            WriteMigrationMarker(mapping, lines);
        }

        private static void RetryPendingPacks(
            string chartsRoot,
            List<PackMapping> mapping,
            List<string> lines,
            Action<string> progress)
        {
            foreach (string directory in Directory
                         .EnumerateDirectories(chartsRoot)
                         .OrderByOrdinal())
            {
                string name = Path.GetFileName(directory);
                if (name.StartsWith(".", StringComparison.Ordinal))
                    continue;
                string configPath = Path.Combine(
                    directory, "pack_config.json");
                if (!File.Exists(configPath))
                    continue;
                progress?.Invoke(
                    "MIGRATING CHARTS: " + name);
                try
                {
                    PackConfig pack = ReadLegacyJson<PackConfig>(
                        configPath, "pack_config.json");
                    PackMapping pending = ConvertPackInPlace(
                        directory, pack, lines);
                    if (pending is not null)
                    {
                        mapping.Add(pending);
                        lines.Add(
                            $"legacy pack retried '{name}': " +
                            $"{pending.Songs.Count} song(s)");
                    }
                }
                catch (Exception exception)
                {
                    lines.Add(
                        $"legacy pack retry failed '{name}': " +
                        exception.Message);
                }
            }
        }

        private static PackMapping ConvertPackInPlace(
            string targetDirectory,
            PackConfig pack,
            List<string> lines)
        {
            string folderName = Path.GetFileName(targetDirectory);
            string jacket = FindPackJacket(targetDirectory);
            WritePackJson(
                Path.Combine(
                    targetDirectory,
                    IfConfigReader.PackFileName),
                pack,
                jacket is null
                    ? null
                    : Path.GetFileName(jacket));

            var songMappings = new List<SongMapping>();
            foreach (string songDirectory in Directory
                         .EnumerateDirectories(targetDirectory)
                         .OrderByOrdinal())
            {
                string songName = Path.GetFileName(songDirectory);
                string ifPath = Path.Combine(
                    songDirectory, IfConfigReader.IfFileName);
                if (File.Exists(ifPath))
                {
                    SongConfig existing = TryReadLegacyJson<SongConfig>(
                        Path.Combine(songDirectory, "config.json"));
                    songMappings.Add(new SongMapping
                    {
                        Folder = songName,
                        OldId = existing?.Id,
                        NewId = existing?.Id,
                    });
                    continue;
                }

                string songConfig = Path.Combine(
                    songDirectory, "config.json");
                if (!File.Exists(songConfig))
                {
                    lines.Add(
                        $"legacy song skipped '{folderName}/{songName}': " +
                        "config.json is missing");
                    continue;
                }

                try
                {
                    SongConfig song = ReadLegacyJson<SongConfig>(
                        songConfig, "config.json");
                    ValidateSong(song, songDirectory);
                    WriteSongIf(ifPath, song);
                    File.Delete(songConfig);
                    songMappings.Add(new SongMapping
                    {
                        Folder = songName,
                        OldId = song.Id,
                        NewId = song.Id,
                    });
                }
                catch (Exception exception)
                {
                    lines.Add(
                        $"legacy song failed " +
                        $"'{folderName}/{songName}': " +
                        exception.Message);
                }
            }

            bool allConverted = true;
            foreach (string songDirectory in Directory
                         .EnumerateDirectories(targetDirectory))
            {
                if (!File.Exists(Path.Combine(
                        songDirectory, "config.json")))
                    continue;
                if (File.Exists(Path.Combine(
                        songDirectory,
                        IfConfigReader.IfFileName)))
                    continue;
                allConverted = false;
                break;
            }
            if (allConverted)
            {
                string packConfig = Path.Combine(
                    targetDirectory, "pack_config.json");
                if (File.Exists(packConfig))
                    File.Delete(packConfig);
            }

            if (songMappings.Count == 0)
            {
                lines.Add(
                    $"legacy pack '{folderName}' produced no song; " +
                    "pack.json kept for manual review");
            }
            return new PackMapping
            {
                Folder = folderName,
                OldId = pack.Id,
                NewId = pack.Id,
                Songs = songMappings,
            };
        }

        private static void ValidateSong(
            SongConfig song,
            string directory)
        {
            if (song.SchemaVersion != 1 ||
                string.IsNullOrWhiteSpace(song.Id) ||
                string.IsNullOrWhiteSpace(song.Title) ||
                string.IsNullOrWhiteSpace(song.Audio) ||
                song.Difficulties is null ||
                song.Difficulties.Count == 0)
            {
                throw new InvalidDataException(
                    "config.json is invalid");
            }

            ValidateLegacyPath(
                directory,
                song.Audio,
                CustomSongPaths.IsAudioPathValid,
                "audio");
            ValidateLegacyPath(
                directory,
                song.Preview?.Audio,
                CustomSongPaths.IsAudioPathValid,
                "preview audio");
            ValidateLegacyPath(
                directory,
                song.JacketLarge,
                CustomSongPaths.IsJacketPathValid,
                "jacketLarge");
            ValidateLegacyPath(
                directory,
                song.JacketSmall,
                CustomSongPaths.IsJacketPathValid,
                "jacketSmall");
            ValidateLegacyPath(
                directory,
                song.Background,
                CustomSongPaths.IsBackgroundPathValid,
                "background");
            foreach (KeyValuePair<string, DifficultyConfig> item
                     in song.Difficulties)
            {
                if (item.Value is null)
                    continue;
                ValidateLegacyPath(
                    directory,
                    item.Value.Chart,
                    CustomSongPaths.IsChartPathValid,
                    item.Key + " chart");
                ValidateLegacyPath(
                    directory,
                    item.Value.Background,
                    CustomSongPaths.IsBackgroundPathValid,
                    item.Key + " background");
            }
        }

        private static bool CanDropLegacyDuplicate(
            string source,
            string target)
        {
            int sourceSongs = CountSongDirectories(
                source, "config.json");
            int targetSongs = CountSongDirectories(
                target, IfConfigReader.IfFileName);
            return targetSongs >= sourceSongs;
        }

        private static int CountSongDirectories(
            string directory,
            string fileName)
        {
            int count = 0;
            foreach (string song in Directory
                         .EnumerateDirectories(directory))
            {
                if (File.Exists(Path.Combine(song, fileName)))
                    count++;
            }
            return count;
        }

        private static void CleanupLegacyRoot(
            string legacyRoot,
            string chartsRoot,
            List<string> lines)
        {
            if (!Directory.Exists(legacyRoot))
                return;
            var entries = new List<string>(
                Directory.EnumerateFileSystemEntries(legacyRoot));
            if (entries.Count == 0)
            {
                Directory.Delete(legacyRoot, true);
                lines.Add(
                    "removed empty legacy CustomCharts directory");
                return;
            }

            string leftoverRoot = Path.Combine(
                chartsRoot, ".ifce-legacy", "legacy-root");
            Directory.CreateDirectory(leftoverRoot);
            foreach (string entry in entries.OrderByOrdinal())
            {
                string name = Path.GetFileName(entry);
                string target = UniquePath(leftoverRoot, name);
                if (Directory.Exists(entry))
                    Directory.Move(entry, target);
                else
                    File.Move(entry, target);
                lines.Add(
                    $"moved legacy leftover '{name}' -> " +
                    "Charts/.ifce-legacy/legacy-root");
            }
            Directory.Delete(legacyRoot, true);
            lines.Add(
                "removed legacy CustomCharts directory after moving " +
                "leftovers");
        }

        private static string UniquePath(
            string root,
            string name)
        {
            string candidate = Path.Combine(root, name);
            int suffix = 2;
            while (Directory.Exists(candidate) ||
                   File.Exists(candidate))
            {
                candidate = Path.Combine(
                    root, name + "-" + suffix++);
            }
            return candidate;
        }

        private static T TryReadLegacyJson<T>(string path)
            where T : class
        {
            if (!File.Exists(path))
                return null;
            try
            {
                return ReadLegacyJson<T>(path, "config.json");
            }
            catch
            {
                return null;
            }
        }

        private static void WritePackJson(
            string path,
            PackConfig pack,
            string jacketFile)
        {
            WriteAtomically(path, writer =>
            {
                writer.WriteStartObject();
                writer.WriteNumber("schemaVersion", 1);
                writer.WriteString("id", pack.Id);
                WriteLanguageArray(
                    writer, "name", pack.Title);
                if (!string.IsNullOrWhiteSpace(pack.Artist))
                    writer.WriteString("artist", pack.Artist);
                if (!string.IsNullOrWhiteSpace(pack.Description))
                    writer.WriteString(
                        "description", pack.Description);
                writer.WriteString(
                    "stylePackSlug",
                    string.IsNullOrWhiteSpace(pack.StylePackSlug)
                        ? "act-4"
                        : pack.StylePackSlug);
                writer.WriteNumber("sortOrder", pack.SortOrder);
                if (!string.IsNullOrWhiteSpace(jacketFile))
                {
                    writer.WritePropertyName("visuals");
                    writer.WriteStartObject();
                    writer.WriteString(
                        "songSelect",
                        ToJsonPath(jacketFile));
                    writer.WriteString(
                        "songSelectHover",
                        ToJsonPath(jacketFile));
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }, overwrite: true);
        }

        private static void WriteSongIf(
            string path,
            SongConfig song)
        {
            string jacket = FirstNonEmpty(
                song.JacketLarge, song.JacketSmall);
            string jacketLarge = FirstNonEmpty(
                song.JacketLarge, jacket);
            string jacketSmall = FirstNonEmpty(
                song.JacketSmall, jacket);
            string previewAudio = FirstNonEmpty(
                song.Preview?.Audio, song.Audio);
            float previewStart =
                song.Preview?.Start ??
                song.PreviewStart ??
                0f;
            float previewEnd =
                song.Preview?.End ??
                song.PreviewEnd ??
                previewStart;
            if (!float.IsFinite(previewStart) ||
                !float.IsFinite(previewEnd))
            {
                previewStart = 0f;
                previewEnd = 0f;
            }
            else if (previewEnd < previewStart)
            {
                previewEnd = previewStart;
            }

            var charts = new string[DifficultyCount];
            var ratings = new int?[DifficultyCount];
            var levelSections = new string[DifficultyCount];
            var charters = new string[DifficultyCount];
            var backgrounds = new string[DifficultyCount];
            var indicator = new string[DifficultyCount];
            foreach (KeyValuePair<string, DifficultyConfig> item
                     in song.Difficulties)
            {
                if (item.Value is null ||
                    string.IsNullOrWhiteSpace(item.Value.Chart))
                    continue;
                if (!DifficultySlots.TryGetValue(
                        item.Key, out int slot))
                {
                    throw new InvalidDataException(
                        $"unknown difficulty '{item.Key}'");
                }
                charts[slot] = item.Value.Chart;
                ratings[slot] = item.Value.Rating;
                charters[slot] = song.ChartDesigner;
                backgrounds[slot] = FirstNonEmpty(
                    item.Value.Background,
                    song.Background);
                indicator[slot] =
                    item.Value.LevelSectionIndicator;
                levelSections[slot] = string.IsNullOrWhiteSpace(
                    item.Value.LevelSectionIndicator)
                    ? (item.Value.Rating > 0
                        ? item.Value.Rating.ToString()
                        : "?")
                    : item.Value.LevelSectionIndicator;
            }

            WriteAtomically(path, writer =>
            {
                writer.WriteStartObject();
                writer.WriteNumber("schemaVersion", 1);
                writer.WriteString("id", song.Id);
                WriteLanguageArray(
                    writer, "name", song.Title);
                WriteLanguageArray(
                    writer,
                    "composer",
                    FirstNonEmpty(song.Artist, song.Title));
                if (!string.IsNullOrWhiteSpace(
                        song.JacketDesigner))
                {
                    WriteLanguageArray(
                        writer,
                        "illust",
                        song.JacketDesigner);
                }
                WriteDifficultyArray(
                    writer, "song", song.Audio);
                WriteDifficultyArray(
                    writer, "preview_audio", previewAudio);
                WritePreviewWindows(
                    writer, previewStart, previewEnd);
                if (!string.IsNullOrWhiteSpace(jacket))
                {
                    WriteDifficultyArray(
                        writer, "jacket", jacket);
                    WriteDifficultyArray(
                        writer, "jacket_large", jacketLarge);
                    WriteDifficultyArray(
                        writer, "jacket_small", jacketSmall);
                }
                WriteNullableDifficultyArray(
                    writer, "background", backgrounds);
                WriteNullableDifficultyArray(
                    writer, "chart", charts);
                WriteNullableDifficultyArray(
                    writer, "charter", charters);
                WriteLevelArray(writer, "lv", ratings);
                WriteNullableDifficultyArray(
                    writer, "sp", indicator);
                WriteNullableDifficultyArray(
                    writer,
                    "level_section_indicator",
                    levelSections);
                writer.WriteEndObject();
            });
        }

        private static void WritePreviewWindows(
            Utf8JsonWriter writer,
            float start,
            float end)
        {
            writer.WritePropertyName("preview_seconds");
            writer.WriteStartArray();
            for (int i = 0; i < DifficultyCount; i++)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(start);
                writer.WriteNumberValue(end);
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
        }

        private static void WriteLanguageArray(
            Utf8JsonWriter writer,
            string key,
            string value)
        {
            writer.WritePropertyName(key);
            writer.WriteStartArray();
            for (int i = 0; i < LanguageCount; i++)
                writer.WriteStringValue(value ?? string.Empty);
            writer.WriteEndArray();
        }

        private static void WriteDifficultyArray(
            Utf8JsonWriter writer,
            string key,
            string value)
        {
            writer.WritePropertyName(key);
            writer.WriteStartArray();
            for (int i = 0; i < DifficultyCount; i++)
                writer.WriteStringValue(value ?? string.Empty);
            writer.WriteEndArray();
        }

        private static void WriteNullableDifficultyArray(
            Utf8JsonWriter writer,
            string key,
            string[] values)
        {
            writer.WritePropertyName(key);
            writer.WriteStartArray();
            for (int i = 0; i < DifficultyCount; i++)
            {
                string value = values?[i];
                if (string.IsNullOrWhiteSpace(value))
                    writer.WriteNullValue();
                else
                    writer.WriteStringValue(ToJsonPath(value));
            }
            writer.WriteEndArray();
        }

        private static void WriteLevelArray(
            Utf8JsonWriter writer,
            string key,
            int?[] values)
        {
            writer.WritePropertyName(key);
            writer.WriteStartArray();
            for (int i = 0; i < DifficultyCount; i++)
            {
                if (values[i].HasValue)
                    writer.WriteNumberValue(values[i].Value);
                else
                    writer.WriteNullValue();
            }
            writer.WriteEndArray();
        }

        private static string ToJsonPath(string value) =>
            (value ?? string.Empty)
                .Replace('\\', '/');

        private static void ValidateLegacyPath(
            string directory,
            string configured,
            Func<string, bool> allowed,
            string label)
        {
            if (string.IsNullOrWhiteSpace(configured))
                return;
            if (!CustomSongPaths.TryResolveFile(
                    directory,
                    configured,
                    null,
                    out string resolved,
                    out string error))
            {
                throw new InvalidDataException(
                    label + ": " + error);
            }
            if (allowed is not null && !allowed(resolved))
            {
                throw new InvalidDataException(
                    label + ": unsupported extension '" +
                    Path.GetExtension(resolved) + "'");
            }
        }

        private static string FirstNonEmpty(
            params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
                if (!string.IsNullOrWhiteSpace(values[i]))
                    return values[i];
            return null;
        }

        private static string FindPackJacket(string directory)
        {
            foreach (string name in new[]
                     {
                         "pack_jacket.png",
                         "pack_jacket.jpg",
                         "pack_jacket.jpeg",
                     })
            {
                string path = Path.Combine(directory, name);
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        private static T ReadLegacyJson<T>(
            string path,
            string label)
            where T : class
        {
            byte[] bytes = File.ReadAllBytes(path);
            try
            {
                T value = JsonSerializer.Deserialize<T>(
                    new ReadOnlySpan<byte>(bytes),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling =
                            JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    });
                return value ?? throw new InvalidDataException(
                    label + " is empty");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException(
                    label + " is invalid JSON: " +
                    exception.Message,
                    exception);
            }
        }

        private static void WriteAtomically(
            string path,
            Action<Utf8JsonWriter> write,
            bool overwrite = false)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            string temporary = path + ".tmp-" +
                Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(
                           temporary,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None))
                using (var writer = new Utf8JsonWriter(
                           stream,
                           new JsonWriterOptions
                           {
                               Indented = true,
                           }))
                {
                    write(writer);
                }
                File.Move(temporary, path, overwrite);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private static PackMapping ReadPackMapping(
            string targetDirectory,
            string fallbackId)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(
                    File.ReadAllBytes(Path.Combine(
                        targetDirectory,
                        IfConfigReader.PackFileName)),
                    JsonOptions);
                string id = fallbackId;
                if (document.RootElement.TryGetProperty(
                        "id",
                        out JsonElement value) &&
                    value.ValueKind == JsonValueKind.String)
                    id = value.GetString();
                return new PackMapping
                {
                    Folder = Path.GetFileName(targetDirectory),
                    OldId = fallbackId,
                    NewId = id,
                    Songs = new List<SongMapping>(),
                };
            }
            catch
            {
                return null;
            }
        }

        private static void WriteMigrationMarker(
            List<PackMapping> packs,
            List<string> lines)
        {
            string path = CustomSongPaths.MigrationMarkerPath;
            WriteAtomically(path, writer =>
            {
                writer.WriteStartObject();
                writer.WriteNumber("schemaVersion", MigrationSchema);
                writer.WriteString(
                    "createdUtc",
                    DateTime.UtcNow.ToString("O"));
                writer.WriteString(
                    "legacyRoot",
                    CustomSongPaths.LegacyChartsRoot);
                writer.WritePropertyName("packs");
                writer.WriteStartArray();
                foreach (PackMapping pack in packs)
                {
                    writer.WriteStartObject();
                    writer.WriteString("folder", pack.Folder);
                    writer.WriteString("oldId", pack.OldId);
                    writer.WriteString("newId", pack.NewId);
                    writer.WritePropertyName("songs");
                    writer.WriteStartArray();
                    foreach (SongMapping song in pack.Songs)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("folder", song.Folder);
                        writer.WriteString("oldId", song.OldId);
                        writer.WriteString("newId", song.NewId);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }, overwrite: true);
            lines.Add(
                $"migration marker written with {packs.Count} pack(s)");
        }

        private sealed class PackMapping
        {
            internal string Folder;
            internal string OldId;
            internal string NewId;
            internal List<SongMapping> Songs;
        }

        private sealed class SongMapping
        {
            internal string Folder;
            internal string OldId;
            internal string NewId;
        }
    }

    internal static class LegacyMigrationOrdering
    {
        internal static IEnumerable<string> OrderByOrdinal(
            this IEnumerable<string> values)
        {
            var list = new List<string>(values);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }
    }
}
