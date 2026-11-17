# DisplayModeRuntime.cs

> 源码：`InFalsusChartExt/Features/Display/DisplayModeRuntime.cs`
> SHA256：`BBC70B4428954EAD70E0B92BAC956190A5B8507C90C725A63AED0B32735F07C1`
> 代码行数：441
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

按文件入口和调用方确定；变更时必须补充。

## 顶层及类型声明

- `ConfiguredDisplayMode`（`enum`，源码第 14 行）
- `DisplayModeRuntime`（`class`，源码第 27 行）

## 方法/属性职责

- `Initialize`（第 51 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Initialize(`
- `Attach`（第 77 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Attach(UiSettingDisplayPanel panel)`
- `Tick`（第 103 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `OnApplicationFocus`（第 124 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnApplicationFocus(bool focused)`
- `Sync`（第 132 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Sync(UiSettingDisplayPanel panel)`
- `BeginPanelRefresh`（第 137 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void BeginPanelRefresh()`
- `EndPanelRefresh`（第 142 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void EndPanelRefresh(`
- `EndPanelRefreshWithException`（第 155 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void EndPanelRefreshWithException()`
- `BeforeFullscreenCallback`（第 160 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static bool BeforeFullscreenCallback()`
- `BeforeIndexChange`（第 163 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void BeforeIndexChange(`
- `BeforeResolutionRequest`（第 174 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void BeforeResolutionRequest(`
- `DrawWarning`（第 192 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void DrawWarning()`
- `SyncSelection`（第 219 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SyncSelection(`
- `EnsureThirdOption`（第 258 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EnsureThirdOption(`
- `DesiredIndex`（第 284 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int DesiredIndex()`
- `IndexToMode`（第 305 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ConfiguredDisplayMode IndexToMode(int index)`
- `SetMode`（第 316 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SetMode(`
- `RequestConfiguredMode`（第 335 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RequestConfiguredMode()`
- `BeforeFocusGained`（第 351 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void BeforeFocusGained()`
- `ScheduleVerification`（第 361 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ScheduleVerification()`
- `VerifyExclusiveMode`（第 367 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void VerifyExclusiveMode()`
- `Parse`（第 400 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ConfiguredDisplayMode Parse(string value)`
- `Save`（第 413 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Save()`
- `ShowWarning`（第 421 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ShowWarning(string message)`
- `Log`（第 428 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Log(string message)`
- `LogError`（第 435 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(string message)`

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
