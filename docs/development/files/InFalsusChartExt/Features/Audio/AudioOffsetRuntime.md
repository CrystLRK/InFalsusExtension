# AudioOffsetRuntime.cs

> 源码：`InFalsusChartExt/Features/Audio/AudioOffsetRuntime.cs`
> SHA256：`0F2A1D97172D84EAC15C54A9F4FC16E6575A903705E35E7A8933A9DB24B30DFC`
> 代码行数：186
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

FMOD 音乐偏移、`_RFA` 捕获

## 生命周期

歌曲控制器创建后到退出/切歌；Cached FMOD 调用受能力位控制。

## 顶层及类型声明

- `AudioOffsetRuntime`（`class`，源码第 18 行）
- `ChannelState`（`class`，源码第 24 行）

## 方法/属性职责

- `Register`（第 39 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Register(_Cg controller)`
- `ResetForChartSwitch`（第 51 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `Tick`（第 56 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `SetPosition`（第 120 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SetPosition(IntPtr channel, uint positionMs)`
- `SetPaused`（第 130 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SetPaused(IntPtr channel, bool paused)`
- `ResolveSetPosition`（第 140 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SetPositionDelegate ResolveSetPosition()`
- `ResolveSetPaused`（第 150 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SetPausedDelegate ResolveSetPaused()`
- `ResolveExport`（第 160 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr ResolveExport(string name)`
- `GetModuleHandle`（第 174 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static extern IntPtr GetModuleHandle(string moduleName);`
- `GetProcAddress`（第 177 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static extern IntPtr GetProcAddress(IntPtr module, string procedureName);`
- `SetPositionDelegate`（第 180 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private delegate int SetPositionDelegate(IntPtr channel, uint position, uint timeUnit);`
- `SetPausedDelegate`（第 183 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private delegate int SetPausedDelegate(IntPtr channel, int paused);`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 21 行：`NativeLayoutCatalog.Audio.Channel;`
- 第 75 行：`IntPtr channel = Marshal.ReadIntPtr(`
- 第 146 行：`Marshal.GetDelegateForFunctionPointer<SetPositionDelegate>(fn);`
- 第 156 行：`Marshal.GetDelegateForFunctionPointer<SetPausedDelegate>(fn);`
- 第 173 行：`[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]`
- 第 176 行：`[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]`

## 所有权与释放点

- 第 29 行：`public bool PositiveReleased;`
- 第 51 行：`internal static void ResetForChartSwitch()`
- 第 53 行：`Channels.Clear();`
- 第 102 行：`else if (!state.PositiveReleased)`
- 第 105 行：`state.PositiveReleased = true;`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
