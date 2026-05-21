# TypeCollector 类型收集器

路径：`Common/Editor/`

三个泛型静态类，在编辑器启动时通过反射扫描所有已加载程序集，结果惰性缓存（首次访问时计算，后续直接返回缓存）。

---

## 三个收集器

### `TypeCollector<TBaseType>`

收集所有实现（或继承）`TBaseType` 的非抽象、非接口具体类型。

```csharp
// 获取所有 IFlowNode 的具体实现类型
List<Type> nodeTypes = TypeCollector<IFlowNode>.Types;
```

| 成员 | 类型 | 说明 |
|------|------|------|
| `Types` | `List<Type>` | 惰性缓存的类型列表 |

---

### `AttributeTagTypeCollector<TAttribute>`

收集所有标注了 `TAttribute` 的非抽象、非接口类型。

```csharp
// 获取所有标注了 [FlowNodePath] 的类型及其 Attribute 实例
Dictionary<Type, FlowNodePathAttribute> nodes =
    AttributeTagTypeCollector<FlowNodePathAttribute>.Types;
```

| 成员 | 类型 | 说明 |
|------|------|------|
| `Types` | `Dictionary<Type, TAttribute>` | 惰性缓存，Key = 类型，Value = Attribute 实例 |

---

### `TypeWithAttributeCollector<TBaseType, TAttribute>`

同时满足：实现 `TBaseType` **且** 标注了 `TAttribute`。

```csharp
// 获取所有继承 PropertyElement 且标注了 [CustomPropertyElement] 的编辑器类
Dictionary<Type, CustomPropertyElementAttribute> editors =
    TypeWithAttributeCollector<PropertyElement, CustomPropertyElementAttribute>.Types;

// 也可获取不缓存的新结果（用于需要每次最新结果的场景）
var fresh = TypeWithAttributeCollector<PropertyElement, CustomPropertyElementAttribute>.Collector();
```

| 成员 | 类型 | 说明 |
|------|------|------|
| `Types` | `Dictionary<Type, TAttribute>` | 惰性缓存版本 |
| `Collector()` | `Dictionary<Type, TAttribute>` | 每次调用都重新扫描，不使用缓存 |

---

## 框架内典型用途

| 使用方 | 收集器 | 目的 |
|--------|--------|------|
| `PropertyElementFactory` | `TypeWithAttributeCollector<PropertyElement, CustomPropertyElementAttribute>` | 发现所有已注册的属性编辑器 |
| `FlowGraph` 节点菜单 | `AttributeTagTypeCollector<FlowNodePathAttribute>` | 构建节点右键创建菜单 |
| `TypeSerializerHelper` | `AttributeTagTypeCollector<TypeIdentifyAttribute>` | 构建 GUID → Type 映射表 |
| `DataVisit` 代码生成 | `AttributeTagTypeCollector<VisitCatalogAttribute>` | 收集待生成序列化代码的类型 |

---

## 注意事项

- 收集结果在 **编辑器进程生命周期内** 只计算一次（静态字段缓存），Domain Reload 后自动重置。
- 所有收集器均跳过抽象类和接口，只返回可实例化的具体类型。
- `Collector()` 方法（仅 `TypeWithAttributeCollector` 有）每次都重新扫描，适合需要实时结果的极少数场景；一般优先使用 `.Types`。
