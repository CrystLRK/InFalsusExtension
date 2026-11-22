using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
#if MELONLOADER
using Il2Cppifapp.Game.Data;
using Il2Cppifapp.Game.Scenes;
using Il2Cppifapp.Game.Scenes.Recipe;
#elif BEPINEX
using ifapp.Game.Data;
using ifapp.Game.Scenes;
using ifapp.Game.Scenes.Recipe;
#endif

namespace InFalsusChartExt
{
    /// <summary>
    /// Replaces only the Autoplay result presentation and reward/encounter
    /// writes. All targets are managed properties or normal managed methods;
    /// no native offset is required.
    /// </summary>
    internal static class AutoplayResultsDecorator
    {
        private static int _errors;

        internal static void Apply(ResultsScene scene)
        {
            if (scene is null)
                return;
            try
            {
                Set(scene.encounterBacking, false);
                Set(scene.nonEncounterBacking, true);
                Set(scene.rewardBanner, false);
                Set(scene.resultsBattleLayer, false);
                IotaDropsContainer container =
                    scene.iotaDropsContainer;
                if (container is not null)
                    Apply(container);
            }
            catch (Exception exception)
            {
                LogError("scene", exception);
            }
        }

        internal static void Apply(IotaDropsContainer container)
        {
            if (container is null)
                return;
            try
            {
                Set(container.encounterFailedContainer, true);
                Set(container.assistModeContainer, false);
                Set(container.leftButton, false);
                Set(container.rightButton, false);
                HideCells(container);
                WriteTexts(container);
            }
            catch (Exception exception)
            {
                LogError("drops", exception);
            }
        }

        private static void HideCells(
            IotaDropsContainer container)
        {
            var cells =
                container.GetComponentsInChildren<IotaItemCell>(
                    true);
            if (cells is null)
                return;
            for (int i = 0; i < cells.Length; i++)
            {
                IotaItemCell cell = cells[i];
                if (cell is null)
                    continue;
                GameObject target = cell.gameObject;
                if (target is not null && target.activeSelf)
                    target.SetActive(false);
            }
        }

        private static void WriteTexts(
            IotaDropsContainer container)
        {
            var texts = container.encounterFailedTexts;
            if (texts is null)
                return;
            for (int i = 0; i < texts.Length; i++)
            {
                Il2CppFastText.FastText text = texts[i];
                if (text is null)
                    continue;
                text.SetTextNonLocalized(
                    AutoplayScoreGuard.ResultLabel,
                    Il2CppFastText.FastText.UpdateType.Default);
            }
        }

        private static void Set(
            Component component,
            bool active)
        {
            if (component is null)
                return;
            GameObject target = component.gameObject;
            if (target is not null &&
                target.activeSelf != active)
                target.SetActive(active);
        }

        private static void LogError(
            string stage,
            Exception exception)
        {
            if (_errors++ >= 5)
                return;
            try
            {
                ChartExtMod.Log.Error(
                    $"[AutoplayResults] {stage} decoration failed: " +
                    exception);
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch]
    internal static class AutoplayResultsScenePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            BuildProfile.Supports(
                NativeCapability.AutoplayScoreGuard);

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "resultsStart");

        [HarmonyPriority(Priority.First)]
        [HarmonyPrefix]
        private static void Prefix() =>
            AutoplayScoreGuard.BeginResultsStart();

        [HarmonyPriority(Priority.First)]
        [HarmonyPostfix]
        private static void Postfix(ResultsScene __instance)
        {
            if (AutoplayScoreGuard.ShouldSuppressResults)
                AutoplayResultsDecorator.Apply(__instance);
        }
    }

    [HarmonyPatch]
    internal static class AutoplayDropsAwakePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            BuildProfile.Supports(
                NativeCapability.AutoplayScoreGuard) &&
            AccessTools.Method(
                typeof(IotaDropsContainer),
                "Awake") is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AccessTools.Method(
                typeof(IotaDropsContainer),
                "Awake");

        [HarmonyPostfix]
        private static void Postfix(
            IotaDropsContainer __instance)
        {
            if (AutoplayScoreGuard.ShouldSuppressResults)
            {
                AutoplayScoreGuard.BeginResultsStart();
                AutoplayResultsDecorator.Apply(__instance);
            }
        }
    }

    [HarmonyPatch]
    internal static class AutoplayEncounterWritePatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
        {
            if (!BuildProfile.Supports(
                    NativeCapability.AutoplayScoreGuard))
                return false;
            return AccessTools.Method(
                       typeof(EncounterResults),
                       "UpdateEncounterResult") is not null;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AccessTools.Method(
                typeof(EncounterResults),
                "UpdateEncounterResult");

        [HarmonyPrefix]
        private static bool Prefix() =>
            !AutoplayScoreGuard.ResultScreenSuppression;
    }

    [HarmonyPatch]
    internal static class AutoplayInventoryAddPatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
        {
            if (!BuildProfile.Supports(
                    NativeCapability.AutoplayScoreGuard))
                return false;
            return AccessTools.Method(
                       typeof(IotaInventory),
                       "Add",
                       new[] { typeof(IotaInstance) }) is not null;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AccessTools.Method(
                typeof(IotaInventory),
                "Add",
                new[] { typeof(IotaInstance) });

        [HarmonyPrefix]
        private static bool Prefix() =>
            !AutoplayScoreGuard.ResultScreenSuppression;
    }
}
