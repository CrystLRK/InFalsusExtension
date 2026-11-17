# FmodExports.cs

> 源码：`InFalsusChartExt/CustomSong/Audio/FmodExports.cs`
> SHA256：`C9EDA6466041E25CA5E89DB376BDFBE603D31D5DA4FAB788BEB0A307EB23EDE3`
> 代码行数：273
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `FmodExports`（`class`，源码第 6 行）

## 方法/属性职责

- `SoundGetSystem`（第 9 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int SoundGetSystem(`
- `ChannelGetGroup`（第 14 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelGetGroup(`
- `PlaySound`（第 19 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int PlaySound(`
- `ChannelSetMode`（第 27 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelSetMode(`
- `ChannelSetLoopCount`（第 32 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelSetLoopCount(`
- `ChannelSetLoopPoints`（第 37 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelSetLoopPoints(`
- `ChannelSetPosition`（第 45 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelSetPosition(`
- `ChannelSetPaused`（第 51 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelSetPaused(`
- `ChannelGetPaused`（第 56 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelGetPaused(`
- `ChannelIsPlaying`（第 62 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelIsPlaying(`
- `ChannelGetPosition`（第 67 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelGetPosition(`
- `ChannelStop`（第 74 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int ChannelStop(IntPtr channel);`
- `SoundRelease`（第 77 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal delegate int SoundRelease(IntPtr sound);`
- `PrepareSoundLifecycle`（第 101 行）：在创建 replacement Sound 前解析并缓存 play/configure/stop/release 所需全部 FMOD 导出，避免半所有权 Sound。
  - 签名：`internal static void PrepareSoundLifecycle()`
- `GetSystem`（第 138 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal static int GetSystem(IntPtr sound, out IntPtr system) =>`
- `GetChannelGroup`（第 143 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal static int GetChannelGroup(`
- `Play`（第 150 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int Play(`
- `SetMode`（第 161 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int SetMode(`
- `SetLoopCount`（第 168 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int SetLoopCount(`
- `SetLoopPoints`（第 175 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int SetLoopPoints(`
- `SetPosition`（第 186 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int SetPosition(`
- `SetPaused`（第 195 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int SetPaused(`
- `GetPaused`（第 203 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal static int GetPaused(`
- `IsPlaying`（第 212 行）：判断当前对象/状态条件。
  - 签名：`internal static int IsPlaying(`
- `GetPosition`（第 220 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal static int GetPosition(`
- `Stop`（第 230 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int Stop(IntPtr channel) =>`
- `Release`（第 235 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int Release(IntPtr sound) =>`
- `EnsureModule`（第 259 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EnsureModule()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 251 行：`return Marshal.GetDelegateForFunctionPointer<T>(`

## 所有权与释放点

- 第 77 行：`internal delegate int SoundRelease(IntPtr sound);`
- 第 94 行：`private static SoundRelease _release;`
- 第 133 行：`_release ??= Resolve<SoundRelease>(`
- 第 134 行：`"FMOD5_Sound_Release",`
- 第 135 行：`"FMOD_Sound_Release");`
- 第 235 行：`internal static int Release(IntPtr sound) =>`
- 第 236 行：`(_release ??= Resolve<SoundRelease>(`
- 第 237 行：`"FMOD5_Sound_Release",`
- 第 238 行：`"FMOD_Sound_Release"))(sound);`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
