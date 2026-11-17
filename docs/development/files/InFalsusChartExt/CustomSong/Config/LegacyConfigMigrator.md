# LegacyConfigMigrator.cs

> 源码：`InFalsusChartExt/CustomSong/Config/LegacyConfigMigrator.cs`
> SHA256：`C451203A24D9D193D0CE314FF1711EF2C4C43BD3006BBD28C078452622A87A27`
> 代码行数：1019
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `LegacyConfigMigrator`（`class`，源码第 9 行）
- `PackMapping`（`class`，源码第 993 行）
- `SongMapping`（`class`，源码第 1001 行）
- `LegacyMigrationOrdering`（`class`，源码第 1009 行）

## 方法/属性职责

- `EnsureMigrated`（第 39 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void EnsureMigrated(out string report)`
- `EnsureMigrated`（第 42 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void EnsureMigrated(`
- `EnsureMigratedCore`（第 52 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EnsureMigratedCore(`
- `ArchiveDirectSongDirectories`（第 101 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ArchiveDirectSongDirectories(`
- `UniqueArchivePath`（第 127 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string UniqueArchivePath(string name)`
- `MigrateLegacyPacks`（第 142 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void MigrateLegacyPacks(`
- `RetryPendingPacks`（第 269 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RetryPendingPacks(`
- `ConvertPackInPlace`（第 311 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static PackMapping ConvertPackInPlace(`
- `ValidateSong`（第 418 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ValidateSong(`
- `CanDropLegacyDuplicate`（第 476 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool CanDropLegacyDuplicate(`
- `CountSongDirectories`（第 487 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CountSongDirectories(`
- `CleanupLegacyRoot`（第 501 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CleanupLegacyRoot(`
- `UniquePath`（第 539 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string UniquePath(`
- `WritePackJson`（第 569 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WritePackJson(`
- `WriteSongIf`（第 608 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteSongIf(`
- `WritePreviewWindows`（第 724 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WritePreviewWindows(`
- `WriteLanguageArray`（第 741 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteLanguageArray(`
- `WriteDifficultyArray`（第 753 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteDifficultyArray(`
- `WriteNullableDifficultyArray`（第 765 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteNullableDifficultyArray(`
- `WriteLevelArray`（第 783 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteLevelArray(`
- `ToJsonPath`（第 800 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string ToJsonPath(string value) =>`
- `ValidateLegacyPath`（第 804 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ValidateLegacyPath(`
- `FirstNonEmpty`（第 830 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FirstNonEmpty(`
- `FindPackJacket`（第 839 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FindPackJacket(string directory)`
- `WriteAtomically`（第 884 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteAtomically(`
- `ReadPackMapping`（第 919 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static PackMapping ReadPackMapping(`
- `WriteMigrationMarker`（第 950 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteMigrationMarker(`
- `OrderByOrdinal`（第 1011 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static IEnumerable<string> OrderByOrdinal(`

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
