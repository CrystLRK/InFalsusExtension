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
        private MeshRenderer _frontRenderer;
        private MeshRenderer _middleRenderer;
        private MeshRenderer _backRenderer;
        private static readonly int NonHeldMapProperty =
            Shader.PropertyToID("_NonHeldMap");
        private static readonly int HeldMapProperty =
            Shader.PropertyToID("_HeldMap");
        private static readonly int HeldBoostedMapProperty =
            Shader.PropertyToID("_HeldBoostedMap");
#if IFCE_DIAGNOSTICS
        private float _r7LastLoggedProgress = -1f;
        private int _r7HoldLogs;
        private float _r7LastGeometryProgress = -1f;
        private int _r7GeometryLogs;
        private static bool _r7HoldScanLogged;
        private static readonly int HeldStateProperty =
            Shader.PropertyToID("_Held");
        private static readonly int ProgressProperty =
            Shader.PropertyToID("_Progress");
        private static readonly int ModulateProgressProperty =
            Shader.PropertyToID("_ModulateProgress");
#endif

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
#if IFCE_DIAGNOSTICS
                ChartExtMod.Log.Msg(
                    "[r7-hold] create " +
                    $"id={descriptor.SourceId} " +
                    $"group={descriptor.TimingGroup} " +
                    $"lifetime={descriptor.Lifetime} " +
                    $"start={descriptor.StartMs} " +
                    $"end={descriptor.EndMs}");
#endif
                _frontRenderer = _hold.frontMeshRenderer;
                _middleRenderer = _hold.middleMeshRenderer;
                _backRenderer = _hold.backMeshRenderer;
                ApplyHoldGuideSetting(GameObject, root, _hold);
                PrepareNoInputAppearance();
#if IFCE_DIAGNOSTICS
                LogHoldSceneInstances();
#endif
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
            double endLead = frame.Lead(
                Descriptor.TimingGroup, Descriptor.EndMs);
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

            float progress = 0f;
            if (Descriptor.Lifetime != VisualNoteLifetime.NoClip &&
                startLead <= 0.0)
            {
                double span = endLead - startLead;
                progress = span > 1e-6
                    ? (float)Math.Max(
                        0.0,
                        Math.Min(1.0, -startLead / span))
                    : 1f;
            }

            bool noInputHold =
                Descriptor.Lifetime == VisualNoteLifetime.NoInput;
            double holdSpanMs = noInputHold
                ? endLead - startLead
                : (Descriptor.EndMs - Descriptor.StartMs);
            // The native Hold mesh is trimmed from the head by `progress`.
            // Use the visual span so the cut plane follows the judgement
            // line under global/group speed changes. The independent Hold
            // never uses the native "held" material state.
            _hold._cZ(
                (float)Math.Abs(
                    frame.Scale *
                    holdSpanMs),
                (float)frame.DepthFactor,
                progress,
                false,
                width,
                noInputHold,
                frame.NowMs);
#if IFCE_DIAGNOSTICS
            LogHoldGeometry(progress);
#endif
#if IFCE_DIAGNOSTICS
            if (Descriptor.Lifetime == VisualNoteLifetime.NoInput &&
                _r7HoldLogs < 40 &&
                (_r7LastLoggedProgress < 0f ||
                 Math.Abs(progress - _r7LastLoggedProgress) >= 0.1f ||
                 endLead <= 200.0))
            {
                _r7LastLoggedProgress = progress;
                _r7HoldLogs++;
                ChartExtMod.Log.Msg(
                    "[r7-hold] update " +
                    $"id={Descriptor.SourceId} " +
                    $"group={Descriptor.TimingGroup} " +
                    $"now={frame.NowMs:F1} " +
                    $"startLead={startLead:F1} " +
                    $"endLead={endLead:F1} " +
                    $"progress={progress:F3} " +
                    $"active={GameObject.activeSelf}");
            }
#endif
            if (!GameObject.activeSelf)
                GameObject.SetActive(true);
        }

