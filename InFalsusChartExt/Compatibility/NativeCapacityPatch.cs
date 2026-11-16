// Code organization: native capacity patch. Runtime behavior is unchanged.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MelonLoader;

namespace InFalsusChartExt
{
    /// <summary>
    /// Raises the game's fixed native collection capacities before the gameplay
    /// chart containers are constructed.
    ///
    /// The sites are found from the IL2CPP entry points of the constructors that
    /// allocate them. No build-specific absolute RVA is stored here.
    /// </summary>
    internal static class NumericLimitPatch
    {
        internal const int DefaultSideNoteCapacity = 65536;
        internal const int DefaultNoteIndexTableCapacity = 65536;
        internal const int DefaultAuxTableCapacity = 131072;
        internal const int DefaultRecordCapacity = 524288;
        internal const int DefaultJudgeStateCapacity = 65536;

        private static readonly int OriginalSideNoteCapacity =
            NativeLayoutCatalog.Capacity.OriginalSideNote;
        private static readonly int OriginalAuxTableCapacity =
            NativeLayoutCatalog.Capacity.OriginalAuxTable;
        private static readonly int OriginalRecordCapacity =
            NativeLayoutCatalog.Capacity.OriginalRecord;
        private static readonly int OriginalJudgeStateCapacity =
            NativeLayoutCatalog.Capacity.OriginalJudgeState;
        private const int MinimumCapacity = 1;
        private const int MaximumCapacity = 1_000_000;
        private const uint PageExecuteReadWrite = 0x40;

        internal static int SideNoteCapacity { get; private set; } =
            DefaultSideNoteCapacity;
        internal static int NoteIndexTableCapacity { get; private set; } =
            DefaultNoteIndexTableCapacity;
        internal static int AuxTableCapacity { get; private set; } =
            DefaultAuxTableCapacity;
        internal static int RecordCapacity { get; private set; } =
            DefaultRecordCapacity;
        internal static int JudgeStateCapacity { get; private set; } =
            DefaultJudgeStateCapacity;

        private static bool _attempted;
        private static int _warnCount;
        private static NativeCapacityScannerProfile Scanner =>
            NativeAbiProfileStore.Selected?.CapacityScanner
            ?? throw new InvalidOperationException(
                "capacity scanner profile is unavailable");

        private readonly struct ImmediateSite
        {
            internal ImmediateSite(string name, int offset, int oldValue, int newValue)
            {
                Name = name;
                Offset = offset;
                OldValue = oldValue;
                NewValue = newValue;
            }

            internal string Name { get; }
            internal int Offset { get; }
            internal int OldValue { get; }
            internal int NewValue { get; }
        }

