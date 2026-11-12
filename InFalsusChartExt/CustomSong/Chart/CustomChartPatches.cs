using System;
using System.Reflection;
using HarmonyLib;
using InFalsusChartExt.CustomSong.Discovery;
using InFalsusChartExt.CustomSong.Install;
using InFalsusChartExt.CustomSong.Lifecycle;
using Il2Cpp_k;
using Il2Cpp_n;

namespace InFalsusChartExt.CustomSong.Chart
{
    [HarmonyPatch]
    [HarmonyPriority(Priority.High)]
    internal static class CustomChartPayloadPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable("chartParse");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony("chartParse");

        [HarmonyPriority(Priority.First)]
        [HarmonyBefore("InFalsusChartExt.ChartParsePatch")]
        [HarmonyPrefix]
        private static void Prefix(
            ref Il2CppSystem.ReadOnlySpan<byte> __0,
            ref string __1)
        {
            if (!BuildProfile.Supports(
                    NativeCapability.CustomSong) ||
                !CustomSongRuntime.TryGetChartByLogicalKey(
                    __1, out CustomChartPlan chart))
            {
                return;
            }

            try
            {
                CustomChartPayload payload =
                    CustomSongRuntime.GetPayload(chart);
                CustomSongRuntime.Select(chart);
                __0 = new Il2CppSystem.ReadOnlySpan<byte>(
                    payload.PinnedBytes);
                __1 = chart.ParserKey;
                ChartExtMod.Log.Msg(
                    $"[CustomSong] payload selected " +
                    $"{chart.LogicalChartKey} -> {chart.ParserKey}");
            }
            catch (Exception exception)
            {
                ChartExtMod.Log.Error(
                    $"[CustomSong] payload failed for " +
                    $"'{chart.LogicalChartKey}': {exception}");
                throw;
            }
        }
    }

    [HarmonyPatch]
    internal static class CustomChartPathPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable("chartPath");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony("chartPath");

        [HarmonyPrefix]
        private static void Prefix(ref string __0)
        {
            if (!BuildProfile.Supports(
                    NativeCapability.CustomSong) ||
                !CustomSongRuntime.TryGetChartByLogicalKey(
                    __0, out CustomChartPlan chart))
            {
                return;
            }
            string template = CustomSongRuntime
                .GetTemplateParserKey(chart);
            if (string.IsNullOrWhiteSpace(template))
            {
                ChartExtMod.Log.Error(
                    "[CustomSong] chart has no template path: " +
                    chart.LogicalChartKey);
                return;
            }
            __0 = template;
        }
    }

    [HarmonyPatch]
    internal static class CustomSongCacheBoundaryPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable("songCache");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony("songCache");

        [HarmonyPrefix]
        private static void Prefix()
        {
            if (BuildProfile.Supports(
                    NativeCapability.CustomSong) &&
                !CustomSongRuntime.IsInstalled)
            {
                SongLibraryInstaller.Tick();
            }
        }
    }

    [HarmonyPatch]
    internal static class CustomPackScenePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable(
                "packSceneAwake");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "packSceneAwake");

        [HarmonyPrefix]
        private static void Prefix(
            Il2Cppifapp.Game.PackSelectScene __instance)
        {
            if (!BuildProfile.Supports(
                    NativeCapability.CustomSong))
            {
                return;
            }
            SongLibraryInstaller.Tick();
            CustomSongRuntime.OnPackSceneAwake(
                __instance?.packSelectSceneAssets);
        }
    }

    [HarmonyPatch]
    internal static class CustomSongStringMappingPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable(
                "dynamicStringsAfter");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "dynamicStringsAfter");

        [HarmonyPostfix]
        private static void Postfix(
            Il2Cppifapp.Game.Data.DynamicStringMapping __instance)
        {
            if (BuildProfile.Supports(
                    NativeCapability.CustomSong))
            {
                CustomSongRuntime.OnDynamicStringsDeserialized(
                    __instance);
            }
        }
    }
}
