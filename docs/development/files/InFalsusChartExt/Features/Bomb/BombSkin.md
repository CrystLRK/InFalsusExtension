# BombSkin.cs

> 源码：`InFalsusChartExt/Features/Bomb/BombSkin.cs`
> SHA256：`A8B7E8F377F446DE8C7E5BEAEC48C9A6483A683B56F258BBC0EF8D5D8082B18B`
> 代码行数：884
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

Bomb 池、prefab、贴图、独立视觉

## 生命周期

谱面载入建立计划，判定与渲染阶段消费，切谱回收。

## 顶层及类型声明

- `BombSkinRuntime`（`class`，源码第 19 行）
- `VisualSlot`（`class`，源码第 32 行）
- `VisualPose`（`struct`，源码第 41 行）
- `VisualPool`（`class`，源码第 49 行）
- `PngLoader`（`class`，源码第 863 行）

## 方法/属性职责

- `Tick`（第 72 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `ResetForNewTrack`（第 115 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ResetForNewTrack()`
- `ResetForChartSwitch`（第 126 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `DisposeForSceneUnload`（第 133 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void DisposeForSceneUnload()`
- `CollectActive`（第 153 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static unsafe void CollectActive(`
- `IsNativeBombFinal`（第 315 行）：判断当前对象/状态条件。
  - 签名：`private static unsafe bool IsNativeBombFinal(long nativeId)`
- `PresentPool`（第 363 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void PresentPool(VisualPool pool)`
- `TryConfigureStandaloneVisual`（第 428 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryConfigureStandaloneVisual(`
- `CreateVisualSlot`（第 480 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static VisualSlot CreateVisualSlot(GameObject prefab)`
- `DestroyBombVisualResources`（第 550 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyBombVisualResources(`
- `ApplyNativeTapPoolVisuals`（第 570 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyNativeTapPoolVisuals(`
- `TryPrepareTemplates`（第 583 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static void TryPrepareTemplates(`
- `RefreshPoolMaterials`（第 614 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RefreshPoolMaterials()`
- `RefreshPoolMaterials`（第 622 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RefreshPoolMaterials(VisualPool pool)`
- `DestroyPool`（第 628 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyPool(VisualPool pool)`
- `ReadFloat`（第 655 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static float ReadFloat(IntPtr ptr, int offset, float fallback)`
- `ReadDouble`（第 664 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static double ReadDouble(IntPtr ptr, int offset, double fallback)`
- `ReadQuaternion`（第 673 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Quaternion ReadQuaternion(IntPtr ptr, int offset)`
- `TryHardenMaterial`（第 684 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static void TryHardenMaterial(Material m)`
- `WarpedMs`（第 701 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double WarpedMs(double chartMs)`
- `Folder`（第 723 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string Folder()`
- `RefreshBombTextureFromFile`（第 739 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RefreshBombTextureFromFile()`
- `WriteDefaultBombFile`（第 796 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteDefaultBombFile(string path)`
- `DefaultBombTexture`（第 814 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Texture2D DefaultBombTexture()`
- `BombTexture`（第 847 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Texture2D BombTexture()`
- `TryDecode`（第 865 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryDecode(byte[] data, out int width, out int height, out byte[] rgba)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 163 行：`IntPtr mnA = Marshal.ReadIntPtr(`
- 第 164 行：`t + NativeLayoutCatalog.Track.LaneCenters);`
- 第 168 行：`t, NativeLayoutCatalog.Track.NoteWidthScale, 0f);`
- 第 170 行：`t, NativeLayoutCatalog.Track.BaseSpeed, 0.0);`
- 第 172 行：`t, NativeLayoutCatalog.Track.DepthFactor, 0.0);`
- 第 180 行：`t, NativeLayoutCatalog.Track.CenterRotation);`
- 第 182 行：`ReadFloat(t, NativeLayoutCatalog.Track.SideLeftPosition, 0f),`
- 第 183 行：`ReadFloat(t, NativeLayoutCatalog.Track.SideLeftPosition + 4, 0f),`
- 第 184 行：`ReadFloat(t, NativeLayoutCatalog.Track.SideLeftPosition + 8, 0f));`
- 第 186 行：`t, NativeLayoutCatalog.Track.SideLeftRotation);`
- 第 188 行：`ReadFloat(t, NativeLayoutCatalog.Track.SideRightPosition, 0f),`
- 第 189 行：`ReadFloat(t, NativeLayoutCatalog.Track.SideRightPosition + 4, 0f),`
- 第 190 行：`ReadFloat(t, NativeLayoutCatalog.Track.SideRightPosition + 8, 0f));`
- 第 192 行：`t, NativeLayoutCatalog.Track.SideRightRotation);`
- 第 195 行：`ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleX, 1f),`
- 第 196 行：`ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleY, 1f),`
- 第 197 行：`ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleZ, 1f));`
- 第 199 行：`t, NativeLayoutCatalog.Track.SideScaleY, 1f);`
- 第 201 行：`t, NativeLayoutCatalog.Track.SideScaleZ, 1f);`
- 第 244 行：`Marshal.ReadInt32(`
- 第 245 行：`mnA + NativeLayoutCatalog.ArrayDataOffset +`
- 第 342 行：`int arrayLength = Marshal.ReadInt32(`
- 第 344 行：`NativeLayoutCatalog.ArrayLengthOffset);`
- 第 658 行：`Marshal.ReadInt32(ptr + offset));`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 117 行：`DestroyPool(CenterSinglePool);`
- 第 118 行：`DestroyPool(CenterSpanPool);`
- 第 119 行：`DestroyPool(SideLeftPool);`
- 第 120 行：`DestroyPool(SideRightPool);`
- 第 126 行：`internal static void ResetForChartSwitch()`
- 第 133 行：`internal static void DisposeForSceneUnload()`
- 第 135 行：`ResetForChartSwitch();`
- 第 139 行：`UnityEngine.Object.Destroy(_bombTexture);`
- 第 145 行：`UnityEngine.Object.Destroy(_defaultTex);`
- 第 157 行：`CenterSinglePool.Requests.Clear();`
- 第 158 行：`CenterSpanPool.Requests.Clear();`
- 第 159 行：`SideLeftPool.Requests.Clear();`
- 第 160 行：`SideRightPool.Requests.Clear();`
- 第 468 行：`DestroyBombVisualResources(null, ownedMaterial);`
- 第 498 行：`UnityEngine.Object.Destroy(go);`
- 第 505 行：`UnityEngine.Object.Destroy(go);`
- 第 512 行：`UnityEngine.Object.Destroy(go);`
- 第 539 行：`DestroyBombVisualResources(go, material);`
- 第 550 行：`private static void DestroyBombVisualResources(`
- 第 558 行：`UnityEngine.Object.Destroy(material);`
- 第 565 行：`UnityEngine.Object.Destroy(gameObject);`
- 第 628 行：`private static void DestroyPool(VisualPool pool)`
- 第 638 行：`UnityEngine.Object.Destroy(slot.Go);`
- 第 646 行：`UnityEngine.Object.Destroy(slot.Material);`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- BombJudgeValidation
- BombPlanValidation
- PrerollValidation
- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
