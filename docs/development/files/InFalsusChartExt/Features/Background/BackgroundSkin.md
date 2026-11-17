# BackgroundSkin.cs

> 源码：`InFalsusChartExt/Features/Background/BackgroundSkin.cs`
> SHA256：`B69BE1B7E219A4B4FC69A38364B78BCEB86506859460F2FC38805172B38F81DC`
> 代码行数：1027
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

背景材质解析、图片/视频贴图、fallback

## 生命周期

谱面载入、每个游戏帧和场景切换；退出谱面时释放自有资源。

## 顶层及类型声明

- `BackgroundSkin`（`class`，源码第 19 行）

## 方法/属性职责

- `Reload`（第 57 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Reload()`
- `SetEnabled`（第 73 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void SetEnabled(bool enabled)`
- `DisposeForSceneUnload`（第 95 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void DisposeForSceneUnload()`
- `ResetFindState`（第 121 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ResetFindState()`
- `Apply`（第 143 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void Apply(Material materialIn)`
- `Tick`（第 174 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `TryFindBackgroundMaterial`（第 251 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static void TryFindBackgroundMaterial()`
- `NativeClassName`（第 473 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string NativeClassName(Component component)`
- `LooksLikeGameplayBackground`（第 491 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool LooksLikeGameplayBackground(Material material)`
- `TryApply`（第 515 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static void TryApply(Material material)`
- `EnsureCustomTexture`（第 576 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Texture EnsureCustomTexture(string path)`
- `ReleaseCustomTexture`（第 632 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseCustomTexture()`
- `RestoreAppliedBackground`（第 643 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RestoreAppliedBackground()`
- `RestoreNativeMaterial`（第 684 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RestoreNativeMaterial()`
- `CloneNativeBackground`（第 712 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static IntPtr CloneNativeBackground(IntPtr gameMaterial, Texture texture)`
- `HasAttachedNativeMaterial`（第 800 行）：判断当前对象/状态条件。
  - 签名：`internal static bool HasAttachedNativeMaterial()`
- `TryFindNativeMaterialOwner`（第 808 行）：通过 `Constrained2D` 的 material 字段查找并缓存当前持有 Mod 背景 clone 的真实 owner；恢复前必须校验 clone identity。
  - 签名：`private static bool TryFindNativeMaterialOwner(`
- `SetNativeMainTexture`（第 855 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool SetNativeMainTexture(`
- `FindMaterialCopyConstructor`（第 884 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr FindMaterialCopyConstructor(IntPtr materialClass)`
- `FindMaterialMethod`（第 890 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr FindMaterialMethod(`
- `NativeTextureForCurrentChart`（第 925 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static Texture NativeTextureForCurrentChart()`
- `ApplyBaseTexture`（第 946 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyBaseTexture(Material material, Texture texture)`
- `GetLastMaterialView`（第 954 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Material GetLastMaterialView()`
- `ResolveBackgroundPath`（第 967 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string ResolveBackgroundPath(`
- `FindBackgroundFile`（第 1004 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static string FindBackgroundFile(string chartKey)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 24 行：`NativeLayoutCatalog.Background.OutputQuad;`
- 第 26 行：`NativeLayoutCatalog.Background.ConstrainedMaterial;`
- 第 149 行：`if (BackgroundNativeHook.Installed) return;`
- 第 199 行：`if (BackgroundNativeHook.Installed)`
- 第 287 行：`IntPtr quadPtr = Marshal.ReadIntPtr(`
- 第 291 行：`IntPtr materialPtr = Marshal.ReadIntPtr(`
- 第 334 行：`.il2cpp_object_get_class(quad.Pointer);`
- 第 335 行：`string className = Marshal.PtrToStringAnsi(`
- 第 336 行：`Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_name(klass));`
- 第 338 行：`IntPtr materialPtr = Marshal.ReadIntPtr(`
- 第 400 行：`quadPtr = Marshal.ReadIntPtr(`
- 第 403 行：`IntPtr materialPtr = Marshal.ReadIntPtr(`
- 第 480 行：`.il2cpp_object_get_class(component.Pointer);`
- 第 482 行：`return Marshal.PtrToStringAnsi(`
- 第 483 行：`Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_name(klass)) ?? string.Empty;`
- 第 695 行：`Marshal.ReadIntPtr(`
- 第 698 行：`Marshal.WriteIntPtr(`
- 第 721 行：`IntPtr materialClass = Il2CppInterop.Runtime.IL2CPP.GetIl2CppClass(`
- 第 724 行：`Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_class_init(materialClass);`
- 第 728 行：`clone = Il2CppInterop.Runtime.IL2CPP.il2cpp_object_new(materialClass);`
- 第 736 行：`Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_invoke(`
- 第 817 行：`Marshal.ReadIntPtr(`
- 第 839 行：`if (Marshal.ReadIntPtr(`
- 第 873 行：`Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_invoke(`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 60 行：`ReleaseCustomTexture();`
- 第 80 行：`ReleaseCustomTexture();`
- 第 95 行：`internal static void DisposeForSceneUnload()`
- 第 98 行：`VideoBackgroundRuntime.Dispose();`
- 第 99 行：`ReleaseCustomTexture();`
- 第 103 行：`UnityEngine.Object.Destroy(_nativeMaterial);`
- 第 594 行：`ReleaseCustomTexture();`
- 第 619 行：`UnityEngine.Object.Destroy(texture);`
- 第 632 行：`private static void ReleaseCustomTexture()`
- 第 636 行：`try { UnityEngine.Object.Destroy(_texture); } catch { }`
- 第 672 行：`UnityEngine.Object.Destroy(_nativeMaterial);`
- 第 742 行：`try { UnityEngine.Object.Destroy(new Material(clone)); } catch { }`
- 第 750 行：`try { UnityEngine.Object.Destroy(new Material(clone)); } catch { }`
- 第 783 行：`UnityEngine.Object.Destroy(material);`
- 第 785 行：`UnityEngine.Object.Destroy(new Material(clone));`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- NativeHookLifecycleHarness
- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
