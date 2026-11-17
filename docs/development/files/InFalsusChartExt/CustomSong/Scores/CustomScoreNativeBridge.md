# CustomScoreNativeBridge.cs

> 源码：`InFalsusChartExt/CustomSong/Scores/CustomScoreNativeBridge.cs`
> SHA256：`240DAC6ECC400D90DC255D3DDAE0AAEA21EA5C97B483E457AD7F47AA4DBE468E`
> 代码行数：100
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

GameResultV4 与 GameResultsV4 原版 API 转换

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomScoreNativeBridge`（`class`，源码第 15 行）

## 方法/属性职责

- `FindGameResults`（第 17 行）：通过原版 `_NH._CEb._vNA()` 获取当前会话 GameResultsV4；不可用时返回 null 供上层延迟重试。
  - 签名：`internal static GameResultsV4 FindGameResults()`
- `ToNative`（第 33 行）：把 JSON 最佳成绩转换为带当前 SongId/难度的原版 GameResultV4。
  - 签名：`internal static GameResultV4 ToNative(`
- `ToRecord`（第 57 行）：把原版 GameResultV4 复制到纯托管 JSON 记录。
  - 签名：`internal static CustomScoreRecord ToRecord(`
- `ToNative`（第 74 行）：把 JSON 最佳成绩转换为带当前 SongId/难度的原版 GameResultV4。
  - 签名：`private static JudgementTypeCount ToNative(`
- `ToRecord`（第 87 行）：把原版 GameResultV4 复制到纯托管 JSON 记录。
  - 签名：`private static JudgementCounts ToRecord(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 43 行：`CalculatedResultClear =`
- 第 44 行：`(GameResultClear)record.Clear,`
- 第 62 行：`Clear = (int)result.CalculatedResultClear,`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
