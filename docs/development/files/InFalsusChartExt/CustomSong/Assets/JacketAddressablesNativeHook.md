# JacketAddressablesNativeHook.cs

> 源码：`InFalsusChartExt/CustomSong/Assets/JacketAddressablesNativeHook.cs`
> SHA256：`E5B717EE7AFFE4A38F253F3B3857C02B1BC6C72976065433DFECCFFFFD7AE6E9`
> 代码行数：291
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `JacketAddressablesNativeHook`（`class`，源码第 18 行）
- `NativeAsyncOperationHandle`（`struct`，源码第 21 行）

## 方法/属性职责

- `LoadAssetAsyncFn`（第 30 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private delegate IntPtr LoadAssetAsyncFn(`
- `AcquireFn`（第 37 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private delegate IntPtr AcquireFn(`
- `Install`（第 57 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static bool Install()`
- `Register`（第 118 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static object Register(`
- `Unregister`（第 135 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Unregister(`
- `Dispose`（第 147 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Dispose()`
- `LoadAssetDetour`（第 162 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr LoadAssetDetour(`
- `CreateCompletedHandle`（第 204 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static object CreateCompletedHandle(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 18 行：`internal static class JacketAddressablesNativeHook`
- 第 84 行：`_acquire = Marshal.GetDelegateForFunctionPointer<`
- 第 244 行：`IntPtr value = IL2CPP.il2cpp_object_unbox(`
- 第 255 行：`InternalOperation = Marshal.ReadIntPtr(`
- 第 256 行：`value + NativeLayoutCatalog.Runtime`
- 第 258 行：`Version = Marshal.ReadInt32(`
- 第 259 行：`value + NativeLayoutCatalog.Runtime`
- 第 261 行：`Padding = Marshal.ReadInt32(`
- 第 262 行：`value + NativeLayoutCatalog.Runtime`
- 第 264 行：`LocationName = Marshal.ReadIntPtr(`
- 第 265 行：`value + NativeLayoutCatalog.Runtime`

## 所有权与释放点

- 第 147 行：`internal static void Dispose()`
- 第 151 行：`Replacements.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
