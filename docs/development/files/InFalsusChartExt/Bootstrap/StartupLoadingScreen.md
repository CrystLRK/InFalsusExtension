# StartupLoadingScreen.cs

> 源码：`InFalsusChartExt/Bootstrap/StartupLoadingScreen.cs`
> SHA256：`78344F50BEEC494001256049C0F46C6A08885CC518E08C79DBA12CC1B54F9F44`
> 代码行数：451
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

MelonLoader/BepInEx 程序集加载、GameScene 切换和宿主卸载。

## 顶层及类型声明

- `StartupLoadingScreen`（`class`，源码第 19 行）

## 方法/属性职责

- `Begin`（第 47 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Begin()`
- `SetStatus`（第 65 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void SetStatus(string status)`
- `MarkLibraryReady`（第 71 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void MarkLibraryReady()`
- `MarkJacketsReady`（第 78 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void MarkJacketsReady()`
- `OnSceneLoaded`（第 84 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnSceneLoaded(string sceneName)`
- `Tick`（第 104 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `OnGUI`（第 160 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnGUI()`
- `Migrate`（第 260 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Migrate()`
- `EnsureCanvas`（第 300 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EnsureCanvas()`
- `DestroyCanvas`（第 348 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyCanvas()`
- `ConsumeKeyEvents`（第 357 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ConsumeKeyEvents()`
- `IsMainMenu`（第 367 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsMainMenu(string sceneName)`
- `IsStartUpSceneActive`（第 379 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsStartUpSceneActive()`
- `EnsureStyles`（第 405 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EnsureStyles()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 108 行：`DestroyCanvas();`
- 第 348 行：`private static void DestroyCanvas()`
- 第 352 行：`UnityEngine.Object.Destroy(_canvasRoot);`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
