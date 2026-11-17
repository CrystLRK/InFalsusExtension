# CustomSongRuntime.cs

> 源码：`InFalsusChartExt/CustomSong/Lifecycle/CustomSongRuntime.cs`
> SHA256：`C4D01CBB386B900648F68890B88D8D0B85D7DD565E1A6CC537C84B72536FEAE9`
> 代码行数：311
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `InstalledCustomLibrary`（`class`，源码第 13 行）
- `CustomSongRuntime`（`class`，源码第 122 行）

## 方法/属性职责

- `TryGetSongByBaseName`（第 53 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal bool TryGetSongByBaseName(`
- `TryGetSongByRuntimeId`（第 58 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal bool TryGetSongByRuntimeId(`
- `TryGetSongForAudioName`（第 63 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal bool TryGetSongForAudioName(`
- `TryGetChartByLogicalKey`（第 95 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal bool TryGetChartByLogicalKey(`
- `TryGetChartByParserKey`（第 101 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal bool TryGetChartByParserKey(`
- `ChartLogicalKey`（第 106 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static string ChartLogicalKey(string key)`
- `Select`（第 114 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal void Select(CustomChartPlan chart)`
- `ScanIfNeeded`（第 143 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static CustomLibraryPlan ScanIfNeeded()`
- `MarkInstalled`（第 172 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void MarkInstalled(CustomLibraryPlan plan)`
- `TryGetSongByLogicalKey`（第 184 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetSongByLogicalKey(`
- `TryGetChartByLogicalKey`（第 198 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetChartByLogicalKey(`
- `TryGetChartByParserKey`（第 207 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetChartByParserKey(`
- `TryGetChartByAnyKey`（第 216 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetChartByAnyKey(`
- `TryGetSongByBaseName`（第 222 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetSongByBaseName(`
- `TryGetSongByRuntimeId`（第 231 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetSongByRuntimeId(`
- `TryGetSongForAudioName`（第 241 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetSongForAudioName(`
- `Select`（第 251 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Select(CustomChartPlan chart)`
- `GetTemplateParserKey`（第 256 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal static string GetTemplateParserKey(`
- `GetPayload`（第 260 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal static CustomChartPayload GetPayload(`
- `Tick`（第 269 行）：驱动曲库安装、成绩重试、自制音频和封面服务；在未安装时不执行任何扩展子系统。
  - 签名：`internal static void Tick()`
- `OnDynamicStringsDeserialized`（第 280 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnDynamicStringsDeserialized(`
- `OnPackSceneAwake`（第 286 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnPackSceneAwake(`
- `Dispose`（第 292 行）：释放 payload、音频、封面和独立成绩运行时状态；不删除用户成绩文件。
  - 签名：`internal static void Dispose()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 176 行：`_payloads?.Dispose();`
- 第 292 行：`internal static void Dispose()`
- 第 296 行：`_payloads?.Dispose();`
- 第 303 行：`JacketMaterialService.Dispose();`
- 第 304 行：`PackAssetInstaller.Dispose();`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
