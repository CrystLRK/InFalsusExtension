# BackgroundNativeHook.cs

> 源码：`InFalsusChartExt/Features/Background/BackgroundNativeHook.cs`
> SHA256：`0C36059A26C8DA77071765A57A3C62DB1DA7E01AC88BA543A64EAD00F1F67D42`
> 代码行数：186
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

_UmA/_LIA` native detour、背景 clone

## 生命周期

谱面载入、每个游戏帧和场景切换；退出谱面时释放自有资源。

## 顶层及类型声明

- `BackgroundNativeHook`（`class`，源码第 16 行）

## 方法/属性职责

- `UmaFn`（第 19 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private delegate IntPtr UmaFn(IntPtr backgrounds, int background, IntPtr methodInfo);`
- `LiaFn`（第 22 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private delegate IntPtr LiaFn(IntPtr owner, IntPtr reference, IntPtr methodInfo);`
- `Install`（第 37 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Install()`
- `ResetForChartSwitch`（第 91 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `Dispose`（第 97 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Dispose()`
- `UmaDetour`（第 122 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr UmaDetour(IntPtr backgrounds, int background, IntPtr methodInfo)`
- `LiaDetour`（第 146 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr LiaDetour(IntPtr owner, IntPtr reference, IntPtr methodInfo)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 16 行：`internal static unsafe class BackgroundNativeHook`

## 所有权与释放点

- 第 13 行：`/// game load its own Material through AddressableHandleAutoReleaser._LIA,`
- 第 67 行：`"AddressableHandleAutoReleaser._LIA native hook failed");`
- 第 91 行：`internal static void ResetForChartSwitch()`
- 第 97 行：`internal static void Dispose()`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- NativeHookLifecycleHarness
- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
