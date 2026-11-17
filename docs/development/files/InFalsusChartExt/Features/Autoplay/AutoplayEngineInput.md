# AutoplayEngineInput.cs

> 源码：`InFalsusChartExt/Features/Autoplay/AutoplayEngineInput.cs`
> SHA256：`2B67F831298DFFD3369747A3AFC20976E4E8F16834BB376B5E9A55744858D592`
> 代码行数：1428
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

测试模式开启期间，每个原版判定周期和切谱清理。

## 顶层及类型声明

- `AutoplayEngineInput`（`class`，源码第 13 行）
- `ActiveInput`（`class`，源码第 1417 行）

## 方法/属性职责

- `ResetForChartSwitch`（第 96 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `ReleaseAll`（第 120 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void ReleaseAll()`
- `Tick`（第 136 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(double nowMs, bool judgePass)`
- `TickCursorOnly`（第 185 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void TickCursorOnly(double nowMs)`
- `ShouldDrive`（第 199 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ShouldDrive(`
- `Drive`（第 209 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Drive(`
- `EnsureActive`（第 247 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ActiveInput EnsureActive(`
- `UpdateCursor`（第 266 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateCursor(`
- `TeleportCursor`（第 485 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void TeleportCursor(`
- `StartFlickGesture`（第 512 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void StartFlickGesture(`
- `UpdateFlickGesture`（第 628 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateFlickGesture(`
- `MoveCursor`（第 675 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void MoveCursor(`
- `InjectMouseDelta`（第 722 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool InjectMouseDelta(double deltaX)`
- `SyncVisualCursor`（第 769 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SyncVisualCursor(IntPtr judge)`
- `WriteLanes`（第 814 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool WriteLanes(`
- `ChooseGroundLane`（第 865 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ChooseGroundLane(`
- `TryPressGroundWhenDue`（第 906 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryPressGroundWhenDue(`
- `ReadRuntimeLead`（第 978 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static double ReadRuntimeLead(IntPtr note) =>`
- `TryGetFlickRange`（第 983 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetFlickRange(`
- `TryGetFlickSign`（第 1011 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryGetFlickSign(`
- `ReleaseExpired`（第 1023 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseExpired(double nowMs)`
- `ClearIfGround`（第 1059 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ClearIfGround(`
- `ClearLanes`（第 1070 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ClearLanes(`
- `ConsumeGroundEdges`（第 1119 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void ConsumeGroundEdges()`
- `LaneHeldByOther`（第 1166 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool LaneHeldByOther(`
- `TryGetBarRange`（第 1183 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetBarRange(`
- `Ease`（第 1241 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double Ease(int kind, double progress)`
- `TryGetManagedSkyRange`（第 1252 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetManagedSkyRange(`
- `IsInsideActiveSky`（第 1295 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsInsideActiveSky(double value)`
- `TryGetActiveSkyBounds`（第 1310 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryGetActiveSkyBounds(`
- `ReadFloat`（第 1349 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal static float ReadFloat(`
- `ReadDouble`（第 1355 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal static double ReadDouble(`
- `NotePointer`（第 1361 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static IntPtr NotePointer(`
- `JudgePointer`（第 1379 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static IntPtr JudgePointer()`
- `LooksLikeArray`（第 1388 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool LooksLikeArray(IntPtr array)`
- `Fail`（第 1398 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Fail(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 281 行：`judge, NativeLayoutCatalog.SkyJudge.CursorX);`
- 第 495 行：`Marshal.WriteInt32(`
- 第 496 行：`judge + NativeLayoutCatalog.SkyJudge.CursorRaw,`
- 第 498 行：`Marshal.WriteInt32(`
- 第 499 行：`judge + NativeLayoutCatalog.SkyJudge.CursorX,`
- 第 748 行：`NativeLayoutCatalog.SkyJudge.CursorX);`
- 第 751 行：`Marshal.WriteInt32(`
- 第 753 行：`NativeLayoutCatalog.SkyJudge.CursorRaw,`
- 第 791 行：`NativeLayoutCatalog.SkyJudge.CursorX);`
- 第 794 行：`NativeLayoutCatalog.SkyJudge.CursorRaw);`
- 第 821 行：`IntPtr states = Marshal.ReadIntPtr(`
- 第 822 行：`judge + NativeLayoutCatalog.SkyJudge.InputStates);`
- 第 826 行：`int length = Marshal.ReadInt32(`
- 第 828 行：`NativeLayoutCatalog.ArrayLengthOffset);`
- 第 833 行：`states, NativeLayoutCatalog.ArrayDataOffset);`
- 第 835 行：`NativeLayoutCatalog.SkyJudge.InputStateStride;`
- 第 839 行：`Marshal.WriteByte(`
- 第 841 行：`NativeLayoutCatalog.SkyJudge.InputHeld,`
- 第 845 行：`Marshal.WriteByte(`
- 第 847 行：`NativeLayoutCatalog.SkyJudge.InputConsumed,`
- 第 849 行：`Marshal.WriteByte(`
- 第 851 行：`NativeLayoutCatalog.SkyJudge.InputPressed,`
- 第 853 行：`Marshal.WriteInt64(`
- 第 855 行：`NativeLayoutCatalog.SkyJudge.InputPressedAt,`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 88 行：`ReleaseAll();`
- 第 96 行：`internal static void ResetForChartSwitch()`
- 第 98 行：`ReleaseAll();`
- 第 99 行：`SwipedFlicks.Clear();`
- 第 100 行：`TimingLogged.Clear();`
- 第 120 行：`internal static void ReleaseAll()`
- 第 126 行：`ClearIfGround(input, false);`
- 第 132 行：`Active.Clear();`
- 第 145 行：`ReleaseExpired(nowMs);`
- 第 232 行：`input.ReleaseAtMs = nowMs +`
- 第 255 行：`ReleaseAtMs =`
- 第 1023 行：`private static void ReleaseExpired(double nowMs)`
- 第 1025 行：`Scratch.Clear();`
- 第 1037 行：`pair.Value.ReleaseAtMs ==`
- 第 1045 行：`else if (nowMs < pair.Value.ReleaseAtMs)`
- 第 1054 行：`ClearIfGround(input, true);`
- 第 1059 行：`private static void ClearIfGround(`
- 第 1064 行：`ClearLanes(`
- 第 1070 行：`private static void ClearLanes(`
- 第 1425 行：`internal double ReleaseAtMs;`

## 性能边界

- 包含帧级入口：`Tick`, `TickCursorOnly`, `UpdateCursor`, `UpdateFlickGesture`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
