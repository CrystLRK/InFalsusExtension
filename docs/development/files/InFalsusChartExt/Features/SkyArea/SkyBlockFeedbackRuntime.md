# SkyBlockFeedbackRuntime.cs

> 源码：`InFalsusChartExt/Features/SkyArea/SkyBlockFeedbackRuntime.cs`
> SHA256：`723884654B9C3645FADFB323CE33DD29EA637BBB8C888A437EB1F4A04921F6F4`
> 代码行数：164
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `SkyBlockFeedbackRuntime`（`class`，源码第 11 行）

## 方法/属性职责

- `EventDispatchCallback`（第 18 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private delegate void EventDispatchCallback(`
- `PlayHit`（第 30 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void PlayHit()`
- `SetSkyHoldActive`（第 35 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void SetSkyHoldActive(`
- `ResetForChartSwitch`（第 74 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `Dispatch`（第 80 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static unsafe bool Dispatch(byte eventType, bool value)`
- `TryResolve`（第 136 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryResolve()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 43 行：`IntPtr bus = Marshal.ReadIntPtr(`
- 第 45 行：`NativeLayoutCatalog.GameSceneAudio.HoldBus);`
- 第 48 行：`Marshal.ReadInt32(`
- 第 50 行：`NativeLayoutCatalog.GameSceneAudio.HoldBusRunning) != 0;`
- 第 94 行：`NativeLayoutCatalog.GameSceneAudio.ShStride;`
- 第 96 行：`NativeLayoutCatalog.GameSceneAudio.ShType < 0 ||`
- 第 97 行：`NativeLayoutCatalog.GameSceneAudio.ShType >= stride ||`
- 第 98 行：`NativeLayoutCatalog.GameSceneAudio.ShBool < 0 ||`
- 第 99 行：`NativeLayoutCatalog.GameSceneAudio.ShBool >= stride)`
- 第 102 行：`_eventBuffer = Marshal.AllocHGlobal(stride);`
- 第 110 行：`NativeLayoutCatalog.GameSceneAudio.ShType] =`
- 第 115 行：`NativeLayoutCatalog.GameSceneAudio.ShBool] =`
- 第 159 行：`Marshal.GetDelegateForFunctionPointer<`

## 所有权与释放点

- 第 74 行：`internal static void ResetForChartSwitch()`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- SkyBlock alias scan
- SkyBlockOverlapHarness
- SkyBlockTestHarness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