        private readonly struct PatchTarget
        {
            internal PatchTarget(
                IntPtr method,
                ImmediateSite site)
            {
                Method = method;
                Site = site;
            }

            internal IntPtr Method { get; }
            internal ImmediateSite Site { get; }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(
            IntPtr address,
            UIntPtr size,
            uint newProtection,
            out uint oldProtection);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushInstructionCache(
            IntPtr process,
            IntPtr address,
            UIntPtr size);

        /// <summary>Run once from OnInitializeMelon, before chart data allocation.</summary>
        internal static void Apply()
        {
            if (_attempted) return;
            _attempted = true;
            try
            {
                if (!BuildProfile.Current.SupportsCapacityPatch)
                {
                    Warn("native build is not approved for capacity patching; skipped");
                    return;
                }
                LoadConfiguration();
                ValidateConfiguration();

                IntPtr sideConstructor =
                    NativePatchTargetResolver.ResolveNative(
                        "capacitySideNoteConstructor");
                IntPtr chartConstructor =
                    NativePatchTargetResolver.ResolveNative(
                        "capacityChartConstructor");
                IntPtr judgeConstructor =
                    NativePatchTargetResolver.ResolveNative(
                        "capacityJudgeConstructor");
                if (sideConstructor == IntPtr.Zero ||
                    chartConstructor == IntPtr.Zero ||
                    judgeConstructor == IntPtr.Zero)
                {
                    Abort(
                        "native chart constructors were not resolved; " +
                        "capacity patch skipped");
                    return;
                }

                var targets = new List<PatchTarget>(10);
                List<int> sideOffsets =
                    FindSideImmediates(sideConstructor);
                if (sideOffsets.Count !=
                    Scanner.SideNoteSiteCount)
                {
                    Abort(
                        $"_hA side sites={sideOffsets.Count}, " +
                        $"expected {Scanner.SideNoteSiteCount}; " +
                        "all capacity writes skipped");
                    return;
                }
                int sideOffset = sideOffsets[0];
                targets.Add(new PatchTarget(
                    sideConstructor,
                    new ImmediateSite(
                        "_hA._ye(per-side notes)",
                        sideOffset,
                        OriginalSideNoteCapacity,
                        SideNoteCapacity)));

                var chartSites = new List<ImmediateSite>(7);
                if (!TryFindChartSites(chartConstructor, chartSites) ||
                    chartSites.Count != Scanner.ChartSiteCount)
                {
                    Abort(
                        $"_T chart sites={chartSites.Count}, " +
                        $"expected {Scanner.ChartSiteCount}; " +
                        "all capacity writes skipped");
                    return;
                }
                for (int i = 0; i < chartSites.Count; i++)
                    targets.Add(new PatchTarget(
                        chartConstructor, chartSites[i]));

                var judgeOffsets = FindJudgeStateSites(
                    judgeConstructor);
                if (judgeOffsets.Count !=
                    Scanner.JudgeStateSiteCount)
                {
                    Abort(
                        $"_VD judge-state sites={judgeOffsets.Count}, " +
                        $"expected {Scanner.JudgeStateSiteCount}; " +
                        "all capacity writes skipped");
                    return;
                }
                for (int i = 0; i < judgeOffsets.Count; i++)
                    targets.Add(new PatchTarget(
                        judgeConstructor,
                        new ImmediateSite(
                            $"_VD.judgeState[{i}]",
                            judgeOffsets[i],
                            OriginalJudgeStateCapacity,
                            JudgeStateCapacity)));

                for (int i = 0; i < targets.Count; i++)
                {
                    if (ValidateTarget(targets[i])) continue;
                    Abort(
                        "capacity signature validation failed; " +
                        "no sites were written");
                    return;
                }

                int written = 0;
                try
                {
                    for (; written < targets.Count; written++)
                    {
                        if (WriteImmediate(targets[written]))
                            continue;
                        throw new InvalidOperationException(
                            targets[written].Site.Name +
                            ": write failed");
                    }
                    for (int i = 0; i < targets.Count; i++)
                    {
                        if (ReadImmediate(targets[i]) ==
                            targets[i].Site.NewValue)
                            continue;
                        throw new InvalidOperationException(
                            targets[i].Site.Name +
                            ": verification failed");
                    }
                }
                catch (Exception exception)
                {
                    Rollback(targets, written);
                    Abort(
                        "capacity transaction failed and was rolled " +
                        "back: " + exception.Message);
                    return;
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    ImmediateSite site = targets[i].Site;
                    Log(
                        $"{site.Name}: {site.OldValue} -> " +
                        $"{site.NewValue} @0x" +
                        $"{ReadImmediateAddress(targets[i]).ToInt64():X}");
                }
                Log($"capacity patch done: {targets.Count} verified sites" +
                    (_warnCount > 0 ? $" ({_warnCount} warnings)" : string.Empty));
            }
            catch (Exception e)
            {
                Warn("fatal: " + e);
            }
        }

        private static void LoadConfiguration()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory(
                "InFalsusChartExt.Capacity",
                "In Falsus Chart Ext - Native Capacity");
            SideNoteCapacity = category.CreateEntry(
                "SideNoteCapacity",
                DefaultSideNoteCapacity,
                "Per-side note capacity",
                "Applied before chart construction. Restart required.").Value;
            NoteIndexTableCapacity = category.CreateEntry(
                "NoteIndexTableCapacity",
                DefaultNoteIndexTableCapacity,
                "Note index table capacity",
                "Applied before chart construction. Restart required.").Value;
            AuxTableCapacity = category.CreateEntry(
                "AuxTableCapacity",
                DefaultAuxTableCapacity,
                "Auxiliary note table capacity",
                "Applied before chart construction. Restart required.").Value;
            RecordCapacity = category.CreateEntry(
                "RecordCapacity",
                DefaultRecordCapacity,
                "Compiled record capacity",
                "Applied before chart construction. Restart required.").Value;
            JudgeStateCapacity = category.CreateEntry(
                "JudgeStateCapacity",
                DefaultJudgeStateCapacity,
                "Per-chart judge state capacity",
                "Applied before chart construction. Restart required.").Value;
        }

