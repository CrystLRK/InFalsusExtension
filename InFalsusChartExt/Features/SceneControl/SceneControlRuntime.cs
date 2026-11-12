using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// scenecontrol runtime.
    ///
    /// trackdisplay modulates the native track-body materials toward black, which is
    /// visually equivalent to fading the lanes over a black backing plate while the
    /// note containers, judgement lines, cursor and HUD remain untouched.
    ///
    /// hidegroup removes timinggroup notes only from the spawn/render list. The judge
    /// list and every note record remain untouched, so hit effects, combo and results
    /// still work while the note mesh is hidden.
    /// </summary>
    internal static class SceneControlRuntime
    {
        // Horizontal width of the diamond shadow as a fraction of the screen.
        private const float ScreenShadowWidthScale = 1.15f;
        private const float ScreenShadowBottomY = 0.02f;
        private const float ScreenShadowSpawnGap = 0.01f;
        private const float ScreenShadowSizeScale = 1.2f;
        private const float ScreenShadowCenterYOffset = 0.04f;
        private const float ScreenShadowMaxAlpha = 0.82f;

        private static readonly List<MeshRenderer> TrackRenderers =
            new List<MeshRenderer>();
        private static readonly HashSet<long> TrackRendererPtrs = new HashSet<long>();
        private static readonly HashSet<long> ExcludedRendererPtrs = new HashSet<long>();
        private static readonly List<Transform> ProtectedRoots = new List<Transform>();
        private static readonly Dictionary<long, Material> FadeMaterialBySource =
            new Dictionary<long, Material>();
        private static readonly HashSet<long> FadeMaterialPointers =
            new HashSet<long>();
        private static readonly Dictionary<long, Material> OriginalMaterialByRenderer =
            new Dictionary<long, Material>();
        private static readonly Dictionary<long, float> AppliedRendererAlpha =
            new Dictionary<long, float>();
        private static readonly HashSet<int> HiddenGroups = new HashSet<int>();
        private static readonly List<ChartExtState.SceneControlEntry>
            HideGroupEvents =
                new List<ChartExtState.SceneControlEntry>();
        private sealed class TrackDisplayKeyframe
        {
            public double TimeMs;
            public float ValueAtTime;
            public float Target;
            public int DurationMs;
        }
        private static readonly List<TrackDisplayKeyframe>
            TrackDisplayTimeline =
                new List<TrackDisplayKeyframe>();
        private static readonly List<MeshRenderer>
            MeasureLineRenderers =
                new List<MeshRenderer>();
        private static readonly HashSet<long>
            MeasureLineRendererPtrs =
                new HashSet<long>();
        private static Transform _notesContainer;
        private static MaterialPropertyBlock _tintBlock;
        private static MaterialPropertyBlock _clearBlock;
        private static IntPtr _trackPointer;
        private static int _generation = -1;
        private static bool _hasTrackDisplayEvents;
        private static bool _measureLineCacheReady;
        private static int _measureLineRefreshFrame;
        private static int _hideCursor;
        private static double _hideLastNow = double.NaN;
        private static int _lastPreparedFrame = -1;
        private static int _baseColorId;
        private static int _colorId;
        private static int _emissionColorId;
        private static bool _idsReady;
        private static float _appliedAlpha = -1f;
        private static int _errors;
        private static Mesh _screenShadowMesh;
        private static Material _screenShadowMaterial;
        private static MeshRenderer _screenShadowRenderer;
        private static Transform _screenShadowTransform;
        private static Texture2D _screenShadowTexture;
        private static IntPtr _screenShadowCamera;
        private static int _screenShadowErrors;
        internal static void ResetForChartSwitch()
        {
            ClearTrackTint();
            DestroyScreenShadowResources();
            TrackRenderers.Clear();
            TrackRendererPtrs.Clear();
            ExcludedRendererPtrs.Clear();
            ProtectedRoots.Clear();
            HiddenGroups.Clear();
            HideGroupEvents.Clear();
            TrackDisplayTimeline.Clear();
            MeasureLineRenderers.Clear();
            MeasureLineRendererPtrs.Clear();
            _notesContainer = null;
            _trackPointer = IntPtr.Zero;
            _generation = -1;
            _hasTrackDisplayEvents = false;
            _measureLineCacheReady = false;
            _measureLineRefreshFrame = 0;
            _hideCursor = 0;
            _hideLastNow = double.NaN;
            _lastPreparedFrame = -1;
            _appliedAlpha = -1f;
        }

        internal static void DisposeForSceneUnload()
        {
            ResetForChartSwitch();
        }

        private static void DestroyScreenShadowResources()
        {
            try
            {
                if (_screenShadowRenderer != null)
                    UnityEngine.Object.Destroy(
                        _screenShadowRenderer.gameObject);
            }
            catch { }
            try
            {
                if (_screenShadowMaterial != null)
                    UnityEngine.Object.Destroy(_screenShadowMaterial);
            }
            catch { }
            try
            {
                if (_screenShadowMesh != null)
                    UnityEngine.Object.Destroy(_screenShadowMesh);
            }
            catch { }
            try
            {
                if (_screenShadowTexture != null)
                    UnityEngine.Object.Destroy(_screenShadowTexture);
            }
            catch { }
            _screenShadowRenderer = null;
            _screenShadowTransform = null;
            _screenShadowMaterial = null;
            _screenShadowMesh = null;
            _screenShadowTexture = null;
            _screenShadowCamera = IntPtr.Zero;
        }

        /// <summary>Called before `_hb` builds either the spawn or judge list.</summary>
        internal static void PrepareForFrame(double nowMs)
        {
            if (!BuildProfile.Supports(NativeCapability.SceneControl))
                return;
            try
            {
                if (ChartExtState.SceneControls.Count == 0 || double.IsNaN(nowMs)) return;
                int frame = Time.frameCount;
                if (_lastPreparedFrame == frame) return;
                _lastPreparedFrame = frame;
                EnsureTimeline();
                if (double.IsNaN(_hideLastNow) ||
                    nowMs + 0.5 < _hideLastNow)
                {
                    HiddenGroups.Clear();
                    _hideCursor = 0;
                }
                while (_hideCursor < HideGroupEvents.Count &&
                       HideGroupEvents[_hideCursor].TimeMs <= nowMs)
                {
                    ChartExtState.SceneControlEntry e =
                        HideGroupEvents[_hideCursor++];
                    if (e.Value != 0) HiddenGroups.Add(e.GroupId);
                    else HiddenGroups.Remove(e.GroupId);
                }
                _hideLastNow = nowMs;
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[ChartExt] scenecontrol prepare failed: " + e);
            }
        }

        private static void EnsureTimeline()
        {
            int generation = ChartExtState.ChartGeneration;
            if (_generation == generation) return;

            _generation = generation;
            HiddenGroups.Clear();
            HideGroupEvents.Clear();
            TrackDisplayTimeline.Clear();
            MeasureLineRenderers.Clear();
            MeasureLineRendererPtrs.Clear();
            _hideCursor = 0;
            _hideLastNow = double.NaN;
            _hasTrackDisplayEvents = false;
            _measureLineCacheReady = false;
            _measureLineRefreshFrame = 0;

            float trackValue = 1f;
            float trackTarget = 1f;
            double trackCursorMs = 0.0;
            int trackDurationMs = 0;
            for (int i = 0; i < ChartExtState.SceneControls.Count; i++)
            {
                ChartExtState.SceneControlEntry entry =
                    ChartExtState.SceneControls[i];
                if (entry.Mode == ChartExtState.SceneHideGroup)
                {
                    HideGroupEvents.Add(entry);
                    continue;
                }
                if (entry.Mode != ChartExtState.SceneTrackDisplay)
                    continue;

                _hasTrackDisplayEvents = true;
                trackValue = AdvanceAlpha(
                    trackValue, trackCursorMs, entry.TimeMs,
                    trackDurationMs, trackTarget);
                float nextTarget =
                    Math.Clamp(entry.Value / 255f, 0f, 1f);
                TrackDisplayTimeline.Add(new TrackDisplayKeyframe
                {
                    TimeMs = entry.TimeMs,
                    ValueAtTime = trackValue,
                    Target = nextTarget,
                    DurationMs = Math.Max(0, entry.DurationMs),
                });
                trackCursorMs = entry.TimeMs;
                trackTarget = nextTarget;
                trackDurationMs = Math.Max(0, entry.DurationMs);
            }
        }

        internal static bool IsTimingGroupHidden(int group)
        {
            return HiddenGroups.Count > 0 &&
                   HiddenGroups.Contains(group);
        }

        internal static bool IsNoteHidden(long id)
        {
            return IsTimingGroupHidden(
                ChartExtState.TimingGroupOf(id));
        }

        /// <summary>Visual-only filter for the spawner list. Never call this for the
        /// judge list: hiding a note's mesh must not remove its judgement.</summary>
        internal static void FilterSpawnList(
            Il2CppSystem.Collections.Generic.Dictionary<
                Il2Cpp_b._CA,
                Il2Cppifapp.Game.Common.UnorderedListVal<int>> lists)
        {
            try
            {
                if (HiddenGroups.Count == 0 || lists == null ||
                    lists.Pointer == IntPtr.Zero)
                    return;
                int removed = 0;
                for (int side = 0; side < NativeLayoutCatalog.SideCount; side++)
                {
                    var key = (Il2Cpp_b._CA)side;
                    if (!lists.ContainsKey(key)) continue;
                    var list = lists[key];
                    if (list == null || list.Pointer == IntPtr.Zero) continue;
                    IntPtr lp = list.Pointer;
                    IntPtr arr = Marshal.ReadIntPtr(
                        lp + NativeLayoutCatalog.Runtime.ListItems);
                    int count = Marshal.ReadInt32(
                        lp + NativeLayoutCatalog.Runtime.ListCount);
                    if (arr == IntPtr.Zero || count <= 0) continue;
                    int capacity = Marshal.ReadInt32(
                        arr + NativeLayoutCatalog.ArrayLengthOffset);
                    if (count > capacity) continue;
                    IntPtr data = arr + InteropUtil.ArrayDataOffset;
                    IntPtr basePtr = NativeNoteSlotTable.SlotBase(side);
                    if (basePtr == IntPtr.Zero) continue;
                    int write = 0;
                    for (int read = 0; read < count; read++)
                    {
                        int pos = Marshal.ReadInt32(data + read * 4);
                        if (pos < 0) continue;
                        IntPtr note = basePtr +
                            pos * NativeLayoutCatalog.NoteStride;
                        long id = Marshal.ReadInt64(
                            note + NativeLayoutCatalog.Note.Id);
                        bool remove = IsNoteHidden(id);
                        int noteType = Marshal.ReadInt32(
                            note + NativeLayoutCatalog.Note.Type);
                        if (noteType == 5 &&
                            HideGroupSkyColorRuntime.DebugColorEnabled)
                        {
                            // Default hidegroup behavior removes ordinary
                            // SkyArea visuals from the native spawn path.
                            // The diagnostic blue mode keeps them alive so
                            // group membership can be inspected in-game.
                            remove = false;
                        }
                        if (remove)
                        {
                            removed++;
                            continue;
                        }
                        if (write != read)
                            Marshal.WriteInt32(data + write * 4, pos);
                        write++;
                    }
                    if (write != count)
                        Marshal.WriteInt32(
                            lp + NativeLayoutCatalog.Runtime.ListCount, write);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[ChartExt] scenecontrol spawn filter failed: " + e);
            }
        }

        /// <summary>Applies the trackdisplay state after native Track.Update.</summary>
        internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!BuildProfile.Supports(NativeCapability.SceneControl))
                return;
            try
            {
                if (track == null || track.Pointer == IntPtr.Zero) return;
                EnsureTimeline();
                if (!HasTrackDisplayEvents())
                {
                    if (_appliedAlpha >= 0f) ClearTrackTint();
                    return;
                }
                if (_trackPointer != track.Pointer)
                {
                    ClearTrackTint();
                    _trackPointer = track.Pointer;
                    TrackRenderers.Clear();
                    TrackRendererPtrs.Clear();
                    MeasureLineRenderers.Clear();
                    MeasureLineRendererPtrs.Clear();
                    AppliedRendererAlpha.Clear();
                    _measureLineCacheReady = false;
                    _measureLineRefreshFrame = 0;
                    GatherTrackRenderers(track);
                    IntPtr notesPtr = Marshal.ReadIntPtr(
                        track.Pointer + NativeLayoutCatalog.Track.NotesContainer);
                    _notesContainer = notesPtr == IntPtr.Zero
                        ? null : new Transform(notesPtr);
                }
                double nowMs = TimingGroupRuntime.CurrentMs();
                if (double.IsNaN(nowMs)) return;
                float alpha = EvaluateTrackAlpha(nowMs);
                ApplyTrackAlpha(track, alpha);
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[ChartExt] scenecontrol trackdisplay failed: " + e);
            }
        }

        private static bool HasTrackDisplayEvents()
        {
            return _hasTrackDisplayEvents;
        }

        private static float EvaluateTrackAlpha(double nowMs)
        {
            int lo = 0;
            int hi = TrackDisplayTimeline.Count;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (TrackDisplayTimeline[mid].TimeMs <= nowMs)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            int index = lo - 1;
            if (index < 0) return 1f;
            TrackDisplayKeyframe key = TrackDisplayTimeline[index];
            return AdvanceAlpha(
                key.ValueAtTime, key.TimeMs, nowMs,
                key.DurationMs, key.Target);
        }

        private static float AdvanceAlpha(
            float start, double fromMs, double toMs, int durationMs, float target)
        {
            if (durationMs <= 0 || toMs <= fromMs) return target;
            float k = (float)Math.Max(0.0, Math.Min(1.0, (toMs - fromMs) / durationMs));
            return start + (target - start) * k;
        }

        private static void GatherTrackRenderers(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            ExcludedRendererPtrs.Clear();
            ProtectedRoots.Clear();
            AddProtectedRoot(
                track.Pointer + NativeLayoutCatalog.Track.SkyCursor);
            AddProtectedRoot(
                track.Pointer + NativeLayoutCatalog.Track.SkyGhostCursor);
            AddProtectedRoot(
                track.Pointer + NativeLayoutCatalog.Track.CursorExtrasContainer);
            try
            {
                IntPtr skyGlow = Marshal.ReadIntPtr(
                    track.Pointer +
                    NativeLayoutCatalog.Track.SkyJudgementLineGlowRenderer);
                if (skyGlow != IntPtr.Zero) ExcludedRendererPtrs.Add(skyGlow.ToInt64());
            }
            catch { }

            IntPtr masterPtr = Marshal.ReadIntPtr(
                track.Pointer + NativeLayoutCatalog.Track.MasterScene);
            if (masterPtr != IntPtr.Zero)
            {
                var master = new Transform(masterPtr);
                AddProtectedName(master, "JUDGEMENT_SKY_START_LINE");
                AddProtectedName(master, "JUDGEMENT_SKY_END_LINE");
                AddHierarchy(master, track);
            }
            if (TrackRenderers.Count == 0)
            {
                AddPtr(
                    track.Pointer + NativeLayoutCatalog.Track.LeftLaneRenderer);
                AddPtr(
                    track.Pointer +
                    NativeLayoutCatalog.Track.LeftLaneJudgementLine);
                AddPtr(
                    track.Pointer + NativeLayoutCatalog.Track.RightLaneAnchor);
                AddPtr(
                    track.Pointer + NativeLayoutCatalog.Track.RightLaneRenderer);
                AddPtr(
                    track.Pointer + NativeLayoutCatalog.Track.LeftWallRenderer);
                AddPtr(
                    track.Pointer + NativeLayoutCatalog.Track.RightWallRenderer);
                AddRendererArray(Marshal.ReadIntPtr(
                    track.Pointer + NativeLayoutCatalog.Track.RuntimeRenderers));
                AddRendererArray(Marshal.ReadIntPtr(
                    track.Pointer + NativeLayoutCatalog.Track.LaneRenderers));
            }
        }

        private static void AddHierarchy(
            Transform root, Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            try
            {
                MeshRenderer[] renderers =
                    root.GetComponentsInChildren<MeshRenderer>(true);
                if (renderers == null) return;
                Transform notes = track.notesContainer;
                for (int i = 0; i < renderers.Length; i++)
                {
                    MeshRenderer r = renderers[i];
                    if (r == null || r.Pointer == IntPtr.Zero) continue;
                    if (ExcludedRendererPtrs.Contains(r.Pointer.ToInt64())) continue;
                    bool protectedRenderer = false;
                    for (int p = 0; p < ProtectedRoots.Count; p++)
                    {
                        try
                        {
                            if (r.transform.IsChildOf(ProtectedRoots[p]))
                            {
                                protectedRenderer = true;
                                break;
                            }
                        }
                        catch { }
                    }
                    if (protectedRenderer) continue;
                    if (notes != null && notes.Pointer != IntPtr.Zero)
                    {
                        try
                        {
                            if (r.transform.IsChildOf(notes)) continue;
                        }
                        catch { }
                    }
                    if (TrackRendererPtrs.Add(r.Pointer.ToInt64()))
                        TrackRenderers.Add(r);
                }
            }
            catch { }
        }

        private static void AddProtectedRoot(IntPtr fieldAddress)
        {
            try
            {
                if (fieldAddress == IntPtr.Zero) return;
                // The callers pass the address of the Track field, not the Transform
                // object itself. Reading the field is essential: constructing a
                // Transform wrapper around the field address makes IsChildOf walk
                // arbitrary memory inside the Track object.
                IntPtr transformPtr = Marshal.ReadIntPtr(fieldAddress);
                if (transformPtr == IntPtr.Zero) return;
                ProtectedRoots.Add(new Transform(transformPtr));
            }
            catch { }
        }

        private static void AddProtectedName(Transform master, string name)
        {
            try
            {
                Transform root = master.Find(name);
                if (root != null && root.Pointer != IntPtr.Zero)
                    ProtectedRoots.Add(root);
            }
            catch { }
        }

        private static void AddPtr(IntPtr address)
        {
            try
            {
                IntPtr p = Marshal.ReadIntPtr(address);
                if (p == IntPtr.Zero) return;
                if (TrackRendererPtrs.Add(p.ToInt64()))
                    TrackRenderers.Add(new MeshRenderer(p));
            }
            catch { }
        }

        private static void AddRendererArray(IntPtr arrayObject)
        {
            try
            {
                if (arrayObject == IntPtr.Zero) return;
                int length = Marshal.ReadInt32(
                    arrayObject + NativeLayoutCatalog.ArrayLengthOffset);
                if (length <= 0 || length > 64) return;
                IntPtr data = arrayObject + InteropUtil.ArrayDataOffset;
                for (int i = 0; i < length; i++)
                {
                    IntPtr p = Marshal.ReadIntPtr(data + i * IntPtr.Size);
                    if (p != IntPtr.Zero && TrackRendererPtrs.Add(p.ToInt64()))
                        TrackRenderers.Add(new MeshRenderer(p));
                }
            }
            catch { }
        }

        private static void ApplyTrackAlpha(
            Il2Cppifapp.Game.Scenes.Game.Track track, float alpha)
        {
            if (!_idsReady)
            {
                _baseColorId = Shader.PropertyToID("_BaseColor");
                _colorId = Shader.PropertyToID("_Color");
                _emissionColorId = Shader.PropertyToID("_EmissionColor");
                _tintBlock = new MaterialPropertyBlock();
                _clearBlock = new MaterialPropertyBlock();
                _idsReady = true;
            }
            if (_tintBlock == null || _clearBlock == null) return;
            _appliedAlpha = alpha;
            // A renderer may be activated or receive a new native material
            // after alpha has already reached a steady value. ApplyRenderer's
            // per-renderer/alpha cache keeps unchanged entries cheap, while
            // this loop is still required for late material ownership changes.
            for (int i = 0; i < TrackRenderers.Count; i++)
                ApplyRenderer(TrackRenderers[i], alpha);
            ApplyMeasureLines(alpha);
            UpdateScreenShadow(track, alpha);
        }

        private static void ApplyMeasureLines(float alpha)
        {
            if (_notesContainer == null || _notesContainer.Pointer == IntPtr.Zero) return;
            int frame = Time.frameCount;
            if (!_measureLineCacheReady ||
                frame >= _measureLineRefreshFrame)
            {
                RebuildMeasureLineCache();
                _measureLineRefreshFrame = frame + 60;
            }
            for (int i = 0; i < MeasureLineRenderers.Count; i++)
            {
                MeshRenderer renderer = MeasureLineRenderers[i];
                if (renderer == null || renderer.Pointer == IntPtr.Zero)
                    continue;
                ApplyRenderer(renderer, alpha);
            }
        }

        private static void RebuildMeasureLineCache()
        {
            MeasureLineRenderers.Clear();
            MeasureLineRendererPtrs.Clear();
            int count;
            try { count = _notesContainer.childCount; } catch { return; }
            for (int i = 0; i < count; i++)
            {
                Transform child;
                try { child = _notesContainer.GetChild(i); } catch { continue; }
                if (child == null || child.Pointer == IntPtr.Zero) continue;
                string name;
                try { name = child.name; } catch { continue; }
                if (name == null ||
                    name.IndexOf("MeasureLine",
                        StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                MeshRenderer[] renderers;
                try
                {
                    renderers =
                        child.GetComponentsInChildren<MeshRenderer>(true);
                }
                catch { continue; }
                if (renderers == null) continue;
                for (int r = 0; r < renderers.Length; r++)
                {
                    MeshRenderer renderer = renderers[r];
                    if (renderer == null ||
                        renderer.Pointer == IntPtr.Zero)
                        continue;
                    if (MeasureLineRendererPtrs.Add(
                            renderer.Pointer.ToInt64()))
                        MeasureLineRenderers.Add(renderer);
                }
            }
            _measureLineCacheReady = true;
        }

        private static void ApplyRenderer(MeshRenderer renderer, float alpha)
        {
            if (renderer == null || renderer.Pointer == IntPtr.Zero) return;
            try
            {
                long rendererId = renderer.Pointer.ToInt64();
                Material observed = renderer.sharedMaterial;
                if (observed == null || observed.Pointer == IntPtr.Zero) return;
                bool alreadyFaded =
                    FadeMaterialPointers.Contains(
                        observed.Pointer.ToInt64());
                if (alreadyFaded &&
                    AppliedRendererAlpha.TryGetValue(
                        rendererId, out float previousAlpha) &&
                    Math.Abs(previousAlpha - alpha) < 0.0001f)
                    return;
                Material source;
                if (alreadyFaded)
                {
                    if (!OriginalMaterialByRenderer.TryGetValue(rendererId, out source))
                        return;
                }
                else
                {
                    source = observed;
                    OriginalMaterialByRenderer[rendererId] = source;
                }
                Material faded = GetFadeMaterial(source);
                if (faded == null) return;
                ApplyMaterialAlpha(source, faded, alpha);
                if (renderer.sharedMaterial.Pointer != faded.Pointer)
                    renderer.sharedMaterial = faded;
                AppliedRendererAlpha[rendererId] = alpha;
            }
            catch { }
        }

        private static Material GetFadeMaterial(Material source)
        {
            long ptr = source.Pointer.ToInt64();
            if (FadeMaterialBySource.TryGetValue(ptr, out Material cached))
                return cached;
            try
            {
                var material = new Material(source)
                {
                    name = "IFCE_TrackFade_" + source.name,
                };
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (material.HasProperty(Shader.PropertyToID("_Surface")))
                    material.SetFloat(Shader.PropertyToID("_Surface"), 1f);
                if (material.HasProperty(Shader.PropertyToID("_Blend")))
                    material.SetFloat(Shader.PropertyToID("_Blend"), 0f);
                if (material.HasProperty(Shader.PropertyToID("_SrcBlend")))
                    material.SetFloat(Shader.PropertyToID("_SrcBlend"), 5f);
                if (material.HasProperty(Shader.PropertyToID("_DstBlend")))
                    material.SetFloat(Shader.PropertyToID("_DstBlend"), 10f);
                if (material.HasProperty(Shader.PropertyToID("_ZWrite")))
                    material.SetFloat(Shader.PropertyToID("_ZWrite"), 0f);
                material.renderQueue = 3000;
                FadeMaterialBySource[ptr] = material;
                FadeMaterialPointers.Add(material.Pointer.ToInt64());
                return material;
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[ChartExt] trackdisplay material clone failed: " + e);
                return null;
            }
        }

        private static void ApplyMaterialAlpha(
            Material source, Material faded, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            ApplyAlphaColor(source, faded, _baseColorId, alpha, false);
            ApplyAlphaColor(source, faded, _colorId, alpha, true);
            if (source.HasProperty(_emissionColorId) &&
                faded.HasProperty(_emissionColorId))
            {
                Color emission = source.GetColor(_emissionColorId);
                emission.r *= alpha;
                emission.g *= alpha;
                emission.b *= alpha;
                faded.SetColor(_emissionColorId, emission);
            }
        }

        private static void ApplyAlphaColor(
            Material source, Material faded, int id, float alpha, bool fallbackAlpha)
        {
            if (!source.HasProperty(id) || !faded.HasProperty(id)) return;
            Color color = source.GetColor(id);
            color.a = color.a > 0.0001f || !fallbackAlpha
                ? color.a * alpha
                : alpha;
            faded.SetColor(id, color);
        }

        private static void ClearTrackTint()
        {
            try
            {
                if (_screenShadowMaterial != null)
                    _screenShadowMaterial.color = new Color(1f, 1f, 1f, 0f);
                if (_clearBlock == null) _clearBlock = new MaterialPropertyBlock();
                foreach (KeyValuePair<long, Material> item in OriginalMaterialByRenderer)
                {
                    try
                    {
                        var r = new MeshRenderer(new IntPtr(item.Key));
                        if (r == null || r.Pointer == IntPtr.Zero) continue;
                        if (item.Value != null)
                            r.sharedMaterial = item.Value;
                        r.SetPropertyBlock(_clearBlock);
                        r.forceRenderingOff = false;
                    }
                    catch { }
                }
            }
            catch { }
            DestroyFadeMaterials();
            _appliedAlpha = -1f;
            AppliedRendererAlpha.Clear();
        }

        /// <summary>
        /// Screen-space soft shadow following the native track trapezoid. The
        /// gameplay background is a 3D scene, so a background-texture composite
        /// cannot darken it. This persistent mesh is parented to the camera and
        /// sits behind the transparent track and notes.
        ///
        /// Creation happens only from Tick (a live gameplay frame), never from
        /// ResetForChartSwitch / ClearTrackTint.
        /// </summary>
        private static void UpdateScreenShadow(
            Il2Cppifapp.Game.Scenes.Game.Track track, float alpha)
        {
            try
            {
                Camera camera = Camera.main;
                if (camera != null &&
                    (camera.orthographic || (camera.cullingMask & (1 << 7)) == 0))
                    camera = null;
                if (camera == null)
                    camera = FindGameplayCamera();
                if (camera == null) return;
                float depth = ComputeTrackFarDepth(camera) + 0.75f;
                depth = Mathf.Clamp(depth,
                    camera.nearClipPlane + 0.5f,
                    camera.farClipPlane - 1f);
                EnsureScreenShadow(track, camera, depth);
                if (_screenShadowMesh == null || _screenShadowMaterial == null)
                    return;
                var shadowColor =
                    new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(alpha));
                _screenShadowMaterial.color = shadowColor;
                _screenShadowMaterial.SetColor("_BaseColor", shadowColor);
                float frustumHeight =
                    2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * depth;
                float topY = GetShadowTopY(track, camera);
                topY = Mathf.Clamp(
                    topY, ScreenShadowBottomY + 0.22f, 0.90f);
                float height =
                    (topY - ScreenShadowBottomY) * ScreenShadowSizeScale *
                    frustumHeight;
                // Raise the whole diamond; the enlarged shape may extend past
                // the top and bottom edges of the screen.
                float centerY =
                    (topY + ScreenShadowBottomY) * 0.5f +
                    ScreenShadowCenterYOffset;
                float width =
                    frustumHeight * Mathf.Max(1f, camera.aspect) *
                    ScreenShadowWidthScale * ScreenShadowSizeScale;
                if (_screenShadowTransform == null) return;
                _screenShadowTransform.SetParent(camera.transform, false);
                _screenShadowTransform.localPosition = new Vector3(
                    0f, (centerY - 0.5f) * frustumHeight, depth);
                _screenShadowTransform.localRotation = Quaternion.identity;
                _screenShadowTransform.localScale = new Vector3(width, height, 1f);
            }
            catch (Exception e)
            {
                if (_screenShadowErrors++ < 10)
                    ChartExtMod.Log.Error(
                        "[ChartExt] trackdisplay screen shadow failed: " + e);
            }
        }

        /// <summary>
        /// The note spawn edge is the far end of the native track. Put the top
        /// of the diamond slightly below the lowest of its three endpoint markers.
        /// </summary>
        private static float GetShadowTopY(
            Il2Cppifapp.Game.Scenes.Game.Track track, Camera camera)
        {
            float spawnY = 0.86f;
            try
            {
                IntPtr masterPtr = Marshal.ReadIntPtr(
                    track.Pointer + NativeLayoutCatalog.Track.MasterScene);
                if (masterPtr != IntPtr.Zero)
                {
                    Transform master = new Transform(masterPtr);
                    Transform floorEnd = master.Find("TRACK_FLOOR_END");
                    Transform leftEnd = master.Find("TRACK_LEFT_END");
                    Transform rightEnd = master.Find("TRACK_RIGHT_END");
                    bool found = false;
                    float lowest = 1f;
                    SampleTrackEndY(camera, floorEnd, ref found, ref lowest);
                    SampleTrackEndY(camera, leftEnd, ref found, ref lowest);
                    SampleTrackEndY(camera, rightEnd, ref found, ref lowest);
                    if (found) spawnY = lowest;
                }
            }
            catch { }
            return spawnY - ScreenShadowSpawnGap;
        }

        private static void SampleTrackEndY(
            Camera camera, Transform point, ref bool found, ref float lowest)
        {
            try
            {
                if (point == null || point.Pointer == IntPtr.Zero) return;
                Vector3 viewport = camera.WorldToViewportPoint(point.position);
                if (viewport.z <= 0f) return;
                if (viewport.y < 0.45f || viewport.y > 1.05f) return;
                if (viewport.y < lowest) lowest = viewport.y;
                found = true;
            }
            catch { }
        }

        private static Camera FindGameplayCamera()
        {
            try
            {
                Camera[] cameras = Camera.allCameras;
                if (cameras == null) return null;
                Camera best = null;
                Camera bestPerspective = null;
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera camera = cameras[i];
                    if (camera == null || !camera.isActiveAndEnabled) continue;
                    if (camera.cameraType != CameraType.Game) continue;
                    if (camera.orthographic) continue;
                    if (bestPerspective == null ||
                        camera.farClipPlane > bestPerspective.farClipPlane)
                        bestPerspective = camera;
                    // The gameplay camera renders the note layer (7).
                    if ((camera.cullingMask & (1 << 7)) == 0) continue;
                    if (best == null || camera.depth > best.depth)
                        best = camera;
                }
                return best ?? bestPerspective;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Distance from the camera to the farthest point of the native track
        /// meshes along the camera forward axis. The shadow is placed just past
        /// this distance so it stays behind every lane/note and in front of the
        /// 3D background.
        /// </summary>
        private static float ComputeTrackFarDepth(Camera camera)
        {
            float far = 8f;
            try
            {
                Vector3 cameraPosition = camera.transform.position;
                Vector3 forward = camera.transform.forward;
                for (int i = 0; i < TrackRenderers.Count; i++)
                {
                    MeshRenderer renderer = TrackRenderers[i];
                    if (renderer == null || renderer.Pointer == IntPtr.Zero)
                        continue;
                    Bounds bounds = renderer.bounds;
                    Vector3 center = bounds.center;
                    Vector3 extents = bounds.extents;
                    for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + new Vector3(
                            extents.x * x, extents.y * y, extents.z * z);
                        float distance = Vector3.Dot(corner - cameraPosition, forward);
                        if (distance > far) far = distance;
                    }
                }
            }
            catch { }
            return far;
        }

        private static void EnsureScreenShadow(
            Il2Cppifapp.Game.Scenes.Game.Track track, Camera camera, float depth)
        {
            if (_screenShadowTexture == null)
                _screenShadowTexture = CreateScreenShadowTexture(256);
            if (_screenShadowMesh == null)
            {
                // Direct Graphics.DrawMesh submission: no GameObject, no
                // Renderer, and no dependence on the gameplay camera's custom
                // renderer deciding whether to submit a sprite/sprite pass.
                var mesh = new Mesh
                {
                    name = "IFCE_TrackDisplayShadowQuad",
                };
                mesh.vertices = new Il2CppInterop.Runtime.InteropTypes.Arrays
                    .Il2CppStructArray<Vector3>(new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3( 0.5f, -0.5f, 0f),
                    new Vector3( 0.5f,  0.5f, 0f),
                    new Vector3(-0.5f,  0.5f, 0f),
                });
                mesh.uv = new Il2CppInterop.Runtime.InteropTypes.Arrays
                    .Il2CppStructArray<Vector2>(new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                });
                mesh.triangles = new Il2CppInterop.Runtime.InteropTypes.Arrays
                    .Il2CppStructArray<int>(new[] { 0, 2, 1, 0, 3, 2 });
                mesh.RecalculateBounds();
                _screenShadowMesh = mesh;
            }
            if (_screenShadowMaterial == null ||
                _screenShadowMaterial.Pointer == IntPtr.Zero ||
                _screenShadowMaterial.shader == null ||
                _screenShadowMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                _screenShadowMaterial = CreateScreenShadowMaterial(track);
                if (_screenShadowMaterial == null) return;
            }
            if (_screenShadowRenderer == null && _screenShadowMesh != null)
            {
                var go = new GameObject("IFCE_TrackDisplayShadow");
                go.layer = 7;
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = _screenShadowMesh;
                filter.mesh = _screenShadowMesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _screenShadowMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                if (TrackRenderers.Count > 0 && TrackRenderers[0] != null)
                    renderer.renderingLayerMask = TrackRenderers[0].renderingLayerMask;
                renderer.enabled = true;
                _screenShadowRenderer = renderer;
                _screenShadowTransform = go.transform;
            }
            else if (_screenShadowRenderer != null)
            {
                _screenShadowRenderer.sharedMaterial = _screenShadowMaterial;
            }
            _screenShadowCamera = camera.Pointer;
        }

        private static Material CreateScreenShadowMaterial(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            Material source = null;
            try
            {
                source = FindShadowSourceMaterial(track.flickNoteShadowPrefab);
                if (source == null)
                    source = FindShadowSourceMaterial(track.safeAreaShadowPrefab);
            }
            catch { }

            Shader shader = source != null ? source.shader : null;
            if (shader == null)
                shader = Shader.Find("Unlit/StenciledUnlitSprite");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return null;

            Material material = source != null
                ? new Material(source)
                : new Material(shader);
            material.name = "IFCE_TrackDisplayShadowMaterial";
            material.mainTexture = _screenShadowTexture;
            material.SetTexture("_MainTex", _screenShadowTexture);
            material.SetTexture("_BaseMap", _screenShadowTexture);
            material.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
            material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));
            material.SetFloat("_Stencil", 0f);
            material.SetFloat("_ZTest", 8f);  // CompareFunction.Always
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_SrcBlend", 5f); // SrcAlpha
            material.SetFloat("_DstBlend", 10f); // OneMinusSrcAlpha
            // The native shadow shader is part of the gameplay renderer's
            // shader set. It is drawn with the other transparent gameplay
            // geometry, then depth-sorted behind the nearer notes.
            material.renderQueue = 3000;
            return material;
        }

        private static Material FindShadowSourceMaterial(GameObject prefab)
        {
            if (prefab == null || prefab.Pointer == IntPtr.Zero) return null;
            var pending = new Stack<Transform>();
            pending.Push(prefab.transform);
            int guard = 0;
            while (pending.Count > 0 && guard++ < 64)
            {
                Transform current = pending.Pop();
                if (current == null || current.Pointer == IntPtr.Zero) continue;
                GameObject go = current.gameObject;
                if (go == null || go.Pointer == IntPtr.Zero) continue;
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.Pointer != IntPtr.Zero)
                {
                    Material material = renderer.sharedMaterial;
                    if (material != null && material.Pointer != IntPtr.Zero &&
                        material.shader != null)
                        return material;
                }
                int children = 0;
                try { children = current.childCount; } catch { }
                for (int i = children - 1; i >= 0; i--)
                {
                    try { pending.Push(current.GetChild(i)); } catch { }
                }
            }
            return null;
        }

        private static Texture2D CreateScreenShadowTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "IFCE_TrackDisplayDiamondMask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var colors = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float ny = ((y + 0.5f) / size) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = ((x + 0.5f) / size) * 2f - 1f;
                    // Manhattan distance turns the square mask into a diamond.
                    float d = Math.Abs(nx) + Math.Abs(ny);
                    // Same opacity and feather as the previous radial mask.
                    float a =
                        ScreenShadowMaxAlpha * (1f - SmoothStep(0.30f, 1f, d));
                    colors[y * size + x] =
                        new Color32(0, 0, 0, (byte)Math.Round(a * 255f));
                }
            }
            texture.SetPixels32(colors);
            texture.Apply(false, true);
            return texture;
        }

        private static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static void DestroyFadeMaterials()
        {
            foreach (Material material in FadeMaterialBySource.Values)
            {
                try
                {
                    if (material != null && material.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(material);
                }
                catch { }
            }
            FadeMaterialBySource.Clear();
            FadeMaterialPointers.Clear();
            OriginalMaterialByRenderer.Clear();
            AppliedRendererAlpha.Clear();
        }
    }

}
