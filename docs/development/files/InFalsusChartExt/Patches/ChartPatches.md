# ChartPatches.cs

> 源码：`InFalsusChartExt/Patches/ChartPatches.cs`
> SHA256：`D614167B900C97D783E7A504FB23299C00108BF01B15DCBA5264248AEFD0D5F0`
> 代码行数：969
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

_Gab/_Kb/_Ib` 入口与 stage facade

## 生命周期

Harmony 目标方法调用时；生命周期跟随原版调用方。

## 顶层及类型声明

- `ChartParsePatch`（`class`，源码第 17 行）
- `CompiledRecordWarmup`（`class`，源码第 780 行）
- `ChartCompilePatch`（`class`，源码第 831 行）

## 方法/属性职责

- `Prepare`（第 26 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 30 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static System.Reflection.MethodBase TargetMethod() =>`
- `Prefix`（第 34 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix(ref Il2CppSystem.ReadOnlySpan<byte> __0, string __1)`
- `Postfix`（第 41 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix()`
- `Finalizer`（第 45 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Exception Finalizer(Exception __exception)`
- `HookPostfix`（第 53 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void HookPostfix(ref Il2CppSystem.ReadOnlySpan<byte> __0, ref string __1)`
- `TransformSpan`（第 56 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void TransformSpan(ref Il2CppSystem.ReadOnlySpan<byte> span, string key, string source)`
- `IsCustomChartKey`（第 616 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsCustomChartKey(string key)`
- `CaptureGlobalSpeedEvents`（第 635 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CaptureGlobalSpeedEvents(`
- `CompareTimeAndOrder`（第 657 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CompareTimeAndOrder(`
- `CompareTimeAndOrder`（第 664 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CompareTimeAndOrder(`
- `CompareTimeAndOrder`（第 671 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CompareTimeAndOrder(`
- `CompareTimeAndOrder`（第 678 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CompareTimeAndOrder(`
- `CompareTimeAndOrder`（第 685 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CompareTimeAndOrder(`
- `CompareTimeAndOrder`（第 692 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CompareTimeAndOrder(`
- `ReplaceEncodedSpan`（第 704 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReplaceEncodedSpan(`
- `PinSpanArray`（第 719 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void PinSpanArray(object managed)`
- `ReleasePinnedArrays`（第 724 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleasePinnedArrays()`
- `EndGabCall`（第 729 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EndGabCall()`
- `TryCopy`（第 740 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static byte[] TryCopy(IntPtr pointer, int length, ref string via, string candidate)`
- `Head`（第 757 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string Head(byte[] data)`
- `LogOnce`（第 765 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogOnce(string source, string key, int length, string head, string status, string extra)`
- `Prepare`（第 785 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 790 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static System.Reflection.MethodBase TargetMethod() =>`
- `Prefix`（第 795 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix(Il2Cppifapp.Game.LogicalNotePlayer __instance)`
- `Prepare`（第 836 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 841 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static System.Reflection.MethodBase TargetMethod() =>`
- `Prefix`（第 846 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix()`
- `Postfix`（第 859 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(Il2Cppifapp.Game.LogicalNotePlayer __instance)`

## 原版/Harmony/native 访问点

- 第 15 行：`[HarmonyPatch]`
- 第 779 行：`[HarmonyPatch]`
- 第 830 行：`[HarmonyPatch]`
- 第 105 行：`p = box == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(Il2CppInterop.Runtime.IL2CPP.il2cpp_object_unbox(box));`
- 第 745 行：`if (Marshal.ReadInt32(pointer) != 0x31504349) return null; // 'I','C','P','1'`
- 第 747 行：`Marshal.Copy(pointer, data, 0, length);`
- 第 891 行：`for (int side = 0; side < NativeLayoutCatalog.SideCount; side++)`
- 第 932 行：`StartMs = Marshal.ReadInt32(`
- 第 933 行：`notesData + i * NativeLayoutCatalog.NoteStride +`
- 第 934 行：`NativeLayoutCatalog.Note.StartMs),`
- 第 935 行：`EndMs = Marshal.ReadInt32(`
- 第 936 行：`notesData + i * NativeLayoutCatalog.NoteStride +`
- 第 937 行：`NativeLayoutCatalog.Note.EndMs),`

## 所有权与释放点

- 第 349 行：`ChartExtState.HideGroupControlledTimingGroups.Clear();`
- 第 505 行：`ChartExtState.NoteTimingGroup.Clear();`
- 第 506 行：`ChartExtState.BombIds.Clear();`
- 第 529 行：`ChartExtState.SkyAreaGroupById.Clear();`
- 第 555 行：`chart.Notes.Clear();`
- 第 557 行：`chart.Events.Clear();`
- 第 593 行：`ChartRuntimeLifecycle.DisposeForChartTransform();`
- 第 638 行：`ChartExtState.SpeedEvents.Clear();`
- 第 724 行：`private static void ReleasePinnedArrays()`
- 第 726 行：`PinnedArrays.Clear();`
- 第 734 行：`ReleasePinnedArrays();`
- 第 851 行：`AutoplayRuntime.ResetForChartSwitch();`
- 第 854 行：`ChartExtState.SkyAreaSlots.Clear();`
- 第 855 行：`ChartExtState.GroupedSlots.Clear();`
- 第 886 行：`ChartExtState.AutoplayDues.Clear();`

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
