using System;
using System.Collections.Generic;
using System.Linq;
using InFalsusChartExt.CustomSong.Assets;
using InFalsusChartExt.CustomSong.Discovery;
using InFalsusChartExt.CustomSong.Lifecycle;
using InFalsusChartExt.CustomSong.Scores;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cppifapp.Game;
using Il2Cppifapp.Game.Data;
using Il2Cppifapp.Game.Scenes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InFalsusChartExt.CustomSong.Install
{
    internal static class SongLibraryInstaller
    {
        private static bool _installed;
        private static bool _installing;
        private static bool _attemptedThisPhase;
        private static float _nextRetryTime;
        private static int _failures;
        [ThreadStatic]
        private static bool _refreshingMetadata;

        internal static void Tick()
        {
            if (_installed || _installing) return;
            float now = Time.realtimeSinceStartup;
            if (_attemptedThisPhase && now < _nextRetryTime)
                return;
            _attemptedThisPhase = true;
            _nextRetryTime = now + Math.Min(30f, 2f + _failures * 2f);

            try
            {
                CustomLibraryPlan plan =
                    CustomSongRuntime.ScanIfNeeded();
                if (plan is null)
                {
                    ChartExtMod.Log.Warning(
                        "[CustomSong] CustomCharts scan was unavailable.");
                    return;
                }
                if (plan.Packs.Count == 0)
                {
                    _installed = true;
                    ChartExtMod.Log.Msg(
                        "[CustomSong] installed empty custom library.");
                    return;
                }

                DataAccess access = FindLoadedDataAccess();
                if (access?.SongData is null ||
                    access.PackData is null ||
                    access.DynamicStringMapping is null)
                {
                    return;
                }

                _installing = true;
                try
                {
                    Install(access, plan);
                }
                finally
                {
                    _installing = false;
                }
                _installed = true;
                _failures = 0;
            }
            catch (Exception exception)
            {
                _failures++;
                ChartExtMod.Log.Error(
                    "[CustomSong] library install failed; retrying: " +
                    exception);
            }
        }

        internal static void ReapplyDisplayMetadata(
            DynamicStringMapping mapping)
        {
            if (_refreshingMetadata ||
                !CustomSongRuntime.IsInstalled ||
                mapping is null)
                return;
            _refreshingMetadata = true;
            try
            {
                DisplayMetadataInstaller.Apply(
                    mapping,
                    CustomSongRuntime.Installed.Plan);
                mapping.OnAfterDeserialize();
            }
            catch (Exception exception)
            {
                PackAssetInstaller.Dispose();
                ChartExtMod.Log.Error(
                    "[CustomSong] display metadata refresh failed: " +
                    exception);
            }
            finally
            {
                _refreshingMetadata = false;
            }
        }

        internal static void ApplyPackAssets(
            PackSelectSceneAssets assets)
        {
            if (!CustomSongRuntime.IsInstalled || assets is null)
                return;
            try
            {
                PackAssetInstaller.Apply(
                    assets,
                    CustomSongRuntime.Installed.Plan);
            }
            catch (Exception exception)
            {
                ChartExtMod.Log.Error(
                    "[CustomSong] pack visual install failed: " +
                    exception);
            }
        }

        private static void Install(
            DataAccess access,
            CustomLibraryPlan plan)
        {
            if (!BuildProfile.Supports(
                    NativeCapability.CustomSong))
            {
                throw new InvalidOperationException(
                    "CustomSong capability is disabled; refusing a " +
                    "partial library install.");
            }
            if (!JacketAddressablesNativeHook.Install())
            {
                throw new InvalidOperationException(
                    "Jacket Addressables native hook is unavailable; " +
                    "refusing a partial library install.");
            }

            SongData songData = access.SongData;
            PackData packData = access.PackData;
            DynamicStringMapping strings =
                access.DynamicStringMapping;

            Il2CppReferenceArray<SongInfo> oldSongs =
                songData.allSongInfo ??
                throw new InvalidOperationException(
                    "SongData.allSongInfo is unavailable.");
            Il2CppReferenceArray<PackInfo> oldPacks =
                packData.PackInfo ??
                throw new InvalidOperationException(
                    "PackData.PackInfo is unavailable.");
            if (oldSongs.Length > ushort.MaxValue ||
                oldPacks.Length > ushort.MaxValue)
            {
                throw new InvalidOperationException(
                    "stock library exceeds ushort id range");
            }

            SongInfo templateSong = FindTemplateSong(
                songData, oldSongs);
            ushort templateSongId = templateSong.Id.Value;
            var stockBaseNames = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var stockPackSlugs = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < oldPacks.Length; i++)
            {
                PackInfo item = oldPacks[i];
                if (item is not null &&
                    !string.IsNullOrWhiteSpace(item.Slug))
                {
                    stockPackSlugs.Add(item.Slug);
                }
            }
            for (int i = 0; i < oldSongs.Length; i++)
            {
                SongInfo item = oldSongs[i];
                if (item is not null &&
                    !string.IsNullOrWhiteSpace(item.BaseName))
                {
                    stockBaseNames.Add(item.BaseName);
                }
            }
            int songCursor = oldSongs.Length;
            int packCursor = oldPacks.Length;
            var installedSongs = new List<SongInfo>();
            var installedPacks = new List<PackInfo>();
            var assignments = new List<(
                CustomPackPlan Pack,
                CustomSongPlan Song,
                SongInfo SongInfo,
                PackInfo PackInfo)>();

            foreach (CustomPackPlan pack in plan.Packs)
            {
                if (!stockPackSlugs.Add(pack.Config.Id))
                {
                    throw new InvalidOperationException(
                        $"custom pack id '{pack.Config.Id}' " +
                        "conflicts with an installed pack");
                }
                if (packCursor > ushort.MaxValue)
                    throw new InvalidOperationException(
                        "custom library exceeds PackId range");
                pack.RuntimePackId = (ushort)packCursor++;

                var songIds =
                    new Il2CppStructArray<SongId>(pack.Songs.Count);
                for (int songIndex = 0;
                     songIndex < pack.Songs.Count;
                     songIndex++)
                {
                    if (songCursor > ushort.MaxValue)
                        throw new InvalidOperationException(
                            "custom library exceeds SongId range");
                    CustomSongPlan song = pack.Songs[songIndex];
                    if (!stockBaseNames.Add(song.BaseName))
                    {
                        throw new InvalidOperationException(
                            $"custom base name '{song.BaseName}' " +
                            "conflicts with an installed song");
                    }
                    song.RuntimeSongId = (ushort)songCursor++;
                    song.TemplateSongId = templateSongId;
                    songIds[songIndex] = new SongId
                    {
                        Value = song.RuntimeSongId,
                    };
                    SongInfo songInfo = BuildSong(
                        templateSong, song);
                    installedSongs.Add(songInfo);
                    assignments.Add((
                        pack,
                        song,
                        songInfo,
                        default(PackInfo)));
                }

                PackInfo packInfo = BuildPack(pack, songIds);
                installedPacks.Add(packInfo);
            }

            var nextSongs = new Il2CppReferenceArray<SongInfo>(
                oldSongs.Length + installedSongs.Count);
            for (int i = 0; i < oldSongs.Length; i++)
                nextSongs[i] = oldSongs[i];
            for (int i = 0; i < installedSongs.Count; i++)
            {
                SongInfo value = installedSongs[i];
                nextSongs[value.Id.Value] = value;
            }

            var nextPacks = new Il2CppReferenceArray<PackInfo>(
                oldPacks.Length + installedPacks.Count);
            for (int i = 0; i < oldPacks.Length; i++)
                nextPacks[i] = oldPacks[i];
            for (int i = 0; i < installedPacks.Count; i++)
                nextPacks[oldPacks.Length + i] = installedPacks[i];

            if (songData._Ffb is not null)
            {
                if (Resources.FindObjectsOfTypeAll(
                        Il2CppType.Of<
                            SongSelectScene>())
                    .Length != 0)
                {
                    throw new InvalidOperationException(
                        "SongSelectScene already cached the stock library; " +
                        "refusing a late resize.");
                }
                songData._Ffb = null;
                songData._Efb = null;
            }

            var snapshot = new SongDataInstallSnapshot(
                songData,
                packData,
                strings);
            try
            {
                songData.allSongInfo = nextSongs;
                AppendJackets(
                    songData,
                    templateSong,
                    installedSongs,
                    plan);
                packData.PackInfo = nextPacks;
                DisplayMetadataInstaller.Apply(strings, plan);

                SongDataNativeCache.Reset(songData);
                strings.OnAfterDeserialize();
                songData.OnAfterDeserialize();
                SongDataNativeCache.Reset(songData);
                songData._yOA();
                packData.OnAfterDeserialize();

                AssignChartTemplates(installedSongs, plan);
                JacketMaterialService.RegisterLibrary(
                    songData, plan);
                CustomSongRuntime.MarkInstalled(plan);
            }
            catch
            {
                snapshot.Restore(songData, packData, strings);
                throw;
            }

            ChartExtMod.Log.Msg(
                $"[CustomSong] installed packs={plan.Packs.Count}, " +
                $"songs={installedSongs.Count}, " +
                $"SongData={oldSongs.Length}->{nextSongs.Length}, " +
                $"PackData={oldPacks.Length}->{nextPacks.Length}");
            try
            {
                CustomScoreRuntime.Initialize(plan, installedSongs);
            }
            catch (Exception exception)
            {
                ChartExtMod.Log.Error(
                    "[CustomScore] initialization failed: " +
                    exception);
            }
        }

        private static SongInfo FindTemplateSong(
            SongData data,
            Il2CppReferenceArray<SongInfo> songs)
        {
            for (int i = 0; i < songs.Length; i++)
            {
                SongInfo song = songs[i];
                if (song is null ||
                    song.ChartInfos is null ||
                    song.ChartInfos.Length < 4)
                {
                    continue;
                }
                for (int slot = 0; slot < song.ChartInfos.Length; slot++)
                {
                    SongChartInfo chart = song.ChartInfos[slot];
                    if (chart is not null &&
                        chart.Available &&
                        !string.IsNullOrWhiteSpace(chart.Id) &&
                        HasSongJacket(data, song.Id) &&
                        HasChartJacket(data, chart.Id))
                    {
                        return song;
                    }
                }
            }
            throw new InvalidOperationException(
                "no stock song can serve as a structural template");
        }

        private static bool HasSongJacket(
            SongData data,
            SongId id)
        {
            var entries = data.songIdJacketMaterials;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].SongId.Value == id.Value)
                    return true;
            return false;
        }

        private static bool HasChartJacket(
            SongData data,
            string chartId)
        {
            var entries = data.chartIdJacketMaterials;
            for (int i = 0; i < entries.Length; i++)
                if (string.Equals(
                        entries[i].ChartId,
                        chartId,
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static SongInfo BuildSong(
            SongInfo template,
            CustomSongPlan song)
        {
            var result = new SongInfo
            {
                Id = new SongId { Value = song.RuntimeSongId },
                BaseName = song.BaseName,
                CharacterIdentifier = template.CharacterIdentifier,
                PreviewStartSeconds = song.PreviewStart,
                PreviewEndSeconds = song.PreviewEnd,
                LocalizationToTitleReadingOverride =
                    template.LocalizationToTitleReadingOverride,
                ArtistReadingOverride = template.ArtistReadingOverride,
                GameplayBackground = template.GameplayBackground,
                RewardStyle = template.RewardStyle,
                Copyright = template.Copyright,
            };

            var charts =
                new Il2CppReferenceArray<SongChartInfo>(4);
            for (int slot = 0; slot < charts.Length; slot++)
            {
                charts[slot] = new SongChartInfo
                {
                    Id = string.Empty,
                    Available = false,
                    Difficulty = DifficultyForSlot(slot),
                    DisplayChartDesigner = string.Empty,
                    DisplayJacketDesigner = string.Empty,
                    Rating = 0,
                    LevelSectionIndicator = "?",
                };
            }

            foreach (CustomChartPlan chartPlan in song.Charts)
            {
                int slot = SlotForDifficulty(chartPlan.Difficulty);
                SongChartInfo templateChart =
                    FindTemplateChart(template, slot);
                chartPlan.TemplateParserKey =
                    templateChart.Id + ".spc";
                chartPlan.TemplateChartId = templateChart.Id;
                chartPlan.Song = song;
                charts[slot] = new SongChartInfo
                {
                    Id = chartPlan.LogicalChartKey,
                    Available = true,
                    Difficulty = chartPlan.Difficulty,
                    DisplayChartDesigner =
                        song.Config.ChartDesigner ??
                        templateChart.DisplayChartDesigner ??
                        string.Empty,
                    DisplayJacketDesigner =
                        song.Config.JacketDesigner ??
                        templateChart.DisplayJacketDesigner ??
                        string.Empty,
                    Rating = chartPlan.Rating,
                    LevelSectionIndicator =
                        chartPlan.LevelSectionIndicator,
                };
            }
            song.TemplateAudioName = template.BaseName;
            result.ChartInfos = charts;
            return result;
        }

        private static SongChartInfo FindTemplateChart(
            SongInfo template,
            int slot)
        {
            if (slot < template.ChartInfos.Length)
            {
                SongChartInfo exact = template.ChartInfos[slot];
                if (exact is not null &&
                    !string.IsNullOrWhiteSpace(exact.Id))
                {
                    return exact;
                }
            }
            for (int i = 0; i < template.ChartInfos.Length; i++)
            {
                SongChartInfo chart = template.ChartInfos[i];
                if (chart is not null &&
                    !string.IsNullOrWhiteSpace(chart.Id))
                {
                    return chart;
                }
            }
            throw new InvalidOperationException(
                "template song has no named chart");
        }

        private static PackInfo BuildPack(
            CustomPackPlan pack,
            Il2CppStructArray<SongId> songIds)
        {
            return new PackInfo
            {
                Id = new PackId { Value = pack.RuntimePackId },
                Slug = pack.Config.Id,
                SongIds = songIds,
                ExternalURL = string.Empty,
            };
        }

        private static void AppendJackets(
            SongData songData,
            SongInfo templateSong,
            IReadOnlyList<SongInfo> songs,
            CustomLibraryPlan plan)
        {
            SongJacketEntry selectorTemplate =
                FindSongJacket(songData, templateSong.Id);
            var selector =
                songData.songIdJacketMaterials;
            var charts =
                songData.chartIdJacketMaterials;
            var parserByLogicalKey =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
            foreach (CustomPackPlan pack in plan.Packs)
            {
                foreach (CustomSongPlan song in pack.Songs)
                {
                    foreach (CustomChartPlan chart in song.Charts)
                    {
                        parserByLogicalKey[chart.LogicalChartKey] =
                            chart.ParserKey;
                    }
                }
            }

            foreach (SongInfo song in songs)
            {
                selector = Append(selector, new SongJacketEntry
                {
                    SongId = song.Id,
                    JacketLargeMaterial =
                        selectorTemplate.JacketLargeMaterial,
                    JacketSmallMaterial =
                        selectorTemplate.JacketSmallMaterial,
                });
                foreach (SongChartInfo chart in song.ChartInfos)
                {
                    if (!chart.Available) continue;
                    SongDataChartJacketEntry template =
                        FindChartJacket(
                            charts,
                            FindTemplateSongChartId(
                                templateSong,
                                SlotForDifficulty(
                                    chart.Difficulty)));
                    var chartIds = new List<string>
                    {
                        chart.Id,
                    };
                    if (parserByLogicalKey.TryGetValue(
                            chart.Id,
                            out string parserKey))
                    {
                        string parserStem =
                            System.IO.Path.GetFileNameWithoutExtension(
                                parserKey);
                        if (!chartIds.Contains(
                                parserStem,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            chartIds.Add(parserStem);
                        }
                    }
                    foreach (string chartId in chartIds)
                    {
                    charts = Append(charts, new SongDataChartJacketEntry
                    {
                        ChartId = chartId,
                        JacketLargeMaterial =
                            template.JacketLargeMaterial,
                        JacketSmallMaterial =
                            template.JacketSmallMaterial,
                    });
                    }
                }
            }
            songData.songIdJacketMaterials = selector;
            songData.chartIdJacketMaterials = charts;
        }

        private static string FindTemplateSongChartId(
            SongInfo song,
            int slot)
        {
            if (slot < song.ChartInfos.Length)
            {
                SongChartInfo chart = song.ChartInfos[slot];
                if (chart is not null &&
                    !string.IsNullOrWhiteSpace(chart.Id))
                {
                    return chart.Id;
                }
            }
            return FindTemplateChart(song, slot).Id;
        }

        private static SongJacketEntry FindSongJacket(
            SongData data,
            SongId id)
        {
            var entries = data.songIdJacketMaterials;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].SongId.Value == id.Value)
                    return entries[i];
            throw new InvalidOperationException(
                "template song jacket was not found");
        }

        private static SongDataChartJacketEntry FindChartJacket(
            Il2CppReferenceArray<SongDataChartJacketEntry> entries,
            string chartId)
        {
            for (int i = 0; i < entries.Length; i++)
                if (string.Equals(
                        entries[i].ChartId,
                        chartId,
                        StringComparison.OrdinalIgnoreCase))
                    return entries[i];
            throw new InvalidOperationException(
                $"template chart jacket '{chartId}' was not found");
        }

        private static void AssignChartTemplates(
            IReadOnlyList<SongInfo> songs,
            CustomLibraryPlan plan)
        {
            foreach (CustomPackPlan pack in plan.Packs)
            {
                foreach (CustomSongPlan song in pack.Songs)
                {
                    foreach (CustomChartPlan chart in song.Charts)
                    {
                        if (string.IsNullOrWhiteSpace(
                                chart.TemplateParserKey))
                        {
                            throw new InvalidOperationException(
                                $"chart '{chart.LogicalChartKey}' has no " +
                                "template parser key");
                        }
                    }
                }
            }
        }

        internal static void RefreshSongDataCaches(SongData data)
        {
            SongDataNativeCache.Refresh(data);
        }

        private static int SlotForDifficulty(
            ChartDifficultyFlag difficulty) =>
            difficulty switch
            {
                ChartDifficultyFlag.Minimal => 0,
                ChartDifficultyFlag.Evolved => 1,
                ChartDifficultyFlag.Ultimate => 2,
                ChartDifficultyFlag.Forbidden => 3,
                _ => throw new InvalidOperationException(
                    $"unsupported difficulty {(byte)difficulty}"),
            };

        private static ChartDifficultyFlag DifficultyForSlot(
            int slot) =>
            slot switch
            {
                0 => ChartDifficultyFlag.Minimal,
                1 => ChartDifficultyFlag.Evolved,
                2 => ChartDifficultyFlag.Ultimate,
                3 => ChartDifficultyFlag.Forbidden,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(slot)),
            };

        private static Il2CppReferenceArray<T> Append<T>(
            Il2CppReferenceArray<T> source,
            T value)
            where T : Il2CppSystem.ValueType
        {
            source ??= new Il2CppReferenceArray<T>(0);
            var result =
                new Il2CppReferenceArray<T>(source.Length + 1);
            for (int i = 0; i < source.Length; i++)
                result[i] = source[i];
            result[source.Length] = value;
            return result;
        }

        private static DataAccess FindLoadedDataAccess()
        {
            Il2CppReferenceArray<Object> objects =
                Resources.FindObjectsOfTypeAll(
                    Il2CppType.Of<DataAccess>());
            for (int i = 0; i < objects.Length; i++)
            {
                DataAccess access =
                    objects[i].TryCast<DataAccess>();
                if (access?.SongData is not null &&
                    access.PackData is not null &&
                    access.DynamicStringMapping is not null)
                {
                    return access;
                }
            }
            return null;
        }

    }
}
