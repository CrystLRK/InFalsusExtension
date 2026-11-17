# AutoplaySettingsOverlay.cs

> 源码：`InFalsusChartExt/Features/Autoplay/AutoplaySettingsOverlay.cs`
> SHA256：`754910AF7FEFD68B5D832A6D664B34B3CA3B5E7381642934C3D5590F3AB3CDE6`
> 代码行数：385
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

测试模式开启期间，每个原版判定周期和切谱清理。

## 顶层及类型声明

- `AutoplaySettingsOverlay`（`class`，源码第 13 行）

## 方法/属性职责

- `Attach`（第 34 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Attach(`
- `Sync`（第 45 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Sync(`
- `Tick`（第 70 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `OnGUI`（第 85 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnGUI()`
- `Hide`（第 101 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Hide()`
- `SetVisible`（第 106 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SetVisible(bool visible)`
- `Ensure`（第 117 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Ensure()`
- `Choose`（第 186 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Choose(AutoplayMode mode)`
- `ToggleGround`（第 192 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ToggleGround()`
- `Refresh`（第 199 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Refresh()`
- `Mark`（第 228 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string Mark(`
- `HandleClicks`（第 236 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void HandleClicks()`
- `TryClick`（第 270 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryClick(Vector2 position)`
- `PageAlive`（第 301 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool PageAlive()`
- `Contains`（第 317 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Contains(`
- `ModeLabels`（第 337 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string[] ModeLabels(`
- `GroundLabel`（第 353 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string GroundLabel(`
- `TryLogError`（第 372 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static void TryLogError(string message)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
