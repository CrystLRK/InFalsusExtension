using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Independent bomb visuals for the chart-scoped BombPlan.
    ///
    /// The four native TapNote prefabs are used unchanged:
    /// center single, center span, left side, and right side. Root placement,
    /// rotation, scale, and TapNote._ybA pose updates copy the corresponding
    /// native Track.Update branches (case 0/2/4) without touching native notes.
    /// Only the TapNote.meshRenderer material is replaced; side guide renderers
    /// retain their original hierarchy and materials.
    /// </summary>
    internal static class BombSkinRuntime
    {
        private const string FolderName = "InFalsusChartExt";
        private const float SideRootScaleX = 1.08f;
        private static string _folder;

        // ---- 贴图 ----
        private static Texture2D _bombTexture;
        private static long _bombTextureStamp = -1;
        private static Texture2D _defaultTex;
        private static float _nextFileCheck;
        private static int _errorLogs;

        private sealed class VisualSlot
        {
            public GameObject Go;
            public Transform Root;
            public Il2Cppifapp.Game.Scenes.Game.TapNote Note;
            public MeshRenderer MainRenderer;
            public Material Material;
        }

        private struct VisualPose
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public float TapState;
        }

        private sealed class VisualPool
        {
            public GameObject Prefab;
            public readonly List<VisualSlot> Slots =
                new List<VisualSlot>();
            public readonly List<VisualPose> Requests =
                new List<VisualPose>(1024);
        }

        private static readonly VisualPool CenterSinglePool = new VisualPool();
        private static readonly VisualPool CenterSpanPool = new VisualPool();
        private static readonly VisualPool SideLeftPool = new VisualPool();
        private static readonly VisualPool SideRightPool = new VisualPool();
        private static bool _templatesReady;
        private static Transform _parent;
        private static Il2Cppifapp.Game.Scenes.Game.Track.__c _tapPoolCallback;
        private static IntPtr _cachedTrack;
        private static int _chartGeneration = -1;

        private static double _sharedNow;

        internal static double SharedNowMs => _sharedNow;

        internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!BuildProfile.Supports(NativeCapability.BombVisual))
                return;
            try
            {
                BombPlan plan =
                    ChartRuntimeContext.IsActive
                        ? ChartRuntimeContext.Current?.Bombs
                        : null;
                if (plan == null ||
                    plan.NativeMembers.Count == 0) return;
                if (track == null || track.Pointer == IntPtr.Zero) return;
                if (track.Pointer != _cachedTrack ||
                    ChartExtState.ChartGeneration != _chartGeneration)
                {
                    _cachedTrack = track.Pointer;
                    _chartGeneration = ChartExtState.ChartGeneration;
                    ResetForNewTrack();
                }

                if (UnityEngine.Time.realtimeSinceStartup >= _nextFileCheck)
                {
                    _nextFileCheck = UnityEngine.Time.realtimeSinceStartup + 5.0f;
                    RefreshBombTextureFromFile();
                    RefreshPoolMaterials();
                }

                if (!_templatesReady) TryPrepareTemplates(track);
                if (!_templatesReady) return;

                CollectActive(track, plan);
                PresentPool(CenterSinglePool);
                PresentPool(CenterSpanPool);
                PresentPool(SideLeftPool);
                PresentPool(SideRightPool);
            }
            catch (Exception e)
            {
                if (_errorLogs < 20) { _errorLogs++; ChartExtMod.Log.Error("[ChartExt] bomb visual failed: " + e); }
            }
        }

        private static void ResetForNewTrack()
        {
            DestroyPool(CenterSinglePool);
            DestroyPool(CenterSpanPool);
            DestroyPool(SideLeftPool);
            DestroyPool(SideRightPool);
            _templatesReady = false;
            _parent = null;
            _sharedNow = 0;
        }

        internal static void ResetForChartSwitch()
        {
            ResetForNewTrack();
            _cachedTrack = IntPtr.Zero;
            _chartGeneration = -1;
        }

        internal static void DisposeForSceneUnload()
        {
            ResetForChartSwitch();
            try
            {
                if (_bombTexture != null)
                    UnityEngine.Object.Destroy(_bombTexture);
            }
            catch { }
            try
            {
                if (_defaultTex != null)
                    UnityEngine.Object.Destroy(_defaultTex);
            }
            catch { }
            _bombTexture = null;
            _bombTextureStamp = -1;
            _defaultTex = null;
        }

        private static unsafe void CollectActive(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            BombPlan plan)
        {
            CenterSinglePool.Requests.Clear();
            CenterSpanPool.Requests.Clear();
            SideLeftPool.Requests.Clear();
            SideRightPool.Requests.Clear();

            IntPtr t = track.Pointer;
            IntPtr mnA = Marshal.ReadIntPtr(
                t + NativeLayoutCatalog.Track.LaneCenters);
            if (mnA == IntPtr.Zero) return;

            float nnA = ReadFloat(
                t, NativeLayoutCatalog.Track.NoteWidthScale, 0f);
            double baseSpeed = ReadDouble(
                t, NativeLayoutCatalog.Track.BaseSpeed, 0.0);
            double depthFactor = ReadDouble(
                t, NativeLayoutCatalog.Track.DepthFactor, 0.0);
            double s = (baseSpeed / 3.333333334) * depthFactor / 1000.0;
            double absBaseSpeed = Math.Max(Math.Abs(baseSpeed), 1e-6);
            double maxLeadMs =
                VisualSpawnTiming.MaxLeadMs(
                    baseSpeed, depthFactor);

            Quaternion centerRotation = ReadQuaternion(
                t, NativeLayoutCatalog.Track.CenterRotation);
            Vector3 sideLeftBase = new Vector3(
                ReadFloat(t, NativeLayoutCatalog.Track.SideLeftPosition, 0f),
                ReadFloat(t, NativeLayoutCatalog.Track.SideLeftPosition + 4, 0f),
                ReadFloat(t, NativeLayoutCatalog.Track.SideLeftPosition + 8, 0f));
            Quaternion sideLeftRotation = ReadQuaternion(
                t, NativeLayoutCatalog.Track.SideLeftRotation);
            Vector3 sideRightBase = new Vector3(
                ReadFloat(t, NativeLayoutCatalog.Track.SideRightPosition, 0f),
                ReadFloat(t, NativeLayoutCatalog.Track.SideRightPosition + 4, 0f),
                ReadFloat(t, NativeLayoutCatalog.Track.SideRightPosition + 8, 0f));
            Quaternion sideRightRotation = ReadQuaternion(
                t, NativeLayoutCatalog.Track.SideRightRotation);

            Vector3 centerScaleBase = new Vector3(
                ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleX, 1f),
                ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleY, 1f),
                ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleZ, 1f));
            float sideScaleBaseY = ReadFloat(
                t, NativeLayoutCatalog.Track.SideScaleY, 1f);
            float sideScaleBaseZ = ReadFloat(
                t, NativeLayoutCatalog.Track.SideScaleZ, 1f);

            double now = TimingGroupRuntime.CurrentMs();
            if (double.IsNaN(now) ||
                double.IsInfinity(now))
            {
                _sharedNow = 0;
                return;
            }
            _sharedNow = now;

            // Descriptor-driven path: no live slot scan, no SpawnKeysNow and no
            // _Ee reuse. The authored plan owns identity and geometry; native
            // record state is only read to retire a normal bomb after judgement.
            for (int i = 0; i < plan.NativeMembers.Count; i++)
            {
                BombDescriptor bomb = plan.NativeMembers[i];
                if (SceneControlRuntime.IsNoteHidden(bomb.NativeId) ||
                    IsNativeBombFinal(bomb.NativeId))
                    continue;

                double span = bomb.TimingGroup > 0
                    ? TimingGroupRuntime.VisualTimeMs(
                          bomb.TimingGroup,
                          bomb.StartMs) -
                      TimingGroupRuntime.VisualTimeMs(
                          bomb.TimingGroup,
                          now)
                    : WarpedMs(bomb.StartMs) - WarpedMs(now);
                bool nativeSpawned = bomb.NativeSide >= 0 &&
                    bomb.NativePosition >= 0 &&
                    ChartExtState.SpawnKeysNow.Contains(
                        ((long)bomb.NativeSide << 32) |
                        (uint)bomb.NativePosition);
                if (span < 0.0 ||
                    (span > maxLeadMs && !nativeSpawned))
                    continue;

                float x = 0f;
                int kind;
                if (bomb.Lane >= 1 && bomb.Lane <= 4)
                {
                    float laneCenter = BitConverter.Int32BitsToSingle(
                        Marshal.ReadInt32(
                            mnA + NativeLayoutCatalog.ArrayDataOffset +
                            4 * (bomb.Lane - 1)));
                    x = (nnA * 0.5f * 0.25f) *
                        (bomb.Width - 1) + laneCenter;
                    kind = bomb.Width > 1 ? 1 : 0;
                }
                else
                {
                    if (bomb.Lane != 0 && bomb.Lane != 5)
                        continue;
                    kind = bomb.Lane == 0 ? 2 : 3;
                }

                double distance = s * span;
                float normalized =
                    (float)(Math.Abs(distance) / absBaseSpeed);
                float poseT = Mathf.Clamp01((1f - normalized) / 0.1f);
                float tapState = (4f - 2f * poseT) * poseT * poseT;
                var pose = new VisualPose { TapState = tapState };

                switch (kind)
                {
                    case 1:
                        pose.Position = new Vector3(
                            x, 0f, (float)distance);
                        pose.Rotation = centerRotation;
                        pose.Scale = new Vector3(
                            bomb.Width * centerScaleBase.x,
                            2f * normalized + centerScaleBase.y,
                            centerScaleBase.z);
                        CenterSpanPool.Requests.Add(pose);
                        break;
                    case 2:
                        pose.Position = new Vector3(
                            sideLeftBase.x,
                            sideLeftBase.y,
                            sideLeftBase.z + (float)distance);
                        pose.Rotation = sideLeftRotation;
                        pose.Scale = new Vector3(
                            SideRootScaleX,
                            sideScaleBaseY,
                            2f * normalized + sideScaleBaseZ);
                        SideLeftPool.Requests.Add(pose);
                        break;
                    case 3:
                        pose.Position = new Vector3(
                            sideRightBase.x,
                            sideRightBase.y,
                            sideRightBase.z + (float)distance);
                        pose.Rotation = sideRightRotation;
                        pose.Scale = new Vector3(
                            SideRootScaleX,
                            sideScaleBaseY,
                            2f * normalized + sideScaleBaseZ);
                        SideRightPool.Requests.Add(pose);
                        break;
                    default:
                        pose.Position = new Vector3(
                            x, 0f, (float)distance);
                        pose.Rotation = centerRotation;
                        pose.Scale = new Vector3(
                            bomb.Width * centerScaleBase.x,
                            2f * normalized + centerScaleBase.y,
                            centerScaleBase.z);
                        CenterSinglePool.Requests.Add(pose);
                        break;
                }
            }
        }

        private static unsafe bool IsNativeBombFinal(long nativeId)
        {
            try
            {
                Il2Cppifapp.Game.LogicalNotePlayer player =
                    Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null || player.Pointer == IntPtr.Zero)
                    return false;
                Il2Cpp_b._T compiled = player._ve;
                if (compiled == null ||
                    compiled.Pointer == IntPtr.Zero)
                    return false;
                var records = compiled._XC;
                if (records == null ||
                    records.Pointer == IntPtr.Zero ||
                    !records.ContainsKey(nativeId))
                    return false;
                Il2CppSystem.Memory<Il2Cpp_K._cH> memory =
                    records[nativeId];
                if (memory._object == null ||
                    memory._object.Pointer == IntPtr.Zero)
                    return false;
                int count = memory._length;
                if (count <= 0 || count > 100000)
                    return false;
                IntPtr arrayObject = memory._object.Pointer;
                int sliceIndex = memory._index;
                int arrayLength = Marshal.ReadInt32(
                    arrayObject +
                    NativeLayoutCatalog.ArrayLengthOffset);
                if (sliceIndex < 0 ||
                    sliceIndex + count > arrayLength)
                    return false;
                for (int i = 0; i < count; i++)
                {
                    byte* record = InteropUtil.RecordPointer(
                        arrayObject, sliceIndex, i);
                    if (record[InteropUtil.FinalOffset] == 0)
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void PresentPool(VisualPool pool)
        {
            if (pool.Prefab == null || pool.Prefab.Pointer == IntPtr.Zero)
            {
                for (int i = 0; i < pool.Slots.Count; i++)
                {
                    try
                    {
                        if (pool.Slots[i].Go != null &&
                            pool.Slots[i].Go.activeSelf)
                            pool.Slots[i].Go.SetActive(false);
                    }
                    catch { }
                }
                return;
            }

            while (pool.Slots.Count < pool.Requests.Count)
            {
                VisualSlot slot = CreateVisualSlot(pool.Prefab);
                if (slot == null) break;
                pool.Slots.Add(slot);
            }

            for (int i = 0; i < pool.Slots.Count; i++)
            {
                VisualSlot slot = pool.Slots[i];
                if (slot.Go == null || slot.Go.Pointer == IntPtr.Zero)
                    continue;
                try
                {
                    bool used = i < pool.Requests.Count;
                    if (used)
                    {
                        VisualPose pose = pool.Requests[i];
                        if (slot.Root != null &&
                            slot.Root.Pointer != IntPtr.Zero)
                        {
                            slot.Root.localPosition = pose.Position;
                            slot.Root.localRotation = pose.Rotation;
                            slot.Root.localScale = pose.Scale;
                        }
                        if (!slot.Go.activeSelf) slot.Go.SetActive(true);
                        if (slot.Note != null &&
                            slot.Note.Pointer != IntPtr.Zero)
                            slot.Note._ybA(pose.TapState);
                    }
                    else if (slot.Go.activeSelf)
                    {
                        slot.Go.SetActive(false);
                    }
                }
                catch (Exception e)
                {
                    try { slot.Go.SetActive(false); } catch { }
                    if (_errorLogs < 20)
                    {
                        _errorLogs++;
                        ChartExtMod.Log.Error(
                            "[ChartExt] bomb native visual tick failed: " + e);
                    }
                }
            }
        }

        internal static bool TryConfigureStandaloneVisual(
            GameObject gameObject,
            Transform root,
            Il2Cppifapp.Game.Scenes.Game.TapNote note,
            out Material material)
        {
            material = null;
            Material originalMaterial = null;
            Material originalRuntimeMaterial = null;
            Material ownedMaterial = null;
            MeshRenderer main = null;
            try
            {
                if (gameObject == null ||
                    gameObject.Pointer == IntPtr.Zero ||
                    note == null ||
                    note.Pointer == IntPtr.Zero)
                    return false;
                main = note.meshRenderer;
                if (main == null || main.Pointer == IntPtr.Zero)
                    return false;
                Material source = main.sharedMaterial;
                if (source == null || source.Pointer == IntPtr.Zero)
                    return false;
                originalMaterial = source;
                originalRuntimeMaterial = note._bMA;
                ownedMaterial = new Material(source);
                TryHardenMaterial(ownedMaterial);
                main.sharedMaterial = ownedMaterial;
                note._bMA = ownedMaterial;
                ApplyNativeTapPoolVisuals(gameObject, root, note);
                main.forceRenderingOff = false;
                main.enabled = true;
                material = ownedMaterial;
                return true;
            }
            catch (Exception e)
            {
                try { main.sharedMaterial = originalMaterial; } catch { }
                try { note._bMA = originalRuntimeMaterial; } catch { }
                DestroyBombVisualResources(null, ownedMaterial);
                material = null;
                if (_errorLogs < 20)
                {
                    _errorLogs++;
                    ChartExtMod.Log.Error(
                        "[ChartExt] standalone bomb visual failed: " + e);
                }
                return false;
            }
        }

        private static VisualSlot CreateVisualSlot(GameObject prefab)
        {
            GameObject go = null;
            Material material = null;
            try
            {
                if (prefab == null || prefab.Pointer == IntPtr.Zero)
                    return null;
                go = UnityEngine.Object.Instantiate(prefab);
                go.name = "IFCE_BombNative";
                Transform root = go.transform;
                if (_parent != null && _parent.Pointer != IntPtr.Zero)
                    root.SetParent(_parent, false);

                var note = go.GetComponent<
                    Il2Cppifapp.Game.Scenes.Game.TapNote>();
                if (note == null || note.Pointer == IntPtr.Zero)
                {
                    UnityEngine.Object.Destroy(go);
                    return null;
                }

                MeshRenderer main = note.meshRenderer;
                if (main == null || main.Pointer == IntPtr.Zero)
                {
                    UnityEngine.Object.Destroy(go);
                    return null;
                }

                Material source = main.sharedMaterial;
                if (source == null || source.Pointer == IntPtr.Zero)
                {
                    UnityEngine.Object.Destroy(go);
                    return null;
                }

                material = new Material(source);
                TryHardenMaterial(material);
                main.sharedMaterial = material;
                note._bMA = material;
                ApplyNativeTapPoolVisuals(go, root, note);

                // Only the bomb body renderer is ours to force visible. The
                // native Tap pool callback above owns the guide-object state.
                main.forceRenderingOff = false;
                main.enabled = true;

                go.SetActive(false);
                return new VisualSlot
                {
                    Go = go,
                    Root = root,
                    Note = note,
                    MainRenderer = main,
                    Material = material,
                };
            }
            catch (Exception e)
            {
                DestroyBombVisualResources(go, material);
                if (_errorLogs < 20)
                {
                    _errorLogs++;
                    ChartExtMod.Log.Error(
                        "[ChartExt] bomb native visual create failed: " + e);
                }
                return null;
            }
        }

        private static void DestroyBombVisualResources(
            GameObject gameObject,
            Material material)
        {
            try
            {
                if (material != null &&
                    material.Pointer != IntPtr.Zero)
                    UnityEngine.Object.Destroy(material);
            }
            catch { }
            try
            {
                if (gameObject != null &&
                    gameObject.Pointer != IntPtr.Zero)
                    UnityEngine.Object.Destroy(gameObject);
            }
            catch { }
        }

        private static void ApplyNativeTapPoolVisuals(
            GameObject go, Transform root,
            Il2Cppifapp.Game.Scenes.Game.TapNote note)
        {
            if (_tapPoolCallback == null)
                _tapPoolCallback =
                    new Il2Cppifapp.Game.Scenes.Game.Track.__c();
            var tuple = new Il2CppSystem.ValueTuple<
                GameObject, Transform,
                Il2Cppifapp.Game.Scenes.Game.TapNote>(go, root, note);
            _tapPoolCallback._LBA(tuple);
        }

        private static void TryPrepareTemplates(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (CenterSinglePool.Prefab == null)
                CenterSinglePool.Prefab = track.notePrefab;
            if (CenterSpanPool.Prefab == null)
                CenterSpanPool.Prefab = track.noteSpanPrefab;
            if (SideLeftPool.Prefab == null)
                SideLeftPool.Prefab = track.sideNotePrefab;
            if (SideRightPool.Prefab == null)
                SideRightPool.Prefab = track.sideNoteAltPrefab;

            if (_parent == null)
            {
                _parent = track.notesContainer;
                if (_parent == null)
                    _parent = track.transform.Find("NotesContainer");
                if (_parent == null)
                {
                    GameObject found = GameObject.Find("NotesContainer");
                    if (found != null) _parent = found.transform;
                }
            }
            _templatesReady =
                _parent != null &&
                CenterSinglePool.Prefab != null &&
                CenterSpanPool.Prefab != null &&
                SideLeftPool.Prefab != null &&
                SideRightPool.Prefab != null;
        }

        private static void RefreshPoolMaterials()
        {
            RefreshPoolMaterials(CenterSinglePool);
            RefreshPoolMaterials(CenterSpanPool);
            RefreshPoolMaterials(SideLeftPool);
            RefreshPoolMaterials(SideRightPool);
        }

        private static void RefreshPoolMaterials(VisualPool pool)
        {
            for (int i = 0; i < pool.Slots.Count; i++)
                TryHardenMaterial(pool.Slots[i].Material);
        }

        private static void DestroyPool(VisualPool pool)
        {
            for (int i = pool.Slots.Count - 1; i >= 0; i--)
            {
                VisualSlot slot = pool.Slots[i];
                try
                {
                    if (slot.Go != null && slot.Go.Pointer != IntPtr.Zero)
                    {
                        slot.Go.SetActive(false);
                        UnityEngine.Object.Destroy(slot.Go);
                    }
                }
                catch { }
                try
                {
                    if (slot.Material != null &&
                        slot.Material.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(slot.Material);
                }
                catch { }
            }
            pool.Slots.Clear();
            pool.Requests.Clear();
            pool.Prefab = null;
        }

        private static float ReadFloat(IntPtr ptr, int offset, float fallback)
        {
            float value = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(ptr + offset));
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : value;
        }

        private static double ReadDouble(IntPtr ptr, int offset, double fallback)
        {
            double value = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(ptr + offset));
            return double.IsNaN(value) || double.IsInfinity(value)
                ? fallback
                : value;
        }

        private static Quaternion ReadQuaternion(IntPtr ptr, int offset)
        {
            return new Quaternion(
                ReadFloat(ptr, offset, 0f),
                ReadFloat(ptr, offset + 4, 0f),
                ReadFloat(ptr, offset + 8, 0f),
                ReadFloat(ptr, offset + 12, 1f));
        }

        /// <summary>把炸弹贴图写到材质的主纹理上（mainTexture 与常见 URP 属性名
        /// `_BaseMap`/`_MainTex` 都挂一遍，属性不存在时静默跳过），并把主色恢复为白。</summary>
        private static void TryHardenMaterial(Material m)
        {
            if (m == null || m.Pointer == IntPtr.Zero) return;
            try
            {
                Texture2D tex = BombTexture();
                if (tex == null || tex.Pointer == IntPtr.Zero) return;
                try { m.mainTexture = tex; } catch { }
                try { m.SetTexture("_BaseMap", tex); } catch { }
                try { m.SetTexture("_MainTex", tex); } catch { }
                try { if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white); } catch { }
                try { m.color = Color.white; } catch { }
            }
            catch { }
        }

        /// <summary>W(x) = 从 0 到 x 的流速时间积分（ms）。SpeedEvents 为空时等价于 x。</summary>
        private static double WarpedMs(double chartMs)
        {
            var evts = ChartExtState.SpeedEvents;
            if (evts.Count == 0) return chartMs;
            double acc = 0.0, prev = 0.0, m = 1.0;
            for (int i = 0; i < evts.Count; i++)
            {
                double t = evts[i].TimeMs;
                if (chartMs <= t) break;
                acc += (t - prev) * m;
                prev = t;
                m = evts[i].Mult;
            }
            acc += (chartMs - prev) * m;
            return acc;
        }


        /// <summary>現場探针：第一个自绘对象的可见性现场（layer/世界坐标/渲染器 isVisible）。</summary>


        /// <summary>Resources live in Mods/InFalsusChartExt beside the DLL.</summary>
        private static string Folder()
        {
            if (!string.IsNullOrEmpty(_folder)) return _folder;
            try
            {
                string modDir = Path.GetDirectoryName(typeof(ChartExtMod).Assembly.Location);
                _folder = Path.Combine(modDir, FolderName);
            }
            catch
            {
                _folder = FolderName;
            }
            return _folder;
        }

        /// <summary>Loads bomb.png from the resource folder (creating the folder and a
        /// default file when missing); reloads when the file timestamp changes.</summary>
        private static void RefreshBombTextureFromFile()
        {
            try
            {
                string dir = Folder();
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "bomb.png");
                if (!File.Exists(path))
                {
                    WriteDefaultBombFile(path);
                }
                long stamp = File.GetLastWriteTimeUtc(path).Ticks;
                if (_bombTexture != null && _bombTexture.Pointer != IntPtr.Zero && stamp == _bombTextureStamp)
                    return;
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length == 0) return;
                if (!PngLoader.TryDecode(bytes, out int w, out int h, out byte[] rgba))
                {
                    if (_errorLogs < 20)
                    {
                        _errorLogs++;
                        ChartExtMod.Log.Error("[ChartExt] bomb skin: unsupported PNG for " + path +
                                              " (need 8-bit non-interlaced RGB/RGBA/Gray)");
                    }
                    return;
                }
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                GCHandle handle = GCHandle.Alloc(rgba, GCHandleType.Pinned);
                try
                {
                    tex.LoadRawTextureData(handle.AddrOfPinnedObject(), rgba.Length);
                }
                finally
                {
                    handle.Free();
                }
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.filterMode = FilterMode.Bilinear;
                tex.Apply();
                try { tex.name = "IFCE_bomb"; } catch { }
                try
                {
                    if (_bombTexture != null && _bombTexture.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(_bombTexture);
                }
                catch { }
                _bombTexture = tex;
                _bombTextureStamp = stamp;
                ChartExtMod.Log.Msg($"[ChartExt] bomb skin: loaded {path} " +
                                    $"({tex.width}x{tex.height})");
            }
            catch (Exception e)
            {
                if (_errorLogs < 20) { _errorLogs++; ChartExtMod.Log.Error("[ChartExt] bomb skin load failed: " + e); }
            }
        }

        private static void WriteDefaultBombFile(string path)
        {
            try
            {
                Texture2D def = DefaultBombTexture();
                byte[] png = ImageConversion.EncodeToPNG(def);
                if (png != null && png.Length > 0)
                {
                    File.WriteAllBytes(path, png);
                    ChartExtMod.Log.Msg("[ChartExt] bomb skin: wrote default skin to " + path);
                }
            }
            catch (Exception e)
            {
                ChartExtMod.Log.Error("[ChartExt] bomb skin: default write failed: " + e.Message);
            }
        }

        private static Texture2D DefaultBombTexture()
        {
            if (_defaultTex != null && _defaultTex.Pointer != IntPtr.Zero) return _defaultTex;
            const int n = 128;
            byte[] bytes = new byte[n * n * 4];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    bool stripe = ((x + y) & 31) < 16;
                    int i = (y * n + x) * 4;
                    bytes[i] = stripe ? (byte)255 : (byte)26;
                    bytes[i + 1] = stripe ? (byte)200 : (byte)26;
                    bytes[i + 2] = stripe ? (byte)0 : (byte)40;
                    bytes[i + 3] = 255;
                }
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                tex.LoadRawTextureData(handle.AddrOfPinnedObject(), bytes.Length);
            }
            finally
            {
                handle.Free();
            }
            tex.Apply();
            try { tex.name = "IFCE_bomb_default"; } catch { }
            _defaultTex = tex;
            return tex;
        }

        private static Texture2D BombTexture()
        {
            if (_bombTexture == null || _bombTexture.Pointer == IntPtr.Zero)
                RefreshBombTextureFromFile();
            if (_bombTexture == null || _bombTexture.Pointer == IntPtr.Zero)
                return DefaultBombTexture();
            return _bombTexture;
        }
    }

    /// <summary>
    /// Minimal 8-bit non-interlaced PNG decoder (gray/RGB/RGBA, no palette).
    /// The interop `ImageConversion.LoadImage` wrapper is broken in this game build
    /// (references a missing span helper at runtime), so the mod decodes the PNG
    /// itself and uploads pixels through `LoadRawTextureData`, which works.
    /// </summary>
    internal static class PngLoader
    {
        internal static bool TryDecode(byte[] data, out int width, out int height, out byte[] rgba)
        {
            width = 0; height = 0; rgba = null;
            try
            {
                InFalsusChartExt.CustomSong.Assets.DecodedImage decoded =
                    InFalsusChartExt.CustomSong.Assets.PngImage.Decode(
                        data);
                width = decoded.Width;
                height = decoded.Height;
                rgba = decoded.RgbaBottomUp;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
