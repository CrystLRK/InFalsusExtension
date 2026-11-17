# Icp1Codec.cs

> 源码：`InFalsusChartExt/Core/ChartFormat/Icp1Codec.cs`
> SHA256：`14C598A1C211C3119716E5DDE13723BA3BEA82A26CC2F94099273C48FD44D516`
> 代码行数：449
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

ICP1 文件编解码

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `Icp1Codec`（`class`，源码第 12 行）
- `Note`（`class`，源码第 20 行）
- `Event`（`class`，源码第 61 行）
- `Chart`（`class`，源码第 70 行）
- `State`（`struct`，源码第 79 行）

## 方法/属性职责

- `TryGetScaledPair`（第 41 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal bool TryGetScaledPair(`
- `Rol`（第 85 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ulong Rol(ulong value, int amount) => (value << amount) | (value >> (64 - amount));`
- `U32`（第 87 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static uint U32(byte[] data, int offset) =>`
- `U64`（第 90 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ulong U64(byte[] data, int offset) =>`
- `S32`（第 93 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int S32(uint value) => unchecked((int)value);`
- `PositiveGcd`（第 95 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int PositiveGcd(int a, int b)`
- `Gen`（第 109 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ulong Gen(State s, uint idx, uint k)`
- `Finalize`（第 127 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static State Finalize(State s, ulong rdx, ulong r8, ulong r9)`
- `DeriveKey`（第 177 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static State DeriveKey(string seed, int count, ushort u16) =>`
- `DeriveKeyCore`（第 181 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static State DeriveKeyCore(string seed, int count, ushort u16)`
- `FinalizeRecordState`（第 245 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static State FinalizeRecordState(State s, int noteIndex, long field0, int[] pairGcds)`
- `PairLow`（第 268 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ulong PairLow(int value) => value > 0 ? (ulong)value : (uint)value;`
- `PairHigh`（第 270 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ulong PairHigh(int value) => (ulong)(uint)value;`
- `AdvanceNote`（第 276 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static State AdvanceNote(State state, long field0, long group, uint startMs, uint endMs,`
- `ScalePair`（第 293 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ScalePair(int scale, int num, int den, out int outNum, out int outDen)`
- `TryDecode`（第 304 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryDecode(byte[] data, string chartKey, out Chart chart)`
- `Encode`（第 378 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static byte[] Encode(Chart chart, string chartKey)`
- `WriteU32`（第 435 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteU32(byte[] data, int offset, uint value)`
- `WriteU64`（第 443 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteU64(byte[] data, int offset, ulong value)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- 323/323 official raw + semantic roundtrip
- Python unittest
- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
