# InFalsusExtension 扩展谱面语句总览

> 适用版本：InFalsusExtension 0.2.0-patch1
>
> 转换工具：SPCConverter 1.2.4-patch1
>
> 本文以当前 V0 解析器、ChartIngress 和运行时实现为准。

## 语句状态速查

| 语句                                  | 当前状态 | 主要作用                                  |
| ------------------------------------- | -------- | ----------------------------------------- |
| `bomb(...)`                           | 支持     | 独立视觉、独立判定的地雷音符              |
| `timinggroup(...) { ... }`            | 推荐写法 | 独立视觉流速；可附加 `noinput` / `noclip` |
| `trackmove(...)`                      | 支持     | 移动、旋转或回正游戏轨道                  |
| `showtext(...)`                       | 支持     | 按谱面时间显示文本                        |
| `setcolor(...)`                       | 支持     | 修改指定 authored SkyArea group 的配色    |
| `scenecontrol(...)`                   | 支持     | 控制轨道透明度或隐藏 timing group 视觉    |
| `audiooffset(...)`                    | 支持     | 调整 gameplay 音乐起播位置                |

## 1. 基本规则

扩展语句写入 SPC V0 明文谱面。SPCConverter 将 V0 转为 SPC V1 后才可导入
游戏。

- 时间单位均为毫秒；
- 小数毫秒按四舍五入处理；
- 空行、以 `#` 开头的整行、以 `//` 开头的整行会被忽略；
- 其他行内第一个 `//` 及其后内容作为注释；`#` 只在行首具有注释语义；
- 语句名大小写不敏感；
- 未知语句、未知 V1 类型或非法参数会明确报错，不会静默忽略；
- V1 使用最终文件名作为滚动加密种子，生成后不要手动改名；
- 扩展 marker 会在送入原版音符、判定和计分系统前被分离；
- 普通官谱和不含扩展语句的普通音符继续使用原版路径。

推荐文本编码为 UTF-8；读取时也兼容 UTF-8 BOM 和 GB18030。

## 2. 基础音符与事件

以下语句属于基础谱面语法，扩展语句与它们共同使用。

### `chart`

```text
chart(bpm, meter)
```

必须作为第一条有效语句。`bpm` 和 `meter` 必须大于 0。

### `tap`

```text
tap(time_ms, width, lane)
```

- `width`：整数宽度；
- `lane`：`0..5`；
- 地面中心轨通常为 `1..4`，`0/5` 为左右侧轨。

### `hold`

```text
hold(time_ms, lane, width, duration_ms)
```

- `time_ms`：头部判定时间；
- `duration_ms`：持续长度，不能为负数；
- `lane`：`0..5`。

### `flick`

```text
flick(time_ms, center_numerator, denominator, width_numerator, direction)
```

- `center_numerator / denominator`：中心位置；
- `width_numerator / denominator`：宽度，实际宽度为
  `width_numerator / denominator`；
- `denominator` 必须大于 0；
- `direction=4`：向右；
- `direction=16`：向左。

### `skyarea`

自动分组形式：

```text
skyarea(time_ms,
        start_center_num, start_den, start_width_num,
        end_center_num, end_den, end_width_num,
        left_ease, right_ease, duration_ms)
```

显式组号形式：

```text
skyarea(..., duration_ms, authored_group)
```

- 中心位置和宽度均使用分数；
- `left_ease/right_ease`：`0` 线性、`1` SINE_OUT、`2` SINE_IN；
- 显式组号范围为 `0..4294967295`；
- 组号写 `-1` 等价于自动分组；
- 自动分组时，如果当前 SkyArea 的起点与上一段终点相差不超过 `2ms`，
  沿用上一组号，否则分配新组号；
- 同一 authored group 表示颜色和红色警告的身份，不强制把所有空间断开、
  时间不连续的对象合并为一个判定 block；
- SkyArea 实际连接、分叉和合流由运行时根据时间与空间几何分类；
- authored group 与 `timinggroup` 是两套独立概念：前者只表达作者组号，
  后者控制视觉流速和 `noinput` / `noclip` 模式；
- 时间顺序上，author group id 不应回退；出现回退时转换器会警告。

### `bpm`

```text
bpm(time_ms, bpm_value)
bpm(time_ms, bpm_value, beat_unit)
bpm(time_ms, bpm_value, beat_unit, mode)
```

- 组外：写入全局 BPM 事件；
- timing group 内：作为该组视觉流速的基础倍率；
- `mode=0` 为区段型 BPM；
- `mode!=0` 为过渡型 BPM；
- `bpm_value` 必须大于 0。

### `track`

```text
track(time_ms, multiplier)
```

- 组外：写入全局流速事件；
- timing group 内：写入该组独立流速；
- 只影响视觉滚动速度，不改变音符判定时刻；
- timing group 没有组内 `track()` 时默认倍率为 1.0。

### `lane`

```text
lane(time_ms, lane_index, enabled)
```

