using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cppifapp.Game;
using Il2Cppifapp.Game.Data;

namespace InFalsusChartExt.CustomSong.Install
{
    internal sealed class SongDataInstallSnapshot
    {
        private readonly Il2CppReferenceArray<SongInfo> _songs;
        private readonly Il2CppReferenceArray<PackInfo> _packs;
        private readonly Il2CppReferenceArray<SongJacketEntry> _selector;
        private readonly Il2CppReferenceArray<SongDataChartJacketEntry>
            _charts;
        private readonly int _packTitleCount;
        private readonly int _songTitleCount;
        private readonly int _songArtistCount;
        private readonly int _jacketDesignerCount;

        internal SongDataInstallSnapshot(
            SongData songData,
            PackData packData,
            DynamicStringMapping strings)
        {
            _songs = songData.allSongInfo;
            _packs = packData.PackInfo;
            _selector = songData.songIdJacketMaterials;
            _charts = songData.chartIdJacketMaterials;
            _packTitleCount = strings.packIdTypeMapping.Ids.Count;
            _songTitleCount =
                strings.songIdTitleTypeMapping.Ids.Count;
            _songArtistCount =
                strings.songIdArtistTypeMapping.Ids.Count;
            _jacketDesignerCount =
                strings.jacketIllustratorNameTypeMapping.Ids.Count;
        }

        internal void Restore(
            SongData songData,
            PackData packData,
            DynamicStringMapping strings)
        {
            songData.allSongInfo = _songs;
            songData.songIdJacketMaterials = _selector;
            songData.chartIdJacketMaterials = _charts;
            packData.PackInfo = _packs;
            Truncate(
                strings.packIdTypeMapping,
                _packTitleCount);
            Truncate(
                strings.songIdTitleTypeMapping,
                _songTitleCount);
            Truncate(
                strings.songIdArtistTypeMapping,
                _songArtistCount);
            Truncate(
                strings.jacketIllustratorNameTypeMapping,
                _jacketDesignerCount);
            SongDataNativeCache.Reset(songData);
            strings.OnAfterDeserialize();
            songData.OnAfterDeserialize();
            SongDataNativeCache.Reset(songData);
            songData._yOA();
            packData.OnAfterDeserialize();
        }

        private static void Truncate<T>(
            DynamicStringMapping.StringTypeMapping<T> mapping,
            int count)
        {
            Truncate(mapping.Ids, count);
            Truncate(mapping.IdValues, count);
            if (mapping.IdStr is not null)
                Truncate(mapping.IdStr, count);
            mapping._TZA();
        }

        private static void Truncate<T>(
            Il2CppSystem.Collections.Generic.List<T> list,
            int count)
        {
            if (list is null) return;
            while (list.Count > count)
                list.RemoveAt(list.Count - 1);
        }
    }
}
