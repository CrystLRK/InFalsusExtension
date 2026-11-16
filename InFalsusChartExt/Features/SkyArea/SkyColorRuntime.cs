using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// setcolor: replaces only the Default colors on a cloned SafeAreaColorDetails.
    /// The original red warning colors, shader, mesh and SkySafeArea._KZ path stay intact.
    /// </summary>
    internal static class SkyColorRuntime
    {
        private static readonly int ColorDetailsOffset =
            NativeLayoutCatalog.SkySafeArea.ColorDetails;
        private static readonly int FirstDefaultColorOffset =
            NativeLayoutCatalog.SafeAreaColorDetails.MainDefault;
        private static readonly int DefaultColorStride =
            NativeLayoutCatalog.SafeAreaColorDetails.ColorStride;
        private static readonly int EdgeGlowRedOffset =
            NativeLayoutCatalog.SafeAreaColorDetails.EdgeGlowRed;
        private static readonly int EdgeCenterRedOffset =
            NativeLayoutCatalog.SafeAreaColorDetails.EdgeCenterRed;

        private static readonly Dictionary<long, Il2Cppifapp.Game.Scenes.Game.SkySafeArea>
            Applied = new Dictionary<long, Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
        private static readonly Dictionary<long, IntPtr> OriginalDetails =
            new Dictionary<long, IntPtr>();
        private static readonly Dictionary<long, uint> AppliedRgba =
            new Dictionary<long, uint>();
        private static readonly Dictionary<long, bool> AppliedEdgeGlow =
            new Dictionary<long, bool>();
        private static readonly Dictionary<(IntPtr Source, uint Rgba, bool EdgeGlow),
            Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails> Clones =
            new Dictionary<(IntPtr, uint, bool),
                Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails>();
        private static readonly Dictionary<long, List<long>> CandidateIdsByGroup =
            new Dictionary<long, List<long>>();
        private static readonly Dictionary<long,
            List<ChartExtState.SkyColorEntry>> ColorEventsByGroup =
            new Dictionary<long, List<ChartExtState.SkyColorEntry>>();
        private sealed class ColorState
        {
            public int Cursor;
            public double LastNow = double.NaN;
            public uint Rgba;
            public bool EdgeGlow = true;
            public bool HasColor;
        }
        private static readonly Dictionary<long, ColorState> ColorStates =
            new Dictionary<long, ColorState>();
        private static readonly List<ColorZone> ActiveZones = new List<ColorZone>();
        private static readonly Dictionary<long, MeshRenderer> ZoneRenderers =
            new Dictionary<long, MeshRenderer>();
        private static readonly List<MeshRenderer> BracketRenderers =
            new List<MeshRenderer>();
        private static readonly HashSet<long> BracketRendererPtrs =
            new HashSet<long>();
        private static readonly float[] SourceColor = new float[4];
        private static readonly float[] OutputColor = new float[4];
        private static Transform _notesContainer;
        private static IntPtr _cursorTransformPtr;
        private static Transform _cursorTransform;
        private static IntPtr _cursorRendererPtr;
        private static MeshRenderer _cursorRenderer;
        private static MaterialPropertyBlock _tintBlock;
        private static MaterialPropertyBlock _clearBlock;
        private static int _baseColorId;
        private static int _colorId;
        private static int _emissionColorId;
        private static bool _propertyIdsReady;
        private static int _bracketLogs;
        private static bool _bracketCacheReady;
        private static int _bracketCacheRefreshFrame;

        private struct ColorZone
        {
            public long Group;
            public uint Rgba;
            public Bounds Bounds;
        }

        private static int _generation = -1;
        private static int _logs;
        private static int _errors;

        internal static void ResetForChartSwitch()
        {
            try
            {
                foreach (KeyValuePair<long, Il2Cppifapp.Game.Scenes.Game.SkySafeArea> item in Applied)
                {
                    try
                    {
                        Il2Cppifapp.Game.Scenes.Game.SkySafeArea area = item.Value;
                        if (area == null || area.Pointer == IntPtr.Zero) continue;
                        if (OriginalDetails.TryGetValue(item.Key, out IntPtr source) &&
                            source != IntPtr.Zero)
                        {
                            Marshal.WriteIntPtr(area.Pointer + ColorDetailsOffset, source);
                        }
                    }
                    catch { }
                }
                foreach (Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails clone in Clones.Values)
                {
                    try
                    {
                        if (clone != null && clone.Pointer != IntPtr.Zero)
                            UnityEngine.Object.Destroy(clone);
                    }
                    catch { }
                }
            }
            finally
            {
                Applied.Clear();
                OriginalDetails.Clear();
                AppliedRgba.Clear();
                AppliedEdgeGlow.Clear();
                Clones.Clear();
                CandidateIdsByGroup.Clear();
                ColorEventsByGroup.Clear();
                ColorStates.Clear();
                ActiveZones.Clear();
                ZoneRenderers.Clear();
                BracketRenderers.Clear();
                BracketRendererPtrs.Clear();
                _notesContainer = null;
                _cursorTransformPtr = IntPtr.Zero;
                _cursorTransform = null;
                _cursorRendererPtr = IntPtr.Zero;
                _cursorRenderer = null;
                _bracketCacheReady = false;
                _bracketCacheRefreshFrame = 0;
                _generation = -1;
            }
        }

        internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!BuildProfile.Supports(NativeCapability.SkyColor))
                return;
            try
            {
                if (ChartExtState.SkyColorEvents.Count == 0 ||
                    ChartExtState.SkyAreaGroupById.Count == 0 ||
                    track == null || track.Pointer == IntPtr.Zero)
                    return;

                if (_generation != ChartExtState.ChartGeneration)
                    BuildCandidates();

                double nowMs = TimingGroupRuntime.CurrentMs();
                if (double.IsNaN(nowMs)) return;
                ActiveZones.Clear();

                foreach (KeyValuePair<long, List<long>> group in CandidateIdsByGroup)
                {
                    if (!TryGetActiveColor(
                            group.Key, nowMs,
                            out uint rgba, out bool edgeGlow))
                        continue;
                    List<long> ids = group.Value;
                    for (int i = 0; i < ids.Count; i++)
                    {
                        long id = ids[i];
                        Il2Cppifapp.Game.Scenes.Game.SkySafeArea area =
                            FindSafeArea(track, id);
                        if (area == null || area.Pointer == IntPtr.Zero) continue;
                        if (!SceneControlRuntime.IsNoteHidden(id))
                            AddActiveZone(id, group.Key, rgba, area);
                        if (AppliedRgba.TryGetValue(id, out uint oldRgba) &&
                            oldRgba == rgba &&
                            AppliedEdgeGlow.TryGetValue(
                                id, out bool oldEdgeGlow) &&
                            oldEdgeGlow == edgeGlow &&
                            Applied.TryGetValue(id, out var oldArea) &&
                            oldArea != null && oldArea.Pointer == area.Pointer)
                            continue;

                        IntPtr original;
                        if (OriginalDetails.TryGetValue(id, out IntPtr recorded) &&
                            recorded != IntPtr.Zero)
                            original = recorded;
                        else
                            original = Marshal.ReadIntPtr(area.Pointer + ColorDetailsOffset);
                        if (original == IntPtr.Zero) continue;

                        var clone = GetClone(original, rgba, edgeGlow);
                        if (clone == null || clone.Pointer == IntPtr.Zero) continue;
                        Marshal.WriteIntPtr(area.Pointer + ColorDetailsOffset, clone.Pointer);
                        Applied[id] = area;
                        OriginalDetails[id] = original;
                        AppliedRgba[id] = rgba;
                        AppliedEdgeGlow[id] = edgeGlow;
                        if (_logs < 12)
                        {
                            _logs++;
                            ChartExtMod.Log.Msg(
                                $"[ChartExt] setcolor id={id} group={group.Key} rgba={rgba:X8}");
                        }
                    }
                }
                ApplyBracketAndCursorTints(track);
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[ChartExt] setcolor tick failed: " + e);
            }
        }

        private static void AddActiveZone(
            long id,
            long group,
            uint rgba,
            Il2Cppifapp.Game.Scenes.Game.SkySafeArea area)
        {
            try
            {
                MeshRenderer renderer = GetZoneRenderer(id, area);
                if (renderer == null) return;
                ActiveZones.Add(new ColorZone
                {
                    Group = group,
                    Rgba = rgba,
                    Bounds = renderer.bounds,
                });
                Transform parent = area.transform.parent;
                if (parent != null &&
                    (_notesContainer == null ||
                     _notesContainer.Pointer != parent.Pointer))
                {
                    _notesContainer = parent;
                    BracketRenderers.Clear();
                    BracketRendererPtrs.Clear();
                    _bracketCacheReady = false;
                    _bracketCacheRefreshFrame = 0;
                }
            }
            catch { }
        }

        private static MeshRenderer GetZoneRenderer(
            long id,
            Il2Cppifapp.Game.Scenes.Game.SkySafeArea area)
        {
            IntPtr rendererPtr = Marshal.ReadIntPtr(
                area.Pointer + NativeLayoutCatalog.SkySafeArea.MeshRenderer);
            if (rendererPtr == IntPtr.Zero)
                return null;
            if (ZoneRenderers.TryGetValue(id, out MeshRenderer cached) &&
                cached != null &&
                cached.Pointer == rendererPtr)
                return cached;
            var renderer = new MeshRenderer(rendererPtr);
            ZoneRenderers[id] = renderer;
            return renderer;
        }

        private static void ApplyBracketAndCursorTints(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!_propertyIdsReady)
            {
                _baseColorId = Shader.PropertyToID("_BaseColor");
                _colorId = Shader.PropertyToID("_Color");
                _emissionColorId = Shader.PropertyToID("_EmissionColor");
                _tintBlock = new MaterialPropertyBlock();
                _clearBlock = new MaterialPropertyBlock();
                _propertyIdsReady = true;
            }
            if (_tintBlock == null || _clearBlock == null) return;

            if (_notesContainer != null &&
                _notesContainer.Pointer != IntPtr.Zero)
            {
                int frame = Time.frameCount;
                if (!_bracketCacheReady ||
                    frame >= _bracketCacheRefreshFrame)
                {
                    RebuildBracketCache();
                    _bracketCacheRefreshFrame = frame + 30;
                }
                int tinted = 0;
                for (int i = 0; i < BracketRenderers.Count; i++)
                {
                    MeshRenderer mr = BracketRenderers[i];
                    if (mr == null || mr.Pointer == IntPtr.Zero) continue;
                    if (TryZoneColorAt(mr.transform.position, out uint rgba))
                    {
                        ApplyTint(mr, rgba);
                        tinted++;
                    }
                    else
                    {
                        try { mr.SetPropertyBlock(_clearBlock); } catch { }
                    }
                }
                if (tinted > 0 && _bracketLogs < 8)
                {
                    _bracketLogs++;
                    ChartExtMod.Log.Msg(
                        $"[ChartExt] setcolor brackets tinted={tinted} zones={ActiveZones.Count}");
                }
            }

            // The sky cursor material supports normal material colors. The shared
            // judgement-line-glow shader only exposes a texture and opacity, so it
            // intentionally stays on its native purple texture.
            try
            {
                IntPtr cursorTransformPtr = Marshal.ReadIntPtr(
                    track.Pointer + NativeLayoutCatalog.Track.SkyCursor);
                IntPtr cursorRendererPtr = Marshal.ReadIntPtr(
                    track.Pointer + NativeLayoutCatalog.Track.SkyCursorRenderer);
                if (cursorTransformPtr != IntPtr.Zero && cursorRendererPtr != IntPtr.Zero)
                {
                    Transform cursorTransform =
                        GetCursorTransform(cursorTransformPtr);
                    MeshRenderer cursorRenderer =
                        GetCursorRenderer(cursorRendererPtr);
                    if (TryZoneColorAt(cursorTransform.position, out uint rgba))
                        ApplyTint(cursorRenderer, rgba);
                    else
                    {
                        try { cursorRenderer.SetPropertyBlock(_clearBlock); } catch { }
                    }
                }
            }
            catch { }
        }

        private static Transform GetCursorTransform(IntPtr pointer)
        {
            if (_cursorTransform == null ||
                _cursorTransformPtr != pointer)
            {
                _cursorTransformPtr = pointer;
                _cursorTransform = new Transform(pointer);
            }
            return _cursorTransform;
        }

        private static MeshRenderer GetCursorRenderer(IntPtr pointer)
        {
            if (_cursorRenderer == null ||
                _cursorRendererPtr != pointer)
            {
                _cursorRendererPtr = pointer;
                _cursorRenderer = new MeshRenderer(pointer);
            }
            return _cursorRenderer;
        }

        private static void RebuildBracketCache()
        {
            BracketRenderers.Clear();
            BracketRendererPtrs.Clear();
            int children;
            try { children = _notesContainer.childCount; }
            catch
            {
                _bracketCacheReady = true;
                return;
            }
            for (int i = 0; i < children; i++)
            {
                Transform child;
                try { child = _notesContainer.GetChild(i); } catch { continue; }
                if (child == null || child.Pointer == IntPtr.Zero) continue;
                string name;
                try { name = child.name; } catch { continue; }
                if (name == null ||
                    name.IndexOf(
                        "SquareBracket",
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
                    if (BracketRendererPtrs.Add(
                            renderer.Pointer.ToInt64()))
                        BracketRenderers.Add(renderer);
                }
            }
            _bracketCacheReady = true;
        }

        private static bool TryZoneColorAt(Vector3 position, out uint rgba)
        {
            rgba = 0;
            float best = float.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                ColorZone zone = ActiveZones[i];
                Bounds b = zone.Bounds;
                float dx = position.x < b.min.x ? b.min.x - position.x :
                           position.x > b.max.x ? position.x - b.max.x : 0f;
                float dz = position.z < b.min.z ? b.min.z - position.z :
                           position.z > b.max.z ? position.z - b.max.z : 0f;
                if (dx > 0.4f || dz > 0.8f) continue;
                float score = dx + dz * 0.25f;
                if (score < best)
                {
                    best = score;
                    bestIndex = i;
                }
            }
            if (bestIndex < 0) return false;
            uint selected = ActiveZones[bestIndex].Rgba;
            // Overlapping different colors are ambiguous; leave the native material.
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                if (i == bestIndex) continue;
                ColorZone other = ActiveZones[i];
                Bounds b = other.Bounds;
                if (position.x < b.min.x - 0.4f || position.x > b.max.x + 0.4f) continue;
                if (position.z < b.min.z - 0.8f || position.z > b.max.z + 0.8f) continue;
                if (other.Rgba != selected) return false;
            }
            rgba = selected;
            return true;
        }

        private static void ApplyTint(MeshRenderer renderer, uint rgba)
        {
            try
            {
                float r = ((rgba >> 24) & 0xFF) / 255f;
                float g = ((rgba >> 16) & 0xFF) / 255f;
                float b = ((rgba >> 8) & 0xFF) / 255f;
                Color baseColor = new Color(r, g, b, 1f);
                Color emission = new Color(r * 1.25f, g * 1.25f, b * 1.25f, 1f);
                _tintBlock.Clear();
                _tintBlock.SetColor(_baseColorId, baseColor);
                _tintBlock.SetColor(_colorId, baseColor);
                _tintBlock.SetColor(_emissionColorId, emission);
                renderer.SetPropertyBlock(_tintBlock);
            }
            catch { }
        }

        private static void BuildCandidates()
        {
            CandidateIdsByGroup.Clear();
            ColorEventsByGroup.Clear();
            ColorStates.Clear();
            SkyBlockPlan blockPlan =
                ChartRuntimeContext.IsActive
                    ? ChartRuntimeContext.Current?.SkyBlocks
                    : null;
            var groups = new HashSet<long>();
            foreach (ChartExtState.SkyColorEntry entry in ChartExtState.SkyColorEvents)
            {
                groups.Add(entry.SkyGroupId);
                if (!ColorEventsByGroup.TryGetValue(
                        entry.SkyGroupId,
                        out List<ChartExtState.SkyColorEntry> timeline))
                    ColorEventsByGroup[entry.SkyGroupId] =
                        timeline = new List<ChartExtState.SkyColorEntry>();
                timeline.Add(entry);
            }
            foreach (KeyValuePair<long, long> item in ChartExtState.SkyAreaGroupById)
            {
                if (!groups.Contains(item.Value)) continue;
                if (blockPlan != null &&
                    blockPlan.IsVisualMember(item.Key))
                    continue;
                if (!CandidateIdsByGroup.TryGetValue(item.Value, out var ids))
                    CandidateIdsByGroup[item.Value] = ids = new List<long>();
                ids.Add(item.Key);
            }
            _generation = ChartExtState.ChartGeneration;
            if (_logs < 12)
            {
                _logs++;
                ChartExtMod.Log.Msg(
                    $"[ChartExt] setcolor prepared: {groups.Count} group(s), " +
                    $"{CandidateIdsByGroup.Count} mapped group(s), " +
                    $"{ChartExtState.SkyAreaGroupById.Count} skyarea id(s)");
            }
        }

        internal static bool TryGetActiveColor(
            long group, double nowMs,
            out uint rgba, out bool edgeGlow)
        {
            rgba = 0;
            edgeGlow = true;
            if (!ColorEventsByGroup.TryGetValue(
                    group, out List<ChartExtState.SkyColorEntry> events))
                return false;
            if (!ColorStates.TryGetValue(group, out ColorState state))
            {
                state = new ColorState();
                ColorStates[group] = state;
            }
            if (!double.IsNaN(state.LastNow) &&
                nowMs + 0.5 < state.LastNow)
            {
                state.Cursor = 0;
                state.HasColor = false;
                state.Rgba = 0;
                state.EdgeGlow = true;
            }
            while (state.Cursor < events.Count &&
                   events[state.Cursor].TimeMs <= nowMs)
            {
                ChartExtState.SkyColorEntry entry =
                    events[state.Cursor++];
                state.Rgba = entry.Rgba;
                state.EdgeGlow = entry.EdgeGlow;
                state.HasColor = true;
            }
            state.LastNow = nowMs;
            rgba = state.Rgba;
            edgeGlow = state.EdgeGlow;
            return state.HasColor && rgba != 0;
        }

        internal static Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails GetClone(
            IntPtr source, uint rgba, bool edgeGlow)
        {
            var key = (source, rgba, edgeGlow);
            if (Clones.TryGetValue(key, out var cached)) return cached;
            Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails clone =
                null;
            try
            {
                var sourceObject =
                    new Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails(source);
                clone = UnityEngine.Object.Instantiate(sourceObject);
                if (clone == null || clone.Pointer == IntPtr.Zero) return null;
                clone.name = $"IFCE_SkyColor_{rgba:X8}";
                ApplyDefaultColors(
                    source, clone.Pointer, rgba, edgeGlow);
                Clones[key] = clone;
                return clone;
            }
            catch (Exception e)
            {
                try
                {
                    if (clone != null && clone.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(clone);
                }
                catch
                {
                }
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[ChartExt] setcolor clone failed: " + e);
                return null;
            }
        }

        private static Il2Cppifapp.Game.Scenes.Game.SkySafeArea FindSafeArea(
            Il2Cppifapp.Game.Scenes.Game.Track track, long id)
        {
            try
            {
                var pool = track._UMA;
                if (pool != null && pool.Pointer != IntPtr.Zero)
                {
                    if (pool._QXA.ContainsKey(id)) return pool._QXA[id].Item4;
                    if (pool._rXA.ContainsKey(id)) return pool._rXA[id].Item4;
                }
                var shadow = track._vMA;
                if (shadow != null && shadow.Pointer != IntPtr.Zero)
                {
                    if (shadow._QXA.ContainsKey(id)) return shadow._QXA[id].Item4;
                    if (shadow._rXA.ContainsKey(id)) return shadow._rXA[id].Item4;
                }
            }
            catch { }
            return null;
        }

        private static void ApplyDefaultColors(
            IntPtr source, IntPtr target, uint rgba, bool edgeGlow)
        {
            float tr = ((rgba >> 24) & 0xFF) / 255f;
            float tg = ((rgba >> 16) & 0xFF) / 255f;
            float tb = ((rgba >> 8) & 0xFF) / 255f;
            float alphaScale = (rgba & 0xFF) / 255f;
            for (int i = 0; i < 4; i++)
            {
                int offset = FirstDefaultColorOffset + i * DefaultColorStride;
                Marshal.Copy(source + offset, SourceColor, 0, 4);
                if (!edgeGlow && (i == 1 || i == 2))
                {
                    Array.Clear(OutputColor, 0, OutputColor.Length);
                    Marshal.Copy(OutputColor, 0, target + offset, 4);
                    Marshal.Copy(
                        OutputColor, 0,
                        target + (i == 1
                            ? EdgeCenterRedOffset
                            : EdgeGlowRedOffset), 4);
                    continue;
                }
                float luminance = 0.2126f * SourceColor[0] +
                                  0.7152f * SourceColor[1] +
                                  0.0722f * SourceColor[2];
                OutputColor[0] = tr * luminance;
                OutputColor[1] = tg * luminance;
                OutputColor[2] = tb * luminance;
                OutputColor[3] = SourceColor[3] * alphaScale;
                Marshal.Copy(OutputColor, 0, target + offset, 4);
            }
        }
    }
}