        private static void ValidateConfiguration()
        {
            ValidateCapacity(nameof(SideNoteCapacity), SideNoteCapacity);
            ValidateCapacity(nameof(NoteIndexTableCapacity), NoteIndexTableCapacity);
            ValidateCapacity(nameof(AuxTableCapacity), AuxTableCapacity);
            ValidateCapacity(nameof(RecordCapacity), RecordCapacity);
            ValidateCapacity(nameof(JudgeStateCapacity), JudgeStateCapacity);
        }

        private static void ValidateCapacity(string name, int value)
        {
            if (value < MinimumCapacity || value > MaximumCapacity)
                throw new ArgumentOutOfRangeException(
                    name,
                    $"{name}={value} must be between {MinimumCapacity} and {MaximumCapacity}");
        }

        private static List<int> FindSideImmediates(
            IntPtr method)
        {
            var offsets = new List<int>();
            for (int offset = 0;
                 offset <= Scanner.MaxScanBytes - 6;
                 offset++)
            {
                if (Marshal.ReadByte(method, offset) !=
                    Scanner.MovEdxOpcode)
                    continue;
                int value = Marshal.ReadInt32(method, offset + 1);
                if (value != OriginalSideNoteCapacity)
                    continue;
                if (Marshal.ReadByte(method, offset + 5) !=
                    Scanner.CallOpcode)
                    continue;
                offsets.Add(offset);
            }
            return offsets;
        }

        private static bool TryFindChartSites(IntPtr method, List<ImmediateSite> sites)
        {
            var candidates = new List<(int Offset, int Value)>();
            for (int offset = 0;
                 offset <= Scanner.MaxScanBytes - 5;
                 offset++)
            {
                if (Marshal.ReadByte(method, offset) !=
                    Scanner.MovEdxOpcode)
                    continue;
                int value = Marshal.ReadInt32(method, offset + 1);
                if (value == OriginalSideNoteCapacity ||
                    value == OriginalAuxTableCapacity ||
                    value == OriginalRecordCapacity)
                    candidates.Add((offset, value));
            }

            int[] expected =
            {
                OriginalSideNoteCapacity,
                OriginalSideNoteCapacity,
                OriginalAuxTableCapacity,
                OriginalAuxTableCapacity,
                OriginalAuxTableCapacity,
                OriginalRecordCapacity,
                OriginalRecordCapacity,
            };
            string[] names = Scanner.ChartSiteNames;
            int[] replacements =
            {
                NoteIndexTableCapacity,
                NoteIndexTableCapacity,
                AuxTableCapacity,
                AuxTableCapacity,
                AuxTableCapacity,
                RecordCapacity,
                RecordCapacity,
            };

            for (int start = 0; start + expected.Length <= candidates.Count; start++)
            {
                bool matches = true;
                int previous = candidates[start].Offset;
                for (int i = 0; i < expected.Length; i++)
                {
                    var candidate = candidates[start + i];
                    if (candidate.Value != expected[i] ||
                        candidate.Offset - previous >
                        Scanner.MaxPatternGap)
                    {
                        matches = false;
                        break;
                    }
                    previous = candidate.Offset;
                }
                if (!matches) continue;

                for (int i = 0; i < expected.Length; i++)
                {
                    sites.Add(new ImmediateSite(
                        names[i],
                        candidates[start + i].Offset,
                        expected[i],
                        replacements[i]));
                }
                return true;
            }
            return false;
        }

        private static List<int> FindJudgeStateSites(
            IntPtr method)
        {
            var offsets = new List<int>(2);
            for (int offset = 0;
                 offset <= Scanner.MaxScanBytes - 6;
                 offset++)
            {
                if (Marshal.ReadByte(method, offset) !=
                    Scanner.MovEdxOpcode)
                    continue;
                if (Marshal.ReadInt32(method, offset + 1) !=
                    OriginalJudgeStateCapacity)
                    continue;
                offsets.Add(offset);
            }
            return offsets;
        }

