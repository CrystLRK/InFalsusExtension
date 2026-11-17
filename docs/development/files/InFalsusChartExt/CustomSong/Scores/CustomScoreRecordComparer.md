# CustomScoreRecordComparer.cs

> 源码：`InFalsusChartExt/CustomSong/Scores/CustomScoreRecordComparer.cs`
> SHA256：`8C5E002E9A2D3926B333D0EDEE5324A3A0AE7F5DEE0182E3E9535D143E4E1846`
> 代码行数：31
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

最佳成绩字段一致性比较

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomScoreRecordComparer`（`class`，源码第 3 行）

## 方法/属性职责

- `Equals`（第 5 行）：逐字段比较最佳成绩，避免重复写盘。
  - 签名：`internal static bool Equals(`
- `CountsEqual`（第 20 行）：比较四类判定计数对象。
  - 签名：`private static bool CountsEqual(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 9 行：`left.Clear == right.Clear &&`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
