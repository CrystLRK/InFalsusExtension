# SkyBlockVisualRuntime.cs

> 源码：`InFalsusChartExt/Features/SkyArea/SkyBlockVisualRuntime.cs`
> SHA256：`1E0C867FC42676626C0C822FD62C4907A3D78BE12DBFDF45424D973A09B69D16`
> 代码行数：1258
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

SkyBlock native-prefab 独立视觉

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `SkyBlockVisualRuntime`（`class`，源码第 14 行）
- `Entry`（`class`，源码第 22 行）

## 方法/属性职责

- `OnChartCompiled`（第 58 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnChartCompiled()`
- `Tick`（第 91 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(`
- `ResetForChartSwitch`（第 227 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `RefreshActiveMembers`（第 239 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RefreshActiveMembers(`
- `SpawnLeadMs`（第 293 行）：计算独立 SkyArea 的生成提前量；普通 group 优先采用原生 `RuntimeLead`，timinggroup 保留组内视觉时间。
  - 签名：`private static double SpawnLeadMs(`
- `PositionLeadMs`（第 306 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double PositionLeadMs(`
- `TryGetVisualTiming`（第 320 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetVisualTiming(`
- `TryGetCachedGlobalVisualTiming`（第 365 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryGetCachedGlobalVisualTiming(`
- `GlobalVisualNow`（第 396 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double GlobalVisualNow(double now)`
- `RawProgress`（第 411 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static double RawProgress(`
- `ShouldTrackMember`（第 427 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ShouldTrackMember(`
- `ShouldRetainUnavailableMember`（第 433 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static bool ShouldRetainUnavailableMember(`
- `ShouldReleaseAfterLine`（第 440 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ShouldReleaseAfterLine(`
- `ShouldRetainWholeStrip`（第 454 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ShouldRetainWholeStrip(`
- `ReleaseActiveAt`（第 462 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseActiveAt(int index)`
- `SyncFinalRecordState`（第 471 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static unsafe void SyncFinalRecordState(`
- `TryReadFinalRecordState`（第 488 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static unsafe bool TryReadFinalRecordState(`
- `CreateEntry`（第 538 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static Entry CreateEntry(`
- `AcquireEntry`（第 592 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Entry AcquireEntry(`
- `ReleaseMember`（第 613 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseMember(`
- `CreateArea`（第 627 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static GameObject CreateArea(`
- `UpdateEntry`（第 672 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static unsafe void UpdateEntry(`
- `GetRenderRange`（第 810 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Vector2 GetRenderRange(`
- `ApplyColor`（第 819 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyColor(`
- `UpdateBrackets`（第 852 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateBrackets(`
- `CreateBrackets`（第 902 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static void CreateBrackets(`
- `CloneMaterials`（第 951 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static List<Material> CloneMaterials(GameObject root)`
- `SetBracketsActive`（第 990 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SetBracketsActive(`
- `Hide`（第 1005 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Hide(Entry entry)`
- `DestroyAll`（第 1021 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyAll()`
- `DestroyEntry`（第 1032 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyEntry(Entry entry)`
- `TryDestroy`（第 1043 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static void TryDestroy(GameObject go)`
- `DestroyMaterials`（第 1054 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyMaterials(`
- `ReadVector3`（第 1072 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Vector3 ReadVector3(IntPtr address)`
- `ReadQuaternion`（第 1080 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Quaternion ReadQuaternion(`
- `ReadFloat`（第 1090 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static float ReadFloat(IntPtr address)`
- `CurrentPlan`（第 1094 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SkyBlockPlan CurrentPlan()`
- `DiagnosticSnapshot`（第 1104 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static string DiagnosticSnapshot(`
- `AppendActiveFlicks`（第 1194 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AppendActiveFlicks(`
- `IsFinite`（第 1241 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsFinite(double value)`
- `LogError`（第 1245 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 517 行：`int arrayLength = Marshal.ReadInt32(`
- 第 519 行：`NativeLayoutCatalog.ArrayLengthOffset);`
- 第 527 行：`NativeLayoutCatalog.Record.Final) != 0;`
- 第 529 行：`NativeLayoutCatalog.Record.Judgment);`
- 第 688 行：`uint flags = (uint)Marshal.ReadInt32(`
- 第 689 行：`local + NativeLayoutCatalog.Note.Flags);`
- 第 696 行：`Marshal.WriteInt32(`
- 第 697 行：`local + NativeLayoutCatalog.Note.Flags,`
- 第 709 行：`NativeLayoutCatalog.Track.TrackWidth);`
- 第 712 行：`NativeLayoutCatalog.Track.NoteWidthScale);`
- 第 715 行：`Marshal.ReadInt64(`
- 第 717 行：`NativeLayoutCatalog.Track.BaseSpeed));`
- 第 720 行：`Marshal.ReadInt64(`
- 第 722 行：`NativeLayoutCatalog.Track.DepthFactor));`
- 第 765 行：`NativeLayoutCatalog.Track.RestPosition);`
- 第 793 行：`NativeLayoutCatalog.Track.HomePosition);`
- 第 876 行：`NativeLayoutCatalog.Track.TrackWidth);`
- 第 881 行：`NativeLayoutCatalog.Track.RestRotation);`
- 第 1092 行：`Marshal.ReadInt32(address));`
- 第 1111 行：`Marshal.ReadInt64(`
- 第 1113 行：`NativeLayoutCatalog.Track.DepthFactor));`
- 第 1116 行：`Marshal.ReadInt64(`
- 第 1118 行：`NativeLayoutCatalog.Track.BaseSpeed));`
- 第 1169 行：`Marshal.ReadInt64(`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 65 行：`DestroyAll();`
- 第 71 行：`ActiveMembers.Clear();`
- 第 137 行：`ReleaseActiveAt(i);`
- 第 151 行：`ReleaseActiveAt(i);`
- 第 174 行：`ReleaseActiveAt(i);`
- 第 189 行：`if (ShouldReleaseAfterLine(`
- 第 193 行：`ReleaseActiveAt(i);`
- 第 227 行：`internal static void ResetForChartSwitch()`
- 第 229 行：`DestroyAll();`
- 第 230 行：`ActiveMembers.Clear();`
- 第 247 行：`ActiveMembers.Clear();`
- 第 254 行：`// a fraction of a frame. Destroying every entry here`
- 第 440 行：`private static bool ShouldReleaseAfterLine(`
- 第 462 行：`private static void ReleaseActiveAt(int index)`
- 第 465 行：`ReleaseMember(member);`
- 第 557 行：`DestroyEntry(entry);`
- 第 586 行：`DestroyEntry(entry);`
- 第 613 行：`private static void ReleaseMember(`
- 第 667 行：`TryDestroy(go);`
- 第 943 行：`TryDestroy(entry.LeftBracket);`
- 第 944 行：`TryDestroy(entry.RightBracket);`
- 第 977 行：`UnityEngine.Object.Destroy(clone);`
- 第 985 行：`DestroyMaterials(owned);`
- 第 1021 行：`private static void DestroyAll()`

## 性能边界

- 包含帧级入口：`Tick`, `UpdateBrackets`, `UpdateEntry`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- SkyBlock alias scan
- SkyBlockOverlapHarness
- SkyBlockTestHarness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
