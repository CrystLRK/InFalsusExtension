# ChartExtMod.cs

> 源码：`InFalsusChartExt/ChartExtMod.cs`
> SHA256：`13AF60D4B76749EEF2A22829C3D86DA59AB264A8B6DCEF1986CB09B91713C32F`
> 代码行数：409
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

MelonLoader/BepInEx 程序集加载、GameScene 切换和宿主卸载。

## 顶层及类型声明

- `ChartExtMod`（`class`，源码第 31 行）
- `MelonChartExtMod`（`class`，源码第 237 行）
- `BepInExChartExtPlugin`（`class`，源码第 301 行）
- `BepInExRuntimeBehaviour`（`class`，源码第 386 行）

## 方法/属性职责

- `Initialize`（第 36 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Initialize(`
- `InitializeRuntime`（第 44 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void InitializeRuntime()`
- `MigrateLegacyData`（第 76 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void MigrateLegacyData()`
- `CopyMissing`（第 118 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CopyMissing(string source, string target)`
- `Tick`（第 139 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `OnGUI`（第 156 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnGUI()`
- `OnSceneLoaded`（第 167 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnSceneLoaded(`
- `OnSceneUnloaded`（第 183 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnSceneUnloaded(`
- `OnApplicationFocus`（第 191 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnApplicationFocus(bool focus)`
- `OnApplicationQuit`（第 196 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnApplicationQuit()`
- `Dispose`（第 201 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Dispose()`
- `RunMergedSubsystem`（第 215 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RunMergedSubsystem(`
- `OnInitializeMelon`（第 239 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public override void OnInitializeMelon()`
- `OnUpdate`（第 256 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public override void OnUpdate()`
- `OnGUI`（第 261 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public override void OnGUI()`
- `OnSceneWasLoaded`（第 266 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public override void OnSceneWasLoaded(`
- `OnSceneWasUnloaded`（第 273 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public override void OnSceneWasUnloaded(`
- `OnApplicationFocus`（第 280 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public void OnApplicationFocus(bool focus)`
- `OnApplicationQuit`（第 285 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public override void OnApplicationQuit()`
- `OnDeinitializeMelon`（第 290 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public override void OnDeinitializeMelon()`
- `Load`（第 318 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public override void Load()`
- `Unload`（第 351 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public override bool Unload()`
- `OnSceneLoaded`（第 371 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void OnSceneLoaded(`
- `OnSceneUnloaded`（第 379 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void OnSceneUnloaded(Scene scene)`
- `Update`（第 388 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`public void Update()`
- `OnGUI`（第 393 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public void OnGUI()`
- `OnApplicationFocus`（第 398 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public void OnApplicationFocus(bool focus)`
- `OnApplicationQuit`（第 403 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public void OnApplicationQuit()`

## 原版/Harmony/native 访问点

- 第 337 行：`// MelonLoader patches all [HarmonyPatch] types automatically.`
- 第 61 行：`if (BuildProfile.Current.SupportsBackgroundNativeHook)`
- 第 62 行：`BackgroundNativeHook.Install();`
- 第 207 行：`try { BackgroundNativeHook.Dispose(); }`

## 所有权与释放点

- 第 201 行：`internal static void Dispose()`
- 第 204 行：`ChartRuntimeLifecycle.Dispose();`
- 第 207 行：`try { BackgroundNativeHook.Dispose(); }`
- 第 209 行：`try { CustomSongRuntime.Dispose(); }`
- 第 292 行：`ChartExtMod.Dispose();`
- 第 365 行：`ChartExtMod.Dispose();`

## 性能边界

- 包含帧级入口：`OnUpdate`, `Tick`, `Update`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
