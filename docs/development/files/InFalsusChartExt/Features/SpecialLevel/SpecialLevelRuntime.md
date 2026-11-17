# SpecialLevelRuntime.cs

> 源码：`InFalsusChartExt/Features/SpecialLevel/SpecialLevelRuntime.cs`
> SHA256：`20C55931EA267C5101E2973D775D21999EA9E2998A8B1A8F34B97F26430B0ADC`
> 代码行数：1122
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

sp` 难度文字 UI 接管、字体借用与原版字段读取

## 生命周期

按文件入口和调用方确定；变更时必须补充。

## 顶层及类型声明

- `SpecialLevelRuntime`（`class`，源码第 34 行）
- `FontState`（`struct`，源码第 36 行）
- `PendingLevel`（`class`，源码第 46 行）
- `LargeCardPatch`（`class`，源码第 737 行）
- `DifficultyButtonPatch`（`class`，源码第 785 行）
- `HudPatch`（`class`，源码第 873 行）
- `TransitionPatch`（`class`，源码第 894 行）
- `TransitionAfterDivePatch`（`class`，源码第 916 行）
- `ResultsPatch`（`class`，源码第 938 行）
- `SmallCardPatch`（`class`，源码第 963 行）
- `PackSongCardPatch`（`class`，源码第 993 行）
- `PackSongCardAltPatch`（`class`，源码第 1013 行）
- `PackSongCardDifficultyPatch`（`class`，源码第 1033 行）
- `SelectionPatch`（`class`，源码第 1055 行）
- `SelectionDifficultyPatch`（`class`，源码第 1078 行）
- `SelectionDifficultyAltPatch`（`class`，源码第 1092 行）

## 方法/属性职责

- `ResetForScene`（第 70 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForScene()`
- `Tick`（第 82 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `InstallLateApply`（第 97 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void InstallLateApply()`
- `OnWillRenderCanvases`（第 114 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void OnWillRenderCanvases()`
- `OnGUI`（第 119 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnGUI()`
- `TickHud`（第 126 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void TickHud()`
- `TickTransition`（第 182 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void TickTransition()`
- `TickResults`（第 222 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void TickResults()`
- `SlotOf`（第 286 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int SlotOf(ChartDifficultyFlag difficulty)`
- `ChartOf`（第 298 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SongChartInfo ChartOf(`
- `SpecialOf`（第 315 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string SpecialOf(SongInfo song, int slot)`
- `IsCustomSong`（第 329 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsCustomSong(SongInfo song)`
- `CurrentText`（第 343 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string CurrentText(`
- `HasNonAscii`（第 356 行）：判断当前对象/状态条件。
  - 签名：`private static bool HasNonAscii(string value)`
- `ApplyText`（第 363 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyText(`
- `SwapFont`（第 446 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SwapFont(`
- `RestoreFont`（第 475 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RestoreFont(`
- `SetFamily`（第 494 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SetFamily(`
- `ApplyConstrained`（第 508 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyConstrained(`
- `ApplyConstrainedArray`（第 522 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyConstrainedArray(`
- `ApplyTextArray`（第 535 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyTextArray(`
- `DonorFromCard`（第 548 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Il2CppFastText.FastText DonorFromCard(`
- `DonorFromHud`（第 571 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Il2CppFastText.FastText DonorFromHud(`
- `DonorFromTransition`（第 595 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Il2CppFastText.FastText DonorFromTransition(`
- `DonorFromSmall`（第 620 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Il2CppFastText.FastText DonorFromSmall(`
- `DonorFromResults`（第 631 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Il2CppFastText.FastText DonorFromResults(`
- `DonorFromPackCard`（第 646 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Il2CppFastText.FastText DonorFromPackCard(`
- `ApplyPackCard`（第 671 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyPackCard(`
- `RefreshScene`（第 684 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RefreshScene(SongSelectScene scene)`
- `Guard`（第 728 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Guard(string what, Exception e)`
- `Postfix`（第 740 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(LargeSongCard __instance)`
- `Prepare`（第 788 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare()`
- `TargetMethod`（第 794 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod()`
- `Postfix`（第 805 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`
- `ApplyPending`（第 841 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyPending(SongInfo song, int slot)`
- `Postfix`（第 876 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(SongInfoContainer __instance)`
- `Postfix`（第 897 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(TransitionLayer __instance)`
- `Postfix`（第 919 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(TransitionLayer __instance)`
- `Postfix`（第 941 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(ResultsJacketContainer __instance)`
- `Postfix`（第 966 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(SmallSongCard __instance)`
- `Postfix`（第 996 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`
- `Postfix`（第 1016 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`
- `Postfix`（第 1036 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(PackSongCardMember __instance)`
- `Postfix`（第 1058 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`
- `Postfix`（第 1081 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(SongSelectScene __instance)`
- `Postfix`（第 1095 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(SongSelectScene __instance)`
- `FindScene`（第 1105 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SongSelectScene FindScene()`

## 原版/Harmony/native 访问点

- 第 736 行：`[HarmonyPatch(typeof(LargeSongCard), "_OS")]`
- 第 784 行：`[HarmonyPatch]`
- 第 872 行：`[HarmonyPatch(typeof(SongInfoContainer), "_QCA")]`
- 第 893 行：`[HarmonyPatch(typeof(TransitionLayer), "_IA")]`
- 第 915 行：`[HarmonyPatch(typeof(TransitionLayer), "_jA")]`
- 第 937 行：`[HarmonyPatch(typeof(ResultsJacketContainer), "_Wm")]`
- 第 962 行：`[HarmonyPatch(typeof(SmallSongCard), "_tS")]`
- 第 992 行：`[HarmonyPatch(typeof(PackSongCardMember), "_pc")]`
- 第 1012 行：`[HarmonyPatch(typeof(PackSongCardMember), "_ef")]`
- 第 1032 行：`[HarmonyPatch(typeof(PackSongCardMember), "_qc")]`
- 第 1054 行：`[HarmonyPatch(typeof(SongSelectScene), "_MN")]`
- 第 1077 行：`[HarmonyPatch(typeof(SongSelectScene), "_pN")]`
- 第 1091 行：`[HarmonyPatch(typeof(SongSelectScene), "_uN")]`
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 72 行：`Originals.Clear();`
- 第 73 行：`Applied.Clear();`
- 第 74 行：`Pending.Clear();`
- 第 410 行：`if (Applied.Count >= 64) Applied.Clear();`
- 第 415 行：`if (Applied.Count >= 64) Applied.Clear();`
- 第 457 行：`if (Originals.Count >= 64) Originals.Clear();`

## 性能边界

- 包含帧级入口：`Tick`, `TickHud`, `TickResults`, `TickTransition`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
