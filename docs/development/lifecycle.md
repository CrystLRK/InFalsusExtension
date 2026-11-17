# 生命周期说明

## 游戏启动

1. MelonLoader 加载程序集并构造 `ChartExtMod`。
2. `BuildProfile` 校验 GameAssembly、metadata、核心 ABI 和能力位。
   核心 ABI 通过后集中校验 profile 中全部 managed patch target；失配项
   立即禁用对应 capability。
3. native profile v2 先按 GameAssembly/metadata hash 选择并校验；
   每个 Harmony/native target 严格匹配签名、RVA 和 prologue；
   容量补丁、背景 native hook 和 CustomSong 原生操作按能力位安装。

## 谱面载入

1. CustomSong 扫描 `CustomCharts/PackName/SongName`，验证配置、音频、
   chart、背景、封面和 preview 后构造不可变计划。
2. 曲库安装完成前扫描计划独立保存；安装事务失败时恢复数组和文本映射。
3. 曲库安装成功后读取 `InFalsusChartExt/custom-results.json`，
   以当前自制谱 `SongInfo` 注入当前会话的原版 `GameResultsV4`。
4. chart 浏览器使用逻辑 key，`_Gab` 前缀替换为已验证 ICP1 payload 和自然
   parser key。
5. 音频控制器创建后替换为目标 OGG/WAV/MP3；失败保留原控制器。
6. `_Gab` 入口解码 ICP1，扫描扩展 marker。
7. ChartIngress 将 noinput/noclip 视觉物件从原生音符集合中分离。
8. SkyBlock、Bomb、TimingGroup 和视觉谱面计划在载入期建立。
9. 编码结果交给原版编译器，原版 track/record 生命周期继续负责普通对象。
10. 编译槽位同时锁存 Autoplay 音符的 `StartMs/EndMs`。普通音符由起始
    游标处理，Hold 由独立活动集持续到自身结束后的判定尾窗。

## 每帧

- Track 后置入口按固定顺序调用 TrackMove、SceneControl、SkyColor、
  Background、VisualNote、TimingGroup、ShowText、Bomb、HideGroup、
  NormalSkyEdge、SkyBlockVisual 和 SkyBlockLineGlow。
- 拆分禁止改变这一调用顺序。

## 切谱与场景卸载

- `ChartRuntimeLifecycle.Dispose()` 逐个隔离清理各功能模块。
- 新谱面进入时 `BeginChartSession()` 清理上一局未进入结果页的 Autoplay
  守卫状态，防止中途退谱污染下一局。
- 谱面级计划、自有 GameObject、Material、Texture、Mesh、Renderer 缓存
  和静态查找表必须回到空状态。
- 原版共享资源只能恢复，不得销毁。

## 结果页

1. 若本局曾在 gameplay 中开启 Autoplay 且配置要求抑制，
   profile 校验后的 `TryUpdate` native detour 写 `existingScore=0` 并
   返回 `NoChange`；`_SoA` 跳过原版存档文件写入但调用完成回调；
2. `ResultsScene.Start` 先完成原版结果页初始化；
3. `ResultsScoreCapturePatch` 的 Postfix 读取原版最终结果；抑制局跳过
   独立 JSON；
4. 最后一个结果页 Postfix 解除存档冻结，并显示英文 `AUTOPLAY`；
5. 仅当歌曲属于当前自制曲库且未抑制时，查询最佳值并写入独立 JSON；
6. 官谱、无自制曲库或成绩存储不可用时直接跳过。

若 gameplay 中途退出且没有进入结果页，冻结只保留到下一张 gameplay
谱面建立；新谱面边界会完整清除上一局状态。若 score guard 能力缺失或
`TryUpdate` detour 安装失败，Autoplay 立即关闭并显示错误，不得 fail-open。
detour 失败时同时锁存本局成绩抑制：跳过独立 JSON，并继续阻止原版保存
回调，确保失败路径不产生持久化成绩。
`TryUpdate` detour 只在本局首次确认进入 gameplay 并启用 Autoplay 时
安装，启动期 `CustomScoreRuntime` 注入不得经过 detour。停止抑制的
detour 在 Mod 卸载时 `Undo` 和 `Free`，不得让托管 Harmony 再次包装
`TryUpdate` 的三个 by-ref IL2CPP 参数。

## 对象所有权

- 普通 note、普通 SkyArea、record、对象池和共享光效默认属于原版。
- Mod 克隆对象和独立视觉属于各自 feature runtime。
- 独立视觉 Material 由每个 Entry 的 `OwnedMaterials` 显式登记；清理时
  禁止从当前 `sharedMaterial` 反推所有权。
- 背景 clone 记录实际持有它的 `Constrained2D` owner 和原 Material；
  只有 owner 当前仍持有该 clone 时才允许恢复。
- 同一对象不能同时存在两套互不知晓的视觉或判定所有权。
