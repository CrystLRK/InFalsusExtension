using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using InFalsusChartExt.CustomSong.Discovery;

namespace InFalsusChartExt.CustomSong.Config
{
    internal sealed class IfSongDocument
    {
        internal string Id;
        internal string[] Titles;
        internal string[] Composers;
        internal string[] Illustrators;
        internal string[] AudioPaths;
        internal string[] PreviewPaths;
        internal float[] PreviewStarts;
        internal float[] PreviewEnds;
        internal string[] BackgroundPaths;
        internal string[] JacketLargePaths;
        internal string[] JacketSmallPaths;
        internal string[] ChartPaths;
        internal int[] Ratings;
        internal string[] LevelSections;
        internal string[][] ChartDesigners;
        internal string[][] JacketDesigners;
    }

    internal sealed class PackJsonDocument
    {
        internal int SchemaVersion;
        internal string Id;
        internal string[] Titles;
        internal string Artist;
        internal string Description;
        internal string StylePackSlug;
        internal int SortOrder;
        internal Dictionary<string, string[]> Visuals;
    }

    internal static class IfConfigReader
    {
        internal const string IfFileName = "if";
        internal const string PackFileName = "pack.json";

        private const int DifficultyCount = 4;
        private const int LanguageCount = 5;

        private static readonly string[] VisualKeys =
        {
            "songSelect",
            "songSelectHover",
            "selected",
            "selectedHover",
            "deselected",
            "deselectedHover",
            "outline",
        };

        private static readonly JsonDocumentOptions JsonOptions =
            new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