#if IFCE_DIAGNOSTICS
        private void LogHoldGeometry(float progress)
        {
            if (Descriptor.Lifetime != VisualNoteLifetime.NoInput ||
                _r7GeometryLogs >= 24 ||
                (Math.Abs(progress - _r7LastGeometryProgress) < 0.2f &&
                 progress < 0.99f))
                return;
            _r7LastGeometryProgress = progress;
            _r7GeometryLogs++;
            Transform front = _hold.frontTransform;
            Transform middle = _hold.middleTransform;
            Transform back = _hold.backTransform;
            ChartExtMod.Log.Msg(
                "[r7-hold-geo] " +
                $"progress={progress:F3} " +
                $"frontPos={FormatVector(front?.localPosition)} " +
                $"frontScale={FormatVector(front?.localScale)} " +
                $"middlePos={FormatVector(middle?.localPosition)} " +
                $"middleScale={FormatVector(middle?.localScale)} " +
                $"backPos={FormatVector(back?.localPosition)} " +
                $"backScale={FormatVector(back?.localScale)} " +
                $"frontMat={FormatMaterial(_frontRenderer)} " +
                $"middleMat={FormatMaterial(_middleRenderer)} " +
                $"backMat={FormatMaterial(_backRenderer)}");
        }

        private static string FormatVector(Vector3? value)
        {
            if (!value.HasValue)
                return "null";
            Vector3 v = value.Value;
            return $"{v.x:F3},{v.y:F3},{v.z:F3}";
        }

        private static string FormatMaterial(MeshRenderer renderer)
        {
            Material material = renderer?.sharedMaterial;
            if (material == null || material.Pointer == IntPtr.Zero)
                return "null";
            return "h=" + FormatFloat(material, HeldStateProperty) +
                   ",p=" + FormatFloat(material, ProgressProperty) +
                   ",m=" + FormatFloat(material, ModulateProgressProperty) +
                   ",heldTex=" + FormatTexture(
                       material, HeldMapProperty) +
                   ",nonHeldTex=" + FormatTexture(
                       material, NonHeldMapProperty);
        }

        private static string FormatFloat(Material material, int property)
        {
            return material.HasProperty(property)
                ? material.GetFloat(property).ToString("F3")
                : "na";
        }

        private static string FormatTexture(Material material, int property)
        {
            if (!material.HasProperty(property))
                return "na";
            Texture texture = material.GetTexture(property);
            return texture == null || texture.Pointer == IntPtr.Zero
                ? "null"
                : texture.name;
        }

        private void LogHoldSceneInstances()
        {
            if (Descriptor.Lifetime != VisualNoteLifetime.NoInput ||
                _r7HoldScanLogged)
                return;
            _r7HoldScanLogged = true;
            try
            {
                Il2Cppifapp.Game.Scenes.Game.HoldNoteV2[] holds =
                    UnityEngine.Object.FindObjectsOfType<
                        Il2Cppifapp.Game.Scenes.Game.HoldNoteV2>(true);
                ChartExtMod.Log.Msg(
                    $"[r7-hold-scan] count={holds?.Length ?? 0}");
                for (int i = 0;
                     holds != null && i < holds.Length && i < 32;
                     i++)
                {
                    Il2Cppifapp.Game.Scenes.Game.HoldNoteV2 hold =
                        holds[i];
                    if (hold == null || hold.Pointer == IntPtr.Zero)
                        continue;
                    GameObject go = hold.gameObject;
                    ChartExtMod.Log.Msg(
                        "[r7-hold-scan] " +
                        $"name='{go?.name}' " +
                        $"ptr=0x{hold.Pointer.ToInt64():X} " +
                        $"active={go?.activeInHierarchy} " +
                        $"pos={FormatVector(
                            go?.transform?.position)}");
                }
            }
            catch (Exception e)
            {
                ChartExtMod.Log.Warning(
                    "[r7-hold-scan] failed: " + e.Message);
            }
        }

#endif

        private void PrepareNoInputAppearance()
        {
            if (Descriptor.Lifetime != VisualNoteLifetime.NoInput)
                return;
            CopyNonHeldMaps(_frontRenderer);
            CopyNonHeldMaps(_middleRenderer);
            CopyNonHeldMaps(_backRenderer);
        }

        private static void CopyNonHeldMaps(MeshRenderer renderer)
        {
            Material material = renderer?.sharedMaterial;
            if (material == null || material.Pointer == IntPtr.Zero)
                return;
            Texture nonHeld = material.GetTexture(NonHeldMapProperty);
            if (nonHeld == null || nonHeld.Pointer == IntPtr.Zero)
                return;
            if (material.HasProperty(HeldMapProperty))
                material.SetTexture(HeldMapProperty, nonHeld);
            if (material.HasProperty(HeldBoostedMapProperty))
                material.SetTexture(
                    HeldBoostedMapProperty, nonHeld);
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
