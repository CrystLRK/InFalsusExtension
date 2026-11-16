using System.Reflection;
using HarmonyLib;

namespace InFalsusChartExt
{
    internal static class AutoplaySettingsPatchResolver
    {
        internal static MethodBase FindNamed(
            System.Type type,
            string name)
        {
            try
            {
                return AccessTools.Method(type, name);
            }
            catch
            {
                return null;
            }
        }
    }

    [HarmonyPatch]
    internal static class AutoplaySettingsAwakePatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
        {
            MethodBase method = TargetMethod();
            if (method is null)
            {
                ChartExtMod.Log.Warning(
                    "[ChartExt] autoplay settings patch target " +
                    "missing: UiSettingGameplayPanel.Awake");
            }
            return method is not null;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AutoplaySettingsPatchResolver.FindNamed(
                typeof(Il2Cppifapp.Game.UI.Settings
                    .UiSettingGameplayPanel),
                "Awake");

        [HarmonyPostfix]
        private static void Postfix(
            Il2Cppifapp.Game.UI.Settings
                .UiSettingGameplayPanel __instance)
        {
            AutoplaySettingsOverlay.Attach(__instance);
        }
    }

    [HarmonyPatch]
    internal static class AutoplaySettingsUpdatePatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
        {
            MethodBase method = TargetMethod();
            if (method is null)
            {
                ChartExtMod.Log.Warning(
                    "[ChartExt] autoplay settings patch target " +
                    "missing: UiSettingGameplayPanel.Update");
            }
            return method is not null;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AutoplaySettingsPatchResolver.FindNamed(
                typeof(Il2Cppifapp.Game.UI.Settings
                    .UiSettingGameplayPanel),
                "Update");

        [HarmonyPostfix]
        private static void Postfix(
            Il2Cppifapp.Game.UI.Settings
                .UiSettingGameplayPanel __instance)
        {
            AutoplaySettingsOverlay.Sync(__instance);
        }
    }

    [HarmonyPatch]
    internal static class AutoplaySettingsLayerHidePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            TargetMethod() is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AutoplaySettingsPatchResolver.FindNamed(
                typeof(Il2Cppifapp.Game.UI.Settings
                    .UiSettingsLayer),
                "OnDisable");

        [HarmonyPostfix]
        private static void Postfix()
        {
            AutoplaySettingsOverlay.Hide();
        }
    }

    [HarmonyPatch]
    internal static class AutoplaySettingsLayerDestroyPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            TargetMethod() is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AutoplaySettingsPatchResolver.FindNamed(
                typeof(Il2Cppifapp.Game.UI.Settings
                    .UiSettingsLayer),
                "OnDestroy");

        [HarmonyPostfix]
        private static void Postfix()
        {
            AutoplaySettingsOverlay.Hide();
        }
    }
}
