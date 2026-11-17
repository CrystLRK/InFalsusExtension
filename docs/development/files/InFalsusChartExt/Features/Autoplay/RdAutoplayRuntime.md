# RdAutoplayRuntime.cs

> 源码：`InFalsusChartExt/Features/Autoplay/RdAutoplayRuntime.cs`
> SHA256：`6075691FC192A47A4AD8BA4182EEDC5763B4B72BDADA659CDD09731EE31B25E5`
> 代码行数：267
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

测试模式开启期间，每个原版判定周期和切谱清理。

## 顶层及类型声明

- `RdAutoplayRuntime`（`class`，源码第 12 行）

## 方法/属性职责

- `ResetForChartSwitch`（第 36 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `Tick`（第 45 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(double nowMs, bool judgePass)`
- `ReleaseAll`（第 68 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void ReleaseAll()`
- `PinSkyBar`（第 83 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void PinSkyBar(IntPtr judge)`
- `PressGround`（第 98 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void PressGround(IntPtr judge, double nowMs)`
- `ReleaseLanes`（第 154 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseLanes(`
- `ChooseLane`（第 172 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int ChooseLane(`
- `LaneData`（第 205 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr LaneData(`
- `Entry`（第 223 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr Entry(IntPtr data, int lane) =>`
- `ClearLane`（第 229 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ClearLane(IntPtr data, int lane)`
- `LogError`（第 250 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 87 行：`Marshal.WriteInt64(`
- 第 88 行：`judge + NativeLayoutCatalog.SkyJudge.WarningTime,`
- 第 90 行：`Marshal.WriteInt64(`
- 第 91 行：`judge + NativeLayoutCatalog.SkyJudge.PenaltyTime,`
- 第 93 行：`Marshal.WriteByte(`
- 第 94 行：`judge + NativeLayoutCatalog.SkyJudge.StartOk,`
- 第 127 行：`Marshal.WriteByte(`
- 第 129 行：`NativeLayoutCatalog.SkyJudge.InputHeld,`
- 第 131 行：`Marshal.WriteByte(`
- 第 133 行：`NativeLayoutCatalog.SkyJudge.InputConsumed,`
- 第 135 行：`Marshal.WriteByte(`
- 第 137 行：`NativeLayoutCatalog.SkyJudge.InputPressed,`
- 第 139 行：`Marshal.WriteInt64(`
- 第 141 行：`NativeLayoutCatalog.SkyJudge.InputLastInput,`
- 第 177 行：`int first = Marshal.ReadInt32(`
- 第 178 行：`note + NativeLayoutCatalog.Note.LaneFirst);`
- 第 179 行：`int last = Marshal.ReadInt32(`
- 第 180 行：`note + NativeLayoutCatalog.Note.LaneLast);`
- 第 210 行：`IntPtr states = Marshal.ReadIntPtr(`
- 第 211 行：`judge + NativeLayoutCatalog.SkyJudge.InputStates);`
- 第 214 行：`length = Marshal.ReadInt32(`
- 第 216 行：`NativeLayoutCatalog.ArrayLengthOffset);`
- 第 220 行：`states, NativeLayoutCatalog.ArrayDataOffset);`
- 第 227 行：`NativeLayoutCatalog.SkyJudge.InputStateStride);`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 20 行：`private static readonly double[] ReleaseAtMs =`
- 第 29 行：`ReleaseAtMs[i] = -1.0;`
- 第 36 行：`internal static void ResetForChartSwitch()`
- 第 38 行：`Pressed.Clear();`
- 第 40 行：`ReleaseAtMs[i] = -1.0;`
- 第 58 行：`ReleaseLanes(judge, nowMs);`
- 第 68 行：`internal static void ReleaseAll()`
- 第 77 行：`ClearLane(data, lane);`
- 第 80 行：`ResetForChartSwitch();`
- 第 147 行：`ReleaseAtMs[lane] = Math.Max(`
- 第 148 行：`ReleaseAtMs[lane], end);`
- 第 154 行：`private static void ReleaseLanes(`
- 第 164 行：`if (ReleaseAtMs[lane] < 0.0 ||`
- 第 165 行：`nowMs < ReleaseAtMs[lane])`
- 第 167 行：`ClearLane(data, lane);`
- 第 168 行：`ReleaseAtMs[lane] = -1.0;`
- 第 191 行：`if (ReleaseAtMs[lane] >= 0.0)`
- 第 229 行：`private static void ClearLane(IntPtr data, int lane)`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
