# 源码文件文档

本目录由以下工具根据当前源码生成并校验：

```text
internal-docs/tools/generate_code_docs.py
```

当前覆盖：

- `InFalsusChartExt/`：104 个 C#/C++ 源文件；
- `SPCConverter/src/`：19 个 Python 生产文件；
- `SPCConverter/tests/`：12 个 Python 测试文件；
- 合计：135 份文件文档。

每份文档包含源码路径、SHA256、行数、职责、生命周期、类型、方法、原生访问、
清理点、性能边界和关联测试。代码职责或结构改变后必须重新运行生成器并
人工复核生成结果。
