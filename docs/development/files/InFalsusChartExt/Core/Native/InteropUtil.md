# InteropUtil.cs

> 源码：`InFalsusChartExt/Core/Native/InteropUtil.cs`
> SHA256：`79A6A2445366AFDF63175435D13205A136A459397E6FF4E2DC3FF59A76E19D0E`
> 代码行数：88
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

native record/array 读取工具

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `InteropUtil`（`class`，源码第 8 行）

## 方法/属性职责

- `ResolveRecordLayout`（第 34 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static bool ResolveRecordLayout()`
- `FieldOffset`（第 74 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int FieldOffset(IntPtr klass, string name)`
- `RecordPointer`（第 82 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static unsafe byte* RecordPointer(IntPtr arrayObject, int index, int elementIndex)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 11 行：`NativeLayoutCatalog.ArrayDataOffset;`
- 第 19 行：`NativeLayoutCatalog.Record.Stride;`
- 第 21 行：`NativeLayoutCatalog.Record.Judgment;`
- 第 23 行：`NativeLayoutCatalog.Record.Final;`
- 第 39 行：`IL2CPP.il2cpp_runtime_class_init(klass);`
- 第 41 行：`int size = (int)IL2CPP.il2cpp_class_value_size(klass, ref alignment);`
- 第 45 行：`FieldOffset(klass, "_wdb") - NativeLayoutCatalog.BoxedValueHeader;`
- 第 47 行：`FieldOffset(klass, "_Wdb") - NativeLayoutCatalog.BoxedValueHeader;`
- 第 49 行：`FieldOffset(klass, "_udb") - NativeLayoutCatalog.BoxedValueHeader;`
- 第 51 行：`FieldOffset(klass, "_Udb") - NativeLayoutCatalog.BoxedValueHeader;`
- 第 53 行：`FieldOffset(klass, "_vdb") - NativeLayoutCatalog.BoxedValueHeader;`
- 第 55 行：`FieldOffset(klass, "_Vdb") - NativeLayoutCatalog.BoxedValueHeader;`
- 第 58 行：`_relTimeOffset != NativeLayoutCatalog.Record.RelativeTime ||`
- 第 76 行：`IntPtr field = IL2CPP.GetIl2CppField(klass, name);`
- 第 78 行：`return (int)IL2CPP.il2cpp_field_get_offset(field);`

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
