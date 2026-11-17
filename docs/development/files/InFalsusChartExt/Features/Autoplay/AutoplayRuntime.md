# AutoplayRuntime.cs

> 源码：`InFalsusChartExt/Features/Autoplay/AutoplayRuntime.cs`
> SHA256：`F958E34FE22E4DD2982E4B65213BE92F79EEE1341A3A8B1201182CD0280E8D5D`
> 代码行数：392
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

autoplay 游标、`_Oz` 记录改写

## 生命周期

测试模式开启期间，每个原版判定周期和切谱清理。

## 顶层及类型声明

- `AutoplayMode`（`enum`，源码第 8 行）
- `AutoplayRuntime`（`class`，源码第 25 行）

## 方法/属性职责

- `Tick`（第 129 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `ResetForChartSwitch`（第 148 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `ApplyInputs`（第 159 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void ApplyInputs(bool judgePass)`
- `ResetCursors`（第 196 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ResetCursors()`
- `UpdateActiveHolds`（第 203 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateActiveHolds(double now)`
- `ApplyOutcomes`（第 227 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void ApplyOutcomes()`
- `RewriteDue`（第 292 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool RewriteDue(`
- `NowMs`（第 384 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double NowMs()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 306 行：`due.Position * NativeLayoutCatalog.NoteStride;`
- 第 307 行：`if (Marshal.ReadInt64(`
- 第 308 行：`note + NativeLayoutCatalog.Note.Id) != due.Id)`
- 第 311 行：`Marshal.ReadInt64(`
- 第 312 行：`note + NativeLayoutCatalog.Note.RuntimeLead));`
- 第 323 行：`int arrayLength = Marshal.ReadInt32(`
- 第 325 行：`NativeLayoutCatalog.ArrayLengthOffset);`
- 第 356 行：`NativeLayoutCatalog.Record`

## 所有权与释放点

- 第 52 行：`AutoplayEngineInput.ReleaseAll();`
- 第 53 行：`RdAutoplayRuntime.ReleaseAll();`
- 第 83 行：`AutoplayEngineInput.ReleaseAll();`
- 第 84 行：`RdAutoplayRuntime.ReleaseAll();`
- 第 116 行：`AutoplayEngineInput.ReleaseAll();`
- 第 117 行：`RdAutoplayRuntime.ReleaseAll();`
- 第 148 行：`internal static void ResetForChartSwitch()`
- 第 151 行：`AutoplayEngineInput.ResetForChartSwitch();`
- 第 152 行：`RdAutoplayRuntime.ResetForChartSwitch();`
- 第 170 行：`AutoplayEngineInput.ReleaseAll();`
- 第 200 行：`ActiveHolds.Clear();`

## 性能边界

- 包含帧级入口：`Tick`, `UpdateActiveHolds`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
