# 加载器适配说明

## 目标

同一份业务源码生成 MelonLoader 与 BepInEx 6 IL2CPP 两个 DLL。加载器
差异只存在于 `Bootstrap/`、`PluginConfig`、`PluginLog` 和
`NativeDetourHook`，不得进入谱面、判定、视觉或对象池逻辑。

## 构建

MelonLoader 为默认目标：

```powershell
dotnet build InFalsusChartExt\InFalsusChartExt.csproj -c Release `
  -p:GameDir=<path to In Falsus>
```

`GameDir` 用于查找 `MelonLoader/net6` 和 `Il2CppAssemblies`。如果当前
游戏目录已切换为 BepInEx，需把 `GameDir` 指向仍保留 MelonLoader 的
备份目录，不能指向已移走 `MelonLoader/` 的 BepInEx 目录。

BepInEx：

```powershell
dotnet build InFalsusChartExt\InFalsusChartExt.csproj -c Release `
  -p:Loader=BepInEx `
  -p:GameDir=<path to In Falsus> `
  -p:BepInExDir=<extracted BepInEx 6 package> `
  -p:BepInExInteropDir=<BepInEx/interop>
```

BepInEx 参考模板：`6.0.0-be.793+dbc82f0`，Windows x64 IL2CPP。

## 入口与生命周期

- `MelonChartExtMod` 只把 Melon 回调转给 `ChartExtMod`；
- `BepInExChartExtPlugin.Load` 初始化环境、配置、运行时不变量，然后显式
  执行 Harmony `CreateAndPatchAll`；
- `BepInExRuntimeBehaviour` 只负责 Update/OnGUI/Focus/Quit；
- 场景加载/卸载通过 BepInEx 侧的 `SceneManager` 事件转给同一
  `ChartExtMod` 入口。

## 配置与日志

- Melon 继续使用 `MelonPreferences`；
- BepInEx 使用 `BepInEx/config/com.ifce.chartext.cfg`；
- BepInEx 首次运行且目标配置不存在时，读取
  `UserData/MelonPreferences.cfg` 的已知标量键；不覆盖已有配置；
- 日志统一经 `PluginLog`，保留 `Msg/Warning/Error` 调用接口。

## 路径

- `PluginEnvironment.GameRoot` 由宿主设置：Melon 从插件目录父级推导，
  BepInEx 使用 `BepInEx.Paths.GameRootPath`；
- MelonLoader 在 `OnInitializeMelon` 之前就会应用 Harmony 补丁并触发
  补丁类型初始化；`GameRoot` 因此在宿主入口执行前就必须可解析。
  当前回退顺序为运行进程目录（`Environment.ProcessPath`）→ 插件目录
  父级 → 当前工作目录，并把回退值缓存；
- `PluginDirectory` 只表示插件 DLL 所在目录，缺失时回退为
  `GameRoot/Mods`；
- 两个加载器统一把 profile、`bomb.png`、字体和 `custom-results.json`
  放在 `GameRoot/InFalsusChartExt`；旧 `Mods/InFalsusChartExt` 与
  `BepInEx/plugins/InFalsusChartExt` 只在首次启动时迁移缺失文件；
- `NativeAbiProfileStore` 与 `BuildProfile` 会记录上一次解析使用的根目录；
  若首次解析发生在根目录不可用的启动顺序下且没有选中 profile，根目录
  变化后会重新解析，不会永久缓存失败结果；
- `CustomCharts/` 始终位于游戏根目录，两个加载器共享。

## 原生 Hook 与 ABI

- Melon 使用 `MelonLoader.NativeUtils.NativeHook<T>`；
- BepInEx 使用 `BepInEx.Unity.IL2CPP.Hook.INativeDetour.CreateAndApply`；
- 两者都由 `NativeDetourHook<T>` 统一管理 Attach/Detach；
- BepInEx interop 去掉 Melon 的 `Il2Cpp` namespace 前缀；别名集中在
  `Bootstrap/Il2CppNamespaceAliases.cs`；
- 若 profile 的 metadata token 与 BepInEx interop 不一致，只允许在完整
  方法签名匹配时回退，并对每个 target 记录一次警告。

## 禁止事项

- 不得在同一游戏目录同时安装 MelonLoader 与 BepInEx；
- 不得把 BepInEx interop DLL 打入发布包；
- 不得为 BepInEx 复制一套谱面/判定/视觉业务代码；
- 不得在 BepInEx 入口修改原版对象生命周期。
