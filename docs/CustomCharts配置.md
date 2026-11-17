# Charts 曲包配置

## 新标准结构

Mod 运行时只扫描 `Charts/`：

```text
Charts/
  PackName/
    pack.json
    pack_jacket.png
    SongName/
      if
      audio.ogg
      chart.spc
      jacket.png
```

`pack.json` 的 `name`、`artist`、`description` 和 `visuals` 支持
字符串、五语言数组（`sc/tc/en/ja/ko`）或语言对象；`visuals` 支持
`songSelect`、`songSelectHover`、`selected`、`selectedHover`、
`deselected`、`deselectedHover`、`outline` 七张图。

`if` 支持 RD-InFalsus-Mod 的字段：`id`、`name`、`composer`、
`illust`、`song`、`jacket`、`background`、`preview_seconds`、
`charter`、`chart`、`lv`、`sp`。其中 `song`、`jacket`、
`background`、`preview_seconds`、`chart`、`charter`、`lv` 可以是
单值或四难度数组；`name`、`composer`、`illust` 支持五语言。

为兼容旧配置，`if` 还接受可选字段 `preview_audio`、
`jacket_large`、`jacket_small`、`level_section_indicator`。
缺失时分别回退到 `song`、`jacket`、`jacket`、`sp → lv → ?`。

旧 `CustomCharts/` 会在首次启动时自动转换为 `Charts/`；旧目录保留。
直接放在 `Charts/` 下、没有 `pack.json` 的歌曲目录会移动到
`Charts/.ifce-legacy/`，不再参与扫描。

## 旧格式（仅用于迁移参考）

## 目录结构

```text
CustomCharts/
  PackName/
    pack_config.json
    pack_jacket.png
    SongName/
      config.json
      audio.ogg
      chart.spc
      jacketLarge.png
      jacketSmall.png
```

`pack_jacket.png` 可省略；省略时使用游戏内置曲包视觉。

SPCConverter 会自动读取 Steam 正式版（AppID `3971950`）的
`CustomCharts`，不处理 Demo。歌曲默认只写入用户选择的已有曲包，不修改
该曲包配置；曲包创建是独立操作。没有合法曲包时会提示并创建默认
`User Charts` 曲包。

### 重复导入

- 向已有歌曲目录导入时，只更新本次导入的歌曲级元数据、音频、封面、
  背景和选中难度；
- 未选难度及其 `difficulties` 条目保持不变；
- 目标歌曲目录中已存在的同难度谱面会合并到同一个覆盖确认框，取消则
  整个导入不写文件；
- 其他歌曲目录中已使用相同 parser key 时仍拒绝导入；
- 缺失谱面文件或 `note_count=0` 的难度只警告并跳过；所有选中难度均为空
  时停止导入。

GUI 当前不再显示“导出游戏歌曲”页；Studio/Falsum 导入和 SPC 格式互转
仍可使用，CLI 能力保持不变。

## pack_config.json

```json
{
  "schemaVersion": 1,
  "id": "user-charts",
  "title": "User Charts",
  "artist": "",
  "description": "",
  "stylePackSlug": "act-4",
  "sortOrder": 0
}
```

- `id`、`title` 必填。
- `id` 在所有曲包中必须唯一，不区分大小写。
- `stylePackSlug` 决定缺失曲包封面时使用的游戏样式。
- 文件或歌曲损坏时，只拒绝所属曲包；其它曲包继续加载。

## 歌曲 config.json

```json
{
  "schemaVersion": 1,
  "id": "song-id",
  "title": "歌曲标题",
  "artist": "歌手",
  "chartDesigner": "谱师",
  "jacketDesigner": "封面作者",
  "audio": "audio.ogg",
  "previewStart": 30,
  "previewEnd": 45,
  "preview": {
    "audio": "preview.mp3",
    "start": 5,
    "end": 15
  },
  "background": "background.mp4",
  "jacketLarge": "jacket_large.png",
  "jacketSmall": "jacket_small.png",
  "difficulties": {
    "MIN": {
      "chart": "chart_min.spc",
      "rating": 1,
      "levelSectionIndicator": "1"
    },
    "FBD": {
      "chart": "chart_fbd.spc",
      "rating": 12,
      "levelSectionIndicator": "?",
      "background": "background_fbd.mp4"
    }
  }
}
```

