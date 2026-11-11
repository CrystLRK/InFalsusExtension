using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    internal sealed class VisualTapNoteInstance : VisualNoteInstance
    {
        private static Il2Cppifapp.Game.Scenes.Game.Track.__c
            _tapPoolCallback;

        private Il2Cppifapp.Game.Scenes.Game.TapNote _note;
        private MeshRenderer _main;

        internal VisualTapNoteInstance(
            VisualNoteDescriptor descriptor,
            in VisualTrackFrame frame)
        {
            Descriptor = descriptor;
            try
            {
                int width = VisualNoteObjectUtil.Width(descriptor);
                int lane = VisualNoteObjectUtil.Lane(descriptor);
                GameObject prefab = TapPrefab(
                    frame.TypedTrack,
                    (uint)lane,
                    width);
                if (prefab == null || prefab.Pointer == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "native tap prefab is unavailable");
                GameObject = UnityEngine.Object.Instantiate(prefab);
                GameObject.name = "IFCE_VisualTap_" + descriptor.SourceId;
                Transform root = GameObject.transform;
                if (frame.NotesParent != null &&
                    frame.NotesParent.Pointer != IntPtr.Zero)
                    root.SetParent(frame.NotesParent, false);
                VisualNoteObjectUtil.CloneMaterials(
                    GameObject, OwnedMaterials);
                _note = GameObject.GetComponent<
                    Il2Cppifapp.Game.Scenes.Game.TapNote>();
                _main = _note != null ? _note.meshRenderer : null;
                if (_note == null || _main == null ||
                    _main.Pointer == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "native tap component is unavailable");
                _note._bMA = _main.material;
                ApplyTapGuideSetting(GameObject, root, _note);
                GameObject.SetActive(false);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal override void Update(in VisualTrackFrame frame)
        {
            if (GameObject == null || _note == null)
                return;
            double lead = frame.Lead(
                Descriptor.TimingGroup, Descriptor.StartMs);
            int width = VisualNoteObjectUtil.Width(Descriptor);
            int lane = VisualNoteObjectUtil.Lane(Descriptor);
            double distance = frame.Distance(lead);
            float normalized = NormalizedDistance(frame, distance);
            Transform root = GameObject.transform;
            if (lane >= 1 && lane <= 4)
            {
                root.localPosition = new Vector3(
                    frame.LaneX(lane, width),
                    0f,
                    (float)distance);
                root.localRotation = frame.CenterRotation;
                root.localScale = new Vector3(
                    width * frame.CenterScale.x,
                    2f * normalized + frame.CenterScale.y,
                    frame.CenterScale.z);
            }
            else if (lane == 0)
            {
                root.localPosition = new Vector3(
                    frame.SideLeftPosition.x,
                    frame.SideLeftPosition.y,
                    frame.SideLeftPosition.z + (float)distance);
                root.localRotation = frame.SideLeftRotation;
                root.localScale = new Vector3(
                    1.08f,
                    frame.SideScaleY,
                    2f * normalized + frame.SideScaleZ);
            }
            else if (lane == 5)
            {
                root.localPosition = new Vector3(
                    frame.SideRightPosition.x,
                    frame.SideRightPosition.y,
                    frame.SideRightPosition.z + (float)distance);
                root.localRotation = frame.SideRightRotation;
                root.localScale = new Vector3(
                    1.08f,
                    frame.SideScaleY,
                    2f * normalized + frame.SideScaleZ);
            }
            else
            {
                return;
            }
            _note._ybA(frame.Alpha(
                lead,
                Descriptor.Lifetime == VisualNoteLifetime.NoClip));
            if (!GameObject.activeSelf)
                GameObject.SetActive(true);
        }

        internal static GameObject TapPrefab(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            uint lane,
            int width)
        {
            if (track == null || track.Pointer == IntPtr.Zero)
                return null;
            if (lane == 0)
                return track.sideNotePrefab;
            if (lane == 5)
                return track.sideNoteAltPrefab;
            return width > 1
                ? track.noteSpanPrefab
                : track.notePrefab;
        }

        internal static float NormalizedDistance(
            in VisualTrackFrame frame,
            double distance)
        {
            return (float)(
                Math.Abs(distance) /
                Math.Max(Math.Abs(frame.BaseSpeed), 1e-6));
        }

        internal static float TapState(float normalized)
        {
            float t = Mathf.Clamp01((1f - normalized) / 0.1f);
            return (4f - 2f * t) * t * t;
        }

        private static void ApplyTapGuideSetting(
            GameObject gameObject,
            Transform root,
            Il2Cppifapp.Game.Scenes.Game.TapNote note)
        {
            try
            {
                _tapPoolCallback ??= new
                    Il2Cppifapp.Game.Scenes.Game.Track.__c();
                var tuple = new Il2CppSystem.ValueTuple<
                    GameObject,
                    Transform,
                    Il2Cppifapp.Game.Scenes.Game.TapNote>(
                        gameObject, root, note);
                _tapPoolCallback._LBA(tuple);
            }
            catch { }
        }
    }

    internal sealed class VisualFlickNoteInstance : VisualNoteInstance
    {
        private Il2Cpp.FlickNoteV3 _flick;
        private Quaternion _rootRotation;
        private Vector3 _rootScale;

        internal VisualFlickNoteInstance(
            VisualNoteDescriptor descriptor,
            in VisualTrackFrame frame)
        {
            Descriptor = descriptor;
            try
            {
                int direction = (int)((descriptor.Flags >> 8) & 0xFF);
                GameObject prefab = direction == 16
                    ? frame.TypedTrack.flickLeftNotePrefab
                    : frame.TypedTrack.flickRightNotePrefab;
                if (prefab == null || prefab.Pointer == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "native flick prefab is unavailable");
                GameObject = UnityEngine.Object.Instantiate(prefab);
                GameObject.name =
                    "IFCE_VisualFlick_" + descriptor.SourceId;
                Transform root = GameObject.transform;
                _rootRotation = root.localRotation;
                _rootScale = root.localScale;
                Transform parent = frame.NotesParent;
                if (parent == null || parent.Pointer == IntPtr.Zero)
                    parent = frame.CameraParent;
                if (parent != null && parent.Pointer != IntPtr.Zero)
                    root.SetParent(parent, false);
                VisualNoteObjectUtil.CloneMaterials(
                    GameObject, OwnedMaterials);
                _flick = GameObject.GetComponent<Il2Cpp.FlickNoteV3>();
                if (_flick == null || _flick.Pointer == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "native flick component is unavailable");
                GameObject.SetActive(false);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal override void Update(in VisualTrackFrame frame)
        {
            if (GameObject == null || _flick == null)
                return;
            double lead = frame.Lead(
                Descriptor.TimingGroup, Descriptor.StartMs);
            bool left = ((Descriptor.Flags >> 8) & 0xFF) == 16;
            float edge = (float)Descriptor.StartCenter +
                (left
                    ? -(float)Descriptor.StartWidth * 0.5f
                    : (float)Descriptor.StartWidth * 0.5f);
            float x = (edge - 0.5f) * frame.TrackWidth +
                      frame.RestPosition.x;
            Transform root = GameObject.transform;
            root.localPosition = new Vector3(
                x,
                frame.RestPosition.y,
                frame.RestPosition.z +
                (float)frame.Distance(lead));
            root.localRotation = _rootRotation;
            root.localScale = _rootScale;
            float widthFactor = (float)Descriptor.StartWidth;
            _flick._N(
                widthFactor,
                frame.Alpha(
                    lead,
                    Descriptor.Lifetime ==
                    VisualNoteLifetime.NoClip));
            if (!GameObject.activeSelf)
                GameObject.SetActive(true);
        }
    }

    internal sealed class VisualHoldNoteInstance : VisualNoteInstance
    {
        private static Il2Cppifapp.Game.Scenes.Game.Track.__c
            _holdPoolCallback;

        private Il2Cppifapp.Game.Scenes.Game.HoldNoteV2 _hold;
        private Vector3 _baseScale;

        internal VisualHoldNoteInstance(
            VisualNoteDescriptor descriptor,
            in VisualTrackFrame frame)
        {
            Descriptor = descriptor;
            try
            {
                int lane = VisualNoteObjectUtil.Lane(descriptor);
                GameObject prefab = lane == 0
                    ? frame.TypedTrack.sideHoldNotePrefab
                    : lane == 5
                        ? frame.TypedTrack.sideHoldNoteAltPrefab
                        : frame.TypedTrack.holdNotePrefab;
                if (prefab == null || prefab.Pointer == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "native hold prefab is unavailable");
                GameObject = UnityEngine.Object.Instantiate(prefab);
                GameObject.name =
                    "IFCE_VisualHold_" + descriptor.SourceId;
                Transform root = GameObject.transform;
                _baseScale = root.localScale;
                if (frame.NotesParent != null &&
                    frame.NotesParent.Pointer != IntPtr.Zero)
                    root.SetParent(frame.NotesParent, false);
                VisualNoteObjectUtil.CloneMaterials(
                    GameObject, OwnedMaterials);
                _hold = GameObject.GetComponent<
                    Il2Cppifapp.Game.Scenes.Game.HoldNoteV2>();
                if (_hold == null || _hold.Pointer == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "native hold component is unavailable");
                ApplyHoldGuideSetting(GameObject, root, _hold);
                GameObject.SetActive(false);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal override void Update(in VisualTrackFrame frame)
        {
            if (GameObject == null || _hold == null)
                return;
            double startLead = frame.Lead(
                Descriptor.TimingGroup, Descriptor.StartMs);
            int lane = VisualNoteObjectUtil.Lane(Descriptor);
            int width = VisualNoteObjectUtil.Width(Descriptor);
            Transform root = GameObject.transform;
            if (lane >= 1 && lane <= 4)
            {
                root.localPosition = new Vector3(
                    frame.LaneX(lane, width),
                    0f,
                    (float)frame.Distance(startLead));
                root.localRotation = frame.HoldRotation;
                root.localScale = _baseScale;
            }
            else if (lane == 0)
            {
                root.localPosition = new Vector3(
                    frame.SideLeftPosition.x,
                    frame.SideLeftPosition.y,
                    frame.SideLeftPosition.z +
                    (float)frame.Distance(startLead));
                root.localRotation = frame.SideLeftRotation;
                root.localScale = _baseScale;
            }
            else if (lane == 5)
            {
                root.localPosition = new Vector3(
                    frame.SideRightPosition.x,
                    frame.SideRightPosition.y,
                    frame.SideRightPosition.z +
                    (float)frame.Distance(startLead));
                root.localRotation = frame.SideRightRotation;
                root.localScale = _baseScale;
            }
            else
            {
                return;
            }

            _hold._cZ(
                (float)Math.Abs(
                    frame.Scale *
                    (Descriptor.EndMs - Descriptor.StartMs)),
                (float)frame.DepthFactor,
                0f,
                false,
                width,
                false,
                frame.NowMs);
            if (!GameObject.activeSelf)
                GameObject.SetActive(true);
        }

        private static void ApplyHoldGuideSetting(
            GameObject gameObject,
            Transform root,
            Il2Cppifapp.Game.Scenes.Game.HoldNoteV2 note)
        {
            try
            {
                _holdPoolCallback ??= new
                    Il2Cppifapp.Game.Scenes.Game.Track.__c();
                var tuple = new Il2CppSystem.ValueTuple<
                    GameObject,
                    Transform,
                    Il2Cppifapp.Game.Scenes.Game.HoldNoteV2>(
                        gameObject, root, note);
                _holdPoolCallback._MBA(tuple);
            }
            catch { }
        }
    }

    internal sealed class VisualBombNoteInstance : VisualNoteInstance
    {
        private Il2Cppifapp.Game.Scenes.Game.TapNote _note;
        private Material _ownedMaterial;

        internal VisualBombNoteInstance(
            VisualNoteDescriptor descriptor,
            in VisualTrackFrame frame)
        {
            Descriptor = descriptor;
            try
            {
                int width = VisualNoteObjectUtil.Width(descriptor);
                int lane = VisualNoteObjectUtil.Lane(descriptor);
                GameObject prefab = VisualTapNoteInstance.TapPrefab(
                    frame.TypedTrack,
                    (uint)lane,
                    width);
                if (prefab == null || prefab.Pointer == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "native bomb visual prefab is unavailable");
                GameObject = UnityEngine.Object.Instantiate(prefab);
                GameObject.name =
                    "IFCE_VisualBomb_" + descriptor.SourceId;
                Transform root = GameObject.transform;
                if (frame.NotesParent != null &&
                    frame.NotesParent.Pointer != IntPtr.Zero)
                    root.SetParent(frame.NotesParent, false);
                _note = GameObject.GetComponent<
                    Il2Cppifapp.Game.Scenes.Game.TapNote>();
                if (_note == null ||
                    !BombSkinRuntime.TryConfigureStandaloneVisual(
                        GameObject, root, _note,
                        out _ownedMaterial))
                    throw new InvalidOperationException(
                        "native bomb visual material is unavailable");
                OwnedMaterials.Add(_ownedMaterial);
                GameObject.SetActive(false);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal override void Update(in VisualTrackFrame frame)
        {
            if (GameObject == null || _note == null)
                return;
            double lead = frame.Lead(
                Descriptor.TimingGroup, Descriptor.StartMs);
            int width = VisualNoteObjectUtil.Width(Descriptor);
            int lane = VisualNoteObjectUtil.Lane(Descriptor);
            double distance = frame.Distance(lead);
            float normalized =
                VisualTapNoteInstance.NormalizedDistance(
                    in frame, distance);
            float state =
                Descriptor.Lifetime == VisualNoteLifetime.NoClip
                    ? 1f
                    : VisualTapNoteInstance.TapState(normalized);
            Transform root = GameObject.transform;
            if (lane >= 1 && lane <= 4)
            {
                root.localPosition = new Vector3(
                    frame.LaneX(lane, width),
                    0f,
                    (float)distance);
                root.localRotation = frame.CenterRotation;
                root.localScale = new Vector3(
                    width * frame.CenterScale.x,
                    2f * normalized + frame.CenterScale.y,
                    frame.CenterScale.z);
            }
            else if (lane == 0)
            {
                root.localPosition = new Vector3(
                    frame.SideLeftPosition.x,
                    frame.SideLeftPosition.y,
                    frame.SideLeftPosition.z + (float)distance);
                root.localRotation = frame.SideLeftRotation;
                root.localScale = new Vector3(
                    1.08f,
                    frame.SideScaleY,
                    2f * normalized + frame.SideScaleZ);
            }
            else if (lane == 5)
            {
                root.localPosition = new Vector3(
                    frame.SideRightPosition.x,
                    frame.SideRightPosition.y,
                    frame.SideRightPosition.z + (float)distance);
                root.localRotation = frame.SideRightRotation;
                root.localScale = new Vector3(
                    1.08f,
                    frame.SideScaleY,
                    2f * normalized + frame.SideScaleZ);
            }
            else
            {
                return;
            }
            _note._ybA(state);
            if (!GameObject.activeSelf)
                GameObject.SetActive(true);
        }

    }
}
