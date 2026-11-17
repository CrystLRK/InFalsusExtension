# NativeCapacityPatch.cs

> 源码：`InFalsusChartExt/Compatibility/NativeCapacityPatch.cs`
> SHA256：`ACC568F2178B9632311136F15F154DC53335EAF393ACE1104C47716E0969918C`
> 代码行数：563
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

按文件入口和调用方确定；变更时必须补充。

## 顶层及类型声明

- `NumericLimitPatch`（`class`，源码第 15 行）

## 方法/属性职责

- `VirtualProtect`（第 84 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static extern bool VirtualProtect(`
- `GetCurrentProcess`（第 91 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static extern IntPtr GetCurrentProcess();`
- `FlushInstructionCache`（第 94 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static extern bool FlushInstructionCache(`
- `Apply`（第 100 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void Apply()`
- `LoadConfiguration`（第 244 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LoadConfiguration()`
- `ValidateConfiguration`（第 276 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ValidateConfiguration()`
- `ValidateCapacity`（第 285 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ValidateCapacity(string name, int value)`
- `FindSideImmediates`（第 293 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static List<int> FindSideImmediates(`
- `TryFindChartSites`（第 315 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryFindChartSites(IntPtr method, List<ImmediateSite> sites)`
- `FindJudgeStateSites`（第 385 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static List<int> FindJudgeStateSites(`
- `ValidateTarget`（第 404 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ValidateTarget(PatchTarget target)`
- `WriteImmediate`（第 432 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool WriteImmediate(PatchTarget target)`
- `ReadImmediateAddress`（第 495 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static IntPtr ReadImmediateAddress(`
- `ReadImmediate`（第 500 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static int ReadImmediate(PatchTarget target)`
- `Rollback`（第 504 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Rollback(`
- `Log`（第 544 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Log(string message)`
- `Warn`（第 549 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Warn(string message)`
- `Abort`（第 555 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Abort(string message)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 24 行：`NativeLayoutCatalog.Capacity.OriginalSideNote;`
- 第 26 行：`NativeLayoutCatalog.Capacity.OriginalAuxTable;`
- 第 28 行：`NativeLayoutCatalog.Capacity.OriginalRecord;`
- 第 30 行：`NativeLayoutCatalog.Capacity.OriginalJudgeState;`
- 第 83 行：`[DllImport("kernel32.dll", SetLastError = true)]`
- 第 84 行：`private static extern bool VirtualProtect(`
- 第 90 行：`[DllImport("kernel32.dll")]`
- 第 93 行：`[DllImport("kernel32.dll", SetLastError = true)]`
- 第 301 行：`if (Marshal.ReadByte(method, offset) !=`
- 第 304 行：`int value = Marshal.ReadInt32(method, offset + 1);`
- 第 307 行：`if (Marshal.ReadByte(method, offset + 5) !=`
- 第 322 行：`if (Marshal.ReadByte(method, offset) !=`
- 第 325 行：`int value = Marshal.ReadInt32(method, offset + 1);`
- 第 393 行：`if (Marshal.ReadByte(method, offset) !=`
- 第 396 行：`if (Marshal.ReadInt32(method, offset + 1) !=`
- 第 411 行：`if (Marshal.ReadByte(opcode) !=`
- 第 413 行：`Marshal.ReadInt32(immediate) !=`
- 第 437 行：`if (!VirtualProtect(`
- 第 444 行：`$"{target.Site.Name}: VirtualProtect failed " +`
- 第 445 行：`$"err={Marshal.GetLastWin32Error()}");`
- 第 448 行：`Marshal.WriteInt32(`
- 第 452 行：`bool restored = VirtualProtect(`
- 第 458 行：`if (VirtualProtect(`
- 第 464 行：`Marshal.WriteInt32(`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

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
