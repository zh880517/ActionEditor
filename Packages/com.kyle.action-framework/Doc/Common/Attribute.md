# Common Attribute 参考手册

路径：`Common/Runtime/Attriibute/`

框架公共 Attribute 集合，驱动 PropertyEditor UI 渲染、类型发现和运行时行为，分为通用控制和 PropertyEditor 专用两组。

---

## 通用显示控制

| Attribute | 作用目标 | 说明 |
|-----------|---------|------|
| `[DisplayName("名称", "tooltip")]` | 字段 | 覆写字段在 PropertyEditor / Inspector 中的显示名称和悬停提示 |
| `[Alias("名称")]` | 类型 | 为类型指定别名显示名，用于编辑器菜单和类型选择窗口 |
| `[ExpandedInParent]` | struct 字段 | 将 struct 的子字段内联展开于父级 UI，不显示折叠 Foldout |
| `[FixedArraySize(n)]` | 数组/List 字段 | 锁定集合长度，PropertyEditor 隐藏增删按钮 |
| `[HiddenInPropertyEditor]` | 字段 / 类型 | PropertyEditor 完全跳过该字段或类型 |
| `[HiddenInTypeSelect]` | 类型 | 在 `TypeSelectWindow` 弹窗中隐藏该类型 |
| `[TypeCatalog]` | 类型 | 将类型归入指定 Catalog 分组，供 DataVisit 代码生成器收集 |

---

## PropertyEditor 扩展 Attribute

路径：`Common/Runtime/Attriibute/PropertyEditor/`

这组 Attribute 用于**扩展或覆写**特定字段的 PropertyEditor 渲染方式，分两种用途：

### 注册自定义编辑器元素（类型级）

标注在 `PropertyElement` 子类上，声明其负责渲染的数据类型。

| Attribute | 说明 |
|-----------|------|
| `[CustomPropertyElement(typeof(T), priority=0)]` | 为数据类型 `T` 注册对应的编辑器控件；`priority` 越高优先级越高，可覆盖默认实现 |

### 覆写字段渲染（字段级）

标注在目标字段上，触发特定的 `AttributePropertyElement` 渲染逻辑。

| Attribute | 字段类型 | 渲染效果 |
|-----------|---------|---------|
| `[EnumFlag]` | `enum` | 多选 Flag 样式的 EnumFlagsField |
| `[FloatRange(min, max)]` | `float` | MinMaxSlider 范围滑动条 |
| `[IntegerRange(min, max)]` | `int` | MinMaxSlider 范围滑动条 |
| `[IntPopupSelect(options[])]` | `int` | PopupField 下拉列表，值为选项索引 |
| `[StringPopupSelect(options[])]` | `string` | PopupField 下拉列表，值为选项字符串 |
| `[LayerSelect]` | `int` | Unity LayerField 层选择器 |
| `[PlaceHolder]` | 任意 | 渲染为空占位元素（FlowGraph 数据端口专用，不可编辑） |

> 字段级 Attribute 均继承自 `CustomPropertyAttribute`。自定义新的字段级渲染方式只需新建继承 `CustomPropertyAttribute` 的 Attribute，配合 `TAttributePropertyElement<T, A>` 实现编辑器逻辑。详见 [PropertyEditor.md](PropertyEditor.md)。

---

## 序列化相关 Attribute

| Attribute | 说明 |
|-----------|------|
| `[TypeIdentify("GUID")]` | 为类型指定稳定 GUID，供 `TypeSerializerHelper` 进行跨重构的多态反序列化匹配。菜单 `Tools/生成TypeIdentify到剪切板` 可快速生成。 |
