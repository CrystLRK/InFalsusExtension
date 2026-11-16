using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace InFalsusChartExt
{
    internal sealed class NativePresentationProfile
    {
        internal int Stride { get; }
        internal int NoteId { get; }
        internal int RecordStart { get; }
        internal int RecordCount { get; }
        internal int RecordIndex { get; }
        internal int PositionX { get; }
        internal int PositionY { get; }
        internal int PositionZ { get; }
        internal string EnqueueMethodName { get; }
        internal int EnqueueParameterSize { get; }

        internal NativePresentationProfile(JsonElement root)
        {
            Stride = Int(root, "Stride");
            NoteId = Int(root, "NoteId");
            RecordStart = Int(root, "RecordStart");
            RecordCount = Int(root, "RecordCount");
            RecordIndex = Int(root, "RecordIndex");
            PositionX = Int(root, "PositionX");
            PositionY = Int(root, "PositionY");
            PositionZ = Int(root, "PositionZ");
            EnqueueMethodName = String(root, "EnqueueMethodName");
            EnqueueParameterSize = Int(root, "EnqueueParameterSize");
            if (Stride <= 0 ||
                EnqueueParameterSize != Stride)
            {
                throw new InvalidDataException(
                    "Presentation stride and enqueue parameter size " +
                    "must match and be positive");
            }
            foreach (int offset in new[]
                     {
                         NoteId,
                         RecordStart,
                         RecordCount,
                         RecordIndex,
                         PositionX,
                         PositionY,
                         PositionZ,
                     })
            {
                if (offset < 0 || offset >= Stride)
                {
                    throw new InvalidDataException(
                        "Presentation field offset is outside stride");
                }
            }
        }

        private static int Int(JsonElement root, string key)
        {
            if (!root.TryGetProperty(key, out JsonElement value) ||
                value.ValueKind != JsonValueKind.Number ||
                !value.TryGetInt32(out int result))
            {
                throw new InvalidDataException(
                    "Presentation." + key + " must be an integer");
            }
            return result;
        }

        private static string String(JsonElement root, string key)
        {
            if (!root.TryGetProperty(key, out JsonElement value) ||
                value.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new InvalidDataException(
                    "Presentation." + key + " must be a non-empty string");
            }
            return value.GetString();
        }
    }

    internal sealed class NativeCapacityScannerProfile
    {
        internal int MovEdxOpcode { get; }
        internal int CallOpcode { get; }
        internal int MaxScanBytes { get; }
        internal int MaxPatternGap { get; }
        internal int SideNoteSiteCount { get; }
        internal int ChartSiteCount { get; }
        internal int JudgeStateSiteCount { get; }
        internal string[] ChartSiteNames { get; }

        internal NativeCapacityScannerProfile(JsonElement root)
        {
            MovEdxOpcode = Int(root, "MovEdxOpcode");
            CallOpcode = Int(root, "CallOpcode");
            MaxScanBytes = Int(root, "MaxScanBytes");
            MaxPatternGap = Int(root, "MaxPatternGap");
            SideNoteSiteCount = Int(root, "SideNoteSiteCount");
            ChartSiteCount = Int(root, "ChartSiteCount");
            JudgeStateSiteCount = Int(root, "JudgeStateSiteCount");
            ChartSiteNames = Strings(root, "ChartSiteNames");
            if (ChartSiteNames.Length != ChartSiteCount)
            {
                throw new InvalidDataException(
                    "CapacityScanner.ChartSiteNames length does not " +
                    "match ChartSiteCount");
            }
            if (MovEdxOpcode < byte.MinValue ||
                MovEdxOpcode > byte.MaxValue ||
                CallOpcode < byte.MinValue ||
                CallOpcode > byte.MaxValue ||
                MaxScanBytes <= 0 ||
                MaxPatternGap <= 0 ||
                SideNoteSiteCount <= 0 ||
                ChartSiteCount <= 0 ||
                JudgeStateSiteCount <= 0)
            {
                throw new InvalidDataException(
                    "CapacityScanner contains an invalid value");
            }
        }

        private static int Int(JsonElement root, string key)
        {
            if (!root.TryGetProperty(key, out JsonElement value) ||
                value.ValueKind != JsonValueKind.Number ||
                !value.TryGetInt32(out int result))
            {
                throw new InvalidDataException(
                    "CapacityScanner." + key + " must be an integer");
            }
            return result;
        }

        private static string[] Strings(
            JsonElement root,
            string key)
        {
            if (!root.TryGetProperty(key, out JsonElement value) ||
                value.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException(
                    "CapacityScanner." + key + " must be an array");
            }
            return value.EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();
        }
    }

    internal sealed class NativePatchTargetProfile
    {
        internal string Key { get; }
        internal string ManagedType { get; }
        internal string Assembly { get; }
        internal string Namespace { get; }
        internal string Class { get; }
        internal string Method { get; }
        internal string[] ParameterTypes { get; }
        internal string ReturnType { get; }
        internal int GenericArity { get; }
        internal string[] GenericArguments { get; }
        internal int ManagedMetadataToken { get; }
        internal long Rva { get; }
        internal long FallbackRva { get; }
        internal byte[] Prologue { get; }
        internal byte[] FallbackPrologue { get; }
        internal NativeCapability Capabilities { get; }

        internal NativePatchTargetProfile(
            string key,
            JsonElement root)
        {
            Key = key;
            ManagedType = String(root, "managedType");
            Assembly = String(root, "assembly");
            Namespace = OptionalString(root, "namespace") ?? string.Empty;
            Class = String(root, "class");
            Method = String(root, "method");
            ParameterTypes = Strings(root, "parameterTypes");
            ReturnType = String(root, "returnType");
            GenericArity = OptionalInt(root, "genericArity", 0);
            GenericArguments = OptionalStrings(
                root, "genericArguments");
            long managedMetadataToken = OptionalLong(
                root, "managedMetadataToken", 0);
            if (managedMetadataToken < 0 ||
                managedMetadataToken > int.MaxValue)
            {
                throw new InvalidDataException(
                    "PatchTargets.managedMetadataToken is out of range");
            }
            ManagedMetadataToken = (int)managedMetadataToken;
            Rva = OptionalLong(root, "rva", 0);
            FallbackRva = OptionalLong(
                root, "fallbackRva", 0);
            Prologue = ParsePrologue(
                OptionalString(root, "prologue"));
            FallbackPrologue = ParsePrologue(
                OptionalString(root, "fallbackPrologue"));
            Capabilities = NativeCapabilityParser.Parse(
                Strings(root, "capabilities"));
        }

        private static string String(
            JsonElement root,
            string key)
        {
            string value = OptionalString(root, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException(
                    $"PatchTargets.{key} must be a non-empty string");
            }
            return value;
        }

        private static string OptionalString(
            JsonElement root,
            string key)
        {
            return root.TryGetProperty(key, out JsonElement value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static int OptionalInt(
            JsonElement root,
            string key,
            int fallback)
        {
            return root.TryGetProperty(key, out JsonElement value) &&
                   value.ValueKind == JsonValueKind.Number &&
                   value.TryGetInt32(out int result)
                ? result
                : fallback;
        }

        private static long OptionalLong(
            JsonElement root,
            string key,
            long fallback)
        {
            if (!root.TryGetProperty(key, out JsonElement value))
                return fallback;
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt64(out long number))
                return number;
            if (value.ValueKind == JsonValueKind.String &&
                TryParseInteger(value.GetString(), out number))
                return number;
            return fallback;
        }

        private static string[] Strings(
            JsonElement root,
            string key)
        {
            string[] value = OptionalStrings(root, key);
            if (value is null)
            {
                throw new InvalidDataException(
                    $"PatchTargets.{key} must be an array");
            }
            return value;
        }

        private static string[] OptionalStrings(
            JsonElement root,
            string key)
        {
            if (!root.TryGetProperty(key, out JsonElement value))
                return Array.Empty<string>();
            if (value.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException(
                    $"PatchTargets.{key} must be an array");
            }
            return value.EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();
        }

        private static byte[] ParsePrologue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<byte>();
            return value.Split(
                    new[] { ' ', '-', '\t' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(token => byte.Parse(
                    token,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture))
                .ToArray();
        }

        private static bool TryParseInteger(
            string text,
            out long value)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                value = 0;
                return false;
            }
            if (text.StartsWith(
                    "0x",
                    StringComparison.OrdinalIgnoreCase))
            {
                return long.TryParse(
                    text.Substring(2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out value);
            }
            return long.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);
        }
    }

    internal static class NativeCapabilityParser
    {
        internal static NativeCapability Parse(
            IEnumerable<string> values)
        {
            NativeCapability result = NativeCapability.None;
            if (values is null) return result;
            foreach (string value in values)
            {
                if (Enum.TryParse(
                        value,
                        ignoreCase: true,
                        out NativeCapability capability))
                {
                    result |= capability;
                }
                else
                {
                    throw new InvalidDataException(
                        "unknown native capability '" + value + "'");
                }
            }
            return result;
        }
    }
}
