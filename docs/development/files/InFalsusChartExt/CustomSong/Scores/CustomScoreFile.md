# CustomScoreFile.cs

> 源码：`InFalsusChartExt/CustomSong/Scores/CustomScoreFile.cs`
> SHA256：`BC0703FD1661838549865D169B4B47FB52D52CDBD38373E254EE275491928C88`
> 代码行数：227
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

成绩文件校验、原子保存与备份恢复

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomScoreFile`（`class`，源码第 8 行）

## 方法/属性职责

- `Load`（第 24 行）：读取并校验 custom-results.json；损坏时保留副本并恢复 .bak。
  - 签名：`internal static CustomScoreDocument Load(out string error)`
- `Save`（第 89 行）：先写临时文件，再备份上一版并原子替换主文件。
  - 签名：`internal static bool Save(`
- `Key`（第 135 行）：构造 pack id、song id、难度组成的唯一成绩键。
  - 签名：`internal static string Key(`
- `TryReadValid`（第 143 行）：反序列化并完整校验一个成绩文件。
  - 签名：`private static bool TryReadValid(`
- `TryPreserveCorrupt`（第 176 行）：把损坏的主文件改名保留，避免静默覆盖。
  - 签名：`private static void TryPreserveCorrupt(string path)`
- `TryRestoreFromBackup`（第 197 行）：用有效备份原子恢复主文件。
  - 签名：`private static bool TryRestoreFromBackup(string path)`
- `ResolvePath`（第 220 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string ResolvePath()`

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
