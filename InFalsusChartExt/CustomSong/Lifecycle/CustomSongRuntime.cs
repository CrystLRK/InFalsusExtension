using System;
using System.Collections.Generic;
using InFalsusChartExt.CustomSong.Chart;
using InFalsusChartExt.CustomSong.Audio;
using InFalsusChartExt.CustomSong.Assets;
using InFalsusChartExt.CustomSong.Discovery;
using InFalsusChartExt.CustomSong.Install;
using InFalsusChartExt.CustomSong.Scores;

namespace InFalsusChartExt.CustomSong.Lifecycle
{
    internal sealed class InstalledCustomLibrary
    {
        private readonly Dictionary<string, CustomSongPlan> _byBaseName =
            new Dictionary<string, CustomSongPlan>(
                StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CustomChartPlan> _byLogicalKey =
            new Dictionary<string, CustomChartPlan>(
                StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CustomChartPlan> _byParserKey =
            new Dictionary<string, CustomChartPlan>(
                StringComparer.OrdinalIgnoreCase);

        internal CustomLibraryPlan Plan { get; }
        internal CustomSongPlan ActiveSong { get; private set; }
        internal CustomChartPlan ActiveChart { get; private set; }

        internal InstalledCustomLibrary(CustomLibraryPlan plan)
        {
            Plan = plan;
            foreach (CustomPackPlan pack in plan.Packs)
            {
                foreach (CustomSongPlan song in pack.Songs)
                {
                    _byBaseName.Add(song.BaseName, song);
                    foreach (CustomChartPlan chart in song.Charts)
                    {
                        _byLogicalKey.Add(
                            chart.LogicalChartKey, chart);
                        _byParserKey.Add(chart.ParserKey, chart);
                    }
                }
            }
        }

        internal bool TryGetSongByBaseName(
            string baseName,
            out CustomSongPlan song) =>
            _byBaseName.TryGetValue(baseName ?? "", out song);

        internal bool TryGetSongForAudioName(
            string audioName,
            out CustomSongPlan song)
        {
            song = null;
            if (string.IsNullOrWhiteSpace(audioName))
                return false;
            string stem = System.IO.Path.GetFileNameWithoutExtension(
                audioName);
            if (_byBaseName.TryGetValue(stem, out song))
                return true;
            foreach (CustomSongPlan candidate in _byBaseName.Values)
            {
                if (stem.IndexOf(
                        candidate.BaseName,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    song = candidate;
                    return true;
                }
            }
            return false;
        }

        internal bool TryGetChartByLogicalKey(
            string key,
            out CustomChartPlan chart) =>
            _byLogicalKey.TryGetValue(
                ChartLogicalKey(key), out chart);

        internal bool TryGetChartByParserKey(
            string key,
            out CustomChartPlan chart) =>
            _byParserKey.TryGetValue(key ?? "", out chart);

        private static string ChartLogicalKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;
            return System.IO.Path.GetFileNameWithoutExtension(
                key.Trim());
        }

        internal void Select(CustomChartPlan chart)
        {
            ActiveChart = chart;
            ActiveSong = chart?.Song;
        }
    }

    internal static class CustomSongRuntime
    {
        private static readonly object Gate = new object();
        private static InstalledCustomLibrary _installed;
        private static CustomLibraryPlan _plan;
        private static CustomChartPayloadSource _payloads;
        private static bool _scanCompleted;
        private static string _scanReport;

        internal static bool IsInstalled =>
            _installed is not null;

        internal static InstalledCustomLibrary Installed =>
            _installed;

        internal static CustomSongPlan ActiveSong =>
            _installed?.ActiveSong;

        internal static CustomChartPlan ActiveChart =>
            _installed?.ActiveChart;

        internal static CustomLibraryPlan ScanIfNeeded()
        {
            lock (Gate)
            {
                if (_scanCompleted)
                    return _plan;
                _scanCompleted = true;
                _plan =
                    CustomLibraryDiscovery.Scan(out _scanReport);
                ChartExtMod.Log.Msg(
                    string.IsNullOrWhiteSpace(_scanReport)
                        ? "[CustomSong] no custom packs discovered"
                        : "[CustomSong] discovery:\n" + _scanReport);
                return _plan;
            }
        }

        internal static void MarkInstalled(CustomLibraryPlan plan)
        {
            lock (Gate)
            {
                _payloads?.Dispose();
                _payloads = new CustomChartPayloadSource();
                _plan = plan;
                _installed = new InstalledCustomLibrary(plan);
            }
        }

        internal static bool TryGetSongByLogicalKey(
            string key,
            out CustomSongPlan song)
        {
            song = null;
            if (_installed is null ||
                !_installed.TryGetChartByLogicalKey(key, out var chart))
            {
                return false;
            }
            song = chart.Song;
            return true;
        }

        internal static bool TryGetChartByLogicalKey(
            string key,
            out CustomChartPlan chart)
        {
            chart = null;
            return _installed is not null &&
                   _installed.TryGetChartByLogicalKey(key, out chart);
        }

        internal static bool TryGetChartByParserKey(
            string key,
            out CustomChartPlan chart)
        {
            chart = null;
            return _installed is not null &&
                   _installed.TryGetChartByParserKey(key, out chart);
        }

        internal static bool TryGetChartByAnyKey(
            string key,
            out CustomChartPlan chart) =>
            TryGetChartByLogicalKey(key, out chart) ||
            TryGetChartByParserKey(key, out chart);

        internal static bool TryGetSongByBaseName(
            string baseName,
            out CustomSongPlan song)
        {
            song = null;
            return _installed is not null &&
                   _installed.TryGetSongByBaseName(baseName, out song);
        }

        internal static bool TryGetSongForAudioName(
            string audioName,
            out CustomSongPlan song)
        {
            song = null;
            return _installed is not null &&
                   _installed.TryGetSongForAudioName(
                       audioName, out song);
        }

        internal static void Select(CustomChartPlan chart)
        {
            _installed?.Select(chart);
        }

        internal static string GetTemplateParserKey(
            CustomChartPlan chart) =>
            chart?.TemplateParserKey;

        internal static CustomChartPayload GetPayload(
            CustomChartPlan chart)
        {
            if (_payloads is null)
                throw new InvalidOperationException(
                    "CustomSong runtime has not been installed.");
            return _payloads.GetOrLoad(chart);
        }

        internal static void Tick()
        {
            SongLibraryInstaller.Tick();
            if (_installed is not null)
            {
                CustomScoreRuntime.Tick();
                CustomSongAudioController.Tick();
                JacketMaterialService.Tick();
            }
        }

        internal static void OnDynamicStringsDeserialized(
            Il2Cppifapp.Game.Data.DynamicStringMapping mapping)
        {
            SongLibraryInstaller.ReapplyDisplayMetadata(mapping);
        }

        internal static void OnPackSceneAwake(
            Il2Cppifapp.Game.Data.PackSelectSceneAssets assets)
        {
            SongLibraryInstaller.ApplyPackAssets(assets);
        }

        internal static void Dispose()
        {
            lock (Gate)
            {
                _payloads?.Dispose();
                _payloads = null;
                _installed = null;
                _plan = null;
                CustomSongAudioController.Reset();
                CustomScoreRuntime.Reset();
                JacketMaterialService.Dispose();
                PackAssetInstaller.Dispose();
                _scanCompleted = false;
                _scanReport = null;
            }
        }
    }
}
