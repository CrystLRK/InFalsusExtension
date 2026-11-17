# PackAssetInstaller.cs

> 源码：`InFalsusChartExt/CustomSong/Install/PackAssetInstaller.cs`
> SHA256：`6CAA4FCB63C0EF574308A2EF395B92C73DBCFD77B957B1A2CFB7F78AF6948384`
> 代码行数：356
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `PackAssetInstaller`（`class`，源码第 22 行）
- `Owned`（`class`，源码第 344 行）

## 方法/属性职责

- `Apply`（第 31 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void Apply(`
- `FindStyleIndex`（第 84 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int FindStyleIndex(`
- `FindStockPackId`（第 117 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int FindStockPackId(string slug)`
- `Dispose`（第 144 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Dispose()`
- `ReleaseOwnedMaterials`（第 152 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseOwnedMaterials()`
- `ApplyVisuals`（第 164 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyVisuals(`
- `Build`（第 255 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static Owned Build(`
- `CurrentLanguage`（第 290 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CurrentLanguage()`
- `FirstMaterial`（第 310 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Material FirstMaterial(`
- `ParseTrailingNumber`（第 328 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int ParseTrailingNumber(string value)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 43 行：`ReleaseOwnedMaterials();`
- 第 144 行：`internal static void Dispose()`
- 第 146 行：`ReleaseOwnedMaterials();`
- 第 152 行：`private static void ReleaseOwnedMaterials()`
- 第 157 行：`Object.Destroy(OwnedMaterials[i].Material);`
- 第 159 行：`Object.Destroy(OwnedMaterials[i].Texture);`
- 第 161 行：`OwnedMaterials.Clear();`
- 第 272 行：`Object.Destroy(material);`
- 第 274 行：`Object.Destroy(texture);`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
