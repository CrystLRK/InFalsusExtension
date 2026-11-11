# InFalsusExtension

**版本：0.1.0**

InFalsusExtension 是面向 *In Falsus* 的 MelonLoader 扩展，用于加载自制曲包、
扩展谱面语句、兼容游戏原版音符与 SkyArea 行为，并提供成绩、难度和显示相关
辅助功能。

**警告：本 Mod 极大程度上修改了原版逻辑，如发现 Bug 请直接带着日志和具体现象去提 Issue**

## 功能

- 加载 `CustomCharts/PackName/SongName` 结构的自制歌曲与曲包；
- 支持 OGG、WAV、MP3，支持独立 preview、封面和图片/视频背景；
- 支持 Studio+ / Falsum 导入后的自制谱配置、难度和曲包元数据；
- 支持 timing group、noinput、noclip、showtext、setcolor、hidegroup、
  trackmove、trackdisplay、scenecontrol 和 audio offset 等扩展语义；
- 独立处理扩展 Bomb 的视觉与判定，保留原版反馈、文字锚点和计分链路；
- SkyArea 按作者时间、几何和 timing group 在运行时分类，不使用手动
  `skyareapack(...)` / `packpiece(...)` 语法；
- 为自制谱保存独立最佳成绩，避免运行时 `SongId` 漂移导致成绩丢失；
- 对自制谱 config 中实际提供的难度解除存档锁定限制；
- 支持 Autoplay、ground-only 和 Autoplay 本局成绩抑制；
- 在游戏显示设置中提供真正独占全屏选项；
- 使用外部 native profile 校验当前游戏版本，未知版本默认关闭无法验证的
  native 写入能力。

## 环境要求

- Windows 版 *In Falsus*；
- 与游戏版本匹配的 MelonLoader；
- .NET 6 运行环境；
- 已验证的游戏 profile：
  - `infalsus-1.0.5`
  - `infalsus-latest`

游戏更新后，如果 `GameAssembly.dll` 或 `global-metadata.dat` 哈希发生变化，
扩展会自动选择匹配的 profile；没有匹配 profile 时会禁用无法验证的 native
功能。不要直接把旧 profile 当作其他游戏版本可用。

## 安装

完全退出游戏后，将发布包解压并放入：

```text
In Falsus/
  Mods/
    InFalsusChartExt.dll
    InFalsusChartExt/
      native-profiles.json
      bomb.png
      profiles/
        current-105.json
        latest.json
```

目录说明：

- `InFalsusChartExt.dll`：MelonLoader 入口程序集；
- `native-profiles.json`：游戏版本与 profile 的映射；
- `profiles/*.json`：字段偏移、native target 和能力矩阵；
- `bomb.png`：扩展 Bomb 使用的自定义贴图；
- `custom-results.json`：首次运行后自动生成的自制谱最佳成绩文件。

替换 DLL 或 profile 前必须先关闭游戏，并建议备份：

```text
Mods/InFalsusChartExt.dll
Mods/InFalsusChartExt/
```

## 自制谱导入

推荐使用配套工具 **SPCConverter** 导入 Studio 工程或 Falsum 歌曲包。

游戏读取的曲包结构：

```text
CustomCharts/
  PackName/
    pack_config.json
    pack_jacket.png
    SongName/
      config.json
      audio.ogg
      jacketLarge.png
      jacketSmall.png
      chart.spc
```

## 从源码构建

需要安装 .NET SDK 6 或更新版本，并准备已由 MelonLoader 生成互操作程序集
的游戏目录：

```powershell
dotnet build .\InFalsusChartExt.csproj `
  -c Release `
  -p:GameDir="D:\Steam\steamapps\common\In Falsus"
```

构建输出包含：

```text
InFalsusChartExt.dll
InFalsusChartExt/
  native-profiles.json
  bomb.png
  profiles/
```

如果更换游戏版本，需要重新生成或核对 profile 中的字段偏移、签名、RVA 和
prologue；未验证的版本不会自动启用 native 写入。

## 许可证

本项目使用 **GNU General Public License v3.0（GPL-3.0）**。
