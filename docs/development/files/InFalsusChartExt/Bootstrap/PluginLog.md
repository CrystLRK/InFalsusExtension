# PluginLog.cs

> 源码：`InFalsusChartExt/Bootstrap/PluginLog.cs`
> SHA256：`041AAEFD561B832A9CCC3D1ABF272AEE17CA400B7AA4F8984FF3B5B6DA201B44`
> 代码行数：62
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

MelonLoader/BepInEx 程序集加载、GameScene 切换和宿主卸载。

## 顶层及类型声明

- `PluginLog`（`class`，源码第 14 行）

## 方法/属性职责

- `Msg`（第 32 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Msg(string message)`
- `Warning`（第 42 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Warning(string message)`
- `Error`（第 52 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Error(string message)`

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
