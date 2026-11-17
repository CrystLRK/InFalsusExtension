# BepInExValueTypeListBridge.cs

> 源码：`InFalsusChartExt/CustomSong/Install/BepInExValueTypeListBridge.cs`
> SHA256：`D3F00A37AE4AAD6B9A0B61EC8FC38719DFF91184D561EC0BD988BDE2D7532052`
> 代码行数：132
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `BepInExValueTypeListBridge`（`class`，源码第 11 行）
- `AddMethods`（`class`，源码第 98 行）
- `SetItemMethods`（`class`，源码第 104 行）

## 方法/属性职责

- `Invoke`（第 40 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static unsafe void Invoke(`
- `Resolve`（第 110 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr Resolve(string fieldName)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 60 行：`IL2CPP.il2cpp_object_get_class(valuePointer);`
- 第 62 行：`IL2CPP.il2cpp_class_is_valuetype(valueClass))`
- 第 65 行：`IL2CPP.il2cpp_object_unbox(valuePointer);`
- 第 84 行：`IL2CPP.il2cpp_runtime_invoke(`

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
