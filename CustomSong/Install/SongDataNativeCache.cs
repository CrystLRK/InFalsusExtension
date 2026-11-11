using System;
using Il2CppInterop.Runtime;
using Il2Cppifapp.Game.Data;

namespace InFalsusChartExt.CustomSong.Install
{
    internal static class SongDataNativeCache
    {
        internal static void Reset(SongData data)
        {
            IntPtr instance = data.Pointer;
            if (instance == IntPtr.Zero)
                throw new InvalidOperationException(
                    "SongData has no native object");
            ResetField(
                instance,
                NativeLayoutCatalog.CustomSong.SongIdJacketCache);
            ResetField(
                instance,
                NativeLayoutCatalog.CustomSong.ChartIdJacketCache);
        }

        internal static void Refresh(SongData data)
        {
            Reset(data);
            data._yOA();
        }

        private static void ResetField(
            IntPtr instance,
            int offset)
        {
            IntPtr field = IntPtr.Add(instance, offset);
            IL2CPP.il2cpp_gc_wbarrier_set_field(
                instance, field, IntPtr.Zero);
        }
    }
}