- `lane_index`：`0..5`；
- `enabled`：`0` 关闭，`1` 开启。

## 3. `timinggroup`

用于给音符建立独立的视觉流速时间线。推荐使用花括号块。

`timinggroup` 不是 SkyArea 的 authored group。它不会改写 `skyarea(...)`
最后一项作者组号，也不决定两个 SkyArea 是否在空间上连接。

### 语法

```text
timinggroup(id) {
    ...
}

timinggroup(id, noinput=true) {
    ...
}

timinggroup(id, noclip=true) {
    ...
}
```

也支持命名组：

```text
timinggroup("verse", noinput=true) {
    ...
}
```

兼容形式：

```text
timinggroup(id):
    ...
groupend()

timinggroup(id)
...
timinggroup()
```

- 推荐始终使用 `{ ... }`；
- `:` + `groupend()` 仅用于外部工程兼容；
- timing group 块不能嵌套；
- 数字 id 范围为 `1..16777216`；
- `timinggroup()` 将当前状态恢复到组外；
- 同一数字 id 在谱面后部再次出现时仍视为同一个组；
- 命名组按首次出现顺序分配内部数字 id，名称区分大小写。

### 默认速度

- 块内未写 `bpm()` 时，以谱面头部 BPM 作为该组基础 BPM；
- 块内未写 `track()` 时，组倍率为 1.0；
- 组内 `bpm()` 与 `track()` 合成该组视觉速度，不影响全局轨道速度；
- 主 group 的 `track()` 不会改变 timing group 的速度；
- 同一组再次出现时沿用该组之前的速度状态。

### 判定语义

普通 timing group：

- 视觉使用该组独立速度时间线；
- 点击、判定窗口和结算时间仍使用谱面 authored 时间。

`noinput=true`：

- 组内 tap、hold、flick、bomb、SkyArea 全部转成视觉物件；
- 不进入原版音符列表、判定记录、连击、计分或打击反馈；
- 正常出现并按 noinput 生命周期过线消失；
- 组内速度事件继续影响视觉移动。

`noclip=true`：

- 自动包含 noinput；
- 不判定、不计分、不产生判定反馈；
- 越过判定线后不立即消失，继续移动直至自然离开可视范围；
- 可裸写 `timinggroup(id,noclip)`，等价于 `noclip=true`。

示例：

```text
chart(120,4)

timinggroup(1) {
    bpm(0, 90)
    track(0, 0.75)
    tap(2000, 1, 1)
}

timinggroup(2, noinput=true) {
    tap(3000, 1, 1)
    hold(3500, 2, 1, 1000)
}

timinggroup(3, noclip=true) {
    skyarea(5000, 20, 100, 40, 20, 100, 40, 0, 0, 1500, 10)
}
```

## 4. `bomb`

```text
bomb(time_ms, width, lane)
```

- `width`：整数宽度，不能为负数；
- `lane`：`0..5`；
- `0/5` 使用原版左右侧轨形态；
- 独立视觉和独立判定由扩展运行时处理；
- 不计入普通 Tap 的自动改写流程，但仍进入原版计分分类。

判定规则：

- 没有点击：在自身判定时刻记为大 EXACT/Shiny；
- 到 `time_ms` 时仍未记录到点击：最终记最高判定 `Shiny`
  （游戏中的大 EXACT/EXACT+ 等级）；
- 在 `[time_ms-100, time_ms]` 内发生新的按下：记 `BREAK`；
- 判定窗口开始前已经按住不会被当作新按下；
- 提前按下、窗口外按下或按住不放不会触发成功判定；
- `time_ms` 之后不会重新接受成功输入。

示例：

```text
bomb(1000, 1, 0)
bomb(2000, 2, 3)
bomb(3000, 1, 5)
```

## 5. `trackmove`

```text
trackmove(time_ms, x, y, z, angle_xoz, angle_yoz, angle_xoy, ease, duration_ms)
```

- `x/y/z`：轨道位移；
- `angle_xoz/angle_yoz/angle_xoy`：绕对应平面的旋转角度，单位为度；
- 六个运动参数全部为 `0`：返回初始轨道姿态；
- `duration_ms`：动画时长，不能为负数。

`ease`：

- `1`：三次方加速；
- `2`：三次方减速；
- `3`：线性；
- `0`：通常在 `duration_ms=0` 时用于立即到位；时长非 0 时当前实现走线性
  插值分支，建议优先使用 `1/2/3`。

多条非回正 `trackmove` 会相对当前姿态继续叠加。全零语句会插值回初始
姿态。只修改轨道与音符容器，不修改其他 UI。

## 6. `showtext`

### 谱面语句

```text
showtext(time_ms, id)
showtext(time_ms, id, size)
```

- `time_ms`：文本出现时间；
- `id`：文本样式 id；
- `size`：可选整数，大于 0 时覆盖文本文件中的字号。

### 文本文件

与谱面放在同一歌曲目录，推荐文件名：

