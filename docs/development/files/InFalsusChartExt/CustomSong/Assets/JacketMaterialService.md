# JacketMaterialService.cs

> 源码：`InFalsusChartExt/CustomSong/Assets/JacketMaterialService.cs`
> SHA256：`1970B39C42432F9FB2FCF0CD0AF582E6F8398B3D5E14C5A15F15AF3A8F8B2D64`
> 代码行数：508
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `JacketMaterialService`（`class`，源码第 19 行）
- `DeferredSong`（`class`，源码第 436 行）
- `OwnedMaterial`（`class`，源码第 452 行）
- `PendingReference`（`class`，源码第 472 行）

## 方法/属性职责

- `RegisterLibrary`（第 31 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void RegisterLibrary(`
- `Tick`（第 63 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `Dispose`（第 118 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Dispose()`
- `TryApply`（第 138 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryApply(DeferredSong pending)`
- `HasAnyJacket`（第 261 行）：判断当前对象/状态条件。
  - 签名：`private static bool HasAnyJacket(CustomSongPlan song)`
- `FirstPending`（第 274 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static PendingReference FirstPending(`
- `CreatePending`（第 283 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static PendingReference CreatePending(`
- `CreatePending`（第 297 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static PendingReference CreatePending(`
- `Register`（第 337 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Register(PendingReference pending)`
- `UnregisterAndDispose`（第 350 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void UnregisterAndDispose(`
- `Resolve`（第 361 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Material Resolve(`
- `FindSelector`（第 401 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SongJacketEntry FindSelector(`
- `FindSongEntry`（第 413 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int FindSongEntry(`
- `FindChartEntry`（第 423 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int FindChartEntry(`
- `Dispose`（第 496 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal void Dispose()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 36 行：`if (!JacketAddressablesNativeHook.Install())`
- 第 123 行：`JacketAddressablesNativeHook.Unregister(`
- 第 135 行：`JacketAddressablesNativeHook.Dispose();`
- 第 315 行：`JacketAddressablesNativeHook.Register(`
- 第 318 行：`out JacketAddressablesNativeHook`
- 第 329 行：`JacketAddressablesNativeHook.Unregister(reference);`
- 第 479 行：`JacketAddressablesNativeHook`
- 第 493 行：`internal JacketAddressablesNativeHook`
- 第 498 行：`JacketAddressablesNativeHook.Unregister(`

## 所有权与释放点

- 第 35 行：`Pending.Clear();`
- 第 118 行：`internal static void Dispose()`
- 第 120 行：`Pending.Clear();`
- 第 126 行：`UnityEngine.Object.Destroy(`
- 第 129 行：`UnityEngine.Object.Destroy(`
- 第 132 行：`Owned.Clear();`
- 第 133 行：`TemplateMaterials.Clear();`
- 第 134 行：`TemplateFailures.Clear();`
- 第 135 行：`JacketAddressablesNativeHook.Dispose();`
- 第 253 行：`UnregisterAndDispose(value);`
- 第 256 行：`UnregisterAndDispose(value);`
- 第 331 行：`UnityEngine.Object.Destroy(material);`
- 第 332 行：`UnityEngine.Object.Destroy(texture);`
- 第 350 行：`private static void UnregisterAndDispose(`
- 第 358 行：`pending.Dispose();`
- 第 496 行：`internal void Dispose()`
- 第 501 行：`UnityEngine.Object.Destroy(Material);`
- 第 503 行：`UnityEngine.Object.Destroy(Texture);`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
