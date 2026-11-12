using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Independent Bomb judgement. Bombs remain type1 records for count/total
    /// ownership, but the native render/judge lists exclude their ids. This
    /// runtime reads the same native contact-state arrays as `_pz`, finalizes
    /// the record, and enqueues the native `_PEA` tuple consumed by `_Tz`.
    /// </summary>
    internal static class BombJudgeRuntime
    {
        private const double NearWindowMs = 100.0;
        private const double InputWarmupMs = 1000.0;
        private const double ForgetAfterMs = 500.0;

        private sealed class BombState
        {
            internal BombDescriptor Bomb;
            internal bool InputInitialized;
            internal bool WasHeld;
            internal bool PressedInWindow;
            internal int InputIndex;
            internal bool Done;
        }

        private static readonly Dictionary<long, BombState> StateById =
            new Dictionary<long, BombState>();
        private static readonly List<BombState> Active =
            new List<BombState>();
        private static int _dueCursor;
        private static double _lastNow = double.NaN;
        private static IntPtr _presentationQueuePtr;
        private static IntPtr _presentationEnqueueMethod;
        private static IntPtr _presentationTupleBuffer;
        private static bool _queueErrorLogged;
        private static int _errors;

        internal static void OnChartCompiled(
            Il2Cppifapp.Game.LogicalNotePlayer player)
        {
            if (!BuildProfile.Supports(NativeCapability.BombJudge))
                return;
            ResetForChartSwitch();
            try
            {
                BombPlan plan = CurrentPlan();
                if (plan == null ||
                    plan.NativeMembers.Count == 0 ||
                    player == null ||
                    player.Pointer == IntPtr.Zero)
                    return;
                Il2Cpp_b._T compiled = player._ve;
                if (compiled == null ||
                    compiled.Pointer == IntPtr.Zero)
                    return;
                var sides = compiled._vC;
                if (sides == null ||
                    sides.Pointer == IntPtr.Zero)
                    return;
                for (int side = 0;
                     side < NativeLayoutCatalog.SideCount;
                     side++)
                {
                    if (!sides.ContainsKey((Il2Cpp_b._CA)side))
                        continue;
                    Il2Cpp_b._hA list = sides[(Il2Cpp_b._CA)side];
                    if (list == null || list.Pointer == IntPtr.Zero)
                        continue;
                    var notes = list._ye;
                    int count = list._ze;
                    if (notes == null || count <= 0)
                        continue;
                    IntPtr data =
                        notes.Pointer + InteropUtil.ArrayDataOffset;
                    for (int position = 0;
                         position < count;
                         position++)
                    {
                        long id = Marshal.ReadInt64(
                            data +
                            position *
                            NativeLayoutCatalog.NoteStride +
                            NativeLayoutCatalog.Note.Id);
                        if (!plan.TryGetByNativeId(
                                id, out BombDescriptor bomb))
                            continue;
                        bomb.NativeSide = side;
                        bomb.NativePosition = position;
                    }
                }
            }
            catch (Exception e)
            {
                LogError("slot binding", e);
            }
        }

        internal static unsafe void AfterNativeJudge(
            Il2Cpp_E._VD judge)
        {
            if (!BuildProfile.Supports(NativeCapability.BombJudge))
                return;
            try
            {
                BombPlan plan = CurrentPlan();
                if (plan == null ||
                    plan.NativeMembers.Count == 0 ||
                    judge == null ||
                    judge.Pointer == IntPtr.Zero)
                    return;
                double now = BitConverter.Int64BitsToDouble(
                    Marshal.ReadInt64(
                        judge.Pointer +
                        NativeLayoutCatalog.SkyJudge.ChartTime)) *
                    1000.0;
                if (!IsFinite(now))
                    return;
                if (!double.IsNaN(_lastNow) &&
                    now + 0.001 < _lastNow)
                    ResetProgress();
                _lastNow = now;

                Il2Cppifapp.Game.LogicalNotePlayer player =
                    Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null || player.Pointer == IntPtr.Zero)
                    return;
                Il2Cpp_b._T compiled = player._ve;
                if (compiled == null ||
                    compiled.Pointer == IntPtr.Zero)
                    return;
                var records = compiled._XC;
                if (records == null ||
                    records.Pointer == IntPtr.Zero)
                    return;
                Il2Cppifapp.Game.Scenes.Game.Track track =
                    Il2Cppifapp.Game.Scenes.Game.Track._a;
                if (track == null || track.Pointer == IntPtr.Zero)
                    return;
                if (!EnsurePresentationQueue(judge))
                    return;

                while (_dueCursor < plan.Due.Count &&
                       plan.Due[_dueCursor].StartMs -
                       InputWarmupMs <= now)
                {
                    BombDescriptor due = plan.Due[_dueCursor++];
                    if (!StateById.ContainsKey(due.NativeId))
                    {
                        var state = new BombState { Bomb = due };
                        StateById.Add(due.NativeId, state);
                        Active.Add(state);
                    }
                }

                for (int i = Active.Count - 1; i >= 0; i--)
                {
                    BombState state = Active[i];
                    BombDescriptor bomb = state.Bomb;
                    if (state.Done ||
                        now > bomb.StartMs + ForgetAfterMs)
                    {
                        RemoveStateAt(i);
                        continue;
                    }
                    if (!records.ContainsKey(bomb.NativeId) ||
                        !TryGetNativeNote(bomb, out IntPtr note))
                        continue;
                    int heldIndex = -1;
                    bool held = TryReadHeldState(
                        judge.Pointer, note, out heldIndex);
                    bool newPress = IsNewPress(
                        state.InputInitialized,
                        state.WasHeld,
                        held);
                    state.InputInitialized = true;
                    state.WasHeld = held;
                    if (!state.PressedInWindow &&
                        newPress &&
                        now >= bomb.StartMs - NearWindowMs &&
                        now <= bomb.StartMs)
                    {
                        state.PressedInWindow = true;
                        state.InputIndex = heldIndex;
                    }

                    bool press = state.PressedInWindow;
                    if (!press && now < bomb.StartMs)
                        continue;
                    FinalizeBomb(
                        track,
                        bomb,
                        state,
                        note,
                        records[bomb.NativeId],
                        press);
                    if (state.Done)
                        RemoveStateAt(i);
                }
            }
            catch (Exception e)
            {
                LogError("judge tick", e);
            }
        }

        internal static void ResetForChartSwitch()
        {
            ResetProgress();
            _presentationQueuePtr = IntPtr.Zero;
            _presentationEnqueueMethod = IntPtr.Zero;
            if (_presentationTupleBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_presentationTupleBuffer);
                _presentationTupleBuffer = IntPtr.Zero;
            }
            _queueErrorLogged = false;
        }

        private static void ResetProgress()
        {
            StateById.Clear();
            Active.Clear();
            _dueCursor = 0;
            _lastNow = double.NaN;
        }

        private static unsafe void FinalizeBomb(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            BombDescriptor bomb,
            BombState state,
            IntPtr note,
            Il2CppSystem.Memory<Il2Cpp_K._cH> memory,
            bool pressed)
        {
            if (memory._object == null ||
                memory._object.Pointer == IntPtr.Zero)
                return;
            int count = memory._length;
            if (count != 1)
            {
                LogError(
                    "record shape",
                    new InvalidOperationException(
                        $"bomb id={bomb.NativeId} has {count} records"));
                state.Done = true;
                return;
            }
            IntPtr arrayObject = memory._object.Pointer;
            int sliceIndex = memory._index;
            int arrayLength = Marshal.ReadInt32(
                arrayObject +
                NativeLayoutCatalog.ArrayLengthOffset);
            if (sliceIndex < 0 ||
                sliceIndex + count > arrayLength)
                return;

            byte* record = InteropUtil.RecordPointer(
                arrayObject, sliceIndex, 0);
            if (record[InteropUtil.FinalOffset] != 0)
            {
                state.Done = true;
                return;
            }
            byte grade = pressed
                ? ChartExtState.JudgeMiss
                : ChartExtState.JudgeShiny;
            long globalIndex = *(long*)record;
            int recordStart = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.RecordStart);
            int recordEnd = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.RecordEnd);
            int recordCount = recordEnd - recordStart + 1;
            if (recordStart < 0 || recordCount <= 0)
                return;
            byte oldJudgment = record[InteropUtil.JudgmentOffset];
            byte oldFinal = record[InteropUtil.FinalOffset];
            record[InteropUtil.JudgmentOffset] = grade;
            record[InteropUtil.FinalOffset] = 1;

            float presentationProgress =
                (recordStart + recordEnd) * 0.5f / 5f;
            Vector3 position =
                track._dBA(presentationProgress);
            if (!EnqueuePresentation(
                    bomb.NativeId,
                    recordStart,
                    recordCount,
                    globalIndex,
                    position))
            {
                record[InteropUtil.JudgmentOffset] = oldJudgment;
                record[InteropUtil.FinalOffset] = oldFinal;
                return;
            }
            if (pressed && state.InputIndex >= 0)
            {
                Il2Cpp_b._fA noteValue =
                    *(Il2Cpp_b._fA*)note;
                track._HBA(
                    ref noteValue,
                    state.InputIndex);
            }
            state.Done = true;
        }

        private static bool TryGetNativeNote(
            BombDescriptor bomb,
            out IntPtr note)
        {
            note = IntPtr.Zero;
            IntPtr basePtr =
                NativeNoteSlotTable.SlotBase(bomb.NativeSide);
            if (basePtr == IntPtr.Zero)
                return false;
            note = basePtr +
                bomb.NativePosition *
                NativeLayoutCatalog.NoteStride;
            return Marshal.ReadInt64(
                note + NativeLayoutCatalog.Note.Id) ==
                bomb.NativeId;
        }

        /// <summary>
        /// Window A: only a new press in [t-100,t] counts. Input is sampled
        /// from one second before the window, so a key held before entry into
        /// the window is not mistaken for a new press.
        /// </summary>
        private static bool TryReadHeldState(
            IntPtr judge,
            IntPtr note,
            out int inputIndex)
        {
            inputIndex = -1;
            if (judge == IntPtr.Zero || note == IntPtr.Zero)
                return false;
            IntPtr states = Marshal.ReadIntPtr(
                judge + NativeLayoutCatalog.SkyJudge.InputStates);
            if (states == IntPtr.Zero)
                return false;
            int stateCount = Marshal.ReadInt32(
                states + NativeLayoutCatalog.ArrayLengthOffset);
            int start = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.RecordStart);
            int end = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.RecordEnd);
            if (start < 0 || end < start || end >= stateCount)
                return false;
            IntPtr data =
                states + NativeLayoutCatalog.ArrayDataOffset;
            for (int i = start; i <= end; i++)
            {
                IntPtr state = data +
                    i * NativeLayoutCatalog.SkyJudge.InputStateStride;
                if (Marshal.ReadByte(
                        state +
                        NativeLayoutCatalog.SkyJudge.InputHeld) != 0)
                {
                    inputIndex = i;
                    return true;
                }
            }
            return false;
        }

        internal static bool IsNewPress(
            bool initialized,
            bool wasHeld,
            bool held)
            => initialized && held && !wasHeld;

        private static unsafe bool EnqueuePresentation(
            long noteId,
            int recordStart,
            int recordCount,
            long recordIndex,
            Vector3 position)
        {
            if (_presentationEnqueueMethod == IntPtr.Zero ||
                _presentationTupleBuffer == IntPtr.Zero)
                return false;
            WritePresentationTuple(
                _presentationTupleBuffer,
                noteId,
                recordStart,
                recordCount,
                recordIndex,
                position);
            IntPtr* arguments = stackalloc IntPtr[1];
            arguments[0] = _presentationTupleBuffer;
            IntPtr exception = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(
                _presentationEnqueueMethod,
                _presentationQueuePtr,
                (void**)arguments,
                ref exception);
            if (exception != IntPtr.Zero)
            {
                LogError(
                    "presentation",
                    new InvalidOperationException(
                        "native _PEA.Enqueue raised an exception"));
                return false;
            }
            return true;
        }

        internal static void WritePresentationTuple(
            IntPtr buffer,
            long noteId,
            int recordStart,
            int recordCount,
            long recordIndex,
            Vector3 position)
        {
            if (buffer == IntPtr.Zero)
                return;
            Marshal.WriteInt64(
                buffer + NativeLayoutCatalog.Presentation.NoteId,
                noteId);
            Marshal.WriteInt32(
                buffer + NativeLayoutCatalog.Presentation.RecordStart,
                recordStart);
            Marshal.WriteInt32(
                buffer + NativeLayoutCatalog.Presentation.RecordCount,
                recordCount);
            Marshal.WriteInt64(
                buffer + NativeLayoutCatalog.Presentation.RecordIndex,
                recordIndex);
            Marshal.WriteInt32(
                buffer + NativeLayoutCatalog.Presentation.PositionX,
                BitConverter.SingleToInt32Bits(position.x));
            Marshal.WriteInt32(
                buffer + NativeLayoutCatalog.Presentation.PositionY,
                BitConverter.SingleToInt32Bits(position.y));
            Marshal.WriteInt32(
                buffer + NativeLayoutCatalog.Presentation.PositionZ,
                BitConverter.SingleToInt32Bits(position.z));
        }

        private static bool EnsurePresentationQueue(
            Il2Cpp_E._VD judge)
        {
            IntPtr queuePtr = Marshal.ReadIntPtr(
                judge.Pointer +
                NativeLayoutCatalog.SkyJudge.PendingPresentation);
            if (queuePtr == IntPtr.Zero)
                return false;
            if (_presentationEnqueueMethod != IntPtr.Zero &&
                _presentationQueuePtr == queuePtr)
                return true;
            _presentationQueuePtr = queuePtr;
            _presentationEnqueueMethod =
                FindPresentationEnqueue(queuePtr);
            if (_presentationEnqueueMethod == IntPtr.Zero)
            {
                if (!_queueErrorLogged)
                {
                    _queueErrorLogged = true;
                    ChartExtMod.Log.Error(
                        "[ChartExt] bomb judge: " +
                        "_PEA.Enqueue signature was not found");
                }
                return false;
            }
            if (_presentationTupleBuffer == IntPtr.Zero)
                _presentationTupleBuffer =
                    Marshal.AllocHGlobal(
                        NativeLayoutCatalog.Presentation.Stride);
            return _presentationTupleBuffer != IntPtr.Zero;
        }

        private static IntPtr FindPresentationEnqueue(
            IntPtr queuePtr)
        {
            IntPtr klass =
                IL2CPP.il2cpp_object_get_class(queuePtr);
            if (klass == IntPtr.Zero)
                return IntPtr.Zero;
            IL2CPP.il2cpp_runtime_class_init(klass);
            IntPtr iterator = IntPtr.Zero;
            while (true)
            {
                IntPtr method = IL2CPP.il2cpp_class_get_methods(
                    klass, ref iterator);
                if (method == IntPtr.Zero)
                    break;
                IntPtr namePtr =
                    IL2CPP.il2cpp_method_get_name(method);
                if (namePtr == IntPtr.Zero)
                    continue;
                string name =
                    Marshal.PtrToStringAnsi(namePtr);
                if (!string.Equals(
                        name,
                        NativeLayoutCatalog.Presentation
                            .EnqueueMethodName,
                        StringComparison.Ordinal))
                    continue;
                if (IL2CPP.il2cpp_method_get_param_count(
                        method) != 1)
                    continue;
                IntPtr paramType =
                    IL2CPP.il2cpp_method_get_param(method, 0);
                IntPtr paramClass = paramType == IntPtr.Zero
                    ? IntPtr.Zero
                    : IL2CPP.il2cpp_class_from_type(paramType);
                if (paramClass == IntPtr.Zero)
                    continue;
                uint alignment = 0;
                int valueSize = (int)IL2CPP.il2cpp_class_value_size(
                    paramClass, ref alignment);
                if (valueSize ==
                    NativeLayoutCatalog.Presentation
                        .EnqueueParameterSize)
                    return method;
            }
            return IntPtr.Zero;
        }

        private static void RemoveStateAt(int index)
        {
            BombState state = Active[index];
            Active.RemoveAt(index);
            StateById.Remove(state.Bomb.NativeId);
        }

        private static BombPlan CurrentPlan()
        {
            if (!ChartRuntimeContext.IsActive)
                return null;
            return ChartRuntimeContext.Current?.Bombs;
        }

        private static void LogError(
            string stage,
            Exception error)
        {
            if (_errors++ < 10)
                ChartExtMod.Log.Error(
                    $"[ChartExt] bomb judge {stage} failed: " +
                    error);
        }

        private static bool IsFinite(double value)
            => !double.IsNaN(value) &&
               !double.IsInfinity(value);
    }
}
