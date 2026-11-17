# NativeProfileV2Models.cs

> 源码：`InFalsusChartExt/Compatibility/NativeProfileV2Models.cs`
> SHA256：`C3F2234FB823FF2D3377EEE5B54663155029FC0FDC2F51CB38F2578A944D5575`
> 代码行数：356
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

按文件入口和调用方确定；变更时必须补充。

## 顶层及类型声明

- `NativePresentationProfile`（`class`，源码第 10 行）
- `NativeCapacityScannerProfile`（`class`，源码第 86 行）
- `NativePatchTargetProfile`（`class`，源码第 156 行）
- `NativeCapabilityParser`（`class`，源码第 331 行）

## 方法/属性职责

- `Int`（第 61 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int Int(JsonElement root, string key)`
- `String`（第 73 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string String(JsonElement root, string key)`
- `Int`（第 128 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int Int(JsonElement root, string key)`
- `Strings`（第 140 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string[] Strings(`
- `String`（第 210 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string String(`
- `OptionalString`（第 223 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string OptionalString(`
- `OptionalInt`（第 233 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int OptionalInt(`
- `OptionalLong`（第 245 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static long OptionalLong(`
- `Strings`（第 261 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string[] Strings(`
- `OptionalStrings`（第 274 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string[] OptionalStrings(`
- `ParsePrologue`（第 290 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static byte[] ParsePrologue(string value)`
- `TryParseInteger`（第 304 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryParseInteger(`
- `Parse`（第 333 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static NativeCapability Parse(`

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
