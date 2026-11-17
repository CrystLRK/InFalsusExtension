# 原版目标对象写入特例登记

本文件登记 Mod 在明确目标对象上执行的原生或原版对象写入。登记不等于
允许扩大修改范围；每个入口必须保持目标身份校验、恢复条件和失败降级。

## 1. 谱面/判定/成绩

| 入口 | 目标 | 写入内容 | 所有权与恢复 | 失败策略 |
|---|---|---|---|---|
| `ChartParsePatch` / `_Gab` | 当前谱面 `ReadOnlySpan<byte>` | 以重新编码的 ICP1 payload 替换当前 span | 不修改原版 parser；新数组由 `PinnedArrays` 保持到本次 `_Gab` 结束 | 解码、准备或安装失败恢复原 span 和 Mod state |
| `TimingGroupRuntime` | 目标 grouped note 的 `StartMs/EndMs/RuntimeLead` 与编译 `_IA` 项 | 渲染相临时使用组时间线 | compiled owner、compile generation、slot id/type 全部校验；Track.Update 末尾恢复 | owner/slot 变化时跳过恢复写入并丢弃事务 |
| `SkyBlockJudgeRuntime` | 目标 SkyBlock note record | judgment/final 与原生 presentation tuple | 只处理当前 plan 的 judge member；presentation 失败回滚 record | 目标 id/几何/record slice 校验失败时不写入 |
| `BombJudgeRuntime` | 目标 Bomb note record | judgment/final 与原生 presentation tuple | 只处理 `BombPlan` native member；presentation 失败回滚 | slot id/record shape/queue 校验失败时不写入 |
| `AutoplayRuntime` | 目标 note record | Autoplay judgment bytes | 目标 slot id 校验；用户已接受此测试功能风险 | 缺 record/slot 或 SkyBlock 管理成员时跳过 |
| `AutoplayResultsUpdateNativePatch` | `GameResultsV4.TryUpdate` native entry | 本局返回 `NoChange`，抑制成绩更新 | profile/RVA/prologue 校验；仅 Autoplay 会话调用 | 守卫不可用则关闭 Autoplay，不继续 fail-open |
| `AutoplaySaveFreezePatch` | `_NH._SoA` 保存入口 | 抑制本局保存回调 | 仅 `_freezeActive` 时生效；结果页完成或下一谱面边界清理 | 回调异常仍保持抑制 |
| `CustomDifficultyUnlockPatch` | `_NH._uoA` native entry | 对已安装自制谱的已有难度返回解锁值 | 按 BaseName 查找当前 CustomSong plan；不改存档 | 查找/解析失败返回原版结果 |

## 2. 视觉对象

| 入口 | 目标 | 写入内容 | 所有权与恢复 | 失败策略 |
|---|---|---|---|---|
| `SceneControlRuntime` | Track 子层级 renderer | 本地 fade Material 替换 | 按 renderer pointer 缓存原始 Material；reset 恢复并销毁 Mod clone | 构造失败不替换；原材质不销毁 |
| `NormalSkyInternalEdgeRuntime` | 目标普通 SkySafeArea renderer | 本地 `MaterialPropertyBlock._MissingRange` | 每帧按 native note id 校验；属性块每帧清理 | 失败只跳过目标 renderer |
| `SkyColorRuntime` | 目标普通 SkySafeArea `colorDetails` | 临时指向 Mod color clone | 按 note id 记录原指针；reset 恢复后销毁 clone | 只处理属于 setcolor authored group 的目标 |
| `HideGroupSkyColorRuntime` | 目标 SkySafeArea `colorDetails` | 调试蓝/当前 setcolor clone | 仅 debug 配置启用；按 native note id 校验 | 目标失配时跳过 |
| `BackgroundSkin` | 原生背景 clone 当前 owner | `Constrained2D` material 字段返回 Mod clone | 记录原 Material 与 owner pointer；恢复时校验 clone identity | owner/current material 不匹配时不写 |
| `BackgroundSkin` / `SkyColorRuntime` / independent SkyArea runtimes | Mod 实例化 GameObject/Material | 独立对象 | 每个 Entry 显式保存 `OwnedMaterials` | 失败只销毁已登记 clone |
| `BombSkinRuntime` | Mod 池实例的 TapNote main renderer | Bomb 材质 | `VisualSlot.Material` 单独持有；池销毁时释放 | side guide 保持原版设置和材质 |

## 3. 启动期原生补丁

| 入口 | 目标 | 写入内容 | 所有权与恢复 | 失败策略 |
|---|---|---|---|---|
| `NumericLimitPatch` | 已 profile/RVA/prologue/站点签名验证的构造器 immediate | 容量常量 | 事务回滚旧立即数；写入和回滚显式 flush instruction cache | 任一站点不匹配则不写；事务失败回滚并禁用能力 |
| `BackgroundNativeHook` | `_UmA/_LIA` | 返回背景 Material clone | profile/RVA/prologue 校验；卸载 detach | 安装失败回退 legacy lookup |
| `JacketAddressablesNativeHook` | Addressables material load/acquire | Mod completed handle | marker pointer 注册；库卸载 unregister + dispose | hook 不可用则拒绝部分曲库安装 |

## 4. 约束

- 禁止把本表中的目标写入扩展为所有普通 Track、SkyArea、Note 或共享 Material。
- 新增目标级写入必须同时更新本表、对应文件开发文档和 `internal-docs/` 审查记录。
- 如果恢复所需 owner、identity 或生命周期无法确认，必须停止实现并先完成 IDA/
  日志查证。
