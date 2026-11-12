using System;
using System.Reflection;
using HarmonyLib;
using Il2Cppifapp.Game.UI.Settings;
using MelonLoader;
using UnityEngine;

namespace InFalsusChartExt
{
    internal static class DisplayModePatchResolver
    {
        internal static MethodBase FindNamed(
            Type type,
            string name,
            params Type[] parameters)
        {
            try
            {
                return AccessTools.Method(
                    type, name, parameters);
            }
            catch
            {
                return null;
            }
        }

        internal static MethodBase FindBySignature(
            Type type,
            Type returnType,
            params Type[] parameters)
        {
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.ReturnType != returnType)
                    continue;
                ParameterInfo[] actual =
                    method.GetParameters();
                if (actual.Length != parameters.Length)
                    continue;
                bool match = true;
                for (int p = 0; p < parameters.Length; p++)
                {
                    if (actual[p].ParameterType != parameters[p])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return method;
            }
            return null;
        }

        internal static MethodBase Report(
            string name,
            MethodBase method)
        {
            if (method is null)
            {
                MelonLogger.Warning(
                    $"[ChartExt] display mode patch target missing: {name}");
            }
            return method;
        }

        internal static MethodBase ResolveScreenSetResolution()
        {
            return AccessTools.Method(
                typeof(Screen),
                "SetResolution",
                new[]
                {
                    typeof(int),
                    typeof(int),
                    typeof(FullScreenMode),
                    typeof(RefreshRate),
                });
        }
    }

    [HarmonyPatch]
    internal static class DisplayPanelAwakePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            DisplayModePatchResolver.Report(
                "UiSettingDisplayPanel.Awake",
                TargetMethod()) is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            DisplayModePatchResolver.FindNamed(
                typeof(UiSettingDisplayPanel), "Awake");

        [HarmonyPostfix]
        private static void Postfix(
            UiSettingDisplayPanel __instance) =>
            DisplayModeRuntime.Attach(__instance);
    }

    [HarmonyPatch]
    internal static class DisplayPanelUpdatePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            DisplayModePatchResolver.Report(
                "UiSettingDisplayPanel.Update",
                TargetMethod()) is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            DisplayModePatchResolver.FindNamed(
                typeof(UiSettingDisplayPanel), "Update");

        [HarmonyPostfix]
        private static void Postfix(
            UiSettingDisplayPanel __instance) =>
            DisplayModeRuntime.Sync(__instance);
    }

    [HarmonyPatch]
    internal static class DisplayPanelRefreshPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            DisplayModePatchResolver.Report(
                "UiSettingDisplayPanel.refresh(bool)",
                TargetMethod()) is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            Type panel = typeof(UiSettingDisplayPanel);
            return DisplayModePatchResolver.FindNamed(
                       panel, "_vf", typeof(bool)) ??
                   DisplayModePatchResolver.FindBySignature(
                       panel, typeof(void), typeof(bool));
        }

        [HarmonyPrefix]
        private static void Prefix() =>
            DisplayModeRuntime.BeginPanelRefresh();

        [HarmonyPostfix]
        private static void Postfix(
            UiSettingDisplayPanel __instance) =>
            DisplayModeRuntime.EndPanelRefresh(__instance);

        [HarmonyFinalizer]
        private static Exception Finalizer(
            Exception __exception)
        {
            if (__exception is not null)
                DisplayModeRuntime.EndPanelRefreshWithException();
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class DisplayPanelFullscreenCallbackPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            DisplayModePatchResolver.Report(
                "UiSettingDisplayPanel.fullscreenChanged",
                TargetMethod()) is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            Type panel = typeof(UiSettingDisplayPanel);
            return DisplayModePatchResolver.FindNamed(
                       panel, "_zf",
                       typeof(UiSettingMultiselectorArrow),
                       typeof(int)) ??
                   DisplayModePatchResolver.FindBySignature(
                       panel, typeof(void),
                       typeof(UiSettingMultiselectorArrow),
                       typeof(int));
        }

        [HarmonyPrefix]
        private static bool Prefix() =>
            DisplayModeRuntime.BeforeFullscreenCallback();
    }

    [HarmonyPatch]
    internal static class DisplaySelectorIndexPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            DisplayModePatchResolver.Report(
                "UiSettingMultiselectorArrow.setIndex(int)",
                TargetMethod()) is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            Type selector =
                typeof(UiSettingMultiselectorArrow);
            return DisplayModePatchResolver.FindNamed(
                       selector, "_Tg", typeof(int)) ??
                   DisplayModePatchResolver.FindBySignature(
                       selector, typeof(void), typeof(int));
        }

        [HarmonyPrefix]
        private static void Prefix(
            UiSettingMultiselectorArrow __instance,
            int __0) =>
            DisplayModeRuntime.BeforeIndexChange(
                __instance, __0);
    }

    [HarmonyPatch]
    internal static class ScreenSetResolutionModePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            DisplayModePatchResolver.Report(
                "UnityEngine.Screen.SetResolution",
                TargetMethod()) is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            DisplayModePatchResolver
                .ResolveScreenSetResolution();

        [HarmonyPrefix]
        private static void Prefix(
            ref FullScreenMode __2) =>
            DisplayModeRuntime.BeforeResolutionRequest(
                ref __2);
    }
}
