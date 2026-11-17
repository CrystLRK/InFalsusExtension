# AutoplayResultsUpdateNativePatch.cs

> 源码：`InFalsusChartExt/Patches/AutoplayResultsUpdateNativePatch.cs`
> SHA256：`EEBB0467C4A81CB2972C3484854BAC4E023E258FCF02413B7CABF6F0FB702B0B`
> 代码行数：143
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

Harmony 目标方法调用时；生命周期跟随原版调用方。

## 顶层及类型声明

- `AutoplayResultsUpdateNativePatch`（`class`，源码第 16 行）

## 方法/属性职责

- `UpdateResultDelegate`（第 24 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private delegate int UpdateResultDelegate(`
- `Install`（第 31 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static bool Install()`
- `Uninstall`（第 74 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Uninstall()`
- `Detour`（第 85 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int Detour(`
- `LogError`（第 126 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 49 行：`Marshal.GetFunctionPointerForDelegate(_replacement),`

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 包含帧级入口：`UpdateResultDelegate`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
