# RegressionDiagnostics.cs

> 源码：`InFalsusChartExt/Diagnostics/RegressionDiagnostics.cs`
> SHA256：`C0ED01B4CFCC900F5DBE2D07653A006982E70A50EC479CFEF8197AFFBAE91B36`
> 代码行数：356
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

按文件入口和调用方确定；变更时必须补充。

## 顶层及类型声明

- `RegressionDiagnostics`（`class`，源码第 18 行）
- `AudioFactoryTrace`（`class`，源码第 36 行）

## 方法/属性职责

- `LogState`（第 53 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogState(string message)`
- `AudioFactory`（第 62 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void AudioFactory(`
- `AudioTick`（第 95 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void AudioTick()`
- `GameplayStateTick`（第 130 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void GameplayStateTick(`
- `TrackTick`（第 167 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void TrackTick(`
- `PauseTick`（第 188 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void PauseTick(`
- `AudioOwned`（第 207 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void AudioOwned(`
- `Reset`（第 222 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Reset()`
- `Refresh`（第 236 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Refresh(`
- `WriteFactory`（第 295 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteFactory(`
- `AudioFactory`（第 316 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void AudioFactory(`
- `AudioTick`（第 324 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void AudioTick()`
- `GameplayStateTick`（第 329 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void GameplayStateTick(`
- `TrackTick`（第 335 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void TrackTick(`
- `AudioOwned`（第 341 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void AudioOwned(`
- `Reset`（第 351 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Reset()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 243 行：`IntPtr sound = Marshal.ReadIntPtr(`
- 第 245 行：`NativeLayoutCatalog.CustomSong.AudioSound);`
- 第 246 行：`IntPtr channel = Marshal.ReadIntPtr(`
- 第 248 行：`NativeLayoutCatalog.CustomSong.AudioChannel);`

## 所有权与释放点

- 第 224 行：`Factories.Clear();`

## 性能边界

- 包含帧级入口：`AudioTick`, `GameplayStateTick`, `PauseTick`, `TrackTick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
