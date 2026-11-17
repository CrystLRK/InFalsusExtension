# CustomChartPayloadSource.cs

> 源码：`InFalsusChartExt/CustomSong/Chart/CustomChartPayloadSource.cs`
> SHA256：`242A59DD809C4FEBFB0F175FAB5C57A07A03211560B17A6766610C936B8E4BD8`
> 代码行数：149
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomChartPayload`（`class`，源码第 9 行）
- `CustomChartPayloadSource`（`class`，源码第 26 行）

## 方法/属性职责

- `GetOrLoad`（第 33 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal CustomChartPayload GetOrLoad(CustomChartPlan plan)`
- `Clear`（第 73 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal void Clear()`
- `Dispose`（第 78 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`public void Dispose()`
- `DecodeSam`（第 83 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DecodeSam(byte[] bytes)`
- `TrimToStructuralLength`（第 92 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void TrimToStructuralLength(ref byte[] bytes)`
- `IsPlausible`（第 120 行）：在 parser key 解码后检查 note/event 基础域；失败时拒绝 payload，避免密文错钥解码结果进入游戏。
  - 签名：`private static bool IsPlausible(Icp1Codec.Chart chart)`
- `ReadUInt32`（第 143 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static uint ReadUInt32(byte[] bytes, int offset) =>`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 73 行：`internal void Clear()`
- 第 75 行：`_payloads.Clear();`
- 第 78 行：`public void Dispose()`
- 第 80 行：`Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
