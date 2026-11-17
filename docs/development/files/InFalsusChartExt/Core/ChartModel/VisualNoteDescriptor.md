# VisualNoteDescriptor.cs

> 源码：`InFalsusChartExt/Core/ChartModel/VisualNoteDescriptor.cs`
> SHA256：`D77BFB5B891171087574CD1703292FD4404DCC914D1D273BEB9B8E5AAEE2C316`
> 代码行数：93
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `VisualNoteKind`（`enum`，源码第 6 行）
- `VisualNoteLifetime`（`enum`，源码第 15 行）
- `VisualNoteDescriptor`（`class`，源码第 25 行）

## 方法/属性职责

- `FromNative`（第 44 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static VisualNoteDescriptor FromNative(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
