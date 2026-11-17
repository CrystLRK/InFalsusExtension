# CustomChartPatches.cs

> 源码：`InFalsusChartExt/CustomSong/Chart/CustomChartPatches.cs`
> SHA256：`BCF190B64C2341B1C321622ABB8D1C0FC512EF7E0F09F8C8A3ADD2EBE529E2B1`
> 代码行数：176
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomChartPayloadPatch`（`class`，源码第 19 行）
- `CustomChartPathPatch`（`class`，源码第 67 行）
- `CustomSongCacheBoundaryPatch`（`class`，源码第 101 行）
- `CustomPackScenePatch`（`class`，源码第 124 行）
- `CustomSongStringMappingPatch`（`class`，源码第 152 行）

## 方法/属性职责

- `Prepare`（第 22 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 26 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Prefix`（第 32 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix(`
- `Prepare`（第 70 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 74 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Prefix`（第 78 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix(ref string __0)`
- `Prepare`（第 104 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 108 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Prefix`（第 112 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix()`
- `Prepare`（第 127 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 132 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Prefix`（第 137 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix(`
- `Prepare`（第 155 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 160 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Postfix`（第 165 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`

## 原版/Harmony/native 访问点

- 第 17 行：`[HarmonyPatch]`
- 第 66 行：`[HarmonyPatch]`
- 第 100 行：`[HarmonyPatch]`
- 第 123 行：`[HarmonyPatch]`
- 第 151 行：`[HarmonyPatch]`
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
