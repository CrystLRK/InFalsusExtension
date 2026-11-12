using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace InFalsusChartExt
{
    internal sealed class NativeAbiProfile
    {
        private readonly Dictionary<string, JsonElement> _sections;

        internal string Id { get; }
        internal string Kind { get; }
        internal string GameAssemblySha256 { get; }
        internal string MetadataSha256 { get; }
        internal NativeCapability Capabilities { get; }
        internal JsonElement Root { get; }
        internal NativePresentationProfile Presentation { get; }
        internal NativeCapacityScannerProfile CapacityScanner { get; }
        internal IReadOnlyDictionary<string, NativePatchTargetProfile>
            PatchTargets { get; }

        internal NativeAbiProfile(
            string id,
            string kind,
            string gameAssemblySha256,
            string metadataSha256,
            NativeCapability capabilities,
            JsonElement root)
        {
            Id = id;
            Kind = kind;
            GameAssemblySha256 = gameAssemblySha256;
            MetadataSha256 = metadataSha256;
            Capabilities = capabilities;
            Root = root;
            _sections = new Dictionary<string, JsonElement>(
                StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in root.EnumerateObject())
                _sections[property.Name] = property.Value;
            Presentation = new NativePresentationProfile(
                Section("Presentation"));
            CapacityScanner = new NativeCapacityScannerProfile(
                Section("CapacityScanner"));
            var targets =
                new Dictionary<string, NativePatchTargetProfile>(
                    StringComparer.OrdinalIgnoreCase);
            JsonElement targetRoot = Section("PatchTargets");
            foreach (JsonProperty property in
                     targetRoot.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException(
                        $"PatchTargets.{property.Name} must be an object");
                }
                targets[property.Name] =
                    new NativePatchTargetProfile(
                        property.Name, property.Value);
            }
            PatchTargets = targets;
        }

        internal int Int(string section, string key)
        {
            JsonElement value = Get(section, key);
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out int number))
            {
                return number;
            }
            if (value.ValueKind == JsonValueKind.String &&
                value.GetString() is string text &&
                TryParseInteger(text, out number))
            {
                return number;
            }
            throw new InvalidDataException(
                $"native profile {section}.{key} is not an integer");
        }

        internal string String(string section, string key)
        {
            JsonElement value = Get(section, key);
            if (value.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    $"native profile {section}.{key} is not a string");
            }
            return value.GetString();
        }

        internal string[] StringArray(string section, string key)
        {
            JsonElement value = Get(section, key);
            if (value.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException(
                    $"native profile {section}.{key} is not an array");
            }
            return value.EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();
        }

        internal JsonElement Get(string section, string key)
        {
            if (!_sections.TryGetValue(
                    section, out JsonElement objectValue) ||
                objectValue.ValueKind != JsonValueKind.Object ||
                !objectValue.TryGetProperty(
                    key, out JsonElement value))
            {
                throw new InvalidDataException(
                    $"native profile is missing {section}.{key}");
            }
            return value;
        }

        private JsonElement Section(string section)
        {
            if (!_sections.TryGetValue(
                    section, out JsonElement value) ||
                value.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException(
                    "native profile is missing section " + section);
            }
            return value;
        }

        private static bool TryParseInteger(
            string text,
            out int value)
        {
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(
                    text.Substring(2),
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value);
            return int.TryParse(
                text,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }
    }

    internal static class NativeAbiProfileStore
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

        private static bool _attempted;
        private static string _error;

        internal static NativeAbiProfile Selected { get; private set; }
        internal static string Error => _error;

        internal static string EntryPath
        {
            get
            {
                string mods = Path.GetDirectoryName(
                    typeof(ChartExtMod).Assembly.Location);
                return Path.Combine(
                    mods ?? Environment.CurrentDirectory,
                    "InFalsusChartExt",
                    "native-profiles.json");
            }
        }

        internal static NativeAbiProfile Select(
            string gameHash,
            string metadataHash)
        {
            if (_attempted)
                return Selected;
            _attempted = true;
            try
            {
                string entryPath = EntryPath;
                if (!File.Exists(entryPath))
                {
                    _error =
                        "native profile entry is missing: " + entryPath;
                    return null;
                }

                ProfileEntry entry = Read<ProfileEntry>(entryPath);
                if (entry.SchemaVersion != 2 ||
                    entry.Profiles is null ||
                    entry.Profiles.Length == 0)
                {
                    throw new InvalidDataException(
                        "native profile entry schema is invalid");
                }

                string root = Path.GetDirectoryName(entryPath);
                foreach (ProfileReference reference in entry.Profiles)
                {
                    if (!HashEquals(
                            gameHash,
                            reference.GameAssemblySha256) ||
                        !HashEquals(
                            metadataHash,
                            reference.MetadataSha256))
                    {
                        continue;
                    }
                    if (reference.File is null ||
                        Path.IsPathRooted(reference.File) ||
                        reference.File.Contains(".."))
                    {
                        throw new InvalidDataException(
                            "native profile file path is unsafe");
                    }
                    string profilePath = Path.GetFullPath(
                        Path.Combine(root, reference.File));
                    if (!profilePath.StartsWith(
                            Path.GetFullPath(root) +
                            Path.DirectorySeparatorChar,
                            StringComparison.OrdinalIgnoreCase) ||
                        !File.Exists(profilePath))
                    {
                        throw new FileNotFoundException(
                            "native profile file is missing",
                            profilePath);
                    }

                    ProfileDocument document =
                        Read<ProfileDocument>(profilePath);
                    if (document.SchemaVersion != 2 ||
                        !HashEquals(
                            document.GameAssemblySha256,
                            gameHash) ||
                        !HashEquals(
                            document.MetadataSha256,
                            metadataHash))
                    {
                        throw new InvalidDataException(
                            "native profile hash does not match entry");
                    }
                    NativeCapability capabilities =
                        NativeCapabilityParser.Parse(
                            document.Capabilities);
                    Selected = new NativeAbiProfile(
                        document.Id,
                        document.Kind,
                        document.GameAssemblySha256,
                        document.MetadataSha256,
                        capabilities,
                        document.Root);
                    _error = null;
                    return Selected;
                }
                _error =
                    "no native profile matches GameAssembly/metadata hash";
            }
            catch (Exception exception)
            {
                _error = exception.Message;
                Selected = null;
            }
            return Selected;
        }

        private static T Read<T>(string path) where T : class
        {
            byte[] bytes = File.ReadAllBytes(path);
            T value = JsonSerializer.Deserialize<T>(
                new ReadOnlySpan<byte>(bytes), JsonOptions);
            return value ?? throw new InvalidDataException(
                "native profile JSON is empty");
        }

        private static bool HashEquals(
            string actual,
            string expected) =>
            !string.IsNullOrWhiteSpace(actual) &&
            !string.IsNullOrWhiteSpace(expected) &&
            string.Equals(
                actual,
                expected,
                StringComparison.OrdinalIgnoreCase);

        private sealed class ProfileEntry
        {
            public int SchemaVersion { get; set; }
            public ProfileReference[] Profiles { get; set; }
        }

        private sealed class ProfileReference
        {
            public string Id { get; set; }
            public string File { get; set; }
            public string GameAssemblySha256 { get; set; }
            public string MetadataSha256 { get; set; }
        }

        private sealed class ProfileDocument
        {
            public int SchemaVersion { get; set; }
            public string Id { get; set; }
            public string Kind { get; set; }
            public string GameAssemblySha256 { get; set; }
            public string MetadataSha256 { get; set; }
            public string[] Capabilities { get; set; }

            [System.Text.Json.Serialization.JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }

            internal JsonElement Root
            {
                get
                {
                    var dictionary =
                        new Dictionary<string, object>(
                            StringComparer.OrdinalIgnoreCase);
                    if (ExtensionData is not null)
                    {
                        foreach (KeyValuePair<string, JsonElement> item
                                 in ExtensionData)
                        {
                            dictionary[item.Key] = item.Value;
                        }
                    }
                    string json = JsonSerializer.Serialize(dictionary);
                    using JsonDocument document =
                        JsonDocument.Parse(json);
                    return document.RootElement.Clone();
                }
            }
        }
    }
}