        internal static bool TryReadPack(
            string directory,
            int defaultSortOrder,
            out PackJsonDocument document,
            out string error)
        {
            document = null;
            error = null;
            string path = Path.Combine(directory, PackFileName);
            if (!File.Exists(path))
            {
                error = PackFileName + " was not found";
                return false;
            }

            try
            {
                using JsonDocument json = ParseFile(path, PackFileName);
                JsonElement root = json.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException(
                        PackFileName + " must be a JSON object");

                var built = new PackJsonDocument
                {
                    SchemaVersion = 1,
                    Id = Path.GetFileName(directory),
                    Titles = SameFive(Path.GetFileName(directory)),
                    StylePackSlug = "act-4",
                    SortOrder = defaultSortOrder,
                    Visuals = new Dictionary<string, string[]>(
                        StringComparer.Ordinal),
                };

                if (TryProperty(root, "schemaVersion", out JsonElement schema))
                {
                    int value = ReadInt(schema, "pack schemaVersion");
                    if (value != 1)
                        throw new InvalidDataException(
                            "pack schemaVersion must be 1");
                    built.SchemaVersion = value;
                }
                if (TryProperty(root, "id", out JsonElement id))
                {
                    string text = ReadText(id, "pack id");
                    built.Id = text;
                }
                if (TryProperty(root, "name", out JsonElement name))
                {
                    string[] titles = ReadLanguageFaces(
                        name, "pack name", true);
                    if (titles is not null)
                        built.Titles = titles;
                }
                if (TryProperty(root, "artist", out JsonElement artist))
                    built.Artist = ReadOptionalText(
                        artist, "pack artist");
                if (TryProperty(root, "description", out JsonElement description))
                    built.Description = ReadOptionalText(
                        description, "pack description");
                if (TryProperty(root, "stylePackSlug", out JsonElement style))
                    built.StylePackSlug = ReadText(
                        style, "pack stylePackSlug");
                if (TryProperty(root, "sortOrder", out JsonElement order))
                    built.SortOrder = ReadInt(
                        order, "pack sortOrder");

                if (TryProperty(root, "visuals", out JsonElement visuals) &&
                    visuals.ValueKind != JsonValueKind.Null)
                {
                    if (visuals.ValueKind != JsonValueKind.Object)
                        throw new InvalidDataException(
                            "pack visuals must be an object");
                    foreach (JsonProperty entry in visuals.EnumerateObject())
                    {
                        if (Array.IndexOf(
                                VisualKeys,
                                entry.Name) < 0)
                        {
                            ChartExtMod.Log.Warning(
                                $"[CustomSong] pack.json '{entry.Name}' " +
                                "is not one of the seven visual keys; ignored");
                            continue;
                        }
                        string[] slots = ReadPictureFaces(
                            directory,
                            entry.Value,
                            "pack visual " + entry.Name);
                        if (slots is not null)
                            built.Visuals[entry.Name] = slots;
                    }
                }

                if (string.IsNullOrWhiteSpace(built.Id))
                    throw new InvalidDataException(
                        "pack id is empty");
                if (built.Titles is null ||
                    string.IsNullOrWhiteSpace(
                        CustomSongSchema.Language(built.Titles, 2)))
                    throw new InvalidDataException(
                        "pack name is empty");

                document = built;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        internal static bool TryReadSong(
            string directory,
            out IfSongDocument document,
            out string error)
        {
            document = null;
            error = null;
            string path = Path.Combine(directory, IfFileName);
            if (!File.Exists(path))
            {
                error = IfFileName + " was not found";
                return false;
            }

            try
            {
                using JsonDocument json = ParseFile(path, IfFileName);
                JsonElement root = json.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException(
                        IfFileName + " must be a JSON object");

                var built = new IfSongDocument
                {
                    Id = ReadRequiredText(root, "id"),
                };
                if (TryProperty(root, "schemaVersion", out JsonElement schema))
                {
                    int value = ReadInt(schema, "song schemaVersion");
                    if (value != 1)
                        throw new InvalidDataException(
                            "song schemaVersion must be 1");
                }

                Faces names = ReadFaces(
                    root, "name", true, "song name");
                built.Titles = names.Base;
                Faces composers = ReadFaces(
                    root, "composer", true, "song composer");
                built.Composers = composers.Base;
                Faces illustrators = ReadFaces(
                    root, "illust", false, "song illust");
                built.Illustrators = illustrators?.Base ??
                    new string[LanguageCount];

                built.AudioPaths = ReadAudioFaces(
                    root,
                    directory,
                    "song",
                    true,
                    "song audio");

                built.PreviewPaths = ReadAudioFaces(
                    root,
                    directory,
                    "preview_audio",
                    false,
                    "preview audio");
                if (built.PreviewPaths is null)
                {
                    built.PreviewPaths =
                        (string[])built.AudioPaths.Clone();
                }

                ReadPreviewWindows(
                    root, out built.PreviewStarts, out built.PreviewEnds);

                built.JacketLargePaths = ReadPictureDifficulties(
                    root,
                    directory,
                    "jacket_large",
                    false,
                    "jacket_large");
                built.JacketSmallPaths = ReadPictureDifficulties(
                    root,
                    directory,
                    "jacket_small",
                    false,
                    "jacket_small");
                string[] jackets = ReadPictureDifficulties(
                    root,
                    directory,
                    "jacket",
                    false,
                    "jacket");
                if (built.JacketLargePaths is null)
                    built.JacketLargePaths =
                        jackets ?? new string[DifficultyCount];
                if (built.JacketSmallPaths is null)
                    built.JacketSmallPaths =
                        jackets ?? new string[DifficultyCount];

                built.BackgroundPaths = ReadBackgroundDifficulties(
                    root, directory);

                string[] charts = ReadDifficultyTexts(
                    root, "chart", false, "chart");
                Faces charters = ReadCharterFaces(root);
                int[] ratings = ReadDifficultyLevels(root);
                string[] specials = ReadDifficultySpecials(root);
                string[] sections = ReadDifficultyTexts(
                    root,
                    "level_section_indicator",
                    false,
                    "level_section_indicator",
                    singleToAll: true);

                built.ChartPaths = new string[DifficultyCount];
                built.Ratings = ratings;
                built.LevelSections = new string[DifficultyCount];
                built.ChartDesigners = new string[DifficultyCount][];
                built.JacketDesigners = new string[DifficultyCount][];
                bool anyChart = false;
                for (int slot = 0; slot < DifficultyCount; slot++)
                {
                    built.ChartDesigners[slot] =
                        new string[LanguageCount];
                    built.JacketDesigners[slot] =
                        new string[LanguageCount];
                    ChartDesignerFace designer = ReadChartDesigner(
                        charters, slot);
                    built.ChartDesigners[slot] = designer.Languages;
                    built.JacketDesigners[slot] =
                        ReadIllustratorFace(
                            illustrators,
                            slot);

                    string section = sections?[slot];
                    if (string.IsNullOrWhiteSpace(section))
                        section = specials?[slot];
                    if (string.IsNullOrWhiteSpace(section) &&
                        ratings[slot] >= 0)
                    {
                        section = ratings[slot].ToString(
                            CultureInfo.InvariantCulture);
                    }
                    if (string.IsNullOrWhiteSpace(section))
                        section = "?";
                    built.LevelSections[slot] = section;

                    string chartName = charts?[slot];
                    if (string.IsNullOrWhiteSpace(chartName))
                        continue;
                    built.ChartPaths[slot] = ResolveFile(
                        directory,
                        chartName,
                        CustomSongPaths.IsChartPathValid,
                        "difficulty " + slot + " chart");
                    anyChart = true;
                }
                if (!anyChart)
                    throw new InvalidDataException(
                        "song contains no available difficulty");

                document = built;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static JsonDocument ParseFile(
            string path,
            string label)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                return JsonDocument.Parse(
                    new ReadOnlyMemory<byte>(bytes),
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException(
                    label + " is invalid JSON: " + exception.Message,
                    exception);
            }
        }

        private static string ReadRequiredText(
            JsonElement root,
            string key)
        {
            if (!TryProperty(root, key, out JsonElement value))
                throw new InvalidDataException(key + " is required");
            return ReadText(value, key);
        }

        private static string ReadText(
            JsonElement value,
            string label)
        {
            if (value.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new InvalidDataException(
                    label + " must be a non-empty string");
            }
            return value.GetString().Trim();
        }

        private static string ReadOptionalText(
            JsonElement value,
            string label)
        {
            if (value.ValueKind == JsonValueKind.Null)
                return string.Empty;
            if (value.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    label + " must be a string");
            }
            return value.GetString().Trim();
        }

        private static int ReadInt(
            JsonElement value,
            string label)
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out int number))
                return number;
            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(
                    value.GetString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out number))
                return number;
            throw new InvalidDataException(
                label + " must be an integer");
        }

        private static bool TryProperty(
            JsonElement root,
            string name,
            out JsonElement value) =>
            root.TryGetProperty(name, out value) &&
            value.ValueKind != JsonValueKind.Null;

        private sealed class Faces
        {
            internal string[] Base;
            internal string[][] PerDifficulty;
        }

        private static Faces ReadFaces(
            JsonElement root,
            string key,
            bool required,
            string label)
        {
            if (!TryProperty(root, key, out JsonElement token))
            {
                if (required)
                    throw new InvalidDataException(
                        label + " is required");
                return null;
            }

            var baseSlots = new string[LanguageCount];
            var perDifficulty = new string[DifficultyCount][];
            bool anyBase = false;
            bool anySet = false;

            switch (token.ValueKind)
            {
                case JsonValueKind.String:
                {
                    string text = ReadText(token, label);
                    for (int i = 0; i < baseSlots.Length; i++)
                        baseSlots[i] = text;
                    anyBase = true;
                    break;
                }
                case JsonValueKind.Object:
                    ReadLanguageObject(
                        token,
                        label,
                        baseSlots,
                        perDifficulty,
                        ref anyBase,
                        ref anySet);
                    break;
                case JsonValueKind.Array:
                    ReadFacesArray(
                        token,
                        label,
                        baseSlots,
                        perDifficulty,
                        ref anyBase,
                        ref anySet);
                    break;
                default:
                    throw new InvalidDataException(
                        label + " must be a string, object or array");
            }

            if (!anyBase && !anySet)
            {
                if (required)
                    throw new InvalidDataException(
                        label + " is empty");
                return null;
            }

            if (anySet)
            {
                FillBaseFromSets(baseSlots, perDifficulty);
                string first = FirstNonEmpty(baseSlots);
                if (first is null)
                {
                    if (required)
                        throw new InvalidDataException(
                            label + " is empty");
                    return null;
                }
                for (int i = 0; i < baseSlots.Length; i++)
                    if (string.IsNullOrEmpty(baseSlots[i]))
                        baseSlots[i] = first;
                FillSets(perDifficulty, baseSlots);
            }
            else
            {
                string first = FirstNonEmpty(baseSlots);
                if (first is null)
                {
                    if (required)
                        throw new InvalidDataException(
                            label + " is empty");
                    return null;
                }
                for (int i = 0; i < baseSlots.Length; i++)
                    if (string.IsNullOrEmpty(baseSlots[i]))
                        baseSlots[i] = first;
            }

            return new Faces
            {
                Base = baseSlots,
                PerDifficulty = perDifficulty,
            };
        }

        private static void ReadFacesArray(
            JsonElement token,
            string label,
            string[] baseSlots,
            string[][] perDifficulty,
            ref bool anyBase,
            ref bool anySet)
        {
            int count = token.GetArrayLength();
            if (count == LanguageCount)
            {
                bool nested = true;
                for (int language = 0;
                     language < LanguageCount;
                     language++)
                {
                    JsonElement row = token[language];
                    if (row.ValueKind != JsonValueKind.Array ||
                        row.GetArrayLength() != DifficultyCount)
                    {
                        nested = false;
                        break;
                    }
                }

                if (nested)
                {
                    for (int language = 0;
                         language < LanguageCount;
                         language++)
                    {
                        JsonElement row = token[language];
                        for (int slot = 0;
                             slot < DifficultyCount;
                             slot++)
                        {
                            if (row[slot].ValueKind ==
                                JsonValueKind.Null)
                                continue;
                            perDifficulty[slot] ??=
                                new string[LanguageCount];
                            perDifficulty[slot][language] =
                                ReadText(row[slot], label);
                            anySet = true;
                        }
                    }
                    return;
                }

                for (int language = 0;
                     language < LanguageCount;
                     language++)
                {
                    baseSlots[language] = ReadText(
                        token[language], label);
                }
                anyBase = true;
                return;
            }

            if (count == DifficultyCount)
            {
                for (int slot = 0; slot < DifficultyCount; slot++)
                {
                    if (token[slot].ValueKind == JsonValueKind.Null)
                        continue;
                    string text = ReadText(token[slot], label);
                    perDifficulty[slot] = SameFive(text);
                    anySet = true;
                }
                return;
            }

            throw new InvalidDataException(
                label + " must have five languages or four difficulties");
        }

        private static void ReadLanguageObject(
            JsonElement token,
            string label,
            string[] baseSlots,
            string[][] perDifficulty,
            ref bool anyBase,
            ref bool anySet)
        {
            foreach (JsonProperty entry in token.EnumerateObject())
            {
                int language = CustomSongSchema.LanguageIndex(
                    entry.Name);
                if (language < 0)
                {
                    ChartExtMod.Log.Warning(
                        $"[CustomSong] {label}: '{entry.Name}' is not " +
                        "a language (sc/tc/en/ja/ko); ignored");
                    continue;
                }

                if (entry.Value.ValueKind == JsonValueKind.String)
                {
                    baseSlots[language] = ReadText(
                        entry.Value, label);
                    anyBase = true;
                    continue;
                }
                if (entry.Value.ValueKind != JsonValueKind.Array ||
                    entry.Value.GetArrayLength() != DifficultyCount)
                {
                    throw new InvalidDataException(
                        label + "." + entry.Name +
                        " must be a string or four-item array");
                }
                for (int slot = 0; slot < DifficultyCount; slot++)
                {
                    if (entry.Value[slot].ValueKind ==
                        JsonValueKind.Null)
                        continue;
                    perDifficulty[slot] ??=
                        new string[LanguageCount];
                    perDifficulty[slot][language] = ReadText(
                        entry.Value[slot], label);
                    anySet = true;
                }
            }
        }

        private static void FillBaseFromSets(
            string[] baseSlots,
            string[][] perDifficulty)
        {
            for (int language = 0;
                 language < LanguageCount;
                 language++)
            {
                if (!string.IsNullOrEmpty(baseSlots[language]))
                    continue;
                for (int slot = 0; slot < DifficultyCount; slot++)
                {
                    string[] row = perDifficulty[slot];
                    if (row is null ||
                        string.IsNullOrEmpty(row[language]))
                        continue;
                    baseSlots[language] = row[language];
                    break;
                }
            }
        }

        private static void FillSets(
            string[][] perDifficulty,
            string[] baseSlots)
        {
            for (int slot = 0; slot < DifficultyCount; slot++)
            {
                string[] row = perDifficulty[slot];
                if (row is null)
                    continue;
                for (int language = 0;
                     language < LanguageCount;
                     language++)
                    if (string.IsNullOrEmpty(row[language]))
                        row[language] = baseSlots[language];
            }
        }

        private static string FirstNonEmpty(string[] values)
        {
            if (values is null)
                return null;
            for (int i = 0; i < values.Length; i++)
                if (!string.IsNullOrEmpty(values[i]))
                    return values[i];
            return null;
        }

        private static string[] SameFive(string text)
        {
            var result = new string[LanguageCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = text;
            return result;
        }

        private static string[] ReadLanguageFaces(
            JsonElement token,
            string label,
            bool required)
        {
            switch (token.ValueKind)
            {
                case JsonValueKind.String:
                    return SameFive(ReadText(token, label));
                case JsonValueKind.Array:
                {
                    if (token.GetArrayLength() != LanguageCount)
                        throw new InvalidDataException(
                            label + " must have five languages");
                    var result = new string[LanguageCount];
                    for (int i = 0; i < result.Length; i++)
                        result[i] = ReadText(token[i], label);
                    return result;
                }
                case JsonValueKind.Object:
                {
                    var result = new string[LanguageCount];
                    foreach (JsonProperty entry in token.EnumerateObject())
                    {
                        int language =
                            CustomSongSchema.LanguageIndex(entry.Name);
                        if (language < 0)
                            continue;
                        result[language] = ReadText(
                            entry.Value, label);
                    }
                    string first = FirstNonEmpty(result);
                    if (first is null)
                    {
                        if (required)
                            throw new InvalidDataException(
                                label + " is empty");
                        return null;
                    }
                    for (int i = 0; i < result.Length; i++)
                        if (string.IsNullOrEmpty(result[i]))
                            result[i] = first;
                    return result;
                }
                default:
                    throw new InvalidDataException(
                        label + " must be a string, object or array");
            }
        }

        private static string[] ReadPictureFaces(
            string directory,
            JsonElement token,
            string label)
        {
            string[] names = ReadLanguageFaces(token, label, false);
            if (names is null)
                return null;
            var result = new string[LanguageCount];
            bool any = false;
            for (int i = 0; i < result.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(names[i]))
                    continue;
                try
                {
                    result[i] = ResolveFile(
                        directory,
                        names[i],
                        CustomSongPaths.IsJacketPathValid,
                        label);
                    any = true;
                }
                catch (Exception exception)
                {
                    ChartExtMod.Log.Warning(
                        "[CustomSong] " + label + ": " +
                        exception.Message);
                }
            }
            return any ? result : null;
        }

        private static string[] ReadAudioFaces(
            JsonElement root,
            string directory,
            string key,
            bool required,
            string label)
        {
            if (!TryProperty(root, key, out JsonElement token))
            {
                if (required)
                    throw new InvalidDataException(
                        label + " is required");
                return null;
            }
            string[] result = ReadFileFaces(
                token,
                directory,
                label,
                CustomSongPaths.IsAudioPathValid,
                true);
            if (string.IsNullOrWhiteSpace(result[0]))
            {
                throw new InvalidDataException(
                    label + "[0] is required");
            }
            for (int i = 1; i < result.Length; i++)
                if (string.IsNullOrWhiteSpace(result[i]))
                    result[i] = result[0];
            return result;
        }

        private static string[] ReadFileFaces(
            JsonElement token,
            string directory,
            string label,
            Func<string, bool> allowed,
            bool allowNullEntries)
        {
            var result = new string[DifficultyCount];
            if (token.ValueKind == JsonValueKind.String)
            {
                string path = ResolveFile(
                    directory,
                    token.GetString(),
                    allowed,
                    label);
                for (int i = 0; i < result.Length; i++)
                    result[i] = path;
                return result;
            }
            if (token.ValueKind != JsonValueKind.Array ||
                token.GetArrayLength() != DifficultyCount)
            {
                throw new InvalidDataException(
                    label + " must be a path or four-item array");
            }
            for (int i = 0; i < result.Length; i++)
            {
                if (token[i].ValueKind == JsonValueKind.Null)
                {
                    if (allowNullEntries)
                        continue;
                    throw new InvalidDataException(
                        label + "[" + i + "] is null");
                }
                result[i] = ResolveFile(
                    directory,
                    token[i].GetString(),
                    allowed,
                    label);
            }
            return result;
        }

        private static string[] ReadPictureDifficulties(
            JsonElement root,
            string directory,
            string key,
            bool required,
            string label)
        {
            if (!TryProperty(root, key, out JsonElement token))
            {
                if (required)
                    throw new InvalidDataException(
                        label + " is required");
                return null;
            }
            return ReadFileFaces(
                token,
                directory,
                label,
                CustomSongPaths.IsJacketPathValid,
                true);
        }

        private static string[] ReadBackgroundDifficulties(
            JsonElement root,
            string directory)
        {
            if (!TryProperty(root, "background", out JsonElement token))
                return new string[DifficultyCount];
            return ReadFileFaces(
                token,
                directory,
                "background",
                CustomSongPaths.IsBackgroundPathValid,
                true);
        }

        private static void ReadPreviewWindows(
            JsonElement root,
            out float[] starts,
            out float[] ends)
        {
            starts = new float[DifficultyCount];
            ends = new float[DifficultyCount];
            if (!TryProperty(
                    root,
                    "preview_seconds",
                    out JsonElement token))
                return;
            if (token.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException(
                    "preview_seconds must be an array");

            if (token.GetArrayLength() == 2 &&
                token[0].ValueKind != JsonValueKind.Array)
            {
                ReadWindow(token, out float start, out float end);
                for (int i = 0; i < DifficultyCount; i++)
                {
                    starts[i] = start;
                    ends[i] = end;
                }
                return;
            }

            if (token.GetArrayLength() == 0 ||
                token.GetArrayLength() > DifficultyCount)
            {
                throw new InvalidDataException(
                    "preview_seconds must hold one or four windows");
            }
            var present = new bool[DifficultyCount];
            for (int i = 0; i < token.GetArrayLength(); i++)
            {
                if (token[i].ValueKind == JsonValueKind.Null)
                    continue;
                ReadWindow(
                    token[i],
                    out starts[i],
                    out ends[i]);
                present[i] = true;
            }
            if (!present[0])
            {
                if (token.GetArrayLength() > 0)
                    throw new InvalidDataException(
                        "preview_seconds[0] is required");
            }
            for (int i = 1; i < DifficultyCount; i++)
            {
                if (present[i])
                    continue;
                starts[i] = starts[0];
                ends[i] = ends[0];
            }
        }

        private static void ReadWindow(
            JsonElement token,
            out float start,
            out float end)
        {
            if (token.ValueKind != JsonValueKind.Array ||
                token.GetArrayLength() != 2)
            {
                throw new InvalidDataException(
                    "preview window must be [start, end]");
            }
            start = ReadSeconds(token[0], "preview start");
            end = ReadSeconds(token[1], "preview end");
            if (!float.IsFinite(start) ||
                !float.IsFinite(end) ||
                end < start)
            {
                throw new InvalidDataException(
                    "preview time range is invalid");
            }
        }

        private static float ReadSeconds(
            JsonElement token,
            string label)
        {
            if (token.ValueKind == JsonValueKind.Number &&
                token.TryGetDouble(out double number))
                return (float)number;
            if (token.ValueKind == JsonValueKind.String &&
                float.TryParse(
                    token.GetString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float parsed))
                return parsed;
            throw new InvalidDataException(
                label + " must be a number");
        }

        private static string[] ReadDifficultyTexts(
            JsonElement root,
            string key,
            bool required,
            string label,
            bool singleToAll = false)
        {
            if (!TryProperty(root, key, out JsonElement token))
            {
                if (required)
                    throw new InvalidDataException(
                        label + " is required");
                return null;
            }
            var result = new string[DifficultyCount];
            if (token.ValueKind == JsonValueKind.String)
            {
                string text = ReadText(token, label);
                if (singleToAll)
                {
                    for (int i = 0; i < result.Length; i++)
                        result[i] = text;
                }
                else
                {
                    result[result.Length - 1] = text;
                }
                return result;
            }
            if (token.ValueKind != JsonValueKind.Array ||
                token.GetArrayLength() != DifficultyCount)
            {
                throw new InvalidDataException(
                    label + " must be a string or four-item array");
            }
            for (int i = 0; i < result.Length; i++)
            {
                if (token[i].ValueKind == JsonValueKind.Null)
                    continue;
                result[i] = ReadText(token[i], label);
            }
            return result;
        }

        private static int[] ReadDifficultyLevels(JsonElement root)
        {
            var result = new int[DifficultyCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = -1;
            if (!TryProperty(root, "lv", out JsonElement token))
                return result;
            if (token.ValueKind == JsonValueKind.Number ||
                token.ValueKind == JsonValueKind.String)
            {
                result[result.Length - 1] = ReadInt(token, "lv");
                return result;
            }
            if (token.ValueKind != JsonValueKind.Array ||
                token.GetArrayLength() != DifficultyCount)
            {
                throw new InvalidDataException(
                    "lv must be an integer or four-item array");
            }
            for (int i = 0; i < result.Length; i++)
            {
                if (token[i].ValueKind == JsonValueKind.Null)
                    continue;
                int value = ReadInt(token[i], "lv");
                if (value < 0)
                    throw new InvalidDataException(
                        "lv must not be negative");
                result[i] = value;
            }
            return result;
        }

        private static string[] ReadDifficultySpecials(JsonElement root)
        {
            if (!TryProperty(root, "sp", out JsonElement token))
                return null;
            var result = new string[DifficultyCount];
            if (token.ValueKind == JsonValueKind.Array)
            {
                if (token.GetArrayLength() != DifficultyCount)
                    throw new InvalidDataException(
                        "sp must have four items");
                for (int i = 0; i < result.Length; i++)
                    result[i] = ReadSpecial(token[i]);
                return result;
            }
            result[result.Length - 1] = ReadSpecial(token);
            return result;
        }

        private static string ReadSpecial(JsonElement token)
        {
            if (token.ValueKind == JsonValueKind.Null)
                return null;
            if (token.ValueKind == JsonValueKind.String)
                return token.GetString();
            if (token.ValueKind == JsonValueKind.Number)
                return token.GetRawText();
            throw new InvalidDataException(
                "sp items must be strings or numbers");
        }

        private sealed class ChartDesignerFace
        {
            internal string[] Languages;
        }

        private static Faces ReadCharterFaces(JsonElement root)
        {
            if (!TryProperty(root, "charter", out JsonElement token) ||
                token.ValueKind == JsonValueKind.String)
            {
                if (token.ValueKind == JsonValueKind.String)
                {
                    string single = ReadText(token, "charter");
                    var onlyForbidden =
                        new string[DifficultyCount][];
                    onlyForbidden[DifficultyCount - 1] =
                        SameFive(single);
                    return new Faces
                    {
                        Base = SameFive(single),
                        PerDifficulty = onlyForbidden,
                    };
                }
                return null;
            }

            bool languageShaped =
                token.ValueKind == JsonValueKind.Object ||
                (token.ValueKind == JsonValueKind.Array &&
                 token.GetArrayLength() == LanguageCount);
            if (languageShaped)
                return ReadFaces(root, "charter", false, "charter");

            string[] perDifficulty = ReadDifficultyTexts(
                root, "charter", false, "charter");
            if (perDifficulty is null)
                return null;
            var sets = new string[DifficultyCount][];
            bool any = false;
            for (int i = 0; i < sets.Length; i++)
            {
                if (perDifficulty[i] is null)
                    continue;
                sets[i] = SameFive(perDifficulty[i]);
                any = true;
            }
            return any
                ? new Faces { PerDifficulty = sets }
                : null;
        }

        private static ChartDesignerFace ReadChartDesigner(
            Faces charters,
            int slot)
        {
            var result = new ChartDesignerFace
            {
                Languages = new string[LanguageCount],
            };
            if (charters?.PerDifficulty is not null &&
                charters.PerDifficulty[slot] is not null)
            {
                result.Languages =
                    charters.PerDifficulty[slot];
            }
            else if (charters?.Base is not null)
            {
                result.Languages = charters.Base;
            }
            return result;
        }

        private static string[] ReadIllustratorFace(
            Faces illustrators,
            int slot)
        {
            if (illustrators?.PerDifficulty is not null &&
                illustrators.PerDifficulty[slot] is not null)
                return illustrators.PerDifficulty[slot];
            return illustrators?.Base ??
                new string[LanguageCount];
        }

        private static string ResolveFile(
            string directory,
            string configured,
            Func<string, bool> allowed,
            string label)
        {
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
            return resolved;
        }
    }
}
