# OptionsOverlay.cs

> 源码：`InFalsusChartExt/Bootstrap/OptionsOverlay.cs`
> SHA256：`27AE9D00C440C872A7AC812FE88A6F259B74D704E821D15E4B4EB0A88266F2E7`
> 代码行数：165
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

F7/F8/F9、启动关闭 Autoplay 与控制面板

## 生命周期

MelonLoader/BepInEx 程序集加载、GameScene 切换和宿主卸载。

## 顶层及类型声明

- `OptionsOverlay`（`class`，源码第 18 行）

## 方法/属性职责

- `Init`（第 35 行）：加载 Mod 配置；Autoplay 每次启动强制关闭并写回 false，ground-only 仅保留为选项。
  - 签名：`internal static void Init()`
- `Tick`（第 90 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick()`
- `WinEdge`（第 115 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool WinEdge(int vk, ref bool downState)`
- `OnGUI`（第 123 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnGUI()`
- `DrawContents`（第 143 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DrawContents()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 20 行：`[DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vk);`

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
