# VisualNoteInstances.cs

> 源码：`InFalsusChartExt/Features/VisualNotes/VisualNoteInstances.cs`
> SHA256：`9B91793D0BF3BFF5BCA01C1448994E97472C5A9BE9C40FD5FAB9C6830543B155`
> 代码行数：694
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

Tap/Hold/Flick/Bomb 视觉实例

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `VisualTapNoteInstance`（`class`，源码第 7 行）
- `VisualFlickNoteInstance`（`class`，源码第 166 行）
- `VisualHoldNoteInstance`（`class`，源码第 245 行）
- `VisualBombNoteInstance`（`class`，源码第 585 行）

## 方法/属性职责

- `Update`（第 57 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal override void Update(in VisualTrackFrame frame)`
- `TapPrefab`（第 115 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static GameObject TapPrefab(`
- `NormalizedDistance`（第 131 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static float NormalizedDistance(`
- `TapState`（第 140 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static float TapState(float normalized)`
- `ApplyTapGuideSetting`（第 146 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyTapGuideSetting(`
- `Update`（第 212 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal override void Update(in VisualTrackFrame frame)`
- `Update`（第 332 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal override void Update(in VisualTrackFrame frame)`
- `LogHoldGeometry`（第 436 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private void LogHoldGeometry(float progress)`
- `FormatVector`（第 462 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FormatVector(Vector3? value)`
- `FormatMaterial`（第 470 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FormatMaterial(MeshRenderer renderer)`
- `FormatFloat`（第 484 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FormatFloat(Material material, int property)`
- `FormatTexture`（第 491 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FormatTexture(Material material, int property)`
- `LogHoldSceneInstances`（第 501 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private void LogHoldSceneInstances()`
- `PrepareNoInputAppearance`（第 541 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private void PrepareNoInputAppearance()`
- `CopyNonHeldMaps`（第 550 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopyNonHeldMaps(MeshRenderer renderer)`
- `ApplyHoldGuideSetting`（第 565 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyHoldGuideSetting(`
- `Update`（第 631 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal override void Update(in VisualTrackFrame frame)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 52 行：`Dispose();`
- 第 207 行：`Dispose();`
- 第 327 行：`Dispose();`
- 第 626 行：`Dispose();`

## 性能边界

- 包含帧级入口：`Update`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
