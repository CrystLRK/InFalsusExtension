using System;
using System.Collections.Generic;
using System.IO;
using InFalsusChartExt.CustomSong.Discovery;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace InFalsusChartExt.CustomSong.Chart
{
    internal sealed class CustomChartPayload
    {
        internal CustomChartPlan Plan { get; }
        internal byte[] Bytes { get; }
        internal Il2CppStructArray<byte> PinnedBytes { get; }

        internal CustomChartPayload(
            CustomChartPlan plan,
            byte[] bytes,
            Il2CppStructArray<byte> pinnedBytes)
        {
            Plan = plan;
            Bytes = bytes;
            PinnedBytes = pinnedBytes;
        }
    }

    internal sealed class CustomChartPayloadSource : IDisposable
    {
        private const ulong SamXorKey = 0xA5C39E7D4B2816F0UL;
        private readonly Dictionary<string, CustomChartPayload> _payloads =
            new Dictionary<string, CustomChartPayload>(
                StringComparer.OrdinalIgnoreCase);

        internal CustomChartPayload GetOrLoad(CustomChartPlan plan)
        {
            if (plan is null)
                throw new ArgumentNullException(nameof(plan));
            if (_payloads.TryGetValue(
                    plan.LogicalChartKey,
                    out CustomChartPayload existing))
            {
                if (ReferenceEquals(existing.Plan, plan))
                    return existing;
                _payloads.Remove(plan.LogicalChartKey);
            }

            byte[] bytes = File.ReadAllBytes(plan.SourcePath);
            if (string.Equals(
                    Path.GetExtension(plan.SourcePath),
                    ".sam",
                    StringComparison.OrdinalIgnoreCase))
            {
                DecodeSam(bytes);
            }

            TrimToStructuralLength(ref bytes);
            if (!Icp1Codec.TryDecode(
                    bytes,
                    plan.ParserKey,
                    out Icp1Codec.Chart chart) ||
                !IsPlausible(chart))
            {
                throw new InvalidDataException(
                    $"chart '{plan.LogicalChartKey}' does not decode " +
                    $"with parser key '{plan.ParserKey}'");
            }

            var pinned = new Il2CppStructArray<byte>(bytes);
            var payload = new CustomChartPayload(plan, bytes, pinned);
            _payloads[plan.LogicalChartKey] = payload;
            return payload;
        }

        internal void Clear()
        {
            _payloads.Clear();
        }

        public void Dispose()
        {
            Clear();
        }

        private static void DecodeSam(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= (byte)(
                    SamXorKey >> (8 * (i & 7)));
            }
        }

        private static void TrimToStructuralLength(ref byte[] bytes)
        {
            if (bytes.Length < 28 ||
                bytes[0] != (byte)'I' ||
                bytes[1] != (byte)'C' ||
                bytes[2] != (byte)'P' ||
                bytes[3] != (byte)'1')
            {
                throw new InvalidDataException(
                    "chart payload is not ICP1");
            }

            uint noteCount = ReadUInt32(bytes, 12);
            uint eventCount = ReadUInt32(bytes, 16);
            long length = 28L +
                          80L * noteCount +
                          32L * eventCount;
            if (length < 28 ||
                length > bytes.Length ||
                length > int.MaxValue)
            {
                throw new InvalidDataException(
                    $"invalid ICP1 structural length {length}");
            }
            if (length != bytes.Length)
                Array.Resize(ref bytes, (int)length);
        }

        private static bool IsPlausible(Icp1Codec.Chart chart)
        {
            if (chart is null) return false;
            for (int i = 0; i < chart.Notes.Count; i++)
            {
                Icp1Codec.Note note = chart.Notes[i];
                if (note is null ||
                    note.Type < 1 ||
                    note.Type > 5 ||
                    note.Side > 4)
                {
                    return false;
                }
            }
            for (int i = 0; i < chart.Events.Count; i++)
            {
                Icp1Codec.Event item = chart.Events[i];
                if (item is null || item.Type > 5)
                    return false;
            }
            return true;
        }

        private static uint ReadUInt32(byte[] bytes, int offset) =>
            (uint)(bytes[offset] |
                   (bytes[offset + 1] << 8) |
                   (bytes[offset + 2] << 16) |
                   (bytes[offset + 3] << 24));
    }
}
