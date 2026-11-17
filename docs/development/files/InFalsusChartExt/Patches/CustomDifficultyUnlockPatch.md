# CustomDifficultyUnlockPatch.cs

> 源码：`InFalsusChartExt/Patches/CustomDifficultyUnlockPatch.cs`
> SHA256：`1ED3230DF31DD828AAC2F8C0E497CA81032B67699EB5DE379DB67E69D0E42629`
> 代码行数：159
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

Harmony 目标方法调用时；生命周期跟随原版调用方。

## 顶层及类型声明

- `CustomDifficultyUnlockPatch`（`class`，源码第 19 行）

## 方法/属性职责

- `DifficultyStatusDelegate`（第 32 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private delegate byte DifficultyStatusDelegate(`
- `Install`（第 40 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Install()`
- `Uninstall`（第 74 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Uninstall()`
- `Detour`（第 84 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static byte Detour(`
- `BuildUnlockMask`（第 130 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static byte BuildUnlockMask(string baseName)`
- `LogError`（第 142 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 17 行：`/// cannot safely marshal its by-ref SongInfo argument on IL2CPP.`
- 第 104 行：`(ushort)Marshal.ReadInt16(songInfo);`
- 第 109 行：`Marshal.ReadIntPtr(`
- 第 114 行：`: IL2CPP.Il2CppStringToManaged(`

## 所有权与释放点

- 第 81 行：`UnlockMasks.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
