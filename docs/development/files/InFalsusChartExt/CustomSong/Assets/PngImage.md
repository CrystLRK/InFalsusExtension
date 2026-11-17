# PngImage.cs

> 源码：`InFalsusChartExt/CustomSong/Assets/PngImage.cs`
> SHA256：`31F51E5AFBA4A1EBD6936B9730EE9C5833F6B30AFA45DC020CC924D21033222C`
> 代码行数：363
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `DecodedImage`（`class`，源码第 8 行）
- `PngImage`（`class`，源码第 25 行）

## 方法/属性职责

- `Decode`（第 32 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static DecodedImage Decode(byte[] data)`
- `CreateTexture`（第 140 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`internal static Texture2D CreateTexture(byte[] pngBytes)`
- `Decompress`（第 177 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static byte[] Decompress(`
- `Unfilter`（第 201 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static byte[] Unfilter(`
- `ToRgba`（第 244 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static byte[] ToRgba(`
- `FlipVertically`（第 319 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void FlipVertically(`
- `Paeth`（第 338 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int Paeth(`
- `ReadBigEndianInt32`（第 355 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static int ReadBigEndianInt32(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 156 行：`UnityEngine.Object.Destroy(engineTexture);`
- 第 172 行：`UnityEngine.Object.Destroy(texture);`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
