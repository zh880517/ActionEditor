# Recycle 对象池

路径：`Common/Runtime/Recyleable/`

轻量级 C# 对象池，用于高频创建/销毁场景下减少 GC 压力。

---

## 类型概览

| 类型 | 说明 |
|------|------|
| `IPoolable` | 池化对象标记接口（无方法，仅标记） |
| `IRecyleable` | 可回收对象接口，声明 `Recyle()` 方法 |
| `RecyleableObject` | 基础可回收对象，`Recyle()` 将自身放回所属池 |
| `RecyleablePool<T>` | 泛型对象池，`Get()` 取出实例，`Recyle()` 归还 |
| `RecylePools` | 全局池注册表，`ClearAllPoolObject()` 清空所有池内对象 |

---

## 快速使用

```csharp
// 1. 定义可回收对象，继承 RecyleableObject
public class MyEvent : RecyleableObject
{
    public int EntityId;
    public float Value;
}

// 2. 取出对象（池中无可用实例时自动 new）
var evt = RecyleablePool<MyEvent>.Get();
evt.EntityId = 42;
evt.Value = 3.14f;

// 3. 用完归还（不要在归还后继续访问对象）
evt.Recyle();

// 4. 清空所有池（如场景切换时）
RecylePools.ClearAllPoolObject();
```

---

## 注意事项

- 归还后对象回到池中，**不要继续持有引用**。
- `RecyleableObject.Recyle()` 会将对象加回其注册的 `pool`（在 `RecyleablePool<T>.Get()` 时自动绑定）。
- 如需在归还前重置字段，在 `Recyle()` 前手动清零，或重写 `Recyle()` 做清理再调用 `base.Recyle()`。
