# 代码开发文档

本目录只保存与代码接口、数据结构、生命周期、语法、配置和使用方式直接
相关的说明。拆分计划、审查记录和迁移过程文档保存在 `internal-docs/`。

## 文档

- `architecture.md`：当前及目标模块边界；
- `lifecycle.md`：谱面、场景、对象和 native 资源生命周期；
- `chart-format.md`：V0/V1/JSON 与扩展 marker；
- `compatibility.md`：BuildProfile、ABI 和能力位；
- `display-mode.md`：原版显示设置的独占全屏扩展与配置；
- `loader-adapters.md`：MelonLoader/BepInEx 双宿主入口、配置、日志和
  native detour 适配边界；
- `native-target-write-registry.md`：原版目标对象写入、恢复和失败边界；
- `testing.md`：单元测试、harness 与实机测试边界；
- [files/](files/README.md)：135 个生产源码/测试文件的独立开发文档。

每个文件文档固定包含：

1. 文件职责；
2. 所属生命周期；
3. 顶层类型；
4. 方法/属性说明；
5. Harmony/native/原版对象访问点；
6. 对象所有权和释放点；
7. 调用频率与复杂度；
8. 能力位和失败降级；
9. 对应测试；
10. 已知限制。

## 版本一致性规则

生产代码的每次修改、移动、重命名或拆分，必须在同一提交中同步更新对应
文件文档。文档记录的源码路径、SHA256 和行数必须与代码完全一致。

检查命令：

```powershell
python internal-docs\tools\generate_code_docs.py --check
```

只有返回 `DOC_COVERAGE_OK` 才能继续构建、部署、要求实机测试或冻结稳定版。
