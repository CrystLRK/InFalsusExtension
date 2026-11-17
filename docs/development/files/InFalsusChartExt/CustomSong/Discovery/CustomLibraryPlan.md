# CustomLibraryPlan.cs

> 源码：`InFalsusChartExt/CustomSong/Discovery/CustomLibraryPlan.cs`
> SHA256：`8ED0062865508AA2383CC39D015AC49AB3421800529A4700C468CDD18B5E5C40`
> 代码行数：246
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

启动时安装 CustomSong 数据，随后跟随选曲、谱面编译和资源释放。

## 顶层及类型声明

- `CustomSongSchema`（`class`，源码第 12 行）
- `CustomLibraryPlan`（`class`，源码第 57 行）
- `CustomPackPlan`（`class`，源码第 67 行）
- `CustomSongPlan`（`class`，源码第 99 行）
- `CustomChartPlan`（`class`，源码第 192 行）

## 方法/属性职责

- `LanguageIndex`（第 23 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int LanguageIndex(string key)`
- `Language`（第 38 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static string Language(`
- `Visual`（第 91 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string Visual(string key, int language)`
- `Title`（第 152 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string Title(int language) =>`
- `Composer`（第 155 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string Composer(int language) =>`
- `Illustrator`（第 158 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string Illustrator(int language) =>`
- `AudioFor`（第 161 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string AudioFor(int slot) =>`
- `PreviewFor`（第 164 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string PreviewFor(int slot) =>`
- `PreviewStartFor`（第 167 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal float PreviewStartFor(int slot) =>`
- `PreviewEndFor`（第 170 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal float PreviewEndFor(int slot) =>`
- `BackgroundFor`（第 173 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string BackgroundFor(int slot) =>`
- `JacketLargeFor`（第 176 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string JacketLargeFor(int slot) =>`
- `JacketSmallFor`（第 179 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string JacketSmallFor(int slot) =>`
- `ChartDesigner`（第 240 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string ChartDesigner(int language) =>`
- `JacketDesigner`（第 243 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal string JacketDesigner(int language) =>`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
