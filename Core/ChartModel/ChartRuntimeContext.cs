// Code organization: chart runtime context. Runtime behavior is unchanged.
using System;

namespace InFalsusChartExt
{
    internal sealed class ChartRuntimeFrame
    {
        internal string ChartKey;
        internal string Source;
        internal int Generation;
        internal bool IsExtension;
        internal NativeChart NativeChart;
        internal VisualChart VisualChart;
        internal ChartRuntimeRules Rules;
        internal SkyBlockPlan SkyBlocks;
        internal BombPlan Bombs;

        internal void Dispose()
        {
            NativeChart?.Clear();
            VisualChart?.Clear();
            Rules?.Clear();
            SkyBlocks?.Clear();
            NativeChart = null;
            VisualChart = null;
            Rules = null;
            SkyBlocks = null;
            Bombs = null;
        }
    }

    /// <summary>
    /// Owns the current chart's extension-side data and visual-note catalog.
    /// </summary>
    internal static class ChartRuntimeContext
    {
        private static ChartRuntimeFrame _current;
        private static int _beginLogs;
        private static int _errors;

        internal static ChartRuntimeFrame Current => _current;

        internal static bool IsActive =>
            _current != null &&
            _current.Generation == ChartExtState.ChartGeneration;

        internal static void Begin(ChartIngressResult ingress)
        {
            ChartRuntimeFrame prepared = Prepare(ingress);
            InstallPrepared(prepared);
        }

        internal static ChartRuntimeFrame Prepare(
            ChartIngressResult ingress)
        {
            if (ingress == null)
                return null;
            var prepared = new ChartRuntimeFrame
            {
                ChartKey = ingress.ChartKey,
                Source = ingress.Source,
                Generation = ingress.Generation,
                IsExtension = ingress.IsExtension,
                NativeChart = ingress.NativeChart,
                VisualChart = ingress.VisualChart,
                Rules = ingress.Rules,
                SkyBlocks = ingress.SkyBlocks,
                Bombs = ingress.Bombs,
            };
            ingress.NativeChart = null;
            ingress.VisualChart = null;
            ingress.Rules = null;
            ingress.SkyBlocks = null;
            ingress.Bombs = null;
            return prepared;
        }

        internal static void InstallPrepared(
            ChartRuntimeFrame prepared)
        {
            if (prepared == null)
            {
                DisposeCurrent();
                return;
            }
            DisposeCurrent();
            _current = prepared;

            if (_beginLogs < 24)
            {
                _beginLogs++;
                LogCurrent();
            }
        }

        internal static void DiscardPrepared(
            ChartRuntimeFrame prepared)
        {
            if (prepared == null)
                return;
            try { prepared.Dispose(); }
            catch { }
        }

        private static void LogCurrent()
        {
            try
            {
                ChartExtMod.Log.Msg(
                    $"[ChartExt] ingress: key='{_current.ChartKey}', " +
                    $"generation={_current.Generation}, " +
                    $"native={_current.NativeChart.Count}, " +
                    $"visual={_current.VisualChart.Count}, " +
                    $"rules={_current.Rules.NoteTimingGroups.Count}/" +
                    $"{_current.Rules.ShowTextCount}/" +
                    $"{_current.Rules.SceneControlCount}/" +
                    $"{_current.Rules.SkyColorCount}, " +
                    $"blocks={_current.SkyBlocks?.Blocks.Count ?? 0}, " +
                    $"shared-layers={_current.SkyBlocks?.SharedLayers.Count ?? 0}, " +
                    $"bombs={_current.Bombs?.Count ?? 0}");
            }
            catch { }
        }

        internal static void ResetForChartSwitch()
        {
            DisposeCurrent();
            _beginLogs = 0;
        }

        private static void DisposeCurrent()
        {
            ChartRuntimeFrame current = _current;
            _current = null;
            if (current == null) return;
            try
            {
                current.Dispose();
            }
            catch (Exception e)
            {
                if (_errors++ < 10)
                    ChartExtMod.Log.Error(
                        "[ChartExt] chart context dispose failed: " + e.Message);
            }
        }
    }
}
