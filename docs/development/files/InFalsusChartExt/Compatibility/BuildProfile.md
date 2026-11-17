# BuildProfile.cs

> 源码：`InFalsusChartExt/Compatibility/BuildProfile.cs`
> SHA256：`502453635FD1FBCB77F6EEF20F57B7168ED5745F6C73379F02CA14C775460762`
> 代码行数：333
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

构建识别与能力矩阵

## 生命周期

按文件入口和调用方确定；变更时必须补充。

## 顶层及类型声明

- `NativeCapability`（`enum`，源码第 10 行）
- `NativeBuildKind`（`enum`，源码第 37 行）
- `GameBuildProfile`（`class`，源码第 44 行）
- `BuildProfile`（`class`，源码第 67 行）

## 方法/属性职责

- `Supports`（第 54 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal bool Supports(NativeCapability capability)`
- `Initialize`（第 96 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Initialize()`
- `MarkCoreAbiValidated`（第 173 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void MarkCoreAbiValidated()`
- `Supports`（第 184 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static bool Supports(NativeCapability capability)`
- `DisableCapabilities`（第 190 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void DisableCapabilities(`
- `Detect`（第 214 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static GameBuildProfile Detect()`
- `ParseKind`（第 248 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static NativeBuildKind ParseKind(string kind)`
- `GameRoot`（第 271 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string GameRoot()`
- `FindMetadata`（第 285 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string FindMetadata(string root)`
- `HashFile`（第 306 行）：判断当前对象/状态条件。
  - 签名：`private static string HashFile(string path)`
- `ShortHash`（第 328 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string ShortHash(string value)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 59 行：`internal bool SupportsBackgroundNativeHook`
- 第 107 行：`!NativeLayoutCatalog.ValidateCurrentBuild(out string validationError))`
- 第 290 行：`root, "infalsus_Data", "il2cpp_data",`
- 第 297 行：`dataDir, "il2cpp_data", "Metadata",`

## 所有权与释放点

- 第 137 行：`DisableReasons.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
