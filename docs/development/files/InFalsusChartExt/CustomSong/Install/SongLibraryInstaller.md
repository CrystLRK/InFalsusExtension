# SongLibraryInstaller.cs

> 源码：`InFalsusChartExt/CustomSong/Install/SongLibraryInstaller.cs`
> SHA256：`975140AD47AC70B8F90E786B1D886B63C6A0E2A100223CEA6E27D357E7E7CBAC`
> 代码行数：807
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `SongLibraryInstaller`（`class`，源码第 26 行）

## 方法/属性职责

- `Tick`（第 39 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `ReapplyDisplayMetadata`（第 103 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void ReapplyDisplayMetadata(`
- `ApplyPackAssets`（第 131 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void ApplyPackAssets(`
- `Install`（第 150 行）：事务性安装自制歌曲/曲包；原生库提交成功后初始化独立成绩文件，成绩初始化失败不回滚已经提交的曲库。
  - 签名：`private static void Install(`
- `FindTemplateSong`（第 369 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SongInfo FindTemplateSong(`
- `HasSongJacket`（第 399 行）：判断当前对象/状态条件。
  - 签名：`private static bool HasSongJacket(`
- `HasChartJacket`（第 410 行）：判断当前对象/状态条件。
  - 签名：`private static bool HasChartJacket(`
- `BuildSong`（第 424 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static SongInfo BuildSong(`
- `NonEmpty`（第 495 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string NonEmpty(`
- `FindTemplateChart`（第 504 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SongChartInfo FindTemplateChart(`
- `BuildPack`（第 530 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static PackInfo BuildPack(`
- `AppendJackets`（第 543 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AppendJackets(`
- `FindTemplateSongChartId`（第 634 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FindTemplateSongChartId(`
- `FindSongJacket`（第 650 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SongJacketEntry FindSongJacket(`
- `FindChartJacket`（第 662 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SongDataChartJacketEntry FindChartJacket(`
- `AssignChartTemplates`（第 676 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AssignChartTemplates(`
- `RefreshSongDataCaches`（第 698 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void RefreshSongDataCaches(SongData data)`
- `UpdatePreviewForChart`（第 703 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void UpdatePreviewForChart(`
- `UpdatePreviewForSelection`（第 723 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void UpdatePreviewForSelection(`
- `ResetInstalledSongInfo`（第 744 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetInstalledSongInfo()`
- `SlotForDifficulty`（第 749 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int SlotForDifficulty(`
- `DifficultyForSlot`（第 761 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ChartDifficultyFlag DifficultyForSlot(`
- `FindLoadedDataAccess`（第 787 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static DataAccess FindLoadedDataAccess()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 161 行：`if (!JacketAddressablesNativeHook.Install())`

## 所有权与释放点

- 第 120 行：`PackAssetInstaller.Dispose();`
- 第 746 行：`InstalledSongInfo.Clear();`

## 性能边界

- 包含帧级入口：`Tick`, `UpdatePreviewForChart`, `UpdatePreviewForSelection`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
