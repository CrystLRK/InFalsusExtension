# ChartRuntimeLifecycle.cs

> 源码：`InFalsusChartExt/Bootstrap/ChartRuntimeLifecycle.cs`
> SHA256：`365421020BA2B8116F001911C0918730F21CD3C523B35AD2D06B736628F519A4`
> 代码行数：116
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

场景、切谱、Mod 卸载清理

## 生命周期

MelonLoader/BepInEx 程序集加载、GameScene 切换和宿主卸载。

## 顶层及类型声明

- `ChartRuntimeLifecycle`（`class`，源码第 11 行）

## 方法/属性职责

- `OnSceneLoaded`（第 16 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnSceneLoaded(int buildIndex, string sceneName)`
- `MarkChartActive`（第 20 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void MarkChartActive()`
- `OnSceneUnloaded`（第 26 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnSceneUnloaded(int buildIndex, string sceneName)`
- `Tick`（第 33 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `Dispose`（第 57 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Dispose()`
- `DisposeForChartTransform`（第 62 行）：谱面重编码事务中清理旧功能资源但保留旧 ChartRuntimeContext，直到 replacement span 和 runtime 成功发布。
  - 签名：`internal static void DisposeForChartTransform()`
- `Dispose`（第 67 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void Dispose(bool disposeChartContext)`
- `Run`（第 99 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Run(string name, Action action)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 94 行：`Run("background-native", BackgroundNativeHook.ResetForChartSwitch);`

## 所有权与释放点

- 第 53 行：`Dispose();`
- 第 57 行：`internal static void Dispose()`
- 第 59 行：`Dispose(disposeChartContext: true);`
- 第 62 行：`internal static void DisposeForChartTransform()`
- 第 64 行：`Dispose(disposeChartContext: false);`
- 第 67 行：`private static void Dispose(bool disposeChartContext)`
- 第 70 行：`Run("sky-block-judge", SkyBlockJudgeRuntime.ResetForChartSwitch);`
- 第 71 行：`Run("sky-block-visual", SkyBlockVisualRuntime.ResetForChartSwitch);`
- 第 72 行：`Run("sky-block-line-glow", SkyBlockLineGlowRuntime.ResetForChartSwitch);`
- 第 77 行：`ChartRuntimeContext.ResetForChartSwitch);`
- 第 78 行：`Run("showtext", ShowTextRuntime.DisposeResources);`
- 第 80 行：`Run("bomb", BombSkinRuntime.DisposeForSceneUnload);`
- 第 81 行：`Run("timing-group", TimingGroupRuntime.ResetForChartSwitch);`
- 第 82 行：`Run("timing-group-visual", TimingGroupVisualRuntime.ResetForChartSwitch);`
- 第 83 行：`Run("timing-noinput-sky", TimingNoInputSkyVisualRuntime.ResetForChartSwitch);`
- 第 84 行：`Run("visual-notes", VisualNoteRuntime.ResetForChartSwitch);`
- 第 85 行：`Run("normal-sky-edge", NormalSkyInternalEdgeRuntime.ResetForChartSwitch);`
- 第 86 行：`Run("sky-color", SkyColorRuntime.ResetForChartSwitch);`
- 第 87 行：`Run("scene-control", SceneControlRuntime.DisposeForSceneUnload);`
- 第 88 行：`Run("track-move", TrackMoveRuntime.ResetForChartSwitch);`
- 第 89 行：`Run("autoplay", AutoplayRuntime.ResetForChartSwitch);`
- 第 90 行：`Run("audio-offset", AudioOffsetRuntime.ResetForChartSwitch);`
- 第 93 行：`CustomSongAudioController.ReleaseOwnedForSceneUnload);`
- 第 94 行：`Run("background-native", BackgroundNativeHook.ResetForChartSwitch);`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
