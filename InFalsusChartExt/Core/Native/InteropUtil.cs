// Code organization: native interop primitives. Runtime behavior is unchanged.
using System;
using Il2CppInterop.Runtime;

namespace InFalsusChartExt
{
    /// <summary>Raw access to IL2CPP objects that the generated wrappers cannot express.</summary>
    internal static class InteropUtil
    {
        internal static readonly int ArrayDataOffset =
            NativeLayoutCatalog.ArrayDataOffset;

        // Record layout taken from the game code itself (Track/_VD paths index the
        // 40-byte records as `record + 0x20` for the judgement value and `record + 0x21`
        // for the "processed" flag). Assembly/name lookup can resolve a different
        // same-named metadata class, so validation uses the generated value-type
        // wrapper and removes the boxed-object header from metadata field offsets.
        internal static readonly int RecordStride =
            NativeLayoutCatalog.Record.Stride;
        internal static readonly int JudgmentByte =
            NativeLayoutCatalog.Record.Judgment;
        internal static readonly int FinalByte =
            NativeLayoutCatalog.Record.Final;

        private static IntPtr _cHClass;
        private static int _cHElemSize;
        private static int _judgmentOffset = -1;
        private static int _finalOffset = -1;
        private static int _relTimeOffset = -1;
        private static int _absTimeOffset = -1;
        private static int _recordTypeOffset = -1;
        private static int _recordStateOffset = -1;

        internal static bool ResolveRecordLayout()
        {
            if (_cHClass != IntPtr.Zero) return true;
            IntPtr klass = Il2CppClassPointerStore<Il2Cpp_K._cH>.NativeClassPtr;
            if (klass == IntPtr.Zero) return false;
            IL2CPP.il2cpp_runtime_class_init(klass);
            uint alignment = 0;
            int size = (int)IL2CPP.il2cpp_class_value_size(klass, ref alignment);
            if (size != RecordStride) return false;
            _cHElemSize = size;
            _judgmentOffset =
                FieldOffset(klass, "_wdb") - NativeLayoutCatalog.BoxedValueHeader;
            _finalOffset =
                FieldOffset(klass, "_Wdb") - NativeLayoutCatalog.BoxedValueHeader;
            _relTimeOffset =
                FieldOffset(klass, "_udb") - NativeLayoutCatalog.BoxedValueHeader;
            _absTimeOffset =
                FieldOffset(klass, "_Udb") - NativeLayoutCatalog.BoxedValueHeader;
            _recordTypeOffset =
                FieldOffset(klass, "_vdb") - NativeLayoutCatalog.BoxedValueHeader;
            _recordStateOffset =
                FieldOffset(klass, "_Vdb") - NativeLayoutCatalog.BoxedValueHeader;
            if (_judgmentOffset != JudgmentByte ||
                _finalOffset != FinalByte ||
                _relTimeOffset != NativeLayoutCatalog.Record.RelativeTime ||
                _absTimeOffset < 0 ||
                _recordTypeOffset < 0 ||
                _recordStateOffset < 0)
                return false;
            _cHClass = klass;
            BuildProfile.MarkCoreAbiValidated();
            return true;
        }

        internal static int JudgmentOffset => _judgmentOffset;
        internal static int FinalOffset => _finalOffset;
        internal static int AbsoluteTimeOffset => _absTimeOffset;
        internal static int RecordTypeOffset => _recordTypeOffset;
        internal static int RecordStateOffset => _recordStateOffset;

        internal static int FieldOffset(IntPtr klass, string name)
        {
            IntPtr field = IL2CPP.GetIl2CppField(klass, name);
            if (field == IntPtr.Zero) return -1;
            return (int)IL2CPP.il2cpp_field_get_offset(field);
        }

        /// <summary>Pointer to a record inside the IL2CPP array a Memory&lt;&gt; view points at.</summary>
        internal static unsafe byte* RecordPointer(IntPtr arrayObject, int index, int elementIndex)
        {
            return (byte*)arrayObject + ArrayDataOffset + (long)(index + elementIndex) * RecordStride;
        }

    }
}
