# CustomSongAudioController.cs

> 源码：`InFalsusChartExt/CustomSong/Audio/CustomSongAudioController.cs`
> SHA256：`80D051FD3706182E70837E7C9BBD8F5A64AE23CD046AF4B1857C93DE8CDC1C75`
> 代码行数：1015
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomSongAudioController`（`class`，源码第 24 行）
- `AudioFactoryPatch`（`class`，源码第 111 行）
- `Route`（`class`，源码第 936 行）
- `FactoryDiagnostic`（`class`，源码第 954 行）
- `Pending`（`class`，源码第 969 行）
- `DeferredRoute`（`class`，源码第 988 行）
- `OwnedAudio`（`class`，源码第 1004 行）

## 方法/属性职责

- `Reset`（第 42 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Reset()`
- `PrepareForShutdown`（第 54 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void PrepareForShutdown()`
- `Tick`（第 59 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `ReleaseOwnedForSceneUnload`（第 88 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void ReleaseOwnedForSceneUnload()`
- `Prepare`（第 114 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `Prefix`（第 125 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix(`
- `Postfix`（第 166 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(_Cg __result)`
- `ResolveDeferred`（第 219 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ResolveDeferred()`
- `TryReplace`（第 264 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static void TryReplace(Pending pending)`
- `Replace`（第 278 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Replace(Pending pending)`
- `ConfigureChannel`（第 481 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ConfigureChannel(`
- `Require`（第 535 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Require(`
- `TryGetNativeSound`（第 546 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryGetNativeSound(`
- `TryGetNativeChannel`（第 563 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryGetNativeChannel(`
- `IsNativeAudioConfigured`（第 580 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsNativeAudioConfigured(_Cg controller)`
- `IsMpegAudioFile`（第 593 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsMpegAudioFile(string path)`
- `ResolveSystem`（第 613 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr ResolveSystem(IntPtr sound)`
- `TryReadGameSystem`（第 626 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryReadGameSystem(out IntPtr system)`
- `TrackOwned`（第 649 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void TrackOwned(`
- `TrackSelectionOwned`（第 680 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void TrackSelectionOwned(`
- `ReleaseSelectionOwned`（第 696 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseSelectionOwned()`
- `ReleaseSelectionOwnedIfGameplayActive`（第 708 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseSelectionOwnedIfGameplayActive()`
- `ReleaseDisposedControllerAudio`（第 750 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ReleaseDisposedControllerAudio()`
- `SynchronizeGameplayAudioPause`（第 823 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SynchronizeGameplayAudioPause()`
- `SetOwnedPaused`（第 847 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SetOwnedPaused(`
- `ReleaseAllOwned`（第 876 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseAllOwned()`
- `ReleaseOwned`（第 883 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseOwned(OwnedAudio owned)`
- `ShouldDeferNativeRelease`（第 911 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ShouldDeferNativeRelease()`
- `DifficultySlot`（第 916 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int DifficultySlot(CustomSongPlan song)`
- `ToMilliseconds`（第 929 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static uint ToMilliseconds(double seconds) =>`

## 原版/Harmony/native 访问点

- 第 110 行：`[HarmonyPatch]`
- 第 292 行：`NativeLayoutCatalog.CustomSong.AudioChannel);`
- 第 295 行：`NativeLayoutCatalog.CustomSong.AudioSound);`
- 第 297 行：`Marshal.ReadIntPtr(channelPointer);`
- 第 337 行：`cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),`
- 第 449 行：`Marshal.WriteIntPtr(`
- 第 451 行：`Marshal.WriteIntPtr(`
- 第 556 行：`sound = Marshal.ReadIntPtr(`
- 第 559 行：`NativeLayoutCatalog.CustomSong.AudioSound));`
- 第 573 行：`channel = Marshal.ReadIntPtr(`
- 第 576 行：`NativeLayoutCatalog.CustomSong.AudioChannel));`
- 第 587 行：`return Marshal.ReadByte(`
- 第 590 行：`NativeLayoutCatalog.CustomSong.AudioConfigured)) != 0;`
- 第 774 行：`IntPtr currentSound = Marshal.ReadIntPtr(`
- 第 776 行：`NativeLayoutCatalog.CustomSong.AudioSound);`
- 第 777 行：`IntPtr currentChannel = Marshal.ReadIntPtr(`
- 第 779 行：`NativeLayoutCatalog.CustomSong.AudioChannel);`

## 所有权与释放点

- 第 44 行：`Waiting.Clear();`
- 第 45 行：`Deferred.Clear();`
- 第 46 行：`_routes?.Clear();`
- 第 49 行：`ReleaseAllOwned();`
- 第 63 行：`ReleaseDisposedControllerAudio();`
- 第 64 行：`ReleaseSelectionOwnedIfGameplayActive();`
- 第 88 行：`internal static void ReleaseOwnedForSceneUnload()`
- 第 90 行：`Waiting.Clear();`
- 第 91 行：`Deferred.Clear();`
- 第 105 行：`ReleaseOwned(owned);`
- 第 150 行：`ReleaseSelectionOwned();`
- 第 433 行：`!ShouldDeferNativeRelease())`
- 第 448 行：`ReleaseSelectionOwned();`
- 第 476 行：`FmodExports.Release(replacement.handle);`
- 第 665 行：`ReleaseOwned(previous);`
- 第 677 行：`ReleaseOwned(replaced);`
- 第 685 行：`ReleaseSelectionOwned();`
- 第 696 行：`private static void ReleaseSelectionOwned()`
- 第 704 行：`ReleaseOwned(owned);`
- 第 708 行：`private static void ReleaseSelectionOwnedIfGameplayActive()`
- 第 747 行：`ReleaseSelectionOwned();`
- 第 750 行：`private static void ReleaseDisposedControllerAudio()`
- 第 768 行：`ReleaseOwned(owned);`
- 第 801 行：`ReleaseOwned(owned);`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
