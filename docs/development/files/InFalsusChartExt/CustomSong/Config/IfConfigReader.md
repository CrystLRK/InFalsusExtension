# IfConfigReader.cs

> 源码：`InFalsusChartExt/CustomSong/Config/IfConfigReader.cs`
> SHA256：`38B0A56E5968211221A23B1FEF1A9226D02B37197DD4E63D604F81BABA35EBAE`
> 代码行数：1217
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `IfSongDocument`（`class`，源码第 10 行）
- `PackJsonDocument`（`class`，源码第 30 行）
- `IfConfigReader`（`class`，源码第 42 行）
- `Faces`（`class`，源码第 424 行）
- `ChartDesignerFace`（`class`，源码第 1109 行）

## 方法/属性职责

- `TryReadPack`（第 68 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryReadPack(`
- `TryReadSong`（第 180 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryReadSong(`
- `ParseFile`（第 344 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static JsonDocument ParseFile(`
- `ReadRequiredText`（第 363 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string ReadRequiredText(`
- `ReadText`（第 372 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string ReadText(`
- `ReadOptionalText`（第 385 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string ReadOptionalText(`
- `ReadInt`（第 399 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static int ReadInt(`
- `TryProperty`（第 417 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryProperty(`
- `ReadFaces`（第 430 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Faces ReadFaces(`
- `ReadFacesArray`（第 528 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static void ReadFacesArray(`
- `ReadLanguageObject`（第 605 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static void ReadLanguageObject(`
- `FillBaseFromSets`（第 653 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void FillBaseFromSets(`
- `FillSets`（第 675 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void FillSets(`
- `FirstNonEmpty`（第 692 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FirstNonEmpty(string[] values)`
- `SameFive`（第 702 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string[] SameFive(string text)`
- `ReadLanguageFaces`（第 710 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string[] ReadLanguageFaces(`
- `ReadPictureFaces`（第 760 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string[] ReadPictureFaces(`
- `ReadAudioFaces`（第 793 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string[] ReadAudioFaces(`
- `ReadFileFaces`（第 824 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string[] ReadFileFaces(`
- `ReadPictureDifficulties`（第 867 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string[] ReadPictureDifficulties(`
- `ReadBackgroundDifficulties`（第 889 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string[] ReadBackgroundDifficulties(`
- `ReadPreviewWindows`（第 903 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static void ReadPreviewWindows(`
- `ReadWindow`（第 963 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static void ReadWindow(`
- `ReadSeconds`（第 985 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static float ReadSeconds(`
- `ReadDifficultyTexts`（第 1003 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string[] ReadDifficultyTexts(`
- `ReadDifficultyLevels`（第 1047 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static int[] ReadDifficultyLevels(JsonElement root)`
- `ReadDifficultySpecials`（第 1079 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string[] ReadDifficultySpecials(JsonElement root)`
- `ReadSpecial`（第 1097 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string ReadSpecial(JsonElement token)`
- `ReadCharterFaces`（第 1114 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Faces ReadCharterFaces(JsonElement root)`
- `ReadChartDesigner`（第 1160 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static ChartDesignerFace ReadChartDesigner(`
- `ReadIllustratorFace`（第 1181 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static string[] ReadIllustratorFace(`
- `ResolveFile`（第 1192 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string ResolveFile(`

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
