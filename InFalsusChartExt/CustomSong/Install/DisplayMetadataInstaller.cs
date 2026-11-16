using System;
using InFalsusChartExt.CustomSong.Discovery;
using Il2Cppifapp.Game.Data;

namespace InFalsusChartExt.CustomSong.Install
{
    internal static class DisplayMetadataInstaller
    {
        internal static void Apply(
            DynamicStringMapping mapping,
            CustomLibraryPlan plan)
        {
            foreach (CustomPackPlan pack in plan.Packs)
            {
                AppendText(
                    mapping.packIdTypeMapping,
                    new PackId { Value = pack.RuntimePackId },
                    pack.Config.Title,
                    "pack title");
                foreach (CustomSongPlan song in pack.Songs)
                {
                    SongId id = new SongId
                    {
                        Value = song.RuntimeSongId,
                    };
                    AppendText(
                        mapping.songIdTitleTypeMapping,
                        id,
                        song.Config.Title,
                        "song title");
                    AppendText(
                        mapping.songIdArtistTypeMapping,
                        id,
                        song.Config.Artist ?? string.Empty,
                        "song artist");
                    AppendText(
                        mapping.jacketIllustratorNameTypeMapping,
                        id,
                        song.Config.JacketDesigner ?? string.Empty,
                        "jacket designer");
                }
            }
        }

        private static void AppendText<T>(
            DynamicStringMapping.StringTypeMapping<T> mapping,
            T id,
            string text,
            string label)
        {
            if (mapping?.Ids is null ||
                mapping.IdValues is null)
            {
                throw new InvalidOperationException(
                    label + " mapping is unavailable");
            }

            var values =
                new DynamicStringMapping.TextMappingValues
                {
                    English = text,
                    Japanese = text,
                    Korean = text,
                    TraditionalChinese = text,
                    SimplifiedChinese = text,
                };

            int index = mapping.Ids.IndexOf(id);
            if (index >= 0)
            {
                if (index >= mapping.IdValues.Count)
                    throw new InvalidOperationException(
                        label + " mapping is inconsistent");
                mapping.IdValues[index] = values;
                if (mapping.IdStr is not null &&
                    index < mapping.IdStr.Count)
                {
                    mapping.IdStr[index] = text;
                }
            }
            else
            {
                mapping.Ids.Add(id);
                mapping.IdValues.Add(values);
                mapping.IdStr?.Add(text);
            }
            mapping._TZA();
        }
    }
}
