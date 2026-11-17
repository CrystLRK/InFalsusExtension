# ChartRuntimeContext.cs

> 源码：`InFalsusChartExt/Core/ChartModel/ChartRuntimeContext.cs`
> SHA256：`970ED100527CCABA1E30469B2284296DA88701944522E9C86B6FB884422F0036`
> 代码行数：148
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

当前谱上下文所有权

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `ChartRuntimeFrame`（`class`，源码第 6 行）
- `ChartRuntimeContext`（`class`，源码第 35 行）

## 方法/属性职责

- `Dispose`（第 18 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal void Dispose()`
- `Begin`（第 47 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Begin(ChartIngressResult ingress)`
- `Prepare`（第 53 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static ChartRuntimeFrame Prepare(`
- `InstallPrepared`（第 78 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void InstallPrepared(`
- `DiscardPrepared`（第 96 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void DiscardPrepared(`
- `LogCurrent`（第 105 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogCurrent()`
- `ResetForChartSwitch`（第 125 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `DisposeCurrent`（第 131 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void DisposeCurrent()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 18 行：`internal void Dispose()`
- 第 20 行：`NativeChart?.Clear();`
- 第 21 行：`VisualChart?.Clear();`
- 第 22 行：`Rules?.Clear();`
- 第 23 行：`SkyBlocks?.Clear();`
- 第 83 行：`DisposeCurrent();`
- 第 86 行：`DisposeCurrent();`
- 第 101 行：`try { prepared.Dispose(); }`
- 第 125 行：`internal static void ResetForChartSwitch()`
- 第 127 行：`DisposeCurrent();`
- 第 131 行：`private static void DisposeCurrent()`
- 第 138 行：`current.Dispose();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
