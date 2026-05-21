# PropertyEditor 操作手册

## 概述

PropertyEditor 是 Common 模块提供的**反射驱动通用属性 UI 系统**，基于 UIToolkit（`UnityEngine.UIElements`）为任意可序列化类型自动生成属性编辑界面，是整个框架中 Inspector 与编辑器面板的核心 UI 基础设施。

**核心特性：**
- 纯反射驱动，无需手写 UI 代码即可为任意 struct / class 生成编辑界面
- 通过 `[CustomPropertyElement]` Attribute 注册扩展，无需修改框架代码
- 通过 `[CustomProperty]` Attribute 在字段级别覆写渲染逻辑
- 值变更通过 UIToolkit 事件冒泡（`PropertyValueChangedEvent`）向上传递，与框架事件体系完全兼容
- 支持嵌套 struct/class、数组、`List<T>` 递归渲染
- 支持只读模式、标签宽度统一对齐

---

## 目录

1. [核心类层次](#核心类层次)
2. [字段解析流程](#字段解析流程)
3. [快速开始](#快速开始)
4. [PropertyValueChangedEvent（值变更事件）](#propertyvaluechangedevent值变更事件)
5. [内置类型编辑器](#内置类型编辑器)
6. [扩展：注册新类型编辑器](#扩展注册新类型编辑器)
7. [扩展：字段级自定义渲染](#扩展字段级自定义渲染)
8. [控制行为的 Attribute](#控制行为的-attribute)
9. [API 参考](#api-参考)

---

## 核心类层次

```
VisualElement（UIToolkit）
└── PropertyElement                     抽象基类，所有属性 UI 的根
    ├── TPropertyElement<T>             泛型基类，处理单值类型（int/float/string 等）
    ├── AttributePropertyElement        抽象基类，用于 [CustomProperty] 字段级扩展
    │   └── TAttributePropertyElement<T, A>   泛型版本，T=值类型，A=CustomPropertyAttribute派生类
    ├── StructedPropertyElement         struct / class 容器，递归渲染子字段，支持折叠
    ├── ArrayPropertyElement            数组编辑器
    └── ListPropertyElement             List<T> 编辑器
```

工厂入口：`PropertyElementFactory.CreateByFieldInfo(FieldInfo)`

---

## 字段解析流程

`PropertyElementFactory.CreateByFieldInfo` 按以下优先级依次处理：

```
1. [NonSerialized] / [HideInInspector] / [HiddenInPropertyEditor]
       → 返回 null（跳过该字段）

2. [PlaceHolder]（字段或类型上）
       → 返回 PlaceHolderElement（空占位，只读）

3. 字段标注了 [CustomProperty] 的派生类
       → 查找该 Attribute 类型对应的 AttributePropertyElement 并实例化

4. 字段类型是数组（T[]）
       → ArrayPropertyElement<T>

5. 字段类型是 List<T>
       → ListPropertyElement<T>

6. 字段类型已注册 [CustomPropertyElement]
       → 返回对应的 PropertyElement 实现

7. 字段类型是 enum
       → EnumElement

8. 字段类型继承自 UnityEngine.Object
       → ObjectElement

9. 字段类型是 class 或非基础值类型 struct
       → StructedPropertyElement（递归渲染子字段）

10. 无法处理 → 返回 null
```

> `[FixedArraySize]` 标注在数组/List 字段时，禁用增删按钮，锁定长度。  
> `[ExpandedInParent]` 标注在 struct 字段时，内容直接内联展开于父级，不显示折叠箭头。

---

## 快速开始

### 为任意类型生成属性编辑界面

```csharp
using PropertyEditor;
using UnityEngine.UIElements;

// 目标数据类型（任意可序列化 struct/class）
public struct MyConfig
{
    public int Count;
    public float Speed;
    public string Name;
    public Vector3 Position;
}

// 在编辑器 VisualElement 中创建属性界面
var element = new StructedPropertyElement(typeof(MyConfig));
parentContainer.Add(element);

// 设置初始值
MyConfig config = new MyConfig { Count = 5, Speed = 1.5f, Name = "Test" };
element.SetValue(config);

// 监听变更（UIToolkit 事件冒泡）
parentContainer.RegisterCallback<PropertyValueChangedEvent>(evt =>
{
    // 将变更写回数据
    // evt.Field    —— 发生变更的 FieldInfo
    // evt.Value    —— 新值（object）
    // evt.Index    —— 若在数组内则为元素索引，否则为 -1
});
```

### 单字段创建

```csharp
using System.Reflection;
using PropertyEditor;

FieldInfo field = typeof(MyConfig).GetField("Speed");
PropertyElement element = PropertyElementFactory.CreateByFieldInfo(field);
if (element != null)
{
    element.SetLable("速度", "移动速度(单位/秒)");
    element.SetValue(1.5f);
    parentContainer.Add(element);
}
```

---

## PropertyValueChangedEvent（值变更事件）

`PropertyValueChangedEvent` 继承自 `EventBase<PropertyValueChangedEvent>`，值变更时由叶子节点发出，**向上冒泡**，由任意祖先节点统一处理。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Value` | `object` | 字段的新值 |
| `Field` | `FieldInfo` | 发生变更的字段元数据（数组元素时为数组字段） |
| `Index` | `int` | 数组/List 中的元素索引；普通字段为 `-1` |

```csharp
// 典型用法：在父容器监听，统一写回数据
element.RegisterCallback<PropertyValueChangedEvent>(evt =>
{
    if (evt.Index >= 0)
    {
        // 数组元素变更
        var arr = (int[])evt.Field.GetValue(myData);
        arr[evt.Index] = (int)evt.Value;
    }
    else
    {
        // 普通字段变更
        evt.Field.SetValue(myData, evt.Value);
    }
    evt.StopPropagation();
});
```

> **注意**：`PropertyElement` 内部调用 `evt.StopPropagation()` 拦截 UIToolkit 原生值变更事件（`ChangeEvent<T>`），统一转发为 `PropertyValueChangedEvent`，上层只需处理一种事件。

---

## 内置类型编辑器

所有内置编辑器位于 `PropertyEditor/BuiltIn/`，通过 `[CustomPropertyElement]` 自动注册。

| 数据类型 | 编辑器类 | 说明 |
|---------|---------|------|
| `bool` | `BoolElement` | Toggle |
| `int` | `IntElement` | IntegerField |
| `float` | `FloatElement` | FloatField |
| `double` | `DoubleElement` | DoubleField |
| `string` | `StringElement` | TextField |
| `enum` | `EnumElement` | EnumField（自动处理所有枚举，无需注册） |
| `Vector2` | `Vector2Element` | Vector2Field |
| `Vector3` | `Vector3Element` | Vector3Field |
| `Vector4` | `Vector4Element` | Vector4Field |
| `Vector2Int` | `VectorInt2Element` | Vector2IntField |
| `Vector3Int` | `Vector3IntElement` | Vector3IntField |
| `Rect` | `RectElement` | RectField |
| `RectInt` | `RectIntElement` | RectIntField |
| `Quaternion` | `QuaternionElement` | Vector4Field（xyzw 分量） |
| `Color` | `ColorElement` | ColorField |
| `Color`（HDR/Alpha） | `ColorUsageElement` | ColorField（受 `[ColorUsage]` 控制） |
| `Gradient` | `GradientElement` | GradientField |
| `AnimationCurve` | `CurveElement` | CurveField |
| `LayerMask` | `LayerMaskSelectElement` | MaskField |
| `UnityEngine.Object` 子类 | `ObjectElement` | ObjectField（自动处理所有 Object 子类） |
| `int`（弹出选择） | `IntPopupSelectElement` | 需配合 `[IntPopupSelect(options)]`，PopupField |
| `string`（弹出选择） | `StringPopupSelectElement` | 需配合 `[StringPopupSelect(options)]`，PopupField |
| `int`（Layer） | `LayerSelectElement` | 需配合 `[LayerSelect]`，LayerField |
| `float`（范围） | `FloatRangeElement` | 需配合 `[FloatRange(min,max)]`，MinMaxSlider |
| `int`（范围） | `IntRangeElement` | 需配合 `[IntegerRange(min,max)]`，MinMaxSlider |

---

## 扩展：注册新类型编辑器

为新数据类型（或覆盖已有类型）创建自定义编辑器，只需：

**第一步：继承 `TPropertyElement<T>`**

```csharp
using PropertyEditor;
using UnityEngine.UIElements;

// 1. 继承 TPropertyElement<你的数据类型>
// 2. 标注 [CustomPropertyElement(typeof(你的数据类型))]
[CustomPropertyElement(typeof(MyColor))]
public class MyColorElement : TPropertyElement<MyColor>
{
    private readonly ColorField colorField = new ColorField();

    public MyColorElement()
    {
        Add(colorField);
        colorField.RegisterValueChangedCallback(evt =>
        {
            // 将 UIToolkit 原生事件转换为框架事件
            OnValueChanged(ChangeEvent<MyColor>.GetPooled(value, ConvertBack(evt.newValue)));
        });
    }

    // 将 value 写入 UI 控件
    protected override void SetValueToField()
    {
        colorField.SetValueWithoutNotify(Convert(value));
    }

    public override void SetLable(string name, string tip)
    {
        colorField.label = name;
        colorField.tooltip = tip;
    }

    public override void SetLableWidth(float width)
    {
        colorField.labelElement.style.minWidth = width;
    }

    public override bool ReadOnly
    {
        get => !colorField.enabledSelf;
        set => colorField.SetEnabled(!value);
    }
}
```

> 注册后无需任何手动调用，`PropertyElementFactory` 会在首次使用时自动扫描发现。

**优先级**：`[CustomPropertyElement(typeof(T), priority: 10)]` — 数值越高，优先级越高，可覆盖默认实现。

---

## 扩展：字段级自定义渲染

当需要对**特定字段**（而非特定类型）使用自定义 UI 时，使用 `[CustomProperty]` + `TAttributePropertyElement`：

**第一步：定义 Attribute（继承 `CustomPropertyAttribute`）**

```csharp
using PropertyEditor;

// 携带配置参数的自定义属性
public class MyRangeAttribute : CustomPropertyAttribute
{
    public float Min { get; }
    public float Max { get; }
    public MyRangeAttribute(float min, float max) { Min = min; Max = max; }
}
```

**第二步：实现对应的编辑器（继承 `TAttributePropertyElement<T, A>`）**

```csharp
using PropertyEditor;
using UnityEngine.UIElements;

// 将 MyRangeAttribute 类型注册为编辑器触发键
[CustomPropertyElement(typeof(MyRangeAttribute))]
public class MyRangeElement : TAttributePropertyElement<float, MyRangeAttribute>
{
    private readonly Slider slider = new Slider();

    public MyRangeElement()
    {
        Add(slider);
        slider.RegisterValueChangedCallback(OnValueChanged);
    }

    public override void OnCreate()
    {
        // attribute 在 OnCreate 时已经赋值
        slider.lowValue  = attribute.Min;
        slider.highValue = attribute.Max;
    }

    protected override void SetValueToField() => slider.SetValueWithoutNotify(value);
    public override void SetLable(string name, string tip) { slider.label = name; slider.tooltip = tip; }
    public override void SetLableWidth(float w) => slider.labelElement.style.minWidth = w;
    public override bool ReadOnly { get => !slider.enabledSelf; set => slider.SetEnabled(!value); }
}
```

**第三步：在目标字段上标注**

```csharp
public struct WeaponConfig
{
    [MyRange(0f, 100f)]
    public float Damage;

    [IntPopupSelect(new[] { "单发", "连发", "爆发" })]
    public int FireMode;

    [StringPopupSelect(new[] { "Sword", "Axe", "Spear" })]
    public string WeaponType;
}
```

---

## 控制行为的 Attribute

以下 Attribute 均在 `Runtime/Attriibute/` 下定义，运行时和编辑器均可引用：

| Attribute | 作用目标 | 效果 |
|-----------|---------|------|
| `[HiddenInPropertyEditor]` | 字段 / 类型 | PropertyEditor 中完全跳过该字段或类型 |
| `[HideInInspector]`（Unity 内置） | 字段 | 同上，PropertyEditor 亦遵循此标记 |
| `[NonSerialized]`（C# 内置） | 字段 | PropertyEditor 跳过 |
| `[ExpandedInParent]` | struct 字段 | 将 struct 子字段内联展开，不显示折叠 Foldout |
| `[FixedArraySize(n)]` | 数组 / List 字段 | 锁定长度，隐藏增删按钮 |
| `[DisplayName("名称", "tooltip")]` | 字段 | 覆写字段的显示名和提示文本 |
| `[PlaceHolder]` | 字段 / 类型 | 渲染为空占位元素（FlowGraph 数据端口用） |

---

## API 参考

### `PropertyElementFactory`（静态工厂）

| 方法 | 说明 |
|------|------|
| `CreateByFieldInfo(FieldInfo field)` | 根据字段元数据创建对应的 `PropertyElement`；返回 `null` 表示该字段不参与渲染 |

### `PropertyElement`（抽象基类）

| 成员 | 说明 |
|------|------|
| `Field` | 对应的 `FieldInfo`，数组元素时为数组字段 |
| `Index` | 数组/List 中的元素索引，`-1` 表示非集合元素 |
| `ReadOnly` | 是否只读（getter/setter） |
| `SetLable(name, tip)` | 设置标签文本和 Tooltip |
| `SetLableWidth(width)` | 设置标签最小宽度（用于多字段对齐） |
| `SetValue(object value)` | 将值写入 UI（不触发事件） |
| `Find(name)` | 在 `StructedPropertyElement` 中按字段名查找子 `PropertyElement` |
| `OnCreate()` | 工厂创建并设置 `Field`/`Index` 后的回调，可在此读取 Attribute 配置 |

### `StructedPropertyElement`

| 成员 | 说明 |
|------|------|
| `Value` | 当前持有的数据对象（`object`） |
| `ExpandedInParent` | 是否以内联展开模式渲染（无 Foldout） |
| `Find(fieldName)` | 按字段名查找子 `PropertyElement` |
| `ReadOnly` | 设置时级联传递给所有子字段（`[ReadOnly]` 标注的字段除外） |

### `PropertyValueChangedEvent`

| 成员 | 说明 |
|------|------|
| `Value` | 字段新值（`object`） |
| `Field` | 变更字段的 `FieldInfo` |
| `Index` | 数组元素索引（普通字段为 `-1`） |
| `bubbles` | 始终为 `true`，向上冒泡 |
