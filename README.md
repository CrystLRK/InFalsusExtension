# InFalsusExtension

**版本：0.2.0-patch1**

InFalsusExtension 是适用于 *In Falsus* 的扩展工具，用于加载自制歌曲与
曲包，并支持自制谱专用的显示、判定和辅助功能。

## 主要功能

- 加载按曲包整理的自制歌曲；
- 支持 `Charts/` 曲包目录以及 `pack.json` / `if` 描述文件；
- 支持 OGG、WAV、MP3 音频和独立试听音频；
- 支持自定义封面、图片背景和视频背景；
- 支持常见扩展谱面效果；
- 支持 Studio+ `text(...)` 逐字歌词与 `sp` 自定义难度文字；
- 为自制谱分别保存各难度的最佳成绩；
- 自动解锁自制谱中实际提供的难度；
- 在游戏“音符设定”页右侧提供三向 Autoplay 模式和 `GROUND ONLY` 开关；
- 启动时显示黑屏加载界面，用英文显示迁移/曲库准备状态，进入主菜单后消失；
- 在游戏显示设置中增加真正独占全屏选项。

## 使用要求

- Windows 版 *In Falsus* 正式版；不支持 Demo 版；
- 游戏已正确安装 MelonLoader 0.7.x，或 BepInEx 6 IL2CPP；
- MelonLoader 与 BepInEx 只能二选一，不能同时安装；
- 使用与当前游戏版本匹配的 InFalsusExtension 发行包。

## 安装

发行包分为 MelonLoader 版和 BepInEx 版，请只选择与当前已安装加载器
对应的发行包。两个加载器不能同时安装。

### MelonLoader 版

1. 完全退出游戏；
2. 确认游戏根目录已正确安装 MelonLoader 0.7.x；
3. 解压 MelonLoader 版发行包，把其中的 `Mods/` 文件夹覆盖到游戏根目录；
4. 安装后目录应至少包含：

   ```text
   In Falsus/
     Mods/
       InFalsusChartExt.dll
       InFalsusChartExt/
         native-profiles.json
         profiles/
         bomb.png
   ```

5. 启动游戏。首次启动会把 `Mods/InFalsusChartExt/` 中的资源迁移到游戏
   根目录的 `InFalsusChartExt/`，并保留原文件。

### BepInEx 版

1. 完全退出游戏；
2. 将 BepInEx 6 IL2CPP 安装到游戏根目录；
3. 解压 BepInEx 版发行包，把其中的 `BepInEx/` 文件夹覆盖到游戏根目录；
4. 安装后目录应至少包含：

   ```text
   In Falsus/
     BepInEx/
       plugins/
         InFalsusChartExt.dll
         InFalsusChartExt/
           native-profiles.json
           profiles/
           bomb.png
   ```

5. 启动游戏。首次启动会把 `BepInEx/plugins/InFalsusChartExt/` 中的资源
   迁移到游戏根目录的 `InFalsusChartExt/`，并保留原文件。

### 共用资源目录

从 0.2.0 起，MelonLoader 和 BepInEx 共用游戏根目录下的资源目录：

```text
In Falsus/
  InFalsusChartExt/
    native-profiles.json
    profiles/
    bomb.png
    fonts/
    custom-results.json
```

切换加载器时该目录不需要重建或复制；`fonts/` 与 `custom-results.json`
会在首次运行时自动生成。

### 注意事项

- 不要只替换 DLL 而保留其他版本的附属文件；
- 不要混用不同发行包中的文件；
- 不要在同一个游戏目录中同时安装 MelonLoader 和 BepInEx。MelonLoader 的
  `version.dll` 与 BepInEx 的 `winhttp.dll`、`BepInEx/` 目录不能同时存在。

## 更新

更新前请完全退出游戏。建议先备份当前加载器下的 `InFalsusChartExt` 文件夹，
然后用同一种加载器的发行包整体替换旧文件。

## 导入自制谱

