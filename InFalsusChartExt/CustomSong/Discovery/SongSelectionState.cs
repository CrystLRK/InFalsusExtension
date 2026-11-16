#if MELONLOADER
using Il2Cppifapp.Game.Data;
#elif BEPINEX
using ifapp.Game.Data;
#endif

namespace InFalsusChartExt.CustomSong.Discovery
{
    internal static class SongSelectionState
    {
        internal static ushort SongId { get; private set; }
        internal static int DifficultySlot { get; private set; }
        internal static bool HasSelection { get; private set; }

        internal static void Set(
            ushort songId,
            ChartDifficultyFlag difficulty)
        {
            SongId = songId;
            DifficultySlot = SlotOf(difficulty);
            HasSelection = true;
        }

        internal static void Reset()
        {
            SongId = 0;
            DifficultySlot = 0;
            HasSelection = false;
        }

        internal static int SlotOf(
            ChartDifficultyFlag difficulty) =>
            difficulty switch
            {
                ChartDifficultyFlag.Minimal => 0,
                ChartDifficultyFlag.Evolved => 1,
                ChartDifficultyFlag.Ultimate => 2,
                ChartDifficultyFlag.Forbidden => 3,
                _ => 0,
            };
    }
}
