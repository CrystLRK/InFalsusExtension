# PackConfig.cs

> 源码：`InFalsusChartExt/CustomSong/Config/PackConfig.cs`
> SHA256：`28C59F72C7134DBCCE37B59103F96E9A9C5D6796CCD2504DEF1A180D634DDE08`
> 代码行数：48
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `PackConfig`（`class`，源码第 5 行）
- `SongConfig`（`class`，源码第 16 行）
- `PreviewConfig`（`class`，源码第 34 行）
- `DifficultyConfig`（`class`，源码第 41 行）

## 方法/属性职责

- 无独立函数/方法声明。

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
