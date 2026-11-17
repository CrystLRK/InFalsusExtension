# RuntimeCanvasUi.cs

> 源码：`InFalsusChartExt/Bootstrap/RuntimeCanvasUi.cs`
> SHA256：`F20F7A8F02AF38061F74F99165ADB0C95CED6E4343A41F08C2D0ED86F432D861`
> 代码行数：223
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

MelonLoader/BepInEx 程序集加载、GameScene 切换和宿主卸载。

## 顶层及类型声明

- `RuntimeCanvasUi`（`class`，源码第 11 行）
- `CanvasButton`（`class`，源码第 216 行）

## 方法/属性职责

- `CreateCanvas`（第 13 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`internal static Canvas CreateCanvas(`
- `CreatePanel`（第 40 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`internal static RectTransform CreatePanel(`
- `CreateFullScreenPanel`（第 63 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`internal static RectTransform CreateFullScreenPanel(`
- `CreateCenteredText`（第 81 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`internal static Text CreateCenteredText(`
- `CreateText`（第 107 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`internal static Text CreateText(`
- `CreateButton`（第 133 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`internal static CanvasButton CreateButton(`
- `LoadFont`（第 175 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Font LoadFont(int size)`
- `EnsureRect`（第 207 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static RectTransform EnsureRect(GameObject go)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 18 行：`UnityEngine.Object.DontDestroyOnLoad(root);`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
