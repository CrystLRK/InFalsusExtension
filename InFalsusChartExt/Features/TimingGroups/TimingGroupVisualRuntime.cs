using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// timinggroup 独立视觉时间线（tap）。
    ///
    /// 判定音符仍保留原始时间；渲染相把组内 tap 暂时改成 type=6，使原版
    /// Track 不再绘制它。这里用一份原版 TapNote 模板独立生成视觉对象，
    /// 位置、scaleY 和 alpha 都按原版 Track 的公式计算，但当前时间使用该
    /// timinggroup 自己的 W_g(t)。这样不会修改全局 `_IA` 时间线，也不会
    /// 伪造判定时间。
    /// </summary>
    internal static class TimingGroupVisualRuntime
    {
        private const int VisibleType = 6;
        private static readonly Dictionary<long, VisualEntry> Entries =
            new Dictionary<long, VisualEntry>();
        private static IntPtr _track;
        private static GameObject _template;
        private static GameObject _sideLeftTemplate;
        private static GameObject _sideRightTemplate;
        private static List<Material> _templateMaterials;
        private static List<Material> _sideLeftTemplateMaterials;
        private static List<Material> _sideRightTemplateMaterials;
        private static Il2Cppifapp.Game.Scenes.Game.Track.__c
            _tapPoolCallback;
        private static Transform _parent;
        private static float _rootY;
        private static Quaternion _rootRot = Quaternion.identity;
        private static Vector3 _rootScale = Vector3.one;
        private static int _rootLayer;
        private static string _chartKey;
        private static int _chartGeneration = -1;
        private static int _errors;
#if IFCE_DIAGNOSTICS
        private static readonly HashSet<long> DiagnosticEntries =
            new HashSet<long>();
        private static int _diagnosticGeneration = -1;
        private static int _diagnosticLogs;
#endif

        private readonly struct FlipTarget
        {
            internal FlipTarget(IntPtr address, int currentType, int targetType)
            {
                Address = address;
                CurrentType = currentType;
                TargetType = targetType;
            }

            internal IntPtr Address { get; }
            internal int CurrentType { get; }
            internal int TargetType { get; }
        }

        private static readonly List<FlipTarget> FlipTargets =
            new List<FlipTarget>();

        private sealed class VisualEntry
        {
            public GameObject Go;
            public Il2Cppifapp.Game.Scenes.Game.TapNote Note;
            public MeshRenderer Renderer;
            public float RootY;
            public Quaternion RootRot;
            public Vector3 RootScale;
            public readonly List<Material> OwnedMaterials =
                new List<Material>(4);
        }

        internal static void RegisterTap(
            Il2Cppifapp.Game.Scenes.Game.TapNote live)
        {
            if (live == null || live.Pointer == IntPtr.Zero || _template != null)
                return;
            GameObject tpl = null;
            List<Material> materials = null;
            try
            {
                string liveName = "";
                try { liveName = live.gameObject.name; } catch { }
                if (liveName.StartsWith(
                        "IFCE_", StringComparison.Ordinal))
                    return;
                Transform root = live.transform;
                _parent = root.parent;
                _rootY = root.localPosition.y;
                _rootRot = root.localRotation;
                _rootScale = root.localScale;
                _rootLayer = root.gameObject.layer;

                tpl = UnityEngine.Object.Instantiate(root.gameObject);
                tpl.name = "IFCE_TGTapTemplate";
                tpl.SetActive(false);
                materials = CloneMaterials(tpl);
                _template = tpl;
                _templateMaterials = materials;
                materials = null;
            }
            catch (Exception e)
            {
                DestroyMaterials(materials);
                try
                {
                    if (tpl != null)
                        UnityEngine.Object.Destroy(tpl);
                }
                catch { }
                if (_errors++ < 10)
                    ChartExtMod.Log.Error("[ChartExt] timing visual template failed: " +
                                          e.Message);
            }
        }

        internal static void ApplyFlip(bool spawnPhase)
        {
            try
            {
                FlipTargets.Clear();
                var slots = ChartExtState.GroupedSlots;
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s.Type != 1) continue;
                    if (!TimingGroupRuntime.TryResolveGroupedSlot(
                            s, out IntPtr note, out int currentType, true))
                    {
                        // The whole flip is one transaction. If any locked slot
                        // now belongs to another compile/note, do not risk writes
                        // to the remaining slots either.
                        FlipTargets.Clear();
                        LogFlipIdentityMismatch(s);
                        return;
                    }
                    if (ChartExtState.BombIds.Contains(s.Id))
                        continue;
                    int targetType = spawnPhase ? VisibleType : s.Type;
                    if (currentType == targetType) continue;
                    if ((spawnPhase && currentType != s.Type) ||
                        (!spawnPhase && currentType != VisibleType))
                    {
                        FlipTargets.Clear();
                        LogFlipIdentityMismatch(s);
                        return;
                    }
                    FlipTargets.Add(new FlipTarget(
                        note + NativeLayoutCatalog.Note.Type,
                        currentType,
                        targetType));
                }

                int written = 0;
                try
                {
                    for (; written < FlipTargets.Count; written++)
                    {
                        FlipTarget target = FlipTargets[written];
                        Marshal.WriteInt32(target.Address, target.TargetType);
                    }
                }
                catch (Exception e)
                {
                    RollbackFlipWrites(written);
                    if (_errors++ < 20)
                        ChartExtMod.Log.Error(
                            "[ChartExt] timing visual type flip failed and " +
                            "was rolled back: " + e.Message);
                }
            }
            catch (Exception e)
            {
                RollbackFlipWrites(FlipTargets.Count);
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] timing visual flip failed: " + e.Message);
            }
            finally
            {
                FlipTargets.Clear();
            }
        }

        private static void RollbackFlipWrites(int count)
        {
            for (int i = Math.Min(count, FlipTargets.Count) - 1;
                 i >= 0;
                 i--)
            {
                FlipTarget target = FlipTargets[i];
                try
                {
                    if (Marshal.ReadInt32(target.Address) ==
                        target.TargetType)
                        Marshal.WriteInt32(
                            target.Address, target.CurrentType);
                }
                catch { }
            }
        }

        private static void LogFlipIdentityMismatch(
            ChartExtState.GroupSlot slot)
        {
            if (_errors++ >= 20) return;
            ChartExtMod.Log.Warning(
                $"[ChartExt] timing visual flip skipped: " +
                $"side={slot.Side} pos={slot.Position} id={slot.Id} " +
                $"owner/gen changed");
        }

        internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            try
            {
                if (!TimingGroupRuntime.Active ||
                    track == null || track.Pointer == IntPtr.Zero)
                    return;
                string chartKey = ChartExtState.LastChartName ?? "";
                int chartGeneration = ChartExtState.ChartGeneration;
                if (!string.Equals(chartKey, _chartKey, StringComparison.Ordinal) ||
                    chartGeneration != _chartGeneration)
                {
                    _chartKey = chartKey;
                    _chartGeneration = chartGeneration;
                    DestroyEntries();
                    DestroyTemplates();
#if IFCE_DIAGNOSTICS
                    DiagnosticEntries.Clear();
                    _diagnosticGeneration = chartGeneration;
                    _diagnosticLogs = 0;
#endif
                }
                EnsureTapTemplate(track);
                if (_track != track.Pointer)
                {
                    _track = track.Pointer;
                    DestroyEntries();
                }

                if (!VisualTrackFrame.TryCapture(
                        track, out VisualTrackFrame frame) ||
                    !frame.IsValid)
                    return;
                double maxLeadMs = frame.MaxLeadMs;
                double nowMs = frame.NowMs;

                var slots = ChartExtState.GroupedSlots;
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot.Type != 1) continue;
                    if (!TimingGroupRuntime.TryResolveGroupedSlot(
                            slot, out IntPtr note, out _, true))
                        continue;
                    long id = Marshal.ReadInt64(
                        note + NativeLayoutCatalog.Note.Id);
                    if (ChartExtState.BombIds.Contains(id))
                        continue;

                    int be = Marshal.ReadInt32(
                        note + NativeLayoutCatalog.Note.StartMs);
                    int lane = Marshal.ReadInt32(
                        note + NativeLayoutCatalog.Note.Lane);
                    int width = Marshal.ReadInt32(
                        note + NativeLayoutCatalog.Note.PackedStartWidth);
                    if (lane < 0 || lane > 5) continue;
                    double lead = TimingGroupRuntime.VisualTimeMs(slot.Group, be) -
                                  TimingGroupRuntime.VisualTimeMs(slot.Group, nowMs);
                    bool consumed = Marshal.ReadByte(
                        note + NativeLayoutCatalog.Note.Consumed) != 0;
                    bool groupHidden =
                        SceneControlRuntime.IsNoteHidden(id);
                    bool visible = !groupHidden && lead <= maxLeadMs &&
                        !consumed && lead >= 0.0;
                    if (!visible)
                    {
                        if (Entries.TryGetValue(id, out VisualEntry hidden))
                        {
                            try { hidden.Go.SetActive(false); } catch { }
                        }
                        continue;
                    }

                    if (!Entries.TryGetValue(id, out VisualEntry e))
                    {
                        e = CreateEntry(lane);
                        if (e == null) continue;
                        Entries[id] = e;
#if IFCE_DIAGNOSTICS
                        if (_diagnosticGeneration != chartGeneration)
                        {
                            DiagnosticEntries.Clear();
                            _diagnosticGeneration = chartGeneration;
                            _diagnosticLogs = 0;
                        }
                        if (_diagnosticLogs < 64 &&
                            DiagnosticEntries.Add(id))
                        {
                            _diagnosticLogs++;
                            ChartExtMod.Log.Msg(
                                "[timing-tap] " +
                                $"group={slot.Group} lane={lane} " +
                                $"width={width} pose=" +
                                $"{(lane == 0 ? "side-left" : lane == 5 ? "side-right" : "center")}");
                        }
#endif
                    }
                    Transform tr = e.Go.transform;
                    double distance = frame.Distance(lead);
                    float normalized =
                        VisualTapNoteInstance.NormalizedDistance(
                            in frame,
                            distance);
                    if (lane >= 1 && lane <= 4)
                    {
                        tr.localPosition = new Vector3(
                            frame.LaneX(lane, width),
                            0f,
                            (float)distance);
                        tr.localRotation = frame.CenterRotation;
                        tr.localScale = new Vector3(
                            width * frame.CenterScale.x,
                            2f * normalized + frame.CenterScale.y,
                            frame.CenterScale.z);
                    }
                    else if (lane == 0)
                    {
                        tr.localPosition = new Vector3(
                            frame.SideLeftPosition.x,
                            frame.SideLeftPosition.y,
                            frame.SideLeftPosition.z +
                            (float)distance);
                        tr.localRotation = frame.SideLeftRotation;
                        tr.localScale = new Vector3(
                            1.08f,
                            frame.SideScaleY,
                            2f * normalized + frame.SideScaleZ);
                    }
                    else
                    {
                        tr.localPosition = new Vector3(
                            frame.SideRightPosition.x,
                            frame.SideRightPosition.y,
                            frame.SideRightPosition.z +
                            (float)distance);
                        tr.localRotation = frame.SideRightRotation;
                        tr.localScale = new Vector3(
                            1.08f,
                            frame.SideScaleY,
                            2f * normalized + frame.SideScaleZ);
                    }

                    float alpha = frame.Alpha(lead, false);
                    if (e.Note != null && e.Note.Pointer != IntPtr.Zero)
                        e.Note._ybA(alpha);
                    if (!e.Go.activeSelf) e.Go.SetActive(true);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[ChartExt] timing visual tick failed: " +
                                          e.Message);
            }
        }

        internal static void ResetForChartSwitch()
        {
            _chartKey = null;
            _chartGeneration = -1;
            _track = IntPtr.Zero;
            DestroyEntries();
            DestroyTemplates();
        }

        private static VisualEntry CreateEntry(int lane)
        {
            GameObject go = null;
            List<Material> materials = null;
            try
            {
                GameObject source = lane == 0
                    ? _sideLeftTemplate
                    : lane == 5
                        ? _sideRightTemplate
                        : _template;
                if (source == null) return null;
                Transform sourceTransform = source.transform;
                go = UnityEngine.Object.Instantiate(source);
                go.name = "IFCE_TGTap";
                Transform tr = go.transform;
                if (_parent != null && _parent.Pointer != IntPtr.Zero)
                    tr.SetParent(_parent, false);
                tr.localPosition = sourceTransform.localPosition;
                tr.localRotation = sourceTransform.localRotation;
                tr.localScale = sourceTransform.localScale;
                go.layer = source.layer;
                materials = CloneMaterials(go);
                go.SetActive(true);
                var note = go.GetComponent<Il2Cppifapp.Game.Scenes.Game.TapNote>();
                var mr2 = Marshal.ReadIntPtr(
                        note.Pointer +
                        NativeLayoutCatalog.TapNote.MeshRenderer) != IntPtr.Zero
                    ? new MeshRenderer(Marshal.ReadIntPtr(
                        note.Pointer +
                        NativeLayoutCatalog.TapNote.MeshRenderer))
                    : go.GetComponent<MeshRenderer>();
                if (mr2 != null)
                    Marshal.WriteIntPtr(
                        note.Pointer +
                        NativeLayoutCatalog.TapNote.RuntimeMaterial,
                        mr2.material.Pointer);
                ApplyTapGuideSetting(go, tr, note);
                var result = new VisualEntry
                {
                    Go = go, Note = note, Renderer = mr2,
                    RootY = tr.localPosition.y,
                    RootRot = tr.localRotation,
                    RootScale = tr.localScale,
                };
                result.OwnedMaterials.AddRange(materials);
                materials = null;
                return result;
            }
            catch (Exception e)
            {
                DestroyMaterials(materials);
                try
                {
                    if (go != null)
                        UnityEngine.Object.Destroy(go);
                }
                catch { }
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[ChartExt] timing visual create failed: " +
                                          e.Message);
                return null;
            }
        }

        private static GameObject CloneTemplate(
            GameObject prefab, string name,
            out List<Material> materials)
        {
            materials = null;
            if (prefab == null || prefab.Pointer == IntPtr.Zero) return null;
            GameObject tpl = null;
            try
            {
                tpl = UnityEngine.Object.Instantiate(prefab);
                tpl.name = name;
                tpl.SetActive(false);
                materials = CloneMaterials(tpl);
                return tpl;
            }
            catch
            {
                DestroyMaterials(materials);
                materials = null;
                try
                {
                    if (tpl != null)
                        UnityEngine.Object.Destroy(tpl);
                }
                catch { }
                return null;
            }
        }

        private static void EnsureTapTemplate(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            try
            {
                if (_parent == null)
                {
                    try { _parent = track.transform.Find("NotesContainer"); }
                    catch { }
                    if (_parent == null)
                    {
                        var found = GameObject.Find("NotesContainer");
                        if (found != null) _parent = found.transform;
                    }
                }
                if (_template == null)
                {
                    _template = CloneTemplate(
                        track.notePrefab,
                        "IFCE_TGTapTemplate",
                        out _templateMaterials);
                    ApplyTapGuideSetting(
                        _template,
                        _template?.transform,
                        _template?.GetComponent<
                            Il2Cppifapp.Game.Scenes.Game.TapNote>());
                }
                if (_sideLeftTemplate == null)
                {
                    _sideLeftTemplate = CloneTemplate(
                        track.sideNotePrefab,
                        "IFCE_TGTapSideLeftTemplate",
                        out _sideLeftTemplateMaterials);
                    ApplyTapGuideSetting(
                        _sideLeftTemplate,
                        _sideLeftTemplate?.transform,
                        _sideLeftTemplate?.GetComponent<
                            Il2Cppifapp.Game.Scenes.Game.TapNote>());
                }
                if (_sideRightTemplate == null)
                {
                    _sideRightTemplate = CloneTemplate(
                        track.sideNoteAltPrefab,
                        "IFCE_TGTapSideRightTemplate",
                        out _sideRightTemplateMaterials);
                    ApplyTapGuideSetting(
                        _sideRightTemplate,
                        _sideRightTemplate?.transform,
                        _sideRightTemplate?.GetComponent<
                            Il2Cppifapp.Game.Scenes.Game.TapNote>());
                }
                if (_template != null)
                {
                    Transform tr = _template.transform;
                    _rootY = tr.localPosition.y;
                    _rootRot = tr.localRotation;
                    _rootScale = tr.localScale;
                    _rootLayer = _template.layer;
                }
            }
            catch { }
        }

        private static void ApplyTapGuideSetting(
            GameObject gameObject,
            Transform root,
            Il2Cppifapp.Game.Scenes.Game.TapNote note)
        {
            if (gameObject == null || root == null || note == null)
                return;
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

        private static List<Material> CloneMaterials(GameObject root)
        {
            var owned = new List<Material>(4);
            if (root == null || root.Pointer == IntPtr.Zero)
                return owned;
            try
            {
                var mrs =
                    root.GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0;
                     mrs != null && i < mrs.Length;
                     i++)
                {
                    MeshRenderer renderer = mrs[i];
                    if (renderer == null ||
                        renderer.sharedMaterial == null)
                        continue;
                    Material clone = new Material(
                        renderer.sharedMaterial);
                    try
                    {
                        renderer.sharedMaterial = clone;
                        owned.Add(clone);
                    }
                    catch
                    {
                        UnityEngine.Object.Destroy(clone);
                        throw;
                    }
                }
                return owned;
            }
            catch
            {
                DestroyMaterials(owned);
                throw;
            }
        }

        private static void DestroyEntries()
        {
            foreach (var e in Entries.Values)
            {
                DestroyMaterials(e.OwnedMaterials);
                e.OwnedMaterials.Clear();
                try { if (e.Go != null) UnityEngine.Object.Destroy(e.Go); }
                catch { }
            }
            Entries.Clear();
        }

        private static void DestroyTemplates()
        {
            DestroyMaterials(_templateMaterials);
            _templateMaterials = null;
            DestroyMaterials(_sideLeftTemplateMaterials);
            _sideLeftTemplateMaterials = null;
            DestroyMaterials(_sideRightTemplateMaterials);
            _sideRightTemplateMaterials = null;
            try { if (_template != null) UnityEngine.Object.Destroy(_template); }
            catch { }
            try
            {
                if (_sideLeftTemplate != null)
                    UnityEngine.Object.Destroy(_sideLeftTemplate);
            }
            catch { }
            try
            {
                if (_sideRightTemplate != null)
                    UnityEngine.Object.Destroy(_sideRightTemplate);
            }
            catch { }
            _template = null;
            _sideLeftTemplate = null;
            _sideRightTemplate = null;
            _parent = null;
            _rootY = 0f;
            _rootRot = Quaternion.identity;
            _rootScale = Vector3.one;
            _rootLayer = 0;
        }

        private static void DestroyMaterials(
            IReadOnlyList<Material> materials)
        {
            if (materials == null)
                return;
            for (int i = 0; i < materials.Count; i++)
            {
                Material material = materials[i];
                try
                {
                    if (material != null && material.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(material);
                }
                catch
                {
                }
            }
        }
    }

}
