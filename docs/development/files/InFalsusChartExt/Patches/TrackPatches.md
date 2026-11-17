# TrackPatches.cs

> 源码：`InFalsusChartExt/Patches/TrackPatches.cs`
> SHA256：`4E661B1E413AA46909901545C3A2C362A8C581D3142157686BEE9FEDF105DE64`
> 代码行数：896
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

Track.Update、`_hb`、列表过滤、TrackMove

## 生命周期

Harmony 目标方法调用时；生命周期跟随原版调用方。

## 顶层及类型声明

- `TrackMoveRuntime`（`class`，源码第 14 行）
- `V3`（`struct`，源码第 16 行）
- `Qt`（`struct`，源码第 24 行）
- `TrackTickPatch`（`class`，源码第 316 行）
- `JudgeListFilter`（`class`，源码第 359 行）
- `HbJudgeListPatch`（`class`，源码第 452 行）
- `BombSpawnWatch`（`class`，源码第 588 行）
- `BombRenderListFilter`（`class`，源码第 630 行）
- `SpawnOrderFilter`（`class`，源码第 712 行）
- `TrackSkyGhostGuardPatch`（`class`，源码第 852 行）

## 方法/属性职责

- `Add`（第 19 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public static V3 Add(V3 a, V3 b) => new V3 { x = a.x + b.x, y = a.y + b.y, z = a.z + b.z };`
- `Sub`（第 20 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public static V3 Sub(V3 a, V3 b) => new V3 { x = a.x - b.x, y = a.y - b.y, z = a.z - b.z };`
- `Lerp`（第 21 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public static V3 Lerp(V3 a, V3 b, float t) => new V3 { x = a.x + (b.x - a.x) * t, y = a.y + (b.y - a.y) * t, z = a.z + (b.z - a.z) * t };`
- `Mul`（第 29 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public static Qt Mul(Qt a, Qt b) => new Qt`
- `Axis`（第 37 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public static Qt Axis(float ax, float ay, float az, float degrees)`
- `Euler`（第 45 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public static Qt Euler(float xDeg, float yDeg, float zDeg) =>`
- `Nlerp`（第 48 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`public static Qt Nlerp(Qt a, Qt b, float t)`
- `Tick`（第 85 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `ResetForChartSwitch`（第 184 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `DiscardCapture`（第 206 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DiscardCapture()`
- `Capture`（第 217 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Capture(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `Restore`（第 239 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Restore(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `Apply`（第 260 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void Apply(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `Rotate`（第 285 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static V3 Rotate(V3 v, Qt q)`
- `Read`（第 301 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static V3 Read(UnityEngine.Vector3 v) => new V3 { x = v.x, y = v.y, z = v.z };`
- `Read`（第 303 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Qt Read(UnityEngine.Quaternion q) => new Qt { x = q.x, y = q.y, z = q.z, w = q.w };`
- `Write`（第 305 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Write(Transform t, V3 pos, Qt rot)`
- `Prepare`（第 319 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 323 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static System.Reflection.MethodBase TargetMethod()`
- `Postfix`（第 330 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(Il2Cppifapp.Game.Scenes.Game.Track __instance)`
- `Strip`（第 363 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Strip(`
- `StripList`（第 391 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void StripList(`
- `Prepare`（第 457 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 462 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static System.Reflection.MethodBase TargetMethod() =>`
- `Prefix`（第 471 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix(`
- `Postfix`（第 501 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`
- `Update`（第 592 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Update(`
- `Strip`（第 634 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Strip(`
- `StripList`（第 666 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void StripList(`
- `StripBlockVisuals`（第 717 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void StripBlockVisuals(`
- `HideNativeTarget`（第 785 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void HideNativeTarget(long id)`
- `HideNativeTarget`（第 799 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void HideNativeTarget(`
- `HideNativeTarget`（第 814 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void HideNativeTarget(`
- `Prepare`（第 855 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 860 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static System.Reflection.MethodBase TargetMethod() =>`
- `Prefix`（第 865 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prefix(Il2Cppifapp.Game.Scenes.Game.Track __instance)`

## 原版/Harmony/native 访问点

- 第 315 行：`[HarmonyPatch]`
- 第 451 行：`[HarmonyPatch]`
- 第 851 行：`[HarmonyPatch]`
- 第 376 行：`for (int side = 0; side < NativeLayoutCatalog.SideCount; side++)`
- 第 400 行：`IntPtr arr = Marshal.ReadIntPtr(`
- 第 401 行：`lp + NativeLayoutCatalog.Runtime.ListItems);`
- 第 402 行：`int count = Marshal.ReadInt32(`
- 第 403 行：`lp + NativeLayoutCatalog.Runtime.ListCount);`
- 第 405 行：`int capacity = Marshal.ReadInt32(`
- 第 406 行：`arr + NativeLayoutCatalog.ArrayLengthOffset);`
- 第 408 行：`IntPtr data = arr + NativeLayoutCatalog.ArrayDataOffset;`
- 第 414 行：`int position = Marshal.ReadInt32(data + 4 * read);`
- 第 419 行：`NativeLayoutCatalog.NoteStride;`
- 第 420 行：`long id = Marshal.ReadInt64(`
- 第 421 行：`note + NativeLayoutCatalog.Note.Id);`
- 第 429 行：`Marshal.WriteInt32(data + 4 * write, position);`
- 第 433 行：`Marshal.WriteInt32(`
- 第 434 行：`lp + NativeLayoutCatalog.Runtime.ListCount, write);`
- 第 599 行：`for (int side = 0; side < NativeLayoutCatalog.SideCount; side++)`
- 第 606 行：`IntPtr arr = Marshal.ReadIntPtr(`
- 第 607 行：`lp + NativeLayoutCatalog.Runtime.ListItems);`
- 第 608 行：`int count = Marshal.ReadInt32(`
- 第 609 行：`lp + NativeLayoutCatalog.Runtime.ListCount);`
- 第 611 行：`IntPtr data = arr + NativeLayoutCatalog.ArrayDataOffset;`
- 第 614 行：`int pos = Marshal.ReadInt32(data + 4 * i);`
- 第 648 行：`side < NativeLayoutCatalog.SideCount;`
- 第 674 行：`IntPtr arr = Marshal.ReadIntPtr(`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 184 行：`internal static void ResetForChartSwitch()`
- 第 595 行：`ChartExtState.SpawnKeysNow.Clear();`

## 性能边界

- 包含帧级入口：`Tick`, `Update`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
