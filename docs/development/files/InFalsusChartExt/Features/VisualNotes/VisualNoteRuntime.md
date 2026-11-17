# VisualNoteRuntime.cs

> 源码：`InFalsusChartExt/Features/VisualNotes/VisualNoteRuntime.cs`
> SHA256：`4C80EE3F48AD8126E5F6ACB7FC59C3FF705547F4AA0987E8C343715597211CB6`
> 代码行数：458
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

noinput/noclip 视觉调度

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `VisualTrackFrame`（`struct`，源码第 8 行）
- `VisualNoteInstance`（`class`，源码第 204 行）
- `VisualNoteRuntime`（`class`，源码第 242 行）
- `VisualNoteObjectUtil`（`class`，源码第 405 行）

## 方法/属性职责

- `Lead`（第 41 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal double Lead(int group, uint sourceMs)`
- `Distance`（第 47 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal double Distance(double leadMs)`
- `LaneCenter`（第 52 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal float LaneCenter(int lane)`
- `LaneX`（第 62 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal float LaneX(int lane, int width)`
- `Alpha`（第 68 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal float Alpha(double leadMs, bool noClip)`
- `TryCapture`（第 81 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryCapture(`
- `ReadFloat`（第 170 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static float ReadFloat(`
- `ReadDouble`（第 180 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static double ReadDouble(IntPtr pointer, int offset)`
- `ReadVector3`（第 186 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Vector3 ReadVector3(IntPtr pointer, int offset)`
- `ReadQuaternion`（第 194 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Quaternion ReadQuaternion(IntPtr pointer, int offset)`
- `Update`（第 211 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal abstract void Update(in VisualTrackFrame frame);`
- `SetHidden`（第 213 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal virtual void SetHidden(bool hidden)`
- `Dispose`（第 225 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal virtual void Dispose()`
- `Tick`（第 252 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `ResetForChartSwitch`（第 352 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `Create`（第 365 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static VisualNoteInstance Create(`
- `DestroyInstance`（第 395 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyInstance(long id)`
- `CloneMaterials`（第 407 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void CloneMaterials(`
- `Destroy`（第 434 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Destroy(GameObject root)`
- `Width`（第 442 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int Width(VisualNoteDescriptor descriptor)`
- `Lane`（第 451 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int Lane(VisualNoteDescriptor descriptor)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 57 行：`Marshal.ReadInt32(`
- 第 58 行：`LaneCenters + NativeLayoutCatalog.ArrayDataOffset +`
- 第 92 行：`t, NativeLayoutCatalog.Track.BaseSpeed);`
- 第 94 行：`t, NativeLayoutCatalog.Track.DepthFactor);`
- 第 98 行：`IntPtr laneCenters = Marshal.ReadIntPtr(`
- 第 99 行：`t + NativeLayoutCatalog.Track.LaneCenters);`
- 第 131 行：`t, NativeLayoutCatalog.Track.NoteWidthScale),`
- 第 133 行：`t, NativeLayoutCatalog.Track.TrackWidth),`
- 第 136 行：`t, NativeLayoutCatalog.Track.CenterRotation),`
- 第 138 行：`t, NativeLayoutCatalog.Track.HoldRotation),`
- 第 140 行：`ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleX, 1f),`
- 第 141 行：`ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleY, 1f),`
- 第 142 行：`ReadFloat(t, NativeLayoutCatalog.Track.CenterScaleZ, 1f)),`
- 第 144 行：`t, NativeLayoutCatalog.Track.RestPosition),`
- 第 146 行：`t, NativeLayoutCatalog.Track.SideLeftPosition),`
- 第 148 行：`t, NativeLayoutCatalog.Track.SideLeftRotation),`
- 第 150 行：`t, NativeLayoutCatalog.Track.SideRightPosition),`
- 第 152 行：`t, NativeLayoutCatalog.Track.SideRightRotation),`
- 第 154 行：`t, NativeLayoutCatalog.Track.SideScaleY, 1f),`
- 第 156 行：`t, NativeLayoutCatalog.Track.SideScaleZ, 1f),`
- 第 174 行：`Marshal.ReadInt32(pointer + offset));`
- 第 183 行：`Marshal.ReadInt64(pointer + offset));`

## 所有权与释放点

- 第 225 行：`internal virtual void Dispose()`
- 第 233 行：`try { UnityEngine.Object.Destroy(material); }`
- 第 236 行：`OwnedMaterials.Clear();`
- 第 237 行：`VisualNoteObjectUtil.Destroy(GameObject);`
- 第 263 行：`ResetForChartSwitch();`
- 第 272 行：`ResetForChartSwitch();`
- 第 318 行：`DestroyInstance(descriptor.VisualId);`
- 第 352 行：`internal static void ResetForChartSwitch()`
- 第 356 行：`try { instance.Dispose(); }`
- 第 359 行：`Instances.Clear();`
- 第 360 行：`RuntimeNotes.Clear();`
- 第 395 行：`private static void DestroyInstance(long id)`
- 第 400 行：`try { instance.Dispose(); }`
- 第 434 行：`internal static void Destroy(GameObject root)`
- 第 438 行：`try { UnityEngine.Object.Destroy(root); }`

## 性能边界

- 包含帧级入口：`Tick`, `Update`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