- `id`、`title`、`audio` 必填，`id` 在所有歌曲中必须唯一。
- 难度键为 `MIN`、`EVO`、`ULT`、`FBD`，至少提供一个。
- 所有路径必须位于当前歌曲目录内，禁止绝对路径、`..` 和链接逃逸。
- `difficulty.background` 可选，用于覆盖歌曲级背景。
- 歌曲主音频、独立 preview 支持 OGG、WAV、MP3。
- 音频类型以文件头为准，扩展名不参与格式判定。即使文件名没有扩展名
  或扩展名与实际内容不同，只要文件头有效就可以加载。
- 文件头无法识别时，才按 `.ogg`、`.wav`、`.mp3` 扩展名尝试交给 FMOD；
  两者都无法识别时，该曲会显示明确错误并拒绝载入。
- 独立 preview 未提供的字段逐项回退到主音频和顶层 preview 时间。
- 背景支持 PNG、JPG、WEBP、MP4。
- 歌曲/曲包封面在导入时按文件内容识别；非 PNG 图像会转换为 PNG，
  即使原文件名错误地写成 `.png`。
- 封面缺失时使用歌曲 fallback。

## 文件命名

- `pack_jacket.png` 使用固定文件名。
- 歌曲内文件由 `config.json` 显式引用，可以使用任意安全文件名。
- chart 的 parser key 由其文件名决定，因此所有自制谱的 chart 文件名必须全局唯一。

## 自制谱成绩保存

Mod 会为自制谱维护独立的权威最佳成绩文件：

```text
InFalsusChartExt/custom-results.json
```

- 每首自制谱的每个难度只保存一条最佳成绩。
- 永久身份使用曲包 `pack_config.json` 的 `id`、歌曲 `config.json` 的 `id`
  和难度；运行时 `SongId` 不作为永久身份。
- 启动并完成曲库安装后，Mod 会读取该文件，并通过游戏原版成绩接口注入
  当前会话，使原版选曲页和结果页继续正常显示。
- 首次创建文件时，会接管当前仍能在原生存档中命中的自制谱成绩；已经因
  旧运行时 ID 变化而无法命中的历史成绩不会自动恢复。
- 修改歌曲 `config.json.id` 会被视为新歌曲，原成绩不会自动继承。
- 写入采用临时文件和原子替换，并保留上一版本
  `custom-results.json.bak`。文件损坏时 Mod 会明确报错并尽量恢复备份，
  不会用空数据覆盖损坏文件。
- 若要清空自制谱成绩，请保留文件并将 `entries` 清空；删除文件会被视为
  首次创建，并可能重新接管当前原生存档中仍可命中的成绩。

游戏仍会按原版机制维护成绩表并写入原生存档：

```text
%USERPROFILE%\AppData\LocalLow\lowiro\infalsus\<SteamID64>\
  release\savestate_V3.sav
```

- 存档以 `savestate_V3.sav` 的更新时间为准，`IFCL.sav` 不是当前谱面成绩来源。
- 成绩键包含歌曲 `BaseName`、运行时 `SongId` 和难度；值是游戏原生
  `GameResultV4`，包括分数、Lamp、通关状态、最大连击、Pace、各类判定
  计数和保存时间。
- Mod 不直接编辑该二进制存档。
- 同一玩家目录下另有 `steam_autocloud.vdf`。备份成绩时应在游戏关闭后
  同时复制整个 `<SteamID64>` 目录。
- 自制谱成绩以 `custom-results.json` 为准；原生存档中的运行时 ID 漂移
  不再导致独立成绩文件丢失。

## Autoplay 成绩保存

配置位于 `UserData/MelonPreferences.cfg` 的 `InFalsusChartExt` 分类：

```ini
AutoplaySaveScores = false
```

- 默认 `false`：AUTOPLAY 局不写原生/自制谱最佳成绩，结果页显示
  `AUTOPLAY`；
- 改为 `true` 并重启后：AUTOPLAY 局按普通对局写入最佳成绩；
- 只在启动时读取，游戏内设置页不提供切换；
- 本局只有实际自动演奏过至少一个音符或 SkyArea（包括 ground-only）
  才视为 Autoplay 局；
- 若中途退出且没有进入结果页，存档冻结保持到进程结束，避免误写；
- BepInEx 使用对应 BepInEx 配置路径。

自制谱 config 中实际提供的 MIN/EVO/ULT/FBD 会显示为已解锁；官谱和缺失
难度继续使用原版锁定状态，Mod 不修改存档解锁数据。
