# VideoBackgroundRuntime.cs

> 源码：`InFalsusChartExt/Features/Background/VideoBackgroundRuntime.cs`
> SHA256：`DA996BFCF5CE81499979ACCE28B59060C8ADD3C50CBB98EE9DF88921C545D1F0`
> 代码行数：115
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

MP4 背景/Texture 生命周期

## 生命周期

谱面载入、每个游戏帧和场景切换；退出谱面时释放自有资源。

## 顶层及类型声明

- `VideoBackgroundRuntime`（`class`，源码第 13 行）

## 方法/属性职责

- `Ensure`（第 24 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static Texture Ensure(string path)`
- `Stop`（第 84 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Stop()`
- `Dispose`（第 94 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Dispose()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 35 行：`UnityEngine.Object.DontDestroyOnLoad(_object);`
- 第 72 行：`Dispose();`
- 第 94 行：`internal static void Dispose()`
- 第 100 行：`UnityEngine.Object.Destroy(_object);`
- 第 106 行：`UnityEngine.Object.Destroy(_texture);`

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
