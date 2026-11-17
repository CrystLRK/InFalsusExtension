# HideGroupSkyColorRuntime.cs

> 源码：`InFalsusChartExt/Features/SceneControl/HideGroupSkyColorRuntime.cs`
> SHA256：`07D11D6E2C26F9E99CCD0B34C385A683031D375E32B3EDC5888B539298715DF8`
> 代码行数：151
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

hidegroup 蓝色调试接口

## 生命周期

谱面载入、每个游戏帧和场景切换；退出谱面时释放自有资源。

## 顶层及类型声明

- `HideGroupSkyColorRuntime`（`class`，源码第 12 行）

## 方法/属性职责

- `Tick`（第 29 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(`
- `ApplyPoolColor`（第 109 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyPoolColor(`
- `ApplyEntryColor`（第 121 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyEntryColor(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 16 行：`NativeLayoutCatalog.SkySafeArea.ColorDetails;`
- 第 18 行：`NativeLayoutCatalog.SkySafeArea.NativeNoteId;`
- 第 53 行：`slot.Position * NativeLayoutCatalog.NoteStride;`
- 第 54 行：`if (Marshal.ReadInt64(`
- 第 55 行：`note + NativeLayoutCatalog.Note.Id) !=`
- 第 58 行：`if (Marshal.ReadInt32(`
- 第 59 行：`note + NativeLayoutCatalog.Note.Type) != 5)`
- 第 61 行：`long id = Marshal.ReadInt64(`
- 第 62 行：`note + NativeLayoutCatalog.Note.Id);`
- 第 141 行：`if (Marshal.ReadInt64(`
- 第 144 行：`if (Marshal.ReadIntPtr(area.Pointer + ColorDetailsOffset) ==`
- 第 147 行：`Marshal.WriteIntPtr(`

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

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
