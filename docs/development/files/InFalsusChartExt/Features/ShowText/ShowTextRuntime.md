# ShowTextRuntime.cs

> 源码：`InFalsusChartExt/Features/ShowText/ShowTextRuntime.cs`
> SHA256：`C540D2AE45978BC26D696E97E03CB304D2064DB24F31A4FFB690AFE1E470A2DA`
> 代码行数：1507
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

sidecar、FastText/IMGUI、fatal

## 生命周期

谱面载入、每个游戏帧和场景切换；退出谱面时释放自有资源。

## 顶层及类型声明

- `ShowTextRuntime`（`class`，源码第 25 行）
- `Style`（`class`，源码第 27 行）
- `FastEntry`（`class`，源码第 71 行）

## 方法/属性职责

- `EnsureLoaded`（第 112 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void EnsureLoaded()`
- `ResetForChartSwitch`（第 287 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `ReportFatal`（第 303 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void ReportFatal(string title, string detail)`
- `ReportFatal`（第 308 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void ReportFatal(`
- `DisposeResources`（第 316 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void DisposeResources()`
- `OnGUI`（第 342 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnGUI()`
- `DrawImGuiLine`（第 441 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DrawImGuiLine(`
- `DrawImGuiGlyph`（第 578 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DrawImGuiGlyph(`
- `GlyphAdvance`（第 635 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static float GlyphAdvance(string glyph, float size)`
- `Fail`（第 649 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Fail(`
- `MarkIgnored`（第 668 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void MarkIgnored(`
- `DrawFatalError`（第 687 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DrawFatalError()`
- `NowMs`（第 709 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double NowMs()`
- `EnsureFastRenderer`（第 716 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool EnsureFastRenderer()`
- `DrawFast`（第 850 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DrawFast(double nowMs)`
- `ResetFastSchedule`（第 1079 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ResetFastSchedule()`
- `CreateFast`（第 1094 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static FastEntry CreateFast(long id, Style st)`
- `PlaceFastText`（第 1172 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void PlaceFastText(FastEntry entry, Style st)`
- `SetFastText`（第 1218 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SetFastText(`
- `FindFastCamera`（第 1243 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Camera FindFastCamera()`
- `HasNonAscii`（第 1263 行）：判断当前对象/状态条件。
  - 签名：`private static bool HasNonAscii(string value)`
- `ResetFast`（第 1270 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ResetFast()`
- `CreateShadow`（第 1319 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static void CreateShadow(FastEntry entry, long id)`
- `DestroyFastOwned`（第 1368 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyFastOwned(`
- `EnsureShadowResources`（第 1388 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool EnsureShadowResources()`
- `CreateShadowQuad`（第 1432 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static Mesh CreateShadowQuad()`
- `CreateShadowTexture`（第 1460 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static Texture2D CreateShadowTexture(int size)`
- `ParseColor`（第 1488 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Color ParseColor(string s)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 123 行：`ResetForChartSwitch();`
- 第 132 行：`ResetForChartSwitch();`
- 第 142 行：`ResetForChartSwitch();`
- 第 287 行：`internal static void ResetForChartSwitch()`
- 第 295 行：`Styles.Clear();`
- 第 296 行：`Sorted.Clear();`
- 第 316 行：`internal static void DisposeResources()`
- 第 318 行：`ResetForChartSwitch();`
- 第 322 行：`UnityEngine.Object.Destroy(_shadowMesh);`
- 第 328 行：`UnityEngine.Object.Destroy(_shadowTexture);`
- 第 334 行：`UnityEngine.Object.Destroy(_shadowSourceMaterial);`
- 第 359 行：`ResetForChartSwitch();`
- 第 365 行：`ResetForChartSwitch();`
- 第 663 行：`Styles.Clear();`
- 第 664 行：`Sorted.Clear();`
- 第 673 行：`Styles.Clear();`
- 第 674 行：`Sorted.Clear();`
- 第 1083 行：`ActiveFastIds.Clear();`
- 第 1084 行：`ActiveFastOrder.Clear();`
- 第 1129 行：`UnityEngine.Object.Destroy(go);`
- 第 1162 行：`DestroyFastOwned(go, null);`
- 第 1283 行：`UnityEngine.Object.Destroy(pair.Value.ShadowMaterial);`
- 第 1299 行：`UnityEngine.Object.DestroyImmediate(go);`
- 第 1304 行：`Fast.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
