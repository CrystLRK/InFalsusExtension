using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    internal struct VisualTrackFrame
    {
        internal IntPtr Track;
        internal double NowMs;
        internal double BaseSpeed;
        internal double DepthFactor;
        internal double Scale;
        internal double MaxLeadMs;
        internal double NoClipTailMs;
        internal float LaneScale;
        internal float TrackWidth;
        internal IntPtr LaneCenters;
        internal Vector3 CenterScale;
        internal Quaternion CenterRotation;
        internal Quaternion HoldRotation;
        internal Vector3 RestPosition;
        internal Vector3 SideLeftPosition;
        internal Quaternion SideLeftRotation;
        internal Vector3 SideRightPosition;
        internal Quaternion SideRightRotation;
        internal float SideScaleY;
        internal float SideScaleZ;
        internal Transform NotesParent;
        internal Transform CameraParent;
        internal Il2Cppifapp.Game.Scenes.Game.Track TypedTrack;

        internal bool IsValid =>
            Track != IntPtr.Zero &&
            LaneCenters != IntPtr.Zero &&
            Math.Abs(BaseSpeed) > 1e-9 &&
            Math.Abs(DepthFactor) > 1e-9 &&
            !double.IsNaN(NowMs);

        internal double Lead(int group, uint sourceMs)
        {
            return TimingGroupRuntime.VisualTimeMs(group, sourceMs) -
                   TimingGroupRuntime.VisualTimeMs(group, NowMs);
        }

        internal double Distance(double leadMs)
        {
            return Scale * leadMs;
        }

        internal float LaneCenter(int lane)
        {
            if (lane < 1 || lane > 4 || LaneCenters == IntPtr.Zero)
                return 0f;
            return BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(
                    LaneCenters + NativeLayoutCatalog.ArrayDataOffset +
                    (lane - 1) * 4));
        }

        internal float LaneX(int lane, int width)
        {
            return LaneScale * 0.125f * (Math.Max(1, width) - 1) +
                   LaneCenter(lane);
        }

        internal float Alpha(double leadMs, bool noClip)
        {
            if (noClip)
                return 1f;
            double normalized =
                Math.Abs(leadMs) / 1000.0 *
                Math.Abs(DepthFactor / BaseSpeed);
            double fade = Math.Max(
                0.0,
                Math.Min(1.0, (1.0 - normalized) / 0.1));
            return (float)((3.0 - 2.0 * fade) * fade * fade);
        }

        internal static bool TryCapture(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            out VisualTrackFrame frame)
        {
            frame = default;
            if (track == null || track.Pointer == IntPtr.Zero)
                return false;
            try
            {
                IntPtr t = track.Pointer;
                double baseSpeed = ReadDouble(
                    t, NativeLayoutCatalog.Track.BaseSpeed);
                double depthFactor = ReadDouble(
                    t, NativeLayoutCatalog.Track.DepthFactor);
                if (Math.Abs(baseSpeed) < 1e-9 ||
                    Math.Abs(depthFactor) < 1e-9)
                    return false;
                IntPtr laneCenters = Marshal.ReadIntPtr(
                    t + NativeLayoutCatalog.Track.LaneCenters);
                if (laneCenters == IntPtr.Zero)
                    return false;
                double now = TimingGroupRuntime.CurrentMs();
                if (!VisualSpawnTiming.IsFiniteTime(now))
                    return false;

                Transform notesParent = track.notesContainer;
                if (notesParent == null ||
                    notesParent.Pointer == IntPtr.Zero)
                {
                    try
                    {
                        notesParent = track.transform.Find(
                            "NotesContainer");
                    }
                    catch { }
                }

                frame = new VisualTrackFrame
                {
                    Track = t,
                    TypedTrack = track,
                    NowMs = now,
                    BaseSpeed = baseSpeed,
                    DepthFactor = depthFactor,
                    Scale = (baseSpeed / 3.333333334) *
                            depthFactor / 1000.0,
                    MaxLeadMs = VisualSpawnTiming.MaxLeadMs(
                        baseSpeed, depthFactor),
                    NoClipTailMs = TimingGroupRuntime.NoClipTailMs(),
                    LaneScale = ReadFloat(
                        t, NativeLayoutCatalog.Track.NoteWidthScale),
                    TrackWidth = ReadFloat(
                        t, NativeLayoutCatalog.Track.TrackWidth),
                    LaneCenters = laneCenters,
                    CenterRotation = ReadQuaternion(
                        t, NativeLayoutCatalog.Track.CenterRotation),
                    HoldRotation = ReadQuaternion(
                        t, NativeLayoutCatalog.Track.HoldRotation),
                    CenterScale = new Vector3(
                        ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleX, 1f),
                        ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleY, 1f),
                        ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleZ, 1f)),
                    RestPosition = ReadVector3(
                        t, NativeLayoutCatalog.Track.RestPosition),
                    SideLeftPosition = ReadVector3(
                        t, NativeLayoutCatalog.Track.SideLeftPosition),
                    SideLeftRotation = ReadQuaternion(
                        t, NativeLayoutCatalog.Track.SideLeftRotation),
                    SideRightPosition = ReadVector3(
                        t, NativeLayoutCatalog.Track.SideRightPosition),
                    SideRightRotation = ReadQuaternion(
                        t, NativeLayoutCatalog.Track.SideRightRotation),
                    SideScaleY = ReadFloat(
                        t, NativeLayoutCatalog.Track.SideScaleY, 1f),
                    SideScaleZ = ReadFloat(
                        t, NativeLayoutCatalog.Track.SideScaleZ, 1f),
                    NotesParent = notesParent,
                    CameraParent = track.transform,
                };
                if (frame.TrackWidth <= 1e-4f)
                    frame.TrackWidth = 1.6f;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static float ReadFloat(
            IntPtr pointer, int offset, float fallback = 0f)
        {
            float value = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(pointer + offset));
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : value;
        }

        private static double ReadDouble(IntPtr pointer, int offset)
        {
            return BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(pointer + offset));
        }

        private static Vector3 ReadVector3(IntPtr pointer, int offset)
        {
            return new Vector3(
                ReadFloat(pointer, offset),
                ReadFloat(pointer, offset + 4),
                ReadFloat(pointer, offset + 8));
        }

        private static Quaternion ReadQuaternion(IntPtr pointer, int offset)
        {
            return new Quaternion(
                ReadFloat(pointer, offset),
                ReadFloat(pointer, offset + 4),
                ReadFloat(pointer, offset + 8),
                ReadFloat(pointer, offset + 12, 1f));
        }
    }

    internal abstract class VisualNoteInstance
    {
        internal GameObject GameObject;
        internal VisualNoteDescriptor Descriptor;
        internal readonly List<Material> OwnedMaterials =
            new List<Material>();

        internal abstract void Update(in VisualTrackFrame frame);

        internal virtual void SetHidden(bool hidden)
        {
            if (GameObject == null || GameObject.Pointer == IntPtr.Zero)
                return;
            try
            {
                if (GameObject.activeSelf == hidden)
                    GameObject.SetActive(!hidden);
            }
            catch { }
        }

        internal virtual void Dispose()
        {
            for (int i = 0; i < OwnedMaterials.Count; i++)
            {
                Material material = OwnedMaterials[i];
                if (material == null ||
                    material.Pointer == IntPtr.Zero)
                    continue;
                try { UnityEngine.Object.Destroy(material); }
                catch { }
            }
            OwnedMaterials.Clear();
            VisualNoteObjectUtil.Destroy(GameObject);
            GameObject = null;
        }
    }

    internal static class VisualNoteRuntime
    {
        private static readonly Dictionary<long, VisualNoteInstance> Instances =
            new Dictionary<long, VisualNoteInstance>();
        private static readonly List<VisualNoteDescriptor> RuntimeNotes =
            new List<VisualNoteDescriptor>();
        private static int _generation = -1;
        private static IntPtr _track;
        private static int _errors;

        internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!BuildProfile.Supports(NativeCapability.VisualNotes))
                return;
            try
            {
                ChartRuntimeFrame context = ChartRuntimeContext.Current;
                if (context == null || context.VisualChart == null ||
                    context.VisualChart.Count == 0)
                {
                    if (Instances.Count > 0)
                        ResetForChartSwitch();
                    return;
                }

                if (_generation != context.Generation ||
                    (_track != IntPtr.Zero &&
                     track != null &&
                     _track != track.Pointer))
                {
                    ResetForChartSwitch();
                    _generation = context.Generation;
                    _track = track != null ? track.Pointer : IntPtr.Zero;
                    for (int i = 0; i < context.VisualChart.Notes.Count; i++)
                    {
                        VisualNoteDescriptor descriptor =
                            context.VisualChart.Notes[i];
                        if (descriptor != null &&
                            descriptor.Kind != VisualNoteKind.SkyArea)
                            RuntimeNotes.Add(descriptor);
                    }
                }

                if (!VisualTrackFrame.TryCapture(track, out var frame) ||
                    !frame.IsValid)
                    return;

                for (int i = 0; i < RuntimeNotes.Count; i++)
                {
                    VisualNoteDescriptor descriptor = RuntimeNotes[i];

                    double startLead = frame.Lead(
                        descriptor.TimingGroup, descriptor.StartMs);
                    double endLead = frame.Lead(
                        descriptor.TimingGroup, descriptor.EndMs);
                    bool active = startLead <= frame.MaxLeadMs &&
                        (descriptor.Lifetime == VisualNoteLifetime.NoClip
                            ? endLead >= -frame.NoClipTailMs
                            : endLead >= 0.0);

                    if (!active)
                    {
                        DestroyInstance(descriptor.VisualId);
                        continue;
                    }

                    bool hidden =
                        descriptor.Lifetime != VisualNoteLifetime.NoClip &&
                        SceneControlRuntime.IsTimingGroupHidden(
                            descriptor.TimingGroup);
                    if (hidden && !Instances.ContainsKey(descriptor.VisualId))
                        continue;

                    if (!Instances.TryGetValue(
                            descriptor.VisualId,
                            out VisualNoteInstance instance))
                    {
                        instance = Create(descriptor, in frame);
                        if (instance == null)
                            continue;
                        Instances[descriptor.VisualId] = instance;
                    }

                    instance.SetHidden(hidden);
                    if (!hidden)
                        instance.Update(in frame);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] visual-note tick failed: " + e);
            }
        }

        internal static void ResetForChartSwitch()
        {
            foreach (VisualNoteInstance instance in Instances.Values)
            {
                try { instance.Dispose(); }
                catch { }
            }
            Instances.Clear();
            RuntimeNotes.Clear();
            _generation = -1;
            _track = IntPtr.Zero;
        }

        private static VisualNoteInstance Create(
            VisualNoteDescriptor descriptor,
            in VisualTrackFrame frame)
        {
            try
            {
                switch (descriptor.Kind)
                {
                    case VisualNoteKind.Tap:
                        return new VisualTapNoteInstance(descriptor, in frame);
                    case VisualNoteKind.Hold:
                        return new VisualHoldNoteInstance(descriptor, in frame);
                    case VisualNoteKind.Flick:
                        return new VisualFlickNoteInstance(descriptor, in frame);
                    case VisualNoteKind.Bomb:
                        return new VisualBombNoteInstance(descriptor, in frame);
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        $"[ChartExt] visual-note create failed " +
                        $"kind={descriptor.Kind} id={descriptor.SourceId}: {e}");
                return null;
            }
        }

        private static void DestroyInstance(long id)
        {
            if (!Instances.TryGetValue(id, out VisualNoteInstance instance))
                return;
            Instances.Remove(id);
            try { instance.Dispose(); }
            catch { }
        }
    }

    internal static class VisualNoteObjectUtil
    {
        internal static void CloneMaterials(
            GameObject root,
            List<Material> owned)
        {
            if (root == null || root.Pointer == IntPtr.Zero)
                return;
            try
            {
                var renderers =
                    root.GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0;
                     renderers != null && i < renderers.Length;
                     i++)
                {
                    MeshRenderer renderer = renderers[i];
                    if (renderer == null ||
                        renderer.sharedMaterial == null)
                        continue;
                    Material clone =
                        new Material(renderer.sharedMaterial);
                    renderer.material = clone;
                    owned.Add(clone);
                }
            }
            catch { }
        }

        internal static void Destroy(GameObject root)
        {
            if (root == null || root.Pointer == IntPtr.Zero)
                return;
            try { UnityEngine.Object.Destroy(root); }
            catch { }
        }

        internal static int Width(VisualNoteDescriptor descriptor)
        {
            return Math.Max(
                1,
                (int)Math.Round(
                    descriptor.StartWidth * 4.0,
                    MidpointRounding.AwayFromZero));
        }

        internal static int Lane(VisualNoteDescriptor descriptor)
        {
            return (int)Math.Round(
                descriptor.StartCenter * 4.0,
                MidpointRounding.AwayFromZero);
        }
    }
}