        private static bool ValidateTarget(PatchTarget target)
        {
            try
            {
                IntPtr opcode = IntPtr.Add(
                    target.Method, target.Site.Offset);
                IntPtr immediate = IntPtr.Add(opcode, 1);
                if (Marshal.ReadByte(opcode) !=
                    Scanner.MovEdxOpcode ||
                    Marshal.ReadInt32(immediate) !=
                    target.Site.OldValue)
                {
                    Warn(
                        $"{target.Site.Name}: signature changed " +
                        "before transaction");
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Warn(
                    $"{target.Site.Name}: validation failed: " +
                    e.Message);
                return false;
            }
        }

        private static bool WriteImmediate(PatchTarget target)
        {
            try
            {
                IntPtr immediate = ReadImmediateAddress(target);
                if (!VirtualProtect(
                        immediate,
                        new UIntPtr(4),
                        PageExecuteReadWrite,
                        out uint oldProtect))
                {
                    Warn(
                        $"{target.Site.Name}: VirtualProtect failed " +
                        $"err={Marshal.GetLastWin32Error()}");
                    return false;
                }
                Marshal.WriteInt32(
                    immediate, target.Site.NewValue);
                bool flushed = FlushInstructionCache(
                    GetCurrentProcess(), immediate, new UIntPtr(4));
                bool restored = VirtualProtect(
                    immediate, new UIntPtr(4), oldProtect, out _);
                if (!flushed || !restored)
                {
                    try
                    {
                        if (VirtualProtect(
                                immediate,
                                new UIntPtr(4),
                                PageExecuteReadWrite,
                                out uint retryProtection))
                        {
                            Marshal.WriteInt32(
                                immediate,
                                target.Site.OldValue);
                            FlushInstructionCache(
                                GetCurrentProcess(),
                                immediate,
                                new UIntPtr(4));
                            VirtualProtect(
                                immediate,
                                new UIntPtr(4),
                                retryProtection,
                                out _);
                        }
                    }
                    catch
                    {
                    }
                    Warn(
                        $"{target.Site.Name}: flush={flushed}, " +
                        $"protectionRestore={restored}");
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Warn($"{target.Site.Name}: {e.Message}");
                return false;
            }
        }

        private static IntPtr ReadImmediateAddress(
            PatchTarget target)
            => IntPtr.Add(
                target.Method, target.Site.Offset + 1);

        private static int ReadImmediate(PatchTarget target)
            => Marshal.ReadInt32(
                ReadImmediateAddress(target));

        private static void Rollback(
            List<PatchTarget> targets,
            int written)
        {
            for (int i = Math.Min(written, targets.Count) - 1;
                 i >= 0;
                 i--)
            {
                PatchTarget target = targets[i];
                try
                {
                    IntPtr immediate =
                        ReadImmediateAddress(target);
                    if (!VirtualProtect(
                            immediate,
                            new UIntPtr(4),
                            PageExecuteReadWrite,
                            out uint oldProtect))
                        continue;
                    Marshal.WriteInt32(
                        immediate, target.Site.OldValue);
                    FlushInstructionCache(
                        GetCurrentProcess(),
                        immediate,
                        new UIntPtr(4));
                    if (!VirtualProtect(
                        immediate,
                        new UIntPtr(4),
                        oldProtect,
                        out _))
                    {
                        Warn(
                            $"{target.Site.Name}: rollback " +
                            "protection restore failed");
                    }
                }
                catch { }
            }
        }

        private static void Log(string message)
        {
            try { ChartExtMod.Log.Msg("[Capacity] " + message); } catch { }
        }

        private static void Warn(string message)
        {
            _warnCount++;
            try { ChartExtMod.Log.Warning("[Capacity] " + message); } catch { }
        }

        private static void Abort(string message)
        {
            Warn(message);
            BuildProfile.DisableCapabilities(
                NativeCapability.CapacityPatch,
                message);
        }
    }
}
