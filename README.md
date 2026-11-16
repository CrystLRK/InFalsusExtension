# InFalsusExtension

**版本：0.1.1**

InFalsusExtension 是适用于 *In Falsus* 的扩展工具，用于加载自制歌曲与
曲包，并支持自制谱专用的显示、判定和辅助功能。

## 主要功能

- 加载按曲包整理的自制歌曲；
- 支持 OGG、WAV、MP3 音频和独立试听音频；
- 支持自定义封面、图片背景和视频背景；
- 支持常见扩展谱面效果；
- 为自制谱分别保存各难度的最佳成绩；
- 自动解锁自制谱中实际提供的难度；
- 提供 Autoplay 和仅自动判定地键模式；
- 在游戏显示设置中增加真正独占全屏选项。

## 使用要求

- Windows 版 *In Falsus* 正式版；不支持 Demo 版；
- 游戏已正确安装 MelonLoader；
- 使用与当前游戏版本匹配的 InFalsusExtension 发行包。

## 安装

1. 完全退出游戏；
2. 解压 InFalsusExtension 发行包；
3. 将发行包内的文件和文件夹放进游戏根目录；
4. 确认游戏目录中至少存在：

   ```text
   In Falsus/
     Mods/
       InFalsusChartExt.dll
       InFalsusChartExt/
   ```

5. 启动游戏。

不要只替换 DLL 而保留其他版本的附属文件，也不要将不同发行包中的文件
混合使用。

## 更新

更新前请完全退出游戏。建议先备份现有的 `Mods/InFalsusChartExt` 文件夹，
然后用新发行包整体替换旧文件。

## 导入自制谱

推荐使用 [SPCConverter](https://github.com/CrystLRK/SPCConverter) 导入
In Falsus Studio 工程或 Falsum 歌曲包。

导入完成后完全退出并重新启动游戏，新歌曲和更新后的难度才会出现在曲库中。

## 游戏内操作

| 快捷键 | 功能 |
|---|---|
| `F7` | 切换仅自动判定地键：自动处理 Tap、Hold，Flick 和 SkyArea 仍需手动；开启时会同时启用 Autoplay |
| `F8` | 打开或关闭扩展选项面板 |
| `F9` | 打开或关闭 Autoplay |

Autoplay 在每次启动游戏时都会恢复为关闭状态。默认情况下，本局只要开启过
Autoplay，就不会写入成绩，结果页会显示 `AUTOPLAY`。

## 自制谱成绩

自制谱最佳成绩会自动保存到：

```text
Mods/InFalsusChartExt/custom-results.json
```

该文件只保存每个自制谱难度的最佳成绩。清理或迁移 Mod 时，可以一并备份
此文件。

## 全屏模式

游戏显示设置中会提供窗口模式选项，其中包括真正独占全屏。切换模式后若
游戏暂时失去焦点，返回游戏时会自动恢复所选模式。

## 常见问题

### 游戏没有加载扩展

- 确认 MelonLoader 已正确安装；
- 确认发行包已解压到游戏根目录；
- 确认没有混用不同版本的 Mod 文件；
- 查看 MelonLoader 日志中的报错信息。

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
- MelonLoader 日志。

## 许可证

本项目使用 **GNU General Public License v3.0（GPL-3.0）**。
