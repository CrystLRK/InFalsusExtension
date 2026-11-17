# CustomLibraryDiscovery.cs

> 源码：`InFalsusChartExt/CustomSong/Discovery/CustomLibraryDiscovery.cs`
> SHA256：`5AE3F3859EAC21521B7366560C622DE11B44D8660DF73484C6D13B8722610582`
> 代码行数：399
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomLibraryDiscovery`（`class`，源码第 15 行）

## 方法/属性职责

- `Scan`（第 25 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static CustomLibraryPlan Scan(out string report)`
- `Scan`（第 28 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static CustomLibraryPlan Scan(`
- `ComparePacks`（第 101 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int ComparePacks(`
- `ScanPack`（第 119 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static CustomPackPlan ScanPack(`
- `ScanSong`（第 199 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static CustomSongPlan ScanSong(`
- `ValidateAudio`（第 305 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ValidateAudio(`
- `ValidateAudioSource`（第 323 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ValidateAudioSource(`
- `DetectAudioHeader`（第 336 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool DetectAudioHeader(`
- `BuildBaseName`（第 384 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static string BuildBaseName(string id)`

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
