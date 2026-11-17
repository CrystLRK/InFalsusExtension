# TimingGroupNativeFlickRuntime.cs

> 源码：`InFalsusChartExt/Features/TimingGroups/TimingGroupNativeFlickRuntime.cs`
> SHA256：`DD8033D86122B3188C60727387820576E08ABB31A037F26D042F7E6BE37C6211`
> 代码行数：134
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

grouped Flick native spawn 注入

## 生命周期

谱面编译建立时间线；Track.Update 和判定阶段读取，切谱释放。

## 顶层及类型声明

- `TimingGroupNativeFlickRuntime`（`class`，源码第 19 行）

## 方法/属性职责

- `InjectSpawnList`（第 29 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void InjectSpawnList(`
- `AppendUnique`（第 113 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool AppendUnique(IntPtr list, int position)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 41 行：`Marshal.ReadInt64(`
- 第 42 行：`tp + NativeLayoutCatalog.Track.BaseSpeed));`
- 第 44 行：`Marshal.ReadInt64(`
- 第 45 行：`tp + NativeLayoutCatalog.Track.DepthFactor));`
- 第 70 行：`if (Marshal.ReadByte(`
- 第 71 行：`note + NativeLayoutCatalog.Note.Consumed) != 0)`
- 第 73 行：`int be = Marshal.ReadInt32(`
- 第 74 行：`note + NativeLayoutCatalog.Note.StartMs);`
- 第 115 行：`IntPtr arr = Marshal.ReadIntPtr(`
- 第 116 行：`list + NativeLayoutCatalog.Runtime.ListItems);`
- 第 117 行：`int count = Marshal.ReadInt32(`
- 第 118 行：`list + NativeLayoutCatalog.Runtime.ListCount);`
- 第 120 行：`int capacity = Marshal.ReadInt32(`
- 第 121 行：`arr + NativeLayoutCatalog.ArrayLengthOffset);`
- 第 123 行：`IntPtr data = arr + NativeLayoutCatalog.ArrayDataOffset;`
- 第 125 行：`if (Marshal.ReadInt32(data + 4 * i) == position)`
- 第 128 行：`Marshal.WriteInt32(data + 4 * count, position);`
- 第 129 行：`Marshal.WriteInt32(`
- 第 130 行：`list + NativeLayoutCatalog.Runtime.ListCount, count + 1);`

## 所有权与释放点

- 第 56 行：`DiagnosticInjected.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- FrameScanHarness
- R3/R4 contract harness
- TimingGroupTransactionHarness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
