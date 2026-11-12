using System;
using System.IO;
using System.Runtime.InteropServices;
using HarmonyLib;
using InFalsusChartExt.CustomSong.Assets;
using MelonLoader;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// 游戏内谱面背景的自定义贴图：把与谱面同级的 <c>background.png</c>
    /// （或 <c>&lt;谱名&gt;.background.png</c>；再退到 <c>InFalsusChartExt\background.png</c> 全局文件）
    /// 应用到谱面背景材质上。
    ///
    /// 挂点：<c>GenericGameplayBackground._o(Material)</c> —— 游戏把谱面背景材质
    /// 应用到背景 Quad 的地方；postfix 里把主纹理换成自定义贴图（mainTexture/_BaseMap/_MainTex
    /// 都挂一遍，属性不存在时静默跳过），主色恢复白色。
    /// </summary>
    internal static class BackgroundSkin
    {
        internal static bool Enabled = true;

        private static readonly int OutputQuadOffset =
            NativeLayoutCatalog.Background.OutputQuad;
        private static readonly int ConstrainedMaterialOffset =
            NativeLayoutCatalog.Background.ConstrainedMaterial;

        private static IntPtr _lastMaterial;
        private static string _appliedPath;
        private static long _appliedStamp = -1;
        private static string _loggedMissingFor;
        private static int _hookLogs;
        private static string _lastPath;
        private static long _lastStamp = -1;
        private static Texture2D _texture;
        private static Material _nativeMaterial;
        private static Texture _baseTexture;
        private static IntPtr _baseMaterial;
        private static Color _baseColor = Color.white;
        private static bool _baseColorCaptured;
        private static IntPtr _nativeOriginalMaterial;
        private static int _findAttempts;
        private static int _nextFindFrame;
        private static int _findExhaustedGeneration = -1;
        private static int _resolvedBackgroundGeneration = -1;
        private static string _resolvedBackgroundKey;
        private static string _resolvedBackgroundPath;
        private static bool _backgroundPathResolved;
        private static int _inactiveBackgroundGeneration = -1;
        private static string _inactiveBackgroundKey;
        private static int _logs;
        private static int _errors;

        internal static void Reload()
        {
            RestoreAppliedBackground();
            ReleaseCustomTexture();
            ResetFindState();
            _lastPath = null;
            _lastStamp = -1;
            _baseTexture = null;
            _baseMaterial = IntPtr.Zero;
            _baseColor = Color.white;
            _baseColorCaptured = false;
            _lastMaterial = IntPtr.Zero;
        }

        internal static void SetEnabled(bool enabled)
        {
            if (Enabled == enabled) return;
            Enabled = enabled;
            if (!enabled)
            {
                RestoreAppliedBackground();
                ReleaseCustomTexture();
                ResetFindState();
                _lastMaterial = IntPtr.Zero;
            }
            else
            {
                _lastMaterial = IntPtr.Zero;
                ResetFindState();
            }
        }

        internal static void DisposeForSceneUnload()
        {
            RestoreAppliedBackground();
            VideoBackgroundRuntime.Dispose();
            ReleaseCustomTexture();
            try
            {
                if (_nativeMaterial != null)
                    UnityEngine.Object.Destroy(_nativeMaterial);
            }
            catch { }
            _nativeMaterial = null;
            _nativeOriginalMaterial = IntPtr.Zero;
            _lastMaterial = IntPtr.Zero;
            _baseTexture = null;
            _baseMaterial = IntPtr.Zero;
            _baseColor = Color.white;
            _baseColorCaptured = false;
            _lastPath = null;
            _lastStamp = -1;
            ResetFindState();
        }

        private static void ResetFindState()
        {
            _findAttempts = 0;
            _nextFindFrame = 0;
            _findExhaustedGeneration = -1;
            _resolvedBackgroundGeneration = -1;
            _resolvedBackgroundKey = null;
            _resolvedBackgroundPath = null;
            _backgroundPathResolved = false;
            _inactiveBackgroundGeneration = -1;
            _inactiveBackgroundKey = null;
        }

        internal static string CurrentFile
        {
            get
            {
                try { return FindBackgroundFile(ChartExtState.LastChartName); }
                catch { return null; }
            }
        }

        internal static void Apply(Material materialIn)
        {
            if (!BuildProfile.Supports(NativeCapability.Background))
                return;
            try
            {
                if (BackgroundNativeHook.Installed) return;
                Material material = materialIn;
                if (material == null || material.Pointer == IntPtr.Zero) return;
                if (string.IsNullOrEmpty(
                        ResolveBackgroundPath(
                            ChartExtState.ChartGeneration,
                            ChartExtState.LastChartName)))
                    return;
                _lastMaterial = material.Pointer;
                if (_hookLogs < 3)
                {
                    _hookLogs++;
                    ChartExtMod.Log.Msg($"[ChartExt] background hook fired: material=0x{material.Pointer.ToInt64():X} " +
                                        $"key='{ChartExtState.LastChartName}'");
                }
                TryApply(material);
            }
            catch (Exception e)
            {
                if (_errors < 10) { _errors++; ChartExtMod.Log.Error("[ChartExt] background apply failed: " + e); }
            }
        }

        /// <summary>每帧兜底：背景材质可能在谱面解析（LastChartName 就绪）之前就设置好，
        /// 或切谱后材质被替换；这里用最近的材质重试解析+应用，保证最终一定生效。</summary>
        internal static void Tick()
        {
            if (!BuildProfile.Supports(NativeCapability.Background))
                return;
            if (!Enabled) return;
            int generation = ChartExtState.ChartGeneration;
            string chartKey = ChartExtState.LastChartName;
            string backgroundPath =
                ResolveBackgroundPath(generation, chartKey);
            if (string.IsNullOrEmpty(backgroundPath))
            {
                if (_inactiveBackgroundGeneration != generation ||
                    !string.Equals(
                        _inactiveBackgroundKey,
                        chartKey,
                        StringComparison.Ordinal))
                {
                    _inactiveBackgroundGeneration = generation;
                    _inactiveBackgroundKey = chartKey;
                    VideoBackgroundRuntime.Stop();
                }
                return;
            }
            _inactiveBackgroundGeneration = -1;
            _inactiveBackgroundKey = null;
            if (BackgroundNativeHook.Installed)
            {
                // A successful _LIA clone is already the active background
                // material, even when it is attached to a native background
                // effect rather than GenericGameplayBackground. Scanning
                // again here would re-apply the video texture to unrelated
                // output materials every frame.
                if (_nativeMaterial != null &&
                    _nativeMaterial.Pointer != IntPtr.Zero)
                    return;
                if (HasAttachedNativeMaterial())
                    return;
                if (_lastMaterial == IntPtr.Zero)
                {
                    TryFindBackgroundMaterial();
                    return;
                }
                try
                {
                    var material = new Material(_lastMaterial);
                    if (material != null &&
                        material.Pointer != IntPtr.Zero)
                        TryApply(material);
                }
                catch { }
                return;
            }
            if (_lastMaterial == IntPtr.Zero)
            {
                // Custom-song routes do not always invoke GenericGameplayBackground._o.
                // Locate the already-existing background material instead.
                TryFindBackgroundMaterial();
                return;
            }
            try
            {
                Material material = new Material(_lastMaterial);
                if (material == null || material.Pointer == IntPtr.Zero) return;
                TryApply(material);
            }
            catch
            {
                // 材质可能已销毁
            }
        }

        /// <summary>
        /// Fallback for builds/routes where GenericGameplayBackground._o is not
        /// called (custom-song backgrounds currently take such a route). The song
        /// background material is the only renderer material carrying
        /// <c>_BlueNoiseMap</c>; find it once and feed it into the normal path.
        /// </summary>
        private static void TryFindBackgroundMaterial()
        {
            const int maxAttempts = 180;
            int generation = ChartExtState.ChartGeneration;
            if (_findAttempts >= maxAttempts)
            {
                if (_findExhaustedGeneration != generation)
                {
                    _findExhaustedGeneration = generation;
                    ChartExtMod.Log.Warning(
                        $"[ChartExt] background lookup exhausted after " +
                        $"{maxAttempts} attempts; chart={generation}");
                }
                return;
            }
            int frame = Time.frameCount;
            if (frame < _nextFindFrame) return;
            int backoff = _findAttempts < 10
                ? 1
                : _findAttempts < 30 ? 3 : 6;
            _nextFindFrame = frame + backoff;
            _findAttempts++;
            try
            {
                // Preferred source: the gameplay background component itself.
                // _o(Material) is not invoked on every route (notably custom
                // songs), but the component and its output quad are present.
                try
                {
                    Il2Cpp.GenericGameplayBackground[] components =
                        UnityEngine.Object.FindObjectsOfType<Il2Cpp.GenericGameplayBackground>();
                    if (components != null && components.Length > 0)
                    {
                        Il2Cpp.GenericGameplayBackground component = components[0];
                        if (component != null && component.Pointer != IntPtr.Zero)
                        {
                            IntPtr quadPtr = Marshal.ReadIntPtr(
                                component.Pointer + OutputQuadOffset);
                            if (quadPtr != IntPtr.Zero)
                            {
                                IntPtr materialPtr = Marshal.ReadIntPtr(
                                    quadPtr + ConstrainedMaterialOffset);
                                if (materialPtr != IntPtr.Zero)
                                {
                                    var material = new Material(materialPtr);
                                    _lastMaterial = material.Pointer;
                                    ChartExtMod.Log.Msg(
                                        "[ChartExt] background component: material='" +
                                        material.name + "' shader='" +
                                        (material.shader != null ? material.shader.name : "?") + "'");
                                    TryApply(material);
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (_errors < 10)
                    {
                        _errors++;
                        ChartExtMod.Log.Warning(
                            "[ChartExt] background component probe failed: " + e.Message);
                    }
                }

                // Constrained2D is the actual quad used by both the generic
                // background and the per-song background effects. Its material
                // is cached at +0x150 (the _H property). The proxy type is not
                // generated by Il2CppInterop, so identify it by native class name.
                try
                {
                    MonoBehaviour[] behaviours =
                        UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                    if (behaviours != null)
                    {
                        for (int i = 0; i < behaviours.Length; i++)
                        {
                            MonoBehaviour quad = behaviours[i];
                            if (quad == null || quad.Pointer == IntPtr.Zero)
                                continue;
                            IntPtr klass = Il2CppInterop.Runtime.IL2CPP
                                .il2cpp_object_get_class(quad.Pointer);
                            string className = Marshal.PtrToStringAnsi(
                                Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_name(klass));
                            if (className != "Constrained2D") continue;
                            IntPtr materialPtr = Marshal.ReadIntPtr(
                                quad.Pointer + ConstrainedMaterialOffset);
                            if (materialPtr == IntPtr.Zero) continue;
                            var material = new Material(materialPtr);
                            string materialName = material.name ?? string.Empty;
                            if (!material.HasProperty("_BlueNoiseMap") &&
                                !materialName.EndsWith("BGMaterial", StringComparison.OrdinalIgnoreCase))
                                continue;
                            _lastMaterial = material.Pointer;
                            ChartExtMod.Log.Msg(
                                "[ChartExt] background quad: go='" +
                                quad.gameObject.name + "' material='" + materialName +
                                "' shader='" +
                                (material.shader != null ? material.shader.name : "?") + "'");
                            TryApply(material);
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (_errors < 10)
                    {
                        _errors++;
                        ChartExtMod.Log.Warning(
                            "[ChartExt] background quad probe failed: " + e.Message);
                    }
                }

                // Last-resort structural probe: walk only objects whose names look
                // like gameplay background objects and inspect their native
                // component classes. This covers routes where the proxy type or
                // Renderer lookup is unavailable.
                try
                {
                    GameObject[] objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    if (objects != null)
                    {
                        for (int i = 0; i < objects.Length; i++)
                        {
                            GameObject go = objects[i];
                            if (go == null || go.Pointer == IntPtr.Zero) continue;
                            string goName = go.name ?? string.Empty;
                            bool nameHint =
                                goName.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                goName.IndexOf("Backdrop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                goName.IndexOf("GameplayBG", StringComparison.OrdinalIgnoreCase) >= 0;
                            if (!nameHint) continue;
                            Component[] components = go.GetComponents<Component>();
                            if (components == null) continue;
                            for (int c = 0; c < components.Length; c++)
                            {
                                Component component = components[c];
                                if (component == null || component.Pointer == IntPtr.Zero)
                                    continue;
                                string className = NativeClassName(component);
                                IntPtr quadPtr = IntPtr.Zero;
                                if (className == "Constrained2D")
                                    quadPtr = component.Pointer;
                                else if (!string.IsNullOrEmpty(className) &&
                                         className.IndexOf("GameplayBackground",
                                             StringComparison.OrdinalIgnoreCase) >= 0)
                                    quadPtr = Marshal.ReadIntPtr(
                                        component.Pointer + OutputQuadOffset);
                                if (quadPtr == IntPtr.Zero) continue;
                                IntPtr materialPtr = Marshal.ReadIntPtr(
                                    quadPtr + ConstrainedMaterialOffset);
                                if (materialPtr == IntPtr.Zero) continue;
                                var material = new Material(materialPtr);
                                if (!LooksLikeGameplayBackground(material)) continue;
                                _lastMaterial = material.Pointer;
                                ChartExtMod.Log.Msg(
                                    "[ChartExt] background structural: go='" + goName +
                                    "' cls='" + className + "' material='" + material.name +
                                    "' shader='" +
                                    (material.shader != null ? material.shader.name : "?") + "'");
                                TryApply(material);
                                return;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (_errors < 10)
                    {
                        _errors++;
                        ChartExtMod.Log.Warning(
                            "[ChartExt] background structural probe failed: " + e);
                    }
                }

                Renderer[] renderers =
                    UnityEngine.Object.FindObjectsOfType<Renderer>();
                if (renderers == null) return;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null || renderer.Pointer == IntPtr.Zero)
                        continue;
                    Material material = renderer.sharedMaterial;
                    if (material == null || material.Pointer == IntPtr.Zero)
                        continue;
                    bool isBackground;
                    try
                    {
                        string materialName = material.name ?? string.Empty;
                        isBackground =
                            material.HasProperty("_BlueNoiseMap") ||
                            materialName.EndsWith("BGMaterial", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        isBackground = false;
                    }
                    if (!isBackground) continue;
                    _lastMaterial = material.Pointer;
                    ChartExtMod.Log.Msg(
                        "[ChartExt] background fallback: renderer='" +
                        renderer.gameObject.name + "' material='" + material.name +
                        "' shader='" + (material.shader != null ? material.shader.name : "?") + "'");
                    TryApply(material);
                    return;
                }
            }
            catch (Exception e)
            {
                if (_errors < 10)
                {
                    _errors++;
                    ChartExtMod.Log.Error("[ChartExt] background fallback failed: " + e);
                }
            }
        }

        private static string NativeClassName(Component component)
        {
            try
            {
                if (component == null || component.Pointer == IntPtr.Zero)
                    return string.Empty;
                IntPtr klass = Il2CppInterop.Runtime.IL2CPP
                    .il2cpp_object_get_class(component.Pointer);
                if (klass == IntPtr.Zero) return string.Empty;
                return Marshal.PtrToStringAnsi(
                    Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_name(klass)) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool LooksLikeGameplayBackground(Material material)
        {
            try
            {
                if (material == null || material.Pointer == IntPtr.Zero)
                    return false;
                if (material.HasProperty("_BlueNoiseMap")) return true;
                string name = material.name ?? string.Empty;
                if (name.Equals("background", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("BGMaterial", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".background", StringComparison.OrdinalIgnoreCase))
                    return true;
                Texture main = material.mainTexture;
                int width = main != null && main.Pointer != IntPtr.Zero ? main.width : 0;
                return width >= 512 &&
                       (name.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("bg", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            catch
            {
                return false;
            }
        }

        private static void TryApply(Material material)
        {
            if (!BuildProfile.Supports(NativeCapability.Background))
                return;
            if (!Enabled) return;
            if (_baseMaterial != material.Pointer ||
                (_baseTexture == null && _texture == null))
            {
                _baseMaterial = material.Pointer;
                Texture observed = material.mainTexture;
                // Do not replace the cached source with our own cloned texture.
                if (observed != _texture)
                    _baseTexture = observed;
                try
                {
                    _baseColor = material.color;
                    _baseColorCaptured = true;
                }
                catch { _baseColorCaptured = false; }
            }
            // The game may swap the background texture on a reused material.
            // Keep tracking the real texture while our shadow RT is not active.
            Texture currentMain = material.mainTexture;
            if (currentMain != null && currentMain.Pointer != IntPtr.Zero &&
                currentMain != _texture &&
                currentMain != VideoBackgroundRuntime.CurrentTexture)
            {
                _baseTexture = currentMain;
                try
                {
                    _baseColor = material.color;
                    _baseColorCaptured = true;
                }
                catch { }
            }
            string path = FindBackgroundFile(ChartExtState.LastChartName);
            if (path == null)
            {
                VideoBackgroundRuntime.Stop();
                string key = ChartExtState.LastChartName;
                if (!string.Equals(_loggedMissingFor, key, StringComparison.Ordinal))
                {
                    _loggedMissingFor = key;
                    ChartExtMod.Log.Msg($"[ChartExt] background: no file for key='{key}' " +
                                        $"(expected CustomCharts\\<song>\\background.png)");
                }
                return;
            }
            Texture customTexture = EnsureCustomTexture(path);
            if (customTexture == null) return;
            ApplyBaseTexture(material, customTexture);
            try { if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white); } catch { }
            try { material.color = Color.white; } catch { }
            if (_logs < 6)
            {
                _logs++;
                ChartExtMod.Log.Msg("[ChartExt] background applied to material " +
                                    $"0x{material.Pointer.ToInt64():X}");
            }
        }

        private static Texture EnsureCustomTexture(string path)
        {
            if (path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                Texture video = VideoBackgroundRuntime.Ensure(path);
                if (video != null) return video;
                VideoBackgroundRuntime.Stop();
                return null;
            }

            VideoBackgroundRuntime.Stop();
            long stamp = File.GetLastWriteTimeUtc(path).Ticks;
            if (!string.Equals(path, _appliedPath, StringComparison.OrdinalIgnoreCase) ||
                stamp != _appliedStamp ||
                _texture == null || _texture.Pointer == IntPtr.Zero)
            {
                _lastPath = path;
                _lastStamp = stamp;
                ReleaseCustomTexture();
                byte[] data = File.ReadAllBytes(path);
                DecodedImage png = PngImage.Decode(data);
                Texture2D texture = null;
                try
                {
                    texture = new Texture2D(
                        png.Width, png.Height,
                        TextureFormat.RGBA32, false);
                    unsafe
                    {
                        fixed (byte* bytes = png.RgbaBottomUp)
                            texture.LoadRawTextureData(
                                (IntPtr)bytes,
                                png.RgbaBottomUp.Length);
                    }
                    texture.Apply(false, false);
                    _texture = texture;
                }
                catch
                {
                    try
                    {
                        if (texture != null &&
                            texture.Pointer != IntPtr.Zero)
                            UnityEngine.Object.Destroy(texture);
                    }
                    catch { }
                    throw;
                }
                _appliedPath = path;
                _appliedStamp = stamp;
                ChartExtMod.Log.Msg(
                    $"[ChartExt] background loaded: '{path}' ({texture.width}x{texture.height})");
            }
            return _texture;
        }

        private static void ReleaseCustomTexture()
        {
            if (_texture != null && _texture.Pointer != IntPtr.Zero)
            {
                try { UnityEngine.Object.Destroy(_texture); } catch { }
            }
            _texture = null;
            _appliedPath = null;
            _appliedStamp = -1;
        }

        private static void RestoreAppliedBackground()
        {
            VideoBackgroundRuntime.Stop();
            RestoreNativeMaterial();
            if (_baseMaterial != IntPtr.Zero &&
                _baseTexture != null &&
                _baseTexture.Pointer != IntPtr.Zero)
            {
                try
                {
                    var material = new Material(_baseMaterial);
                    ApplyBaseTexture(material, _baseTexture);
                    if (_baseColorCaptured)
                    {
                        try { material.color = _baseColor; } catch { }
                        try
                        {
                            if (material.HasProperty("_BaseColor"))
                                material.SetColor(
                                    "_BaseColor", _baseColor);
                        }
                        catch { }
                    }
                }
                catch { }
            }
            try
            {
                if (_nativeMaterial != null)
                    UnityEngine.Object.Destroy(_nativeMaterial);
            }
            catch { }
            _nativeMaterial = null;
            _nativeOriginalMaterial = IntPtr.Zero;
            _baseMaterial = IntPtr.Zero;
            _baseTexture = null;
            _baseColor = Color.white;
            _baseColorCaptured = false;
        }

        private static void RestoreNativeMaterial()
        {
            if (_nativeMaterial == null ||
                _nativeMaterial.Pointer == IntPtr.Zero ||
                _nativeOriginalMaterial == IntPtr.Zero)
                return;
            try
            {
                Il2Cpp.GenericGameplayBackground[] components =
                    UnityEngine.Object.FindObjectsOfType<
                        Il2Cpp.GenericGameplayBackground>();
                if (components == null) return;
                for (int i = 0; i < components.Length; i++)
                {
                    Il2Cpp.GenericGameplayBackground component =
                        components[i];
                    if (component == null ||
                        component.Pointer == IntPtr.Zero)
                        continue;
                    IntPtr quad = Marshal.ReadIntPtr(
                        component.Pointer + OutputQuadOffset);
                    if (quad == IntPtr.Zero) continue;
                    IntPtr current = Marshal.ReadIntPtr(
                        quad + ConstrainedMaterialOffset);
                    if (current != _nativeMaterial.Pointer)
                        continue;
                    Marshal.WriteIntPtr(
                        quad + ConstrainedMaterialOffset,
                        _nativeOriginalMaterial);
                }
            }
            catch { }
        }

        /// <summary>
        /// Called by the native background hook after the game has loaded its
        /// own gameplay background material. The clone keeps the exact shader and
        /// renderer parameters the original background expects.
        /// </summary>
        internal static IntPtr CloneNativeBackground(IntPtr gameMaterial, Texture texture)
        {
            if (gameMaterial == IntPtr.Zero || texture == null || texture.Pointer == IntPtr.Zero)
                return IntPtr.Zero;
            IntPtr clone = IntPtr.Zero;
            Material material = null;
            try
            {
                RestoreNativeMaterial();
                IntPtr materialClass = Il2CppInterop.Runtime.IL2CPP.GetIl2CppClass(
                    "UnityEngine.CoreModule.dll", "UnityEngine", "Material");
                if (materialClass == IntPtr.Zero) return IntPtr.Zero;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_class_init(materialClass);
                IntPtr copyConstructor = FindMaterialCopyConstructor(materialClass);
                if (copyConstructor == IntPtr.Zero) return IntPtr.Zero;

                clone = Il2CppInterop.Runtime.IL2CPP.il2cpp_object_new(materialClass);
                if (clone == IntPtr.Zero) return IntPtr.Zero;
                bool copied = true;
                unsafe
                {
                    IntPtr* args = stackalloc IntPtr[1];
                    args[0] = gameMaterial;
                    IntPtr raised = IntPtr.Zero;
                    Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_invoke(
                        copyConstructor, clone, (void**)args, ref raised);
                    copied = raised == IntPtr.Zero;
                }
                if (!copied)
                {
                    try { UnityEngine.Object.Destroy(new Material(clone)); } catch { }
                    clone = IntPtr.Zero;
                    return IntPtr.Zero;
                }

                if (!SetNativeMainTexture(
                        materialClass, clone, texture.Pointer, out string textureError))
                {
                    try { UnityEngine.Object.Destroy(new Material(clone)); } catch { }
                    if (_errors < 10)
                    {
                        _errors++;
                        ChartExtMod.Log.Error(
                            "[ChartExt] background native texture failed: " +
                            textureError);
                    }
                    return IntPtr.Zero;
                }

                material = new Material(clone);
                try { if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white); } catch { }
                try { material.color = Color.white; } catch { }
                _nativeMaterial = material;
                _nativeOriginalMaterial = gameMaterial;
                try
                {
                    ChartExtMod.Log.Msg(
                        $"[ChartExt] background native clone created shader='" +
                        $"{(material.shader != null ? material.shader.name : "?")}' " +
                        $"texture='{texture.width}x{texture.height}'");
                }
                catch { }
                return clone;
            }
            catch (Exception e)
            {
                try
                {
                    if (material != null &&
                        material.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(material);
                    else if (clone != IntPtr.Zero)
                        UnityEngine.Object.Destroy(new Material(clone));
                }
                catch { }
                _nativeMaterial = null;
                _nativeOriginalMaterial = IntPtr.Zero;
                if (_errors < 10)
                {
                    _errors++;
                    ChartExtMod.Log.Error("[ChartExt] background native clone failed: " + e);
                }
                return IntPtr.Zero;
            }
        }

        internal static bool HasAttachedNativeMaterial()
        {
            Material ours = _nativeMaterial;
            if (ours == null || ours.Pointer == IntPtr.Zero) return false;
            try
            {
                Il2Cpp.GenericGameplayBackground[] components =
                    UnityEngine.Object.FindObjectsOfType<Il2Cpp.GenericGameplayBackground>();
                if (components == null) return false;
                for (int i = 0; i < components.Length; i++)
                {
                    Il2Cpp.GenericGameplayBackground component = components[i];
                    if (component == null || component.Pointer == IntPtr.Zero) continue;
                    IntPtr quad = Marshal.ReadIntPtr(
                        component.Pointer + OutputQuadOffset);
                    if (quad == IntPtr.Zero) continue;
                    IntPtr material = Marshal.ReadIntPtr(
                        quad + ConstrainedMaterialOffset);
                    if (material == ours.Pointer) return true;
                }
            }
            catch { }
            return false;
        }

        private static bool SetNativeMainTexture(
            IntPtr materialClass,
            IntPtr material,
            IntPtr texture,
            out string reason)
        {
            reason = null;
            IntPtr setter = FindMaterialMethod(materialClass, "set_mainTexture", 1);
            if (setter == IntPtr.Zero)
            {
                reason = "Material.set_mainTexture was not found";
                return false;
            }
            unsafe
            {
                IntPtr* args = stackalloc IntPtr[1];
                args[0] = texture;
                IntPtr raised = IntPtr.Zero;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_invoke(
                    setter, material, (void**)args, ref raised);
                if (raised != IntPtr.Zero)
                {
                    reason = "Material.set_mainTexture raised a managed exception";
                    return false;
                }
            }
            return true;
        }

        private static IntPtr FindMaterialCopyConstructor(IntPtr materialClass)
        {
            IntPtr method = FindMaterialMethod(materialClass, ".ctor", 1, "Material");
            return method;
        }

        private static IntPtr FindMaterialMethod(
            IntPtr materialClass,
            string wantedName,
            int parameterCount,
            string parameterTypeName = null)
        {
            IntPtr iterator = IntPtr.Zero;
            while (true)
            {
                IntPtr method = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_methods(
                    materialClass, ref iterator);
                if (method == IntPtr.Zero) return IntPtr.Zero;
                string actualName = Marshal.PtrToStringAnsi(
                    Il2CppInterop.Runtime.IL2CPP.il2cpp_method_get_name(method));
                if (!string.Equals(actualName, wantedName, StringComparison.Ordinal))
                    continue;
                if (Il2CppInterop.Runtime.IL2CPP.il2cpp_method_get_param_count(method) !=
                    parameterCount)
                    continue;
                if (parameterTypeName == null) return method;
                IntPtr paramType =
                    Il2CppInterop.Runtime.IL2CPP.il2cpp_method_get_param(method, 0);
                IntPtr paramClass =
                    Il2CppInterop.Runtime.IL2CPP.il2cpp_class_from_type(paramType);
                string actualParameterType = paramClass == IntPtr.Zero
                    ? null
                    : Marshal.PtrToStringAnsi(
                        Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_name(paramClass));
                if (string.Equals(
                        actualParameterType, parameterTypeName,
                        StringComparison.Ordinal))
                    return method;
            }
        }

        internal static Texture NativeTextureForCurrentChart()
        {
            if (!Enabled) return null;
            try
            {
                string path = ResolveBackgroundPath(
                    ChartExtState.ChartGeneration,
                    ChartExtState.LastChartName);
                return string.IsNullOrEmpty(path) ? null : EnsureCustomTexture(path);
            }
            catch (Exception e)
            {
                if (_errors < 10)
                {
                    _errors++;
                    ChartExtMod.Log.Error("[ChartExt] background texture prepare failed: " + e);
                }
                return null;
            }
        }

        private static void ApplyBaseTexture(Material material, Texture texture)
        {
            if (texture == null || material == null) return;
            try { material.mainTexture = texture; } catch { }
            try { material.SetTexture("_BaseMap", texture); } catch { }
            try { material.SetTexture("_MainTex", texture); } catch { }
        }

        private static string ResolveBackgroundPath(
            int generation,
            string chartKey)
        {
            if (_backgroundPathResolved &&
                _resolvedBackgroundGeneration == generation &&
                string.Equals(
                    _resolvedBackgroundKey,
                    chartKey,
                    StringComparison.Ordinal))
                return _resolvedBackgroundPath;

            string path = FindBackgroundFile(chartKey);
            _resolvedBackgroundGeneration = generation;
            _resolvedBackgroundKey = chartKey;
            _resolvedBackgroundPath = path;
            _backgroundPathResolved = true;
            return path;
        }

        /// <summary>
        /// trackdisplay 的黑色背景阴影。
        ///
        /// 不创建任何 Renderer / GameObject。把当前背景贴图和径向柔边 mask
        /// 合成到一张持久 RenderTexture 里，再把背景材质的贴图临时替换成它。
        /// 这样阴影天然位于所有物件下方、背景之上，并且与背景材质同生命周期。
        /// </summary>





        /// <summary>
        /// Returns only a background explicitly assigned by the active song's
        /// config. A folder-local background.png or a global fallback must not
        /// leak into charts that did not request a custom background.
        /// </summary>
        internal static string FindBackgroundFile(string chartKey)
        {
            try
            {
                InFalsusChartExt.CustomSong.Lifecycle.CustomSongRuntime
                    .TryGetChartByAnyKey(
                        chartKey,
                        out InFalsusChartExt.CustomSong.Discovery
                            .CustomChartPlan chart);
                string backgroundPath =
                    chart?.BackgroundPath ??
                    chart?.Song?.BackgroundPath;
                if (!string.IsNullOrWhiteSpace(backgroundPath) &&
                    File.Exists(backgroundPath))
                {
                    return backgroundPath;
                }
            }
            catch { }
            return null;
        }
    }

}
