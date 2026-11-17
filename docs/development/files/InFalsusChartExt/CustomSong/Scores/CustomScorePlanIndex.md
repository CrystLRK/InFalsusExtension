# CustomScorePlanIndex.cs

> 源码：`InFalsusChartExt/CustomSong/Scores/CustomScorePlanIndex.cs`
> SHA256：`D07D55244AB49962F3CCC9C4AE964995FAC046E901D97E446CEF8AE2FF423C3B`
> 代码行数：86
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

当前会话自制谱与 SongInfo 查找索引

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomScorePlanIndex`（`class`，源码第 15 行）

## 方法/属性职责

- `TryGetSong`（第 39 行）：按当前运行时 SongId 查找已安装自制歌曲计划。
  - 签名：`internal bool TryGetSong(`
- `TryGetSongInfo`（第 44 行）：按当前运行时 SongId 查找本次安装的 SongInfo。
  - 签名：`internal bool TryGetSongInfo(`
- `TryGetChart`（第 49 行）：按原版难度位查找歌曲拥有的 authored chart。
  - 签名：`internal bool TryGetChart(`
- `Key`（第 66 行）：构造 pack id、song id、难度组成的稳定文件键。
  - 签名：`internal static string Key(`
- `DifficultyName`（第 75 行）：把原版难度位转换为 MIN/EVO/ULT/FBD。
  - 签名：`internal static string DifficultyName(`

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
