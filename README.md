# InFalsusExtension

------

**当前 main 分支版本：0.3.0**

**当前 dev 分支版本：0.3.0**

**[SPCConverter](https://github.com/CrystLRK/SPCConverter) 当前 main 分支版本：1.2.4-patch2，最低支持版本：0.2.0**

**[SPCConverter](https://github.com/CrystLRK/SPCConverter) 当前 dev 分支版本：1.2.4-patch2，最低支持版本：0.2.0**

------

InFalsusExtension 是 In Falsus 的一个模组，包含了包括但不限于加载自制谱面、自动游玩、扩展语句等一系列功能。

**警告：本模组较大程度上修改了原版的逻辑，尤其是 SkyArea 相关部分，游玩前请自行备份存档数据，如有模组相关问题请附上对应日志进行提问！**

## 主要功能

- 加载自制谱面及自制曲包；

  - 支持自定义背景图片 / 视频。
  - 支持自定义难度角标文本。

- 自动游玩，包括模拟正常操作的轮椅模式及修改判定的自动游玩模式；

  - 提供 Ground-Only 选项，开启时只会自动判定地面 Note。
  - 默认情况下，只要开启任何形式的自动游玩模式，该次游玩将不会记录成绩。

- 新增了对以下自定义语句的支持：

  - `bomb(timems, width, lane)`

  - `trackmove(time_ms, x, y, z, angle_xoz, angle_yoz, angle_xoy, ease, duration_ms)`

  - ```
    timinggroup(id, [noinput=true | noclip=true]) {
    	...
    }
    ```

    - 也支持 In Falsus Studio+ 的 

      ```
      timinggroup(id):
      	...
      groupend()
      ```

      形式。

  - `showtext(time_ms, id, [size])`

  - `setcolor(time_ms, skyarea_groupId, #RRGGBB[AA], [edge_glow])`

  - `scenecontrol(time_ms, params[params...])`

    - `scenecontrol(time_ms, trackdisplay, duration_ms, alpha)`
    - `scenecontrol(time_ms, hidegroup, groupId, flag)`

  - `audiooffset(offset_ms)`

  - 以上语句的具体使用方法及语义请查看 docs/SYNTAX.md。

- 在显示设置中新增了独占全屏模式选项。

## 配置项

配置文件位于对应加载器的配置目录中：

- `EnableNegativeSpeedModify`：是否启用负流速 SkyArea / Hold 的视觉修正，默认 `true`；
- `HideNoInputBrackets`：是否隐藏 NoInput / NoClip SkyArea 的方括号，默认 `true`；
- `AutoplaySaveScores`：自动游玩时是否保存成绩，默认 `false`。

## 使用要求

- 截至当前最新版 **0.3.0**，模组支持 Steam 最新版 **1.0.7b**。
- 游戏本体需要已安装 MelonLoader 0.7.x **或** BepInEx 6 IL2CPP 模组加载器。
- 需在 Release 内获取**符合当前加载器**的模组文件。

## 安装及更新

### MelonLoader

1. 完全退出游戏；

2. 解压 Melonloader 版发行包，把其中的 `Mods/` 文件夹覆盖到**游戏根目录**；

3. 安装后游戏目录结构应如下所示：

```text
In Falsus/
  Mods/
    InFalsusChartExt.dll
    InFalsusChartExt/
      native-profiles.json
      profiles/
      bomb.png
```

4. 启动游戏。

### BepInEx

1. 完全退出游戏；

2. 解压 BepInEx 版发行包，把其中的 `BepInEx/` 文件夹覆盖到**游戏根目录**；

3. 安装后游戏目录结构应如下所示：

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

4. 启动游戏。

如无特殊说明，两种版本的模组更新方法均为直接覆盖旧模组文件。
更新前请完全退出游戏，并保留 `profiles/`、配置文件和成绩文件。


## 导入自制谱

建议使用 [SPCConverter](https://github.com/CrystLRK/SPCConverter) 项目一键导入In Falsus Studio 工程或 Falsum 歌曲包。

导入后需**重启游戏**才可以正常游玩导入的自制谱面。

## 注意事项

1. **使用本模组时，请不要使用其他类型的歌曲加载相关 Mod。**本 Mod 暂未对其他 Mod 作出适配，因此如果加载多个效果相同的 Mod，极易引发错误并导致游戏崩溃，**如果加载了功能不冲突的其他 Mod 并仍然导致游戏崩溃，请禁用本 Mod 并带着日志提交 Issue。**
   - 日志位置：
     - MelonLoader：`./MelonLoader/Logs`下**修改时间最晚的**`.log`文件。
     - BepInEx：`./BepInEx/LogOutput.log`

## 许可证

本项目使用 **GNU General Public License v3.0（GPL-3.0）**。

## 第三方与致谢

Autoplay 的触发方式、成绩隔离和结果页提示设计参考了 [RD-InFalsus-Mod](https://github.com/REDDRAGON-HL/RD-InFalsus-Mod)。

Copyright (c) 2026 RedDragon
Licensed under the MIT License.

`.if` 特殊难度文字格式参考了 [INF_Special-Level](https://github.com/Collapsing2112/INF_Special-Level)。

某新增语句的语法与表现设计参考了 [InfStudio_LyricShake](https://github.com/Collapsing2112/InfStudio_LyricShake)。

