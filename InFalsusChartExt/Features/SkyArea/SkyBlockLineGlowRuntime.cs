using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Supplies the native judgement-line glow for independently judged
    /// SkyBlock members. The native spawn loop never sees these members, so
    /// their _PEA/_Tz events cannot drive Track._gBA's line effect by
    /// themselves.
    ///
    /// The game owns one RenderGraph-backed effect per Track. This runtime
    /// therefore drives that existing instance only while the native SkyArea
    /// presentation is idle, and yields immediately when native writes a
    /// visible line opacity.
    /// </summary>
    internal static class SkyBlockLineGlowRuntime
    {
        private const float NativeOpacityEpsilon = 0.001f;

        private static readonly int CenterXId =
            Shader.PropertyToID("_CenterX");
        private static readonly int WidthId =
            Shader.PropertyToID("_Width");
        private static readonly int OpacityId =
            Shader.PropertyToID("_Opacity");
        private static int _generation = -1;
        private static bool _active;
        private static int _errors;

        internal static void Tick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            long profileStart = FrameProfiler.Begin(out long profileAlloc);
            if (!BuildProfile.Supports(NativeCapability.SkyBlock))
                return;
            try
            {
                SkyBlockPlan plan = CurrentPlan();
                if (track == null ||
                    track.Pointer == IntPtr.Zero ||
                    plan == null ||
                    plan.VisualMembers.Count == 0)
                {
                    ReleaseIfActive(track);
                    _generation = -1;
                    _active = false;
                    return;
                }
                if (!SkyBlockRuntimeSlots.Ensure(plan))
                    return;
                if (_generation != plan.Generation)
                {
                    ReleaseIfActive(track);
                    _generation = plan.Generation;
                    _active = false;
                }
                if (TimingGroupRuntime.IsVisualClockPaused ||
                    TimingGroupRuntime.IsResumeTransition())
                {
                    ReleaseIfActive(track);
                    return;
                }

                IFCEJudgementLineMeshEffect effect =
                    track.judgementLineMeshEffect;
                Material output = track._tnA;
                if (effect == null ||
                    effect.Pointer == IntPtr.Zero ||
                    !effect.enabled ||
                    effect.gameObject == null ||
                    !effect.gameObject.activeInHierarchy ||
                    output == null ||
                    output.Pointer == IntPtr.Zero)
                {
                    ReleaseIfActive(track);
                    return;
                }

                double now = TimingGroupRuntime.CurrentMs();
                if (!IsFinite(now))
                {
                    ReleaseIfActive(track);
                    return;
                }

                // Track.Update owns the shared output if it wrote a visible
                // line this frame. Never overwrite a native presentation.
                if (output.GetFloat(OpacityId) >
                    NativeOpacityEpsilon)
                {
                    _active = false;
                    return;
                }

                // The managed strip is the single source of truth. It
                // publishes the member sitting at the line (and the exact
                // range) after its own release checks have run this frame,
                // so the glow appears and disappears on exactly the same
                // frames instead of reconstructing a second window.
                if (SkyBlockVisualRuntime.TryGetLineState(
                        out long lineId,
                        out float left,
                        out float right,
                        out _) &&
                    !SceneControlRuntime.IsNoteHidden(lineId))
                {
                    _active = true;
                    float opacity = PulseOpacity(now / 1000.0);
                    effect._P(left, right);
                    ApplyOutput(
                        output, left, right,
                        opacity);
                    return;
                }
                if (_active)
                {
                    _active = false;
                    effect._P(0f, 0f);
                    ApplyOutput(output, 0f, 0f, 0f);
                }
            }
            catch (Exception e)
            {
                LogError("tick", e);
            }
            finally
            {
                FrameProfiler.End(
                    FrameProfiler.SkyGlow,
                    profileStart, profileAlloc);
            }
        }

        internal static void ResetForChartSwitch()
        {
            try
            {
                ReleaseIfActive(
                    Il2Cppifapp.Game.Scenes.Game.Track._a);
            }
            catch { }
            _generation = -1;
            _active = false;
        }

        private static float PulseOpacity(
            double noteEndSeconds)
        {
            double slow = Math.Sin(
                noteEndSeconds * 109.0) * 0.5 + 0.5;
            double fast = Math.Sin(
                noteEndSeconds * 34.0) * 0.5 + 0.5;
            return (float)Clamp(
                1.0 - (slow + fast) * 0.1,
                0.0, 1.0);
        }

        private static void ApplyOutput(
            Material output,
            float left,
            float right,
            float opacity)
        {
            float center = (left + right) * 0.5f;
            float width = Math.Max(0f, right - left);
            output.SetFloat(CenterXId, center);
            output.SetFloat(WidthId, width);
            output.SetFloat(OpacityId, opacity);
        }

        private static void ReleaseIfActive(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (track == null ||
                track.Pointer == IntPtr.Zero ||
                !_active)
                return;
            _active = false;
            Material output = track._tnA;
            IFCEJudgementLineMeshEffect effect =
                track.judgementLineMeshEffect;
            if (output != null &&
                output.Pointer != IntPtr.Zero)
                output.SetFloat(OpacityId, 0f);
            if (effect != null &&
                effect.Pointer != IntPtr.Zero)
                effect._P(0f, 0f);
        }

        private static bool IsFinite(double value)
            => !double.IsNaN(value) &&
               !double.IsInfinity(value);

        private static double Clamp(
            double value, double min, double max)
            => value < min
                ? min
                : value > max ? max : value;

        private static SkyBlockPlan CurrentPlan()
        {
            ChartRuntimeFrame frame =
                ChartRuntimeContext.Current;
            return ChartRuntimeContext.IsActive
                ? frame?.SkyBlocks
                : null;
        }

        private static void LogError(
            string where, Exception error)
        {
            if (_errors++ >= 20) return;
            try
            {
                ChartExtMod.Log.Error(
                    $"[ChartExt] skyblock line glow {where} failed: " +
                    error);
            }
            catch { }
        }
    }
}
