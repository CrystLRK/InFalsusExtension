# NativeDetourHook.cs

> 源码：`InFalsusChartExt/Core/Native/NativeDetourHook.cs`
> SHA256：`75F91E5FB7604502F2063095F9BF27CA509C45DC31904760FFDDC3AEB3DE6E13`
> 代码行数：85
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `NativeDetourHook`（`class`，源码第 13 行）

## 方法/属性职责

- `Attach`（第 37 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Attach()`
- `Detach`（第 65 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Detach()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 4 行：`using BepInEx.Unity.IL2CPP.Hook;`
- 第 19 行：`private MelonLoader.NativeUtils.NativeHook<T> _hook;`
- 第 47 行：`new MelonLoader.NativeUtils.NativeHook<T>();`
- 第 50 行：`Marshal.GetFunctionPointerForDelegate(`

## 所有权与释放点

- 第 74 行：`disposable.Dispose();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
