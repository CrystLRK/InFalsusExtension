# PluginConfig.cs

> 源码：`InFalsusChartExt/Bootstrap/PluginConfig.cs`
> SHA256：`07A32967808FA1448290673E170BEAD135CE050B7C1E20682361B7366433BF99`
> 代码行数：224
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

MelonLoader/BepInEx 程序集加载、GameScene 切换和宿主卸载。

## 顶层及类型声明

- `PluginConfig`（`class`，源码第 17 行）
- `PluginConfigCategory`（`class`，源码第 95 行）
- `PluginConfigEntry`（`class`，源码第 205 行）

## 方法/属性职责

- `Category`（第 24 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal PluginConfigCategory Category(`
- `Category`（第 44 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal PluginConfigCategory Category(`
- `ReadLegacyValues`（第 50 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Dictionary<string, string> ReadLegacyValues(`
- `Save`（第 123 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Save()`
- `Save`（第 167 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Save()`

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
