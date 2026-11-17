# CustomScoreRuntime.cs

> 源码：`InFalsusChartExt/CustomSong/Scores/CustomScoreRuntime.cs`
> SHA256：`0338FD73278F3495497AFAE0B472F785080916AE123E7B492990C14B13EC8C57`
> 代码行数：465
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

自制谱成绩加载、接管、注入与结算保存编排

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomScoreRuntime`（`class`，源码第 13 行）

## 方法/属性职责

- `Initialize`（第 30 行）：建立自制谱运行时 ID 映射、加载独立成绩文件，并调度当前可见原生成绩与文件最佳值的合并。
  - 签名：`internal static void Initialize(`
- `Tick`（第 66 行）：在原版成绩表尚未就绪时每两秒重试一次合并；就绪后不再扫描。
  - 签名：`internal static void Tick()`
- `TryApplyPending`（第 76 行）：把文件最佳成绩按当前 SongInfo/SongId 注入原版结果表，再查询最终最佳值同步回文件。
  - 签名：`private static void TryApplyPending()`
- `CaptureFinalResult`（第 153 行）：在 ResultsScene.Start 完成后仅对自制谱读取原版最终最佳成绩，并原子写入独立 JSON。
  - 签名：`internal static void CaptureFinalResult()`
- `LogFinalResultDiagnostic`（第 234 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogFinalResultDiagnostic()`
- `FormatCounts`（第 288 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FormatCounts(`
- `LogRecordStateDiagnostic`（第 297 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogRecordStateDiagnostic()`
- `Reset`（第 393 行）：清空自制谱成绩索引、待安装计划和错误状态；不删除文件。
  - 签名：`internal static void Reset()`
- `MergeNative`（第 406 行）：将原版 GameResultV4 转换为文件记录；仅当字段变化时更新条目。
  - 签名：`private static bool MergeNative(`
- `RebuildEntryIndex`（第 443 行）：按 pack id、song id、难度建立文件条目索引。
  - 签名：`private static void RebuildEntryIndex()`
- `MarkWriteFailure`（第 457 行）：停用本次会话后续写入并记录明确错误。
  - 签名：`private static void MarkWriteFailure(string error)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 328 行：`int arrayLength = Marshal.ReadInt32(`
- 第 330 行：`NativeLayoutCatalog.ArrayLengthOffset);`
- 第 350 行：`NativeLayoutCatalog.Record`

## 所有权与释放点

- 第 395 行：`EntriesByKey.Clear();`
- 第 445 行：`EntriesByKey.Clear();`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
