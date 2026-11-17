# CustomScoreFileValidator.cs

> 源码：`InFalsusChartExt/CustomSong/Scores/CustomScoreFileValidator.cs`
> SHA256：`E5A3FB81438418C26259BF21236317304A16BB128D36EA43C3280D125457DB32`
> 代码行数：136
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

成绩 JSON schema、键和数值域校验

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomScoreFileValidator`（`class`，源码第 6 行）

## 方法/属性职责

- `Validate`（第 8 行）：校验文档 schema、条目唯一键和完整字段。
  - 签名：`internal static bool Validate(`
- `ValidateEntry`（第 46 行）：校验单条成绩的身份、计数、Combo、Pace 和枚举范围。
  - 签名：`private static bool ValidateEntry(`
- `ValidateCounts`（第 98 行）：校验 7 个原版判定计数是否处于 ushort 范围。
  - 签名：`private static bool ValidateCounts(`
- `IsDifficulty`（第 130 行）：只接受 MIN、EVO、ULT、FBD 四个稳定难度键。
  - 签名：`private static bool IsDifficulty(string value) =>`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 83 行：`(uint)entry.Best.Clear > byte.MaxValue)`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
