# 谱面格式说明

## 输入格式

- SPC V0：文本谱面；
- SPC V1：`ICP1` 二进制容器；
- 标准 JSON：转换器内部语义模型。

## ICP1

- Header：28 字节；
- Note：80 字节；
- Event：32 字节；
- note/event 数量和加密字段必须通过长度与边界校验；
- 分数由 cipher 与原始分数对共同还原；
- 重新编码必须保留原始 cipher 和 pairs。

## TimingGroup

- 组号位于 note group 高 32 位；
- noinput/noclip 模式位与 group id 一起校验；
- type5 event 表示组内速度；
- V0/V1 回环必须保持可玩语义。

## 扩展 marker

- trackmove；
- showtext；
- audiooffset；
- scenecontrol；
- setcolor；
- bomb/noinput/noclip flags。

未知 note/event/marker 在用户转换入口必须 fail-closed，不能静默丢弃。
