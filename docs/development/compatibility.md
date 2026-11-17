# 跨版本兼容说明

## BuildProfile

- 当前已验证构建才启用原生写入能力；
- `NewUnvalidated` 和未知构建默认关闭全部原生写能力；
- 能力位必须在每个原生访问入口检查。
- `CustomScores` 能力要求 current/latest 均存在
  `ResultsScene.Start`、`ResultsScene._gq`、`_NH._CEb._vNA()` 和原版
  `GameResultsV4.TryGetResult/TryUpdate`；任一入口缺失时只关闭独立成绩
  持久化，不影响其他 Mod 功能。
- `AutoplayScoreGuard` 要求 current/latest 均存在
  `GameResultsV4.TryUpdate`、`_NH._SoA(Action<_rH>)` 和
  `ResultsScene.Start`；目标缺失时只关闭成绩抑制与结果页标记。
- `GameResultsV4` 与 `_NH` 来自 `Game.Data.dll`；`ResultsScene` 来自
  `Game.dll`。profile 的 assembly 字段必须按实际原生镜像分别配置，不能
  因为 class/method 可见就复用错误程序集身份。
- `CustomDifficultyUnlock` 要求 `_NH._uoA` 的四参数签名；目标缺失时恢复
  原版难度锁定，不影响自制曲库和谱面加载。

## NativeLayoutCatalog

- 固定 note、record、Track、SkyArea、背景、音频和 CustomSong 偏移全部由
  `native-profiles.json` v2 选择；
- Presentation 元组布局、容量扫描机器码参数和所有 Harmony/native
  target 均由 profile 驱动；
- 当前构建启动时逐字段验证；
- 核心 layout 验证通过后，统一解析一次全部 managed `PatchTargets`；
  任一类身份或签名失配只禁用该 target 声明的 capability；
- 新版本必须从 metadata/IDA 重新确认，不能复用旧 RVA。

## 条件型特例

- 容量补丁；
- ChartIngress；
- 背景 `_UmA/_LIA` hook；
- CustomSong 安装与 Addressables marker 前缀；
- CustomSong jacket marker native detour 与 completed-handle acquire；
- SkyArea 原生反馈事件分发入口 `gameSceneEventDispatch`；
- Autoplay 引擎输入 flick 手势直接调用 interop 暴露的
  `_VD._Mz(double, Vector2)`；只在 `轮椅模式` 的 `_Oz` 前缀由
  flick 分支调用，用于写入与原版鼠标横向移动相同的 50ms 滑动脉冲。
  调用异常时仍安全关闭引擎输入与 Autoplay；判定结果不参与模式退出。
- Autoplay 引擎输入：flick 使用 `_VD._Mz` 产生滑动脉冲；
  SkyArea 接取时允许直接写 `CursorRaw/CursorX` 瞬移到中心。
  flick 可提前 80ms 起手，同时间段多 flick 使用 2.0/帧；
  单 flick 使用 0.25/帧，回位 0.04/帧。
- 多 flick 的 waypoint 会夹取到当前 SkyArea 交集范围内；参考
  InFalsus Studio+ 的 deterministic cluster/route 思路，先按方向
  排序所有目标，再在允许范围内完成 route。
- 多 flick 只有在移动前位于 SkyArea 内、移动后离开时才回到
  SkyArea 中心；其他情况停在最后一个 flick 位置。
- managed SkyBlock 的游标用 `SkyBlockVisualRuntime` 的可视时间与
  `RangeAt` 跟随；判定仍由 `SkyBlockJudgeRuntime` 独立完成。
- Autoplay 引擎输入的 flick/SkyArea 游标在 `_hb` 后、原版
  `_Oz` 前调用引擎内 `_VD._Mz`，使用与 `_Oz` 相同的原版帧时钟
  `_sg._A` 写入横向滑动脉冲；
- managed SkyBlock 的独立持续音只遍历当前 `ActiveLayers`；
  `PendingLayers`（结束后保留 2000ms 用于视觉）不参与持续音判断。
- `自动游玩`模式使用 RD-style runtime：
  地面 note 按一个 lane 写 press/held 并做 exact note-time 修正；
  flick 与 SkyArea 的鼠标移动复用 engine mode 的
  `AutoplayEngineInput.TickCursorOnly`，因此同样使用 `_Mz`
  滑动脉冲、SkyArea 瞬移和多 flick route；该鼠标移动只负责
  视觉与输入反馈，不承担最终判定。实际判定仍在 `_Oz` 后通过
  `AutoplayRuntime.RewriteDue` 替换记录判定字节，覆盖所有
  ground/hold/flick/sky 记录。该模式不安装 `_nz` detour。
- `无 / 自动游玩 / 轮椅模式` 和 `GROUND ONLY` 在“音符设定”页打开时由
  独立 Canvas 覆盖层显示；不克隆、不修改任何原版设置行。覆盖层只在该页
  可见时启用，每次启动恢复为“无 / 关闭”。
- engine 模式已删除 judgement record 影子校验；Near/Miss/None 不再关闭
  Autoplay，只有真实输入调用异常仍走安全停止。

`PatchTargets` 使用 assembly、namespace、class、method、参数签名和返回
类型严格匹配。Harmony 目标使用托管签名和 class 身份；只有 native hook
目标才要求原生入口。泛型 native target 必须先膨胀为 profile 指定的封闭
实例，再以主/fallback RVA 和对应 prologue 校验。RVA/prologue 失败或
target 缺失时，只禁用该 target 声明的能力；核心 ABI 校验失败时禁用全部
native-write。

新版 profile 由
`internal-docs/tools/generate_native_profile.py` 从
`GameAssembly.dll`、metadata、`dump.cs` 和 `script.json` 生成候选。
存在 fallback RVA 时必须同时生成 `fallbackPrologue`；不同入口不能复用
同一份首 16 字节。
生成器不会自动标记可信，结构布局仍需人工复核。
- Native Jacket Bridge；
- TrackGhost Guard；
- 目标级 hidegroup spawn 过滤。

这些特例必须保留明确目标条件、能力门控和失败降级。

`gameSceneEventDispatch` 指向 `GameScene._Mk(_Uh, _Sh)`。独立 SkyBlock
只生成两种原生 `_Sh` 事件：

- event type 6：block 首次产生 `EXACT+` 判定时，每个 block 一次；
- event type 5(bool)：该 block 已有成功判定、当前仍覆盖且无 penalty 时
  开启；任一条件失败时关闭。

事件统一交给原版 `_Mk` 分发。音效设置、约 10ms 节拍、voice 分配、
共享 `_ag` 持续音轨及普通 SkyArea 音频路径全部仍由原版负责。Mod 只在
独立 block 需要补充 event 5 时提交；普通 SkyArea 的 event 5 完全由原版
产生。Mod 不再直接调用 `_xk`、`_zk`。该入口使用 `SkyFeedback` 能力位；
目标失败时仅关闭独立 SkyArea 音频。
