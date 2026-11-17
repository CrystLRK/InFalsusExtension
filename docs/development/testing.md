# 测试说明

## 构建

MelonLoader：

```powershell
dotnet build InFalsusChartExt\InFalsusChartExt.csproj -c Release `
  -p:GameDir=<path to In Falsus> --no-restore -warnaserror
```

当目标游戏目录只安装 BepInEx 时，`GameDir` 必须指向仍保留
`MelonLoader/net6` 与 `MelonLoader/Il2CppAssemblies` 的目录（例如切换
加载器前的备份目录）；否则 Melon 构建会因程序集引用缺失而失败。

BepInEx：

```powershell
dotnet build InFalsusChartExt\InFalsusChartExt.csproj -c Release `
  -p:Loader=BepInEx -p:GameDir=<path to In Falsus> `
  -p:BepInExDir=<extracted BepInEx 6 package> `
  -p:BepInExInteropDir=<BepInEx/interop> --no-restore -warnaserror
```

要求：0 warning / 0 error。

## Python

```powershell
python -m unittest discover -s SPCConverter\tests -p 'test_*.py'
```

## 官谱回环

```powershell
python output\reimport_20261108\check_official_roundtrip.py
```

必须达到 raw + semantic 323/323。

## C# harness

主要入口：

- `R3ContractHarness`
- `TimingGroupTransactionHarness`
- `ChartTransformTransactionHarness`
- `BuildProfileMatrixHarness`
- `FrameScanHarness`
- `PinnedArraysHarness`
- `NativeHookLifecycleHarness`
- `ParserKeyHarness`
- `DifficultySlotHarness`
- `AutoplayScoreGuardHarness`
- `CustomScoreApiHarness`
- `CustomScoreStorageHarness`
- `CustomSongAudioHarness`
- `SkyBlockTestHarness`
- `SkyBlockOverlapHarness`
- `SkyBlock alias scan`
- `BombPlanValidation`
- `BombJudgeValidation`
- `PrerollValidation`
- `DescriptorHarness`

## 实机测试

AI 不启动游戏。每轮需要用户按 `internal-docs/测试_*.md` 自行启动并反馈。

## 文件文档覆盖

```powershell
python internal-docs\tools\generate_code_docs.py --check
```

覆盖检查通过时输出：

```text
DOC_COVERAGE_OK
```