推荐使用 [SPCConverter](https://github.com/CrystLRK/SPCConverter) 导入
In Falsus Studio 工程或 Falsum 歌曲包。

新标准曲包目录为：

```text
In Falsus/
  Charts/
    PackName/
      pack.json
      SongName/
        if
        audio.ogg
        jkt.png
        chart.spc
```

首次启动时，旧版 `CustomCharts/` 曲包会自动转换为 `Charts/` 结构。
转换不会删除旧目录；旧目录中的文件会保留，便于回退到旧版 Mod。
转换只执行一次，目标已存在时不会覆盖。

导入完成后完全退出并重新启动游戏，新歌曲和更新后的难度才会出现在曲库中。

## 游戏内操作

| 快捷键 | 功能 |
|---|---|
| `F8` | 打开或关闭扩展选项面板 |

打开游戏的“音符设定”设置页后，右侧会显示 Autoplay 面板：

- `关闭`：不自动演奏；
- `自动游玩`：使用非 engine 模式自动演奏；
- `轮椅模式`：使用 engine 输入模拟自动演奏；
- `GROUND ONLY`：只自动处理 Tap、Hold，Flick 和 SkyArea 仍需手动。

以上选项都只在本次游戏运行期间有效，每次启动游戏都会恢复为关闭状态。
默认不会保存 AUTOPLAY 对局成绩；可在 Mod 配置中将
`AutoplaySaveScores` 设为 `true` 后重启，改为按普通对局保存成绩。

只有本局实际自动演奏过至少一个音符或 SkyArea 后，才会把本局视为
Autoplay 局；此时不会写入原生成绩或自制谱成绩，结果页会显示 `AUTOPLAY`。

## 自制谱成绩

自制谱最佳成绩会自动保存到：

```text
InFalsusChartExt/custom-results.json
```

该文件只保存每个自制谱难度的最佳成绩。清理或迁移 Mod 时，可以一并备份
此文件。

## 全屏模式

游戏显示设置中会提供窗口模式选项，其中包括真正独占全屏。切换模式后若
游戏暂时失去焦点，返回游戏时会自动恢复所选模式。

## 常见问题

### 游戏没有加载扩展

- 确认当前加载器（MelonLoader 或 BepInEx）已正确安装；
- 确认发行包已解压到游戏根目录；
- 确认没有混用不同版本的 Mod 文件；
- 查看加载器日志中的报错信息：
  - MelonLoader：`MelonLoader/Logs/` 下最新的日志文件；
  - BepInEx：`BepInEx/LogOutput.log`。

### 自制歌曲没有出现在曲库

- 使用最新版 SPCConverter 重新导入；
- 确认导入到了当前正在游玩的游戏目录；
- 完全退出并重新启动游戏。

### 歌曲没有声音或没有背景

- 确认谱面中选择了实际存在的音频和背景文件；
- 音频使用 OGG、WAV 或 MP3；
- 背景使用 PNG、JPG、JPEG、WEBP 或 MP4；
- 修改后重新导入并重启游戏。

### 游戏更新后无法使用

请等待与当前游戏版本匹配的新发行包。不要在游戏更新后继续混用旧版本
文件。

## 反馈问题

提交 Issue 时请附上：

- 游戏版本；
- InFalsusExtension 版本；
- 可复现的操作步骤；
- 对应歌曲、难度和时间点；
- 加载器日志（MelonLoader 或 BepInEx）。

## 许可证

本项目使用 **GNU General Public License v3.0（GPL-3.0）**。

## 第三方与致谢

Autoplay 的触发方式、成绩隔离和结果页提示设计参考了 RD-InFalsus-Mod：

https://github.com/REDDRAGON-HL/RD-InFalsus-Mod

Copyright (c) 2026 RedDragon
Licensed under the MIT License.

`.if` 特殊难度文字格式参考了 INF_Special-Level：

https://github.com/Collapsing2112/INF_Special-Level

本项目按行为独立实现了 `sp` 的 UI 接管：选歌大卡片、难度按钮、游戏 HUD、
转场、歌曲条和结算页都会显示自定义难度文字；未填写难度角标时 `sp`
默认等于 Rating 数字。

某新增语句的语法与表现设计参考了
InfStudio_LyricShake：

https://github.com/Collapsing2112/InfStudio_LyricShake
