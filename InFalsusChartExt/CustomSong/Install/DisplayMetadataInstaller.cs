using System;
using InFalsusChartExt.CustomSong.Discovery;
#if MELONLOADER
using Il2Cppifapp.Game.Data;
#elif BEPINEX
using ifapp.Game.Data;
#endif

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
                    pack.Titles,
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
                        song.Titles,
                        "song title");
                    AppendText(
                        mapping.songIdArtistTypeMapping,
                        id,
                        song.Composers,
                        "song artist");
                    AppendText(
                        mapping.jacketIllustratorNameTypeMapping,
                        id,
                        song.Illustrators,
                        "jacket designer");
                }
            }
        }

        private static void AppendText<T>(
            DynamicStringMapping.StringTypeMapping<T> mapping,
            T id,
            string[] languages,
            string label)
        {
            if (mapping?.Ids is null ||
                mapping.IdValues is null)
            {
                throw new InvalidOperationException(
                    label + " mapping is unavailable");
            }

            string simplified =
                Language(languages, 0);
            string traditional =
                Language(languages, 1);
            string english =
                Language(languages, 2);
            string japanese =
                Language(languages, 3);
            string korean =
                Language(languages, 4);
            var values =
                new DynamicStringMapping.TextMappingValues
                {
                    English = english,
                    Japanese = japanese,
                    Korean = korean,
                    TraditionalChinese = traditional,
                    SimplifiedChinese = simplified,
                };

            int index = mapping.Ids.IndexOf(id);
            if (index >= 0)
            {
                if (index >= mapping.IdValues.Count)
                    throw new InvalidOperationException(
                        label + " mapping is inconsistent");
#if BEPINEX
                BepInExValueTypeListBridge.SetItem(
                    mapping.IdValues,
                    index,
                    values);
#else
                mapping.IdValues[index] = values;
#endif
                if (mapping.IdStr is not null &&
                    index < mapping.IdStr.Count)
                {
                    mapping.IdStr[index] = english;
                }
            }
            else
            {
                mapping.Ids.Add(id);
#if BEPINEX
                BepInExValueTypeListBridge.Add(
                    mapping.IdValues,
                    values);
#else
                mapping.IdValues.Add(values);
#endif
                mapping.IdStr?.Add(english);
            }
            mapping._TZA();
        }

        private static string Language(
            string[] values,
            int index)
        {
            if (values is not null &&
                index >= 0 &&
                index < values.Length &&
                !string.IsNullOrEmpty(values[index]))
                return values[index];
            if (values is not null)
            {
                for (int i = 0; i < values.Length; i++)
                    if (!string.IsNullOrEmpty(values[i]))
                        return values[i];
            }
            return string.Empty;
        }
    }
}