```text
<谱面名>.text.json
```

同时兼容：

```text
<谱面名>.spc.text.json
```

示例：

```json
{
  "texts": [
    {
      "id": 1,
      "text": "Hello",
      "x": 0.5,
      "y": 0.12,
      "size": 6,
      "duration": 2500,
      "fadeIn": 300,
      "fadeOut": 400,
      "color": "#7FD4FF",
      "scale": 1.0,
      "shadow": true,
      "shadowAlpha": 0.55,
      "shadowScaleX": 1.8,
      "shadowScaleY": 2.8,
      "shadowOffsetY": 0.0
    }
  ]
}
```

字段默认值：

| 字段            | 默认值 |
| --------------- | ------ |
| `duration`      | `2000` |
| `fadeIn`        | `300`  |
| `fadeOut`       | `300`  |
| `x` / `y`       | `0.5`  |
| `size`          | `6`    |
| `color`         | 白色   |
| `scale`         | `1.0`  |
| `shadow`        | `true` |
| `shadowAlpha`   | `0.55` |
| `shadowScaleX`  | `1.8`  |
| `shadowScaleY`  | `2.8`  |
| `shadowOffsetY` | `0.0`  |

`x/y` 使用屏幕归一化坐标，推荐保持在 `0..1`，`y=0` 在顶部；坐标指向
文字可见包围盒中心，不是左上角。超出该范围不会被转换器直接拒绝，但文字
可能显示在屏幕外。

`duration` 从出现时刻开始计算，淡出发生在显示时长末尾。当前运行时把
`duration <= 0` 视为极长显示时间；如需普通定时文本，请填写正数。

文本配置根节点既可以是 `{ "texts": [...] }`，也可以直接是样式数组。
转换器会在写出谱面前检查缺失文件、无效 JSON 和缺失 id。

错误行为：

- SPCConverter 遇到缺失文本文件、无效 JSON、缺少引用 id 时拒绝输出谱面；
- 游戏运行时缺失文本文件时只忽略本谱 showtext；
- 文本文件存在但无效或缺少 id 时显示错误面板并阻止正常游玩。

## 7. `setcolor`

```text
setcolor(time_ms, skyarea_group, #RRGGBB)
setcolor(time_ms, skyarea_group, #RRGGBBAA)
setcolor(time_ms, skyarea_group, #RRGGBB[AA], edge_glow)
```

- 从 `time_ms` 起修改对应 authored SkyArea group 的默认配色；
- 只按 `skyarea(...)` 最后一项 authored group 匹配；几何相连但组号不同的
  SkyArea 不会被这条语句合并；
- 六位颜色自动补 `FF` alpha；
- 八位颜色的 alpha 作为原色 alpha 的缩放值；
- 多条语句按时间顺序生效，同一时刻以文件靠后的语句为准；
- 原版离区变红和判定警告继续使用红色插值；
- 共享判定线光带纹理不随该语句改色。

`edge_glow`：

- `1/true/yes/on`：显示并使用 tint 后的边缘光纹；
- `0/false/no/off`：隐藏边缘中心和边缘 glow 颜色；
- 省略时为 `1`。

示例：

```text
setcolor(0, 10, #5FD4FF)
setcolor(2000, 10, #FF3050)
setcolor(4000, 10, #3080FF, 0)
```

## 8. `scenecontrol`

### 轨道透明度

```text
scenecontrol(time_ms, trackdisplay, duration_ms, alpha)
```

- `alpha`：`0..255`；
- `255` 为正常显示；
- `0` 为完全透明/隐入黑色；
- `duration_ms=0` 时立即切换；
- 多次指令从当前实际透明度继续过渡；
- 轨道本体、侧壁、高亮、小节线和轨道阴影一起过渡；
- 音符、判定线、天空指针、HUD、打击特效和判定文字不隐藏。

### 显式隐藏 timing group 视觉

```text
scenecontrol(time_ms, hidegroup, groupId, flag)
```

- `flag=1`：隐藏视觉；
- `flag=0`：恢复视觉；
- `groupId` 对应 `timinggroup(id)` 的数字 id；`groupId=0` 表示组外音符；
- 只过滤 spawn/render 列表；
- 判定列表、record、计分、连击和打击特效保持正常。

## 9. `audiooffset`

```text
audiooffset(offset_ms)
```

- 谱面级设置，只接受第一条；
- 后续重复的 `audiooffset(...)` 不会覆盖第一条；
- 正值：gameplay 音乐延迟指定毫秒后再起播；
- 负值：gameplay 音乐从歌曲内部对应位置开始；
- 不改变谱面时间、音符、判定窗口、进度或 UI；
- 不影响选歌 preview、UI 音效和打击音效。

示例：

```text
audiooffset(250)
```

表示谱面时钟先运行 250ms，音乐随后开始。

```text
audiooffset(-500)
```

表示谱面 0ms 时音乐从歌曲 500ms 位置开始。
