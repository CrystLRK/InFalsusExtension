using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace InFalsusChartExt
{
    /// <summary>
    /// 组内 Hold/SkyArea 的 record 密度以该组 BPM 为准（L4）。
    /// 运行期重建整块 record 池：组内长音符按 |组 BPM| 重算条数，
    /// 其余音符原样保留；不改 BPM 事件、不改 `_pb`、不改视觉。
    /// </summary>
    internal static class GroupRecordDensityRuntime
    {
        private const int RecordStride = 0x28;
        private const int RecId = 0x00;
        private const int RecTime = 0x08;
        private const int RecAbs = 0x10;
        private const int RecType = 0x18;
        private const int RecState = 0x1C;
        private const int RecJudge = 0x20;
        private const int RecFinal = 0x21;
        private const int CompiledBaseBpm = 0x28;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SetItemFn(
            IntPtr dictionary, long key, IntPtr memory, IntPtr methodInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct Memory16
        {
            public IntPtr Array;
            public int Start;
            public int Length;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Memory16 GetItemFn(
            IntPtr dictionary, long key, IntPtr methodInfo);

        private static IntPtr _dictClass;
        private static IntPtr _setItem;
        private static IntPtr _getItem;
        private static SetItemFn _setItemFn;
        private static GetItemFn _getItemFn;
        private static int _appliedGeneration = -1;
        private static int _errors;

        internal static void Apply(Il2Cpp_b._T chart)
        {
            if (!BuildProfile.Supports(NativeCapability.TimingGroup))
                return;
            if (_appliedGeneration == ChartExtState.CompileGeneration)
                return;
            _appliedGeneration = ChartExtState.CompileGeneration;
            try
            {
                if (chart == null || chart.Pointer == IntPtr.Zero)
                    return;
                IntPtr dictionary = Marshal.ReadIntPtr(
                    chart.Pointer + 0x38);
                if (dictionary == IntPtr.Zero || !ResolveSetter(dictionary))
                    return;
                float baseBpm = Marshal.PtrToStructure<float>(
                    chart.Pointer + CompiledBaseBpm);
                var targets = CollectTargets(chart);
                int seen = _lastSeen;
                int grouped = _lastGrouped;
                if (targets.Count == 0)
                {
                    LogOnce(
                        $"[ChartExt] group record density: base={baseBpm:F2} " +
                        $"seen={seen} grouped={grouped} rebuilt=0 (no pool)");
                    return;
                }
                int rebuilt = RebuildPool(
                    chart, dictionary,
                    baseBpm > 0f ? baseBpm : 120f, targets);
                LogOnce(
                    "[ChartExt] group record density: " +
                    $"base={baseBpm:F2} seen={seen} grouped={grouped} " +
                    $"rebuilt={rebuilt}");
            }
            catch (Exception exception)
            {
                LogError(exception);
            }
        }

        private static int _lastSeen;
        private static int _lastGrouped;
        private static bool _logged;

        private static void LogOnce(string message)
        {
            if (_logged)
                return;
            _logged = true;
            ChartExtMod.Log.Msg(message);
        }

        private sealed class Target
        {
            public int StartMs;
            public int EndMs;
            public int Group;
            public int Type;
        }

        /// <summary>收集所有“组内长音符”，同时统计 seen/grouped。</summary>
        private static Dictionary<long, Target> CollectTargets(
            Il2Cpp_b._T chart)
        {
            var targets = new Dictionary<long, Target>();
            _lastSeen = 0;
            _lastGrouped = 0;
            var sides = chart._vC;
            if (sides == null)
                return targets;
            for (int side = 0; side < NativeLayoutCatalog.SideCount; side++)
            {
                if (!sides.ContainsKey((Il2Cpp_b._CA)side))
                    continue;
                Il2Cpp_b._hA list = sides[(Il2Cpp_b._CA)side];
                if (list == null || list.Pointer == IntPtr.Zero)
                    continue;
                Il2CppStructArray<Il2Cpp_b._fA> notes = list._ye;
                int count = list._ze;
                if (notes == null || count <= 0)
                    continue;
                for (int i = 0; i < count && i < notes.Length; i++)
                {
                    Il2Cpp_b._fA note = notes[i];
                    if ((int)note._be != 2 && (int)note._be != 5)
                        continue;
                    _lastSeen++;
                    int group = ChartExtState.TimingGroupOf(note._ZD);
                    if (group <= 0)
                        continue;
                    _lastGrouped++;
                    targets[note._ZD] = new Target
                    {
                        StartMs = note._Be,
                        EndMs = note._ce,
                        Group = group,
                        Type = (int)note._be,
                    };
                }
            }
            return targets;
        }

        private static unsafe int RebuildPool(
            Il2Cpp_b._T chart,
            IntPtr dictionary,
            double baseBpm,
            Dictionary<long, Target> targets)
        {
            var keys = chart._XC.Keys;
            if (keys == null)
                return 0;
            var entries = new List<(long Key, Memory16 Mem)>();
            int total = 0;
            foreach (long key in keys)
            {
                Memory16 mem = _getItemFn(dictionary, key, _getItem);
                if (mem.Array == IntPtr.Zero || mem.Length <= 0)
                    continue;
                entries.Add((key, mem));
                total += mem.Length;
            }
            if (entries.Count == 0)
                return 0;
            entries.Sort((a, b) => a.Mem.Start.CompareTo(b.Mem.Start));

            int dd = Marshal.ReadInt32(chart.Pointer + 0xD8);
            if (dd > 0 && Math.Abs(total - dd) > 4)
            {
                ChartExtMod.Log.Warning(
                    "[ChartExt] group record density: slice sum " +
                    $"{total} != Dd {dd}; skipped");
                return 0;
            }

            int planned = 0;
            foreach (var entry in entries)
            {
                int count = entry.Mem.Length;
                if (targets.TryGetValue(entry.Key, out Target target))
                {
                    int density = CountDensityRecords(target, baseBpm);
                    if (density > 0) count = density;
                }
                planned += count;
            }
            if (planned <= 0)
                return 0;

            var rebuilt = new Il2CppStructArray<Il2Cpp_K._cH>(planned);
            IntPtr dstBase =
                rebuilt.Pointer + NativeLayoutCatalog.ArrayDataOffset;
            int write = 0;
            int rebuiltCount = 0;
            var newStarts = new List<(long Key, int Start, int Length)>(
                entries.Count);
            foreach (var entry in entries)
            {
                if (targets.TryGetValue(entry.Key, out Target target))
                {
                    int added = WriteDensityRecords(
                        dstBase, write, entry.Mem, target, baseBpm);
                    if (added > 0)
                    {
                        newStarts.Add((entry.Key, write, added));
                        write += added;
                        rebuiltCount++;
                        continue;
                    }
                }
                IntPtr src = new IntPtr(
                    entry.Mem.Array.ToInt64() +
                    NativeLayoutCatalog.ArrayDataOffset +
                    (long)entry.Mem.Start * RecordStride);
                for (int r = 0; r < entry.Mem.Length; r++)
                {
                    IntPtr s = new IntPtr(
                        src.ToInt64() + (long)r * RecordStride);
                    IntPtr d = new IntPtr(
                        dstBase.ToInt64() +
                        (long)(write + r) * RecordStride);
                    Buffer.MemoryCopy(
                        (void*)s, (void*)d,
                        RecordStride, RecordStride);
                    Marshal.WriteInt64(d + RecId, write + r);
                }
                newStarts.Add((entry.Key, write, entry.Mem.Length));
                write += entry.Mem.Length;
            }

            if (rebuiltCount == 0)
                return 0;

            IntPtr memory = Marshal.AllocHGlobal(16);
            try
            {
                for (int i = 0; i < newStarts.Count; i++)
                {
                    var item = newStarts[i];
                    Marshal.WriteIntPtr(memory, rebuilt.Pointer);
                    Marshal.WriteInt32(
                        new IntPtr(memory.ToInt64() + 8), item.Start);
                    Marshal.WriteInt32(
                        new IntPtr(memory.ToInt64() + 12), item.Length);
                    _setItemFn(dictionary, item.Key, memory, _setItem);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            Marshal.WriteIntPtr(chart.Pointer + 0xB0, rebuilt.Pointer);
            Marshal.WriteInt32(chart.Pointer + 0xB8, 0);
            Marshal.WriteInt32(chart.Pointer + 0xBC, write);
            Marshal.WriteIntPtr(chart.Pointer + 0xC0, rebuilt.Pointer);
            // `_dd` is the live Memory view over `_Cd` (set from `_Cd` by
            // `_T._XA`). It must mirror the rebuilt array as well; otherwise
            // later consumers keep reading the old record array, whose
            // final/judgment bytes are never updated.
            Marshal.WriteIntPtr(chart.Pointer + 0xC8, rebuilt.Pointer);
            Marshal.WriteInt32(chart.Pointer + 0xD0, 0);
            Marshal.WriteInt32(chart.Pointer + 0xD4, write);
            Marshal.WriteInt32(chart.Pointer + 0xD8, write);
            return rebuiltCount;
        }

        private static int WriteDensityRecords(
            IntPtr dstBase,
            int writeIndex,
            Memory16 source,
            Target target,
            double baseBpm)
        {
            double bpm = Math.Abs(
                TimingGroupRuntime.GroupBpmAt(target.Group, target.StartMs));
            if (!(bpm > 0.0))
                bpm = baseBpm;
            if (!(bpm > 0.0) ||
                double.IsNaN(bpm) ||
                double.IsInfinity(bpm))
                return 0;
            double effective = bpm >= 249.99 ? bpm * 0.5 : bpm;
            double interval = 60000.0 / effective * 0.5;
            if (!(interval > 0.0) || double.IsInfinity(interval))
                return 0;

            double duration = target.EndMs - target.StartMs;
            // 记录从 start+interval 开始，末尾按 >=2ms 规则补一条 end。
            var times = new List<double>();
            double acc = 0.0;
            while (duration >= acc + interval)
            {
                acc += interval;
                times.Add(target.StartMs + acc);
            }
            if (duration - acc >= 2.0)
                times.Add(target.EndMs);
            if (times.Count == 0)
                times.Add(target.EndMs);

            // A hold with a single record must keep that record at its
            // authored end: moving it to 0 makes the whole hold end at the
            // judgement line and disappear.
            bool hasHead = target.Type == 2 && times.Count > 1;
            int headType = 1;
            bool sourceHeadAtZero = false;
            IntPtr sourceHead = IntPtr.Zero;
            if (hasHead && source.Length > 0)
            {
                sourceHead = new IntPtr(
                    source.Array.ToInt64() +
                    NativeLayoutCatalog.ArrayDataOffset +
                    (long)source.Start * RecordStride);
                int sourceType = Marshal.ReadInt32(
                    sourceHead + RecType);
                double sourceRelative =
                    BitConverter.Int64BitsToDouble(
                        Marshal.ReadInt64(
                            sourceHead + RecTime));
                if (sourceType == 1 || sourceType == 3)
                    headType = sourceType;
                sourceHeadAtZero =
                    Math.Abs(sourceRelative) <= 1e-6;
                times[0] = target.StartMs;
            }

            if (times.Count == source.Length &&
                (!hasHead || sourceHeadAtZero))
                return 0;

            for (int i = 0; i < times.Count; i++)
            {
                IntPtr item = new IntPtr(
                    dstBase.ToInt64() +
                    (long)(writeIndex + i) * RecordStride);
                Marshal.WriteInt64(item + RecId, writeIndex + i);
                Marshal.WriteInt64(item + RecTime,
                    BitConverter.DoubleToInt64Bits(
                        times[i] - target.StartMs));
                Marshal.WriteInt64(item + RecAbs,
                    BitConverter.DoubleToInt64Bits(times[i]));
                Marshal.WriteInt32(
                    item + RecType,
                    hasHead && i == 0 ? headType : 3);
                Marshal.WriteInt32(item + RecState, 0);
                Marshal.WriteByte(item + RecJudge, 0);
                Marshal.WriteByte(item + RecFinal, 0);
            }
            return times.Count;
        }

        private static int CountDensityRecords(
            Target target, double baseBpm)
        {
            double bpm = Math.Abs(
                TimingGroupRuntime.GroupBpmAt(
                    target.Group, target.StartMs));
            if (!(bpm > 0.0))
                bpm = baseBpm;
            if (!(bpm > 0.0) ||
                double.IsNaN(bpm) ||
                double.IsInfinity(bpm))
                return 0;
            double effective = bpm >= 249.99 ? bpm * 0.5 : bpm;
            double interval = 60000.0 / effective * 0.5;
            if (!(interval > 0.0) || double.IsInfinity(interval))
                return 0;

            double duration = target.EndMs - target.StartMs;
            int count = 0;
            double acc = 0.0;
            while (duration >= acc + interval)
            {
                acc += interval;
                count++;
            }
            if (duration - acc >= 2.0)
                count++;
            return count > 0 ? count : 1;
        }

        private static bool ResolveSetter(IntPtr dictionary)
        {
            if (_setItemFn != null && _getItemFn != null)
                return true;
            IntPtr klass = IL2CPP.il2cpp_object_get_class(dictionary);
            if (klass == IntPtr.Zero)
                return false;
            _dictClass = klass;
            IntPtr iterator = IntPtr.Zero;
            while (true)
            {
                IntPtr method =
                    IL2CPP.il2cpp_class_get_methods(klass, ref iterator);
                if (method == IntPtr.Zero)
                    break;
                IntPtr namePointer =
                    IL2CPP.il2cpp_method_get_name(method);
                if (namePointer == IntPtr.Zero)
                    continue;
                string name = Marshal.PtrToStringAnsi(namePointer);
                int paramCount = (int)
                    IL2CPP.il2cpp_method_get_param_count(method);
                IntPtr entry = Marshal.ReadIntPtr(method);
                if (entry == IntPtr.Zero)
                    continue;
                if (name == "set_Item" && paramCount == 2 &&
                    _setItemFn == null)
                {
                    _setItem = method;
                    _setItemFn = Marshal
                        .GetDelegateForFunctionPointer<SetItemFn>(entry);
                }
                else if (name == "get_Item" && paramCount == 1 &&
                    _getItemFn == null)
                {
                    _getItem = method;
                    _getItemFn = Marshal
                        .GetDelegateForFunctionPointer<GetItemFn>(entry);
                }
                if (_setItemFn != null && _getItemFn != null)
                {
                    return true;
                }
            }
            ChartExtMod.Log.Warning(
                "[ChartExt] group record density: get/set not found");
            return false;
        }

        private static void LogError(Exception exception)
        {
            if (_errors++ >= 5)
                return;
            try
            {
                ChartExtMod.Log.Error(
                    "[ChartExt] group record density failed: " + exception);
            }
            catch
            {
            }
        }
    }
}
