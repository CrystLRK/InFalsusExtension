# CustomScoreModels.cs

> 源码：`InFalsusChartExt/CustomSong/Scores/CustomScoreModels.cs`
> SHA256：`43921ED78EDB41E24209D42CE4BEEA18303D2069639EBFE1639ED86F7EB1B409`
> 代码行数：51
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

自制谱最佳成绩 JSON DTO

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomScoreDocument`（`class`，源码第 5 行）
- `CustomScoreEntry`（`class`，源码第 14 行）
- `CustomScoreRecord`（`class`，源码第 23 行）
- `JudgementCounts`（`class`，源码第 41 行）

## 方法/属性职责

- 无独立函数/方法声明。

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 26 行：`public int Clear { get; set; }`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
