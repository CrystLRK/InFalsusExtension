# NativePatchTargetResolver.cs

> 源码：`InFalsusChartExt/Compatibility/NativePatchTargetResolver.cs`
> SHA256：`57459986BBF933AA5F8C27D083B1321AEBF75BD99BB6120948AF4A913B3648F9`
> 代码行数：758
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

按文件入口和调用方确定；变更时必须补充。

## 顶层及类型声明

- `NativePatchTargetResolver`（`class`，源码第 12 行）
- `Resolution`（`class`，源码第 706 行）
- `NativeTypeName`（`class`，源码第 730 行）

## 方法/属性职责

- `GetModuleHandle`（第 30 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static extern IntPtr GetModuleHandle(string name);`
- `ResolveHarmony`（第 32 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static MethodBase ResolveHarmony(string key)`
- `IsAvailable`（第 40 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsAvailable(string key) =>`
- `ResolveNative`（第 43 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static IntPtr ResolveNative(string key)`
- `ResolveNativeMethod`（第 51 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static IntPtr ResolveNativeMethod(string key)`
- `ValidateManagedTargets`（第 59 行）：启动期遍历 profile 的全部 managed PatchTargets；解析失败会禁用该 target 声明的 capability，并返回聚合错误。
  - 签名：`internal static bool ValidateManagedTargets(`
- `ResolveManaged`（第 92 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Resolution ResolveManaged(string key)`
- `ResolveManagedCore`（第 116 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Resolution ResolveManagedCore(string key)`
- `ResolveNativeTarget`（第 159 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Resolution ResolveNativeTarget(string key)`
- `ResolveManagedMethod`（第 220 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase ResolveManagedMethod(`
- `MethodMatchesProfile`（第 304 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool MethodMatchesProfile(`
- `CloseGenericMethod`（第 321 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase CloseGenericMethod(`
- `TryResolveNativeEntry`（第 344 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryResolveNativeEntry(`
- `EntryMatches`（第 435 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool EntryMatches(`
- `InflateNativeMethod`（第 469 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr InflateNativeMethod(`
- `SignatureMatches`（第 543 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool SignatureMatches(`
- `ClassIdentityMatches`（第 576 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ClassIdentityMatches(`
- `NormalizeProfileTypeName`（第 604 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string NormalizeProfileTypeName(string name)`
- `NormalizeAssemblyName`（第 629 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string NormalizeAssemblyName(string value)`
- `ResolveManagedType`（第 640 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Type ResolveManagedType(string name)`
- `GetRequiredProfile`（第 662 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static NativePatchTargetProfile GetRequiredProfile(`
- `GetProfile`（第 670 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static NativePatchTargetProfile GetProfile(`
- `ToRva`（第 689 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static long ToRva(IntPtr entry)`
- `Failure`（第 698 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Resolution Failure(string error) =>`
- `Normalize`（第 732 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static string Normalize(Type type)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 29 行：`[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]`
- 第 354 行：`IL2CPP.il2cpp_runtime_class_init(classPointer);`
- 第 360 行：`IL2CPP.il2cpp_class_get_methods(`
- 第 365 行：`IL2CPP.il2cpp_method_get_name(method);`
- 第 369 行：`Marshal.PtrToStringAnsi(namePointer);`
- 第 377 行：`if (IL2CPP.il2cpp_method_get_param_count(method) !=`
- 第 390 行：`IL2CPP.il2cpp_method_is_inflated(candidate)`
- 第 391 行：`? Marshal.ReadIntPtr(candidate)`
- 第 412 行：`Marshal.ReadIntPtr(candidate);`
- 第 462 行：`if (Marshal.ReadByte(entry, i) !=`
- 第 507 行：`IL2CPP.il2cpp_method_get_object(`
- 第 527 行：`IL2CPP.il2cpp_method_get_from_reflection(`
- 第 531 行：`: Marshal.ReadIntPtr(closedMethod);`
- 第 580 行：`string name = Marshal.PtrToStringAnsi(`
- 第 581 行：`IL2CPP.il2cpp_class_get_name(classPointer));`
- 第 582 行：`string namespaceName = Marshal.PtrToStringAnsi(`
- 第 583 行：`IL2CPP.il2cpp_class_get_namespace(classPointer));`
- 第 584 行：`IntPtr image = IL2CPP.il2cpp_class_get_image(`
- 第 588 行：`: Marshal.PtrToStringAnsi(`
- 第 589 行：`IL2CPP.il2cpp_image_get_name(image));`

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
