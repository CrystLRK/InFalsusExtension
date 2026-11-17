# AutoplayResultsDecorator.cs

> 源码：`InFalsusChartExt/Features/Autoplay/AutoplayResultsDecorator.cs`
> SHA256：`CCD9ADEACC3FB1E6B68690D43908F41DA2AA4218BD3543799170600649223964`
> 代码行数：239
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

测试模式开启期间，每个原版判定周期和切谱清理。

## 顶层及类型声明

- `AutoplayResultsDecorator`（`class`，源码第 22 行）
- `AutoplayResultsScenePatch`（`class`，源码第 133 行）
- `AutoplayDropsAwakePatch`（`class`，源码第 160 行）
- `AutoplayEncounterWritePatch`（`class`，源码第 189 行）
- `AutoplayInventoryAddPatch`（`class`，源码第 214 行）

## 方法/属性职责

- `Apply`（第 26 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void Apply(ResultsScene scene)`
- `Apply`（第 47 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void Apply(IotaDropsContainer container)`
- `HideCells`（第 66 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void HideCells(`
- `WriteTexts`（第 85 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteTexts(`
- `Set`（第 102 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Set(`
- `LogError`（第 114 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(`
- `Prepare`（第 136 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 141 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Prefix`（第 147 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix() =>`
- `Postfix`（第 152 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(ResultsScene __instance)`
- `Prepare`（第 163 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 171 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Postfix`（第 177 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`
- `Prepare`（第 192 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare()`
- `TargetMethod`（第 203 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Prefix`（第 209 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prefix() =>`
- `Prepare`（第 217 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare()`
- `TargetMethod`（第 229 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Prefix`（第 236 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prefix() =>`

## 原版/Harmony/native 访问点

- 第 132 行：`[HarmonyPatch]`
- 第 159 行：`[HarmonyPatch]`
- 第 188 行：`[HarmonyPatch]`
- 第 213 行：`[HarmonyPatch]`
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
