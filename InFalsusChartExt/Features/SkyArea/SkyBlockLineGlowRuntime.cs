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
        private const double FadeOutMs = 300.0;
        private const float NativeOpacityEpsilon = 0.001f;

        private static readonly int CenterXId =
            Shader.PropertyToID("_CenterX");
        private static readonly int WidthId =
            Shader.PropertyToID("_Width");
        private static readonly int OpacityId =
            Shader.PropertyToID("_Opacity");
        private static int _generation = -1;
        private static double _lastActiveMs =
            double.NegativeInfinity;
        private static int _errors;

        internal static void Tick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
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
                    _lastActiveMs = double.NegativeInfinity;
                    return;
                }
                if (!SkyBlockRuntimeSlots.Ensure(plan))
                    return;
                if (_generation != plan.Generation)
                {
                    ReleaseIfActive(track);
                    _generation = plan.Generation;
                    _lastActiveMs = double.NegativeInfinity;
                }
                if (TimingGroupRuntime.IsVisualClockPaused ||
                    TimingGroupRuntime.IsResumeTransition())
                {
                    ReleaseIfActive(track);
                    return;
                }

                Il2Cpp.JudgementLineMeshEffect effect =
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
                    _lastActiveMs = double.NegativeInfinity;
                    return;
                }

                if (TryResolveRange(
                        plan, now,
                        out float left,
                        out float right,
                        out double endSeconds))
                {
                    _lastActiveMs = now;
                    effect._P(left, right);
                    ApplyOutput(
                        output, left, right,
                        PulseOpacity(endSeconds));
                    return;
                }

                if (IsFinite(_lastActiveMs))
                {
                    float fade = (float)Clamp(
                        1.0 -
                        (now - _lastActiveMs) / FadeOutMs,
                        0.0, 1.0);
                    effect._P(0f, 0f);
                    ApplyOutput(output, 0f, 0f, fade);
                    if (fade <= NativeOpacityEpsilon)
                        _lastActiveMs = double.NegativeInfinity;
                }
            }
            catch (Exception e)
            {
                LogError("tick", e);
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
            _lastActiveMs = double.NegativeInfinity;
        }

        private static bool TryResolveRange(
            SkyBlockPlan plan,
            double now,
            out float left,
            out float right,
            out double endSeconds)
        {
            left = 0f;
            right = 0f;
            endSeconds = 0.0;
            bool found = false;

            Il2Cpp_E._VD judge =
                Il2Cppifapp.Game.Scenes.GameScene._DN;
            if (judge == null ||
                judge.Pointer == IntPtr.Zero)
                return false;
            float cursorX = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(
                    judge.Pointer +
                    NativeLayoutCatalog.SkyJudge.CursorX));
            if (!IsFinite(cursorX))
                return false;

            long ownerSerial = 0L;
            for (int i = 0;
                 i < plan.VisualMembers.Count;
                 i++)
            {
                SkyBlockMember member = plan.VisualMembers[i];
                if (now < member.StartMs ||
                    now >= member.EndMs ||
                    SceneControlRuntime.IsNoteHidden(member.Id))
                    continue;
                if (!SkyBlockJudgeRuntime.IsMemberGlowActive(
                        member, now))
                    continue;
                if (member.GlowSerial <= ownerSerial)
                    continue;

                if (!SkyBlockVisualRuntime.TryGetVisualTiming(
                        member, now,
                        out double visualNow,
                        out double visualStart,
                        out double visualEnd))
                    continue;
                double progress = member.TimingGroup == 0
                    ? SkyBlockVisualRuntime.RawProgress(
                        member, now)
                    : (visualNow - visualStart) /
                      (visualEnd - visualStart);
                if (!(progress > 0.0 && progress < 1.0))
                    continue;

                SkyBandRange range = member.RangeAt(now);
                if (!IsFinite(range.Lo) ||
                    !IsFinite(range.Hi))
                    continue;

                left = range.Lo;
                right = range.Hi;
                endSeconds = visualEnd / 1000.0;
                found = true;
                ownerSerial = member.GlowSerial;
            }
            return found;
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
                !IsFinite(_lastActiveMs))
                return;
            Material output = track._tnA;
            Il2Cpp.JudgementLineMeshEffect effect =
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
