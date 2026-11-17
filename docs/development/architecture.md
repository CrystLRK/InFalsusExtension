# 代码架构说明

## 当前入口

- `ChartExtMod`：加载器无关的初始化、Tick、GUI、场景和清理边界；
- `MelonChartExtMod`：MelonLoader 生命周期薄入口；
- `BepInExChartExtPlugin` / `BepInExRuntimeBehaviour`：BepInEx 6 IL2CPP
  生命周期薄入口；
- `ChartParsePatch`：`_Gab` 谱面重编码；
- `ChartCompilePatch`：`_Ib` 编译槽位锁存；
- `TrackTickPatch`：Track 每帧扩展更新；
- `HbJudgeListPatch`：spawn/judge 列表目标过滤；
- 各 feature runtime：独立视觉、判定、媒体和资源生命周期。

## 规划边界

- `Bootstrap/`：启动、场景和全局生命周期；
- `Core/ChartModel/`：谱面级上下文和数据模型；
- `Core/ChartFormat/`：ICP1 编解码与 marker；
- `Core/Native/`：ABI、slot table 和原生读取；
- `Patches/`：Harmony 入口，只调用运行时；
- `Features/`：功能状态、判定、视觉和资源；
- `Compatibility/`：构建识别、能力位和受控补丁；
- `CustomSong/`：clean-room 曲包发现、安装、chart、音频、preview 和资产管线；
- `CustomSong/Scores/`：自制谱独立最佳成绩文件、当前可见成绩接管与
  原版结果表注入；
- `Profiles/`：当前版/新版 native ABI profile 数据。
- `Compatibility/NativePatchTargetResolver.cs`：统一解析和校验所有
  Harmony/native target；启动期集中校验 managed target，并通过缓存让
  失败项禁用对应 capability。

## 加载器边界

- `PluginEnvironment` 统一提供游戏根、插件目录和加载器类型；
- `PluginLog`、`PluginConfig` 屏蔽 MelonLogger/MelonPreferences 与
  BepInExManualLogSource/ConfigFile 的差异；
- `NativeDetourHook<T>` 在 Melon 下包装 `NativeHook<T>`，在 BepInEx 下
  包装 `INativeDetour.CreateAndApply`；
- BepInEx 的 Harmony 补丁必须在 BuildProfile 和容量准备之后显式
  `CreateAndPatchAll`；
- BepInEx interop 的 namespace 由 `Il2CppNamespaceAliases.cs` 映射；
  metadata token 不一致时只允许按完整签名回退并记录一次警告。

## SkyArea 判定与反馈

- `SkyBlockJudgeRuntime` 负责独立共享分支判定和 record 提交；
- 展示 tuple 继续由原版 `_PEA -> _Tz` 转换；
- `SkyBlockFeedbackRuntime` 只在 `EXACT+` 提交成功后调用原版命中音效
  回调，不拥有声音资源、音量或 FMOD System。

## 自制谱成绩

- `ResultsScoreCapturePatch` 在 `ResultsScene.Start` 完成后观察原版最终
  结果；
- `CustomScoreRuntime` 只处理已安装自制谱，读取/写入独立 JSON 最佳成绩；
- `CustomScoreNativeBridge` 负责与 `GameResultV4`、`GameResultsV4` 的原版
  数据转换和查询/更新；
- 官方谱不写独立文件；损坏或不可写文件只停用自制谱成绩持久化，不阻断
  游戏流程。

## Autoplay 成绩抑制

- `AutoplayScoreGuard` 保存本局使用锁存、存档冻结和结果页标记状态；
- `AutoplayResultsUpdateNativePatch` 对 profile 校验后的
  `GameResultsV4.TryUpdate` native entry 使用 `NativeDetour`；抑制局写
  `existingScore=0` 并返回 `NoChange`，其他情况调用原 trampoline；
- detour 只在 gameplay 中首次启用 Autoplay 时安装，启动期成绩注入不得
  经过该 detour；
- guard 不可用或 detour 安装失败时关闭 Autoplay，绝不继续运行后按普通
  路径保存成绩；
- 下一张 gameplay 谱面建立时清理未完成结果页的上一局守卫状态；
- `TryUpdate` 含三个 by-ref IL2CPP 参数，禁止恢复 Harmony 参数包装；
- `AutoplaySaveFreezePatch` 在冻结窗口内跳过 `_SoA` 文件写入但继续调用
  原版完成回调；
- `AutoplayResultsCompletePatch` 在结果页最后解除冻结；
- `CustomDifficultyUnlockPatch` 对 profile 校验后的 `_uoA` native entry
  使用 `NativeDetour`，先调用原实现，再只对自制谱 config 中实际存在的
  难度覆盖返回值为已解锁；不使用 Harmony 注入 `SongInfo&`。

目录迁移不改变命名空间、Harmony 目标和运行顺序。

## 临时诊断构建

- `IFCE_DIAGNOSTICS` 启用 `RegressionDiagnostics`；
- 只记录音频工厂控制器状态、gameplay 暂停转换和恢复后的少量 SkyBlock
  位置快照；
- 正式 Release 不定义该符号，所有诊断调用由 `ConditionalAttribute`
  编译移除；
- 诊断输出统一使用 `[DiagState]` 前缀，限制记录数量，定位后必须删除。
