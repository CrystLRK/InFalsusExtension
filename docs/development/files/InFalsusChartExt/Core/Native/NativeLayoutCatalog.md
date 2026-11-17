# NativeLayoutCatalog.cs

> 源码：`InFalsusChartExt/Core/Native/NativeLayoutCatalog.cs`
> SHA256：`1F683950FB195D7455E6240D3FBC37A9C39BE119FC4148E0A3E7D7FF2FFE5F13`
> 代码行数：602
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

ABI 偏移与启动校验

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `NativeLayoutCatalog`（`class`，源码第 13 行）
- `Runtime`（`class`，源码第 21 行）
- `CompiledSide`（`class`，源码第 35 行）
- `Note`（`class`，源码第 43 行）
- `Compiled`（`class`，源码第 67 行）
- `Record`（`class`，源码第 77 行）
- `SkyJudge`（`class`，源码第 87 行）
- `GameSceneAudio`（`class`，源码第 117 行）
- `Track`（`class`，源码第 127 行）
- `SkySafeArea`（`class`，源码第 167 行）
- `SafeAreaColorDetails`（`class`，源码第 174 行）
- `Background`（`class`，源码第 187 行）
- `FlickNote`（`class`，源码第 193 行）
- `TapNote`（`class`，源码第 198 行）
- `Audio`（`class`，源码第 206 行）
- `CustomSong`（`class`，源码第 211 行）
- `Result`（`class`，源码第 228 行）
- `Presentation`（`class`，源码第 238 行）
- `Capacity`（`class`，源码第 264 行）

## 方法/属性职责

- `Abi`（第 272 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int Abi(string section, string key)`
- `ValidateCurrentBuild`（第 296 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static bool ValidateCurrentBuild(out string error)`
- `OpenClassAny`（第 519 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr OpenClassAny(`
- `Field`（第 544 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Field(`
- `ValueSize`（第 571 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ValueSize(`
- `AddFailure`（第 593 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddFailure(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 515 行：`IL2CPP.il2cpp_runtime_class_init(klass);`
- 第 530 行：`IntPtr klass = IL2CPP.GetIl2CppClass(`
- 第 556 行：`IntPtr field = IL2CPP.GetIl2CppField(klass, fieldName);`
- 第 562 行：`int actual = (int)IL2CPP.il2cpp_field_get_offset(field);`
- 第 583 行：`int actual = (int)IL2CPP.il2cpp_class_value_size(`

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
