# CustomSongPaths.cs

> 源码：`InFalsusChartExt/CustomSong/Config/CustomSongPaths.cs`
> SHA256：`6DB6F4A8BDE9D00C74BC61CFAEF612B9887475D9AD9BF4DA861907F48481CF3D`
> 代码行数：163
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomSongPaths`（`class`，源码第 8 行）

## 方法/属性职责

- `TryResolveFile`（第 51 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryResolveFile(`
- `IsAudioPathValid`（第 142 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsAudioPathValid(string path) =>`
- `IsBackgroundPathValid`（第 145 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsBackgroundPathValid(string path) =>`
- `IsJacketPathValid`（第 148 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsJacketPathValid(string path) =>`
- `IsChartPathValid`（第 151 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsChartPathValid(string path) =>`
- `EnsureTrailingSeparator`（第 154 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static string EnsureTrailingSeparator(string path)`

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
