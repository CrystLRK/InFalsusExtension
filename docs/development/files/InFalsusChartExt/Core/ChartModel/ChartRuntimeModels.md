# ChartRuntimeModels.cs

> 源码：`InFalsusChartExt/Core/ChartModel/ChartRuntimeModels.cs`
> SHA256：`170E67A0078DFA56A82316406F6106CDA6FD0EFA057E51C838534FC024779929`
> 代码行数：91
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `VisualChart`（`class`，源码第 6 行）
- `NativeChart`（`class`，源码第 32 行）
- `ChartRuntimeRules`（`class`，源码第 68 行）

## 方法/属性职责

- `Add`（第 14 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Add(VisualNoteDescriptor descriptor)`
- `Clear`（第 21 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal void Clear()`
- `Add`（第 46 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Add(Icp1Codec.Note note)`
- `Clear`（第 59 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal void Clear()`
- `Clear`（第 82 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal void Clear()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 21 行：`internal void Clear()`
- 第 23 行：`Notes.Clear();`
- 第 24 行：`Ids.Clear();`
- 第 59 行：`internal void Clear()`
- 第 61 行：`NoteIds.Clear();`
- 第 62 行：`NoteIdSet.Clear();`
- 第 82 行：`internal void Clear()`
- 第 84 行：`NoteTimingGroups.Clear();`
- 第 85 行：`NoInputTimingGroups.Clear();`
- 第 86 行：`NoClipTimingGroups.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
