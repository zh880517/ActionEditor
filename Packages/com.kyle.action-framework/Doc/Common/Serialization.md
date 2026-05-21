# Serialization 类型序列化

路径：`Common/Runtime/Serialization/`

基于 JSON（Newtonsoft.Json）和 GUID 的多态对象序列化方案，将任意多态对象持久化为字符串并还原，主要用于编辑器资产的跨重构稳定存储。

> 高性能零反射的二进制序列化见 [DataVisit.md](DataVisit.md)。

---

## 核心类型

| 类型 | 路径 | 说明 |
|------|------|------|
| `SerializationData` | `Serialization/SerializationData.cs` | 序列化容器，存储类型全名（`Type`）和 JSON 正文（`Data`） |
| `[TypeIdentify("GUID")]` | `Attriibute/TypeIdentifyAttribute.cs` | 为类型附加稳定 GUID，详见 [Attribute.md](Attribute.md) |
| `TypeSerializerHelper` | `TypeSerializerHelper.cs` | 序列化/反序列化静态入口 |

---

## TypeSerializerHelper

### 序列化

```csharp
// 将任意对象序列化为 SerializationData
MyBase obj = new MyDerived { Value = 42 };
SerializationData data = TypeSerializerHelper.Serialize(obj);
// data.Type = "MyNamespace.MyDerived"
// data.Data = "{\"Value\":42}"
```

### 反序列化

```csharp
// 自动按 GUID 或类型全名还原为正确的派生类实例
MyBase obj = (MyBase)TypeSerializerHelper.Deserialize(data);
```

**查找顺序**：
1. 优先按 `[TypeIdentify("GUID")]` 的 GUID 在 `TypeGUIDs` 字典中匹配
2. 回退到按 `data.Type`（类型全名）通过反射查找

### GUID 生成

```
Unity 菜单 → Tools → 生成TypeIdentify到剪切板
```

生成 `[TypeIdentify("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")]`，直接粘贴到目标类上。

---

## 典型用法

```csharp
// 1. 为需要多态存储的类型标注 GUID（防止改名后反序列化失败）
[TypeIdentify("a1b2c3d4-...")]
public class MoveNodeData : NodeDataBase { ... }

// 2. 序列化（写入资产）
var data = TypeSerializerHelper.Serialize(nodeData);
asset.SerializedData = data;   // 存入 ScriptableObject 字段

// 3. 反序列化（加载资产）
var nodeData = TypeSerializerHelper.Deserialize(asset.SerializedData);
```

---

## SerializationData 结构

```csharp
[Serializable]
public class SerializationData
{
    public string Type;   // 类型全名，如 "MyNamespace.MyClass"
    public string Data;   // JSON 序列化内容
}
```
