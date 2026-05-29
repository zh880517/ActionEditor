# ECS 用户手册

## 概述

ECS 模块提供两套轻量实体组件系统：

| 子模块 | 命名空间 | 程序集 | 适用场景 |
| --- | --- | --- | --- |
| `LiteECS` | `ECSLite` | `ECSLite` | 与 Unity 对象无关的纯逻辑 ECS，适合战斗、规则、模拟等运行时数据逻辑 |
| `ViewECS` | `VECS` | `ViewECS` | 绑定 `GameObject` 的视图层 ECS，适合表现层对象、加载状态和按组件变更响应 |
| `Editor/Generator` | `ECSEditor` | `ECS.Editor` | 扫描组件类型并生成 Context、组件 ID、Collector 初始化和组件重置代码 |

**主要特性：**

- Entity 使用索引与版本号识别，销毁后复用槽位时可避免旧引用误用
- Component 由数组、字典和对象复用池管理，移除时会执行生成的重置逻辑
- 支持普通组件、空字段 Flag 组件、Unique 组件和 Static 组件
- 支持 Group / MatchGroup 遍历拥有指定组件的实体
- ViewECS 额外支持 ReactiveGroup，只处理当前版本后新增或修改过的组件
- 通过编辑器代码生成器为业务 Context 生成组件 ID 与初始化代码

**目录结构：**

```text
ECS/
├── LiteECS/             # 纯逻辑 ECS 运行时
│   ├── Component/       # Component 标记、Collector 和重置入口
│   ├── Group/           # Group、MatchGroup 和查找结果
│   └── System/          # System 生命周期接口与 GroupUpdateSystemT
├── ViewECS/             # 面向 GameObject 的视图 ECS
│   ├── Component/       # View Component 标记、Collector 和清理入口
│   ├── Group/           # Group、MatchGroup、ReactiveGroup
│   └── System/          # Update、LateUpdate、Reactive 系统基类
└── Editor/Generator/    # ECSLite / ViewECS 代码生成器
```

---

## 核心概念

### Entity

`LiteECS` 使用 `Entity<IContext>`，`ViewECS` 使用 `ViewEntity`。二者都是轻量值类型句柄，内部引用实际实体对象，并通过 `Index + Version` 判断有效性。

常用操作：

```csharp
var component = entity.Add<MoveComponent>();
var data = entity.Get<MoveComponent>();
bool has = entity.Has<MoveComponent>();      // ViewECS
bool hasLite = entity.Hast<MoveComponent>(); // LiteECS 当前 API 名称
entity.Remove<MoveComponent>();
```

`ViewEntity` 还提供 `Modify<T>()` 和 `TryModifyGet<T>()`，用于标记组件在当前版本被修改，供 Reactive 系统筛选。

### Component

组件是 `class`，必须有无参构造函数。

| 类型 | LiteECS 标记 | ViewECS 标记 | 说明 |
| --- | --- | --- | --- |
| 普通组件 | `IComponent` | `IViewComponent` | 每个实体一份组件实例 |
| Flag 组件 | 普通组件且无 public 实例字段 | 普通组件且无 public 实例字段 | 只表示标签，Collector 共享一个组件实例 |
| Unique 组件 | `IUniqueComponent` | `IViewUniqueComponent` | 同一 Context 内只允许一个实体持有 |
| Static 组件 | `IStaticComponent` | `IViewStaticComponent` | 挂在 Context 上的全局单例数据，不属于某个实体 |

组件被移除或实体销毁时，Collector 会复用组件对象，并调用生成器注册到 `ComponentReset<T>.OnReset` 或 `ViewComponentClear<T>.OnReset` 的重置函数。生成器会重置 public 实例字段；字段类型如果有 public `Clear()` 或 `Reset()` 方法，会优先调用。

### Context

Context 管理实体列表、组件 Collector 和 Static 组件。

`LiteECS` 中通常使用生成器生成的业务 Context，例如：

```csharp
BattleContext context = BattleECS.CreateContext();
var entity = context.Create();
```

`ViewECS` 中使用生成器生成的 `ViewECS.CreateContext()`：

```csharp
VECS.ViewContext context = ViewECS.CreateContext();
VECS.ViewEntity entity = context.CreateEntity();
context.SetGameObject(entity, gameObject);
```

### System 与 Feature

`Feature` 负责按生命周期批量驱动 System。

| 生命周期 | LiteECS 接口 | ViewECS 接口 |
| --- | --- | --- |
| 初始化 | `IInitializeSystem.OnInitialize()` | `IInitializeSystem.OnInitialize()` |
| 每帧更新 | `IUpdateSystem.OnUpdate()` | `IUpdateSystem.OnUpdate()` |
| LateUpdate | — | `ILateUpdateSystem.OnLateUpdate()` |
| 清理 | `ICleanupSystem.OnCleanup()` | `ICleanupSystem.OnCleanup()` |
| 销毁 | `ITearDownSystem.OnTearDown()` | `ITearDownSystem.OnTearDown()` |

`AddSystem` 是 `protected`，业务侧通常继承 `Feature`，在构造函数中按顺序添加系统。

---

## LiteECS 接入

### 定义 Context 与组件

LiteECS 以业务 Context 接口隔离不同 ECS 世界。生成器按 Context 名称约定查找组件接口，Context 接口所在命名空间应与生成器传入的 `nameSpace` 保持一致：

| Context 接口 | 组件接口 | Unique 组件接口 | Static 组件接口 |
| --- | --- | --- | --- |
| `IBattle` | `IBattleComponent` | `IBattleUniqueComponent` | `IBattleStaticComponent` |

组件示例：

```csharp
namespace Game.Battle
{
    public interface IBattle { }

    public class PositionComponent : IBattleComponent
    {
        public UnityEngine.Vector3 Value;
    }

    public class PlayerTagComponent : IBattleComponent
    {
    }

    public class MainCameraComponent : IBattleUniqueComponent
    {
        public UnityEngine.Camera Camera;
    }

    public class BattleTimeComponent : IBattleStaticComponent
    {
        public float Time;
    }
}
```

`IBattleComponent`、`IBattleUniqueComponent` 和 `IBattleStaticComponent` 通常由生成文件提供；业务组件新增后需要再次运行生成器，让组件 ID 与 Collector 初始化同步更新。空 public 字段组件会被识别为 Flag 组件，例如 `PlayerTagComponent`。

### 生成 Context

在项目侧创建一个 Editor 入口调用生成器：

```csharp
using ECSEditor;
using Game.Battle;
using UnityEditor;

public static class BattleECSGenerateMenu
{
    [MenuItem("Tools/Battle ECS/Generate")]
    private static void Generate()
    {
        ECSGenerator.ECSLiteGen<IBattle>(
            "Game.Battle",
            "Assets/Scripts/Battle/Generated",
            customReset: null);
    }
}
```

生成器会输出：

| 文件 | 说明 |
| --- | --- |
| `{ContextName}Context.cs` | 业务 Context 和组件接口定义 |
| `{ContextName}ECS.cs` | 组件 ID 静态初始化与 `CreateContext()` |
| `{ContextName}ComponentReset.cs` | 组件字段重置逻辑 |
| `{ContextName}ComponentReset_Init.cs` | 将重置函数注册到 `ComponentReset<T>` |

### 创建实体与遍历组件

```csharp
BattleContext context = BattleECS.CreateContext();

var entity = context.Create();
entity.Add<PositionComponent>().Value = UnityEngine.Vector3.zero;
entity.Add<PlayerTagComponent>();

var group = context.CreateGroup<PositionComponent>();
while (group.MoveNext())
{
    var e = group.Entity;
    var position = group.Component;
}
```

### System 示例

```csharp
public class MoveSystem : ECSLite.IUpdateSystem
{
    private readonly BattleContext context;

    public MoveSystem(BattleContext context)
    {
        this.context = context;
    }

    public void OnUpdate()
    {
        var group = context.CreateGroup<PositionComponent>();
        while (group.MoveNext())
        {
            group.Component.Value += UnityEngine.Vector3.forward;
        }
    }
}
```

`GroupUpdateSystemT<ContextType, IContext, TComponent>` 已封装 Group 遍历模板，但当前类没有显式实现 `IUpdateSystem`。如果要交给 `Feature.OnUpdate()` 调度，派生系统需要补上生命周期接口，或由项目侧手动调用 `OnUpdate()`。

---

## ViewECS 接入

### 定义组件

ViewECS 使用固定的 `VECS.IView` 作为 Context 标记，业务组件直接实现 ViewECS 接口。

```csharp
public class ViewTransformComponent : VECS.IViewComponent
{
    public UnityEngine.Transform Transform;
}

public class DirtyTagComponent : VECS.IViewComponent
{
}

public class SelectedViewComponent : VECS.IViewUniqueComponent
{
}

public class ViewConfigComponent : VECS.IViewStaticComponent
{
    public float FadeTime;
}
```

### 生成 ViewECS

```csharp
using ECSEditor;
using UnityEditor;

public static class ViewECSGenerateMenu
{
    [MenuItem("Tools/View ECS/Generate")]
    private static void Generate()
    {
        ECSGenerator.ViewECSGen(
            "Game.View",
            "Assets/Scripts/View/Generated",
            customReset: null);
    }
}
```

生成器会输出 `ViewECS.cs`、`ViewComponentClearup_Reset.cs` 和 `ViewComponentClearup_Init.cs`，用于创建 `ViewContext`、初始化组件 ID，以及注册组件移除和重置逻辑。

`ViewECSClearupGenerator` 当前通过 `Type.GetType("VECS.ViewComponentClearup")` 查找组件移除回调。如果项目需要自动注册 `OnRemove(ViewEntity, TComponent)`，扩展类型需要放在生成器能发现的位置；否则可在 `CreateContext()` 前自行注册 `ViewComponentClear<T>.OnRemove`，或调整生成器的查找路径。

### 创建实体与绑定 GameObject

```csharp
VECS.ViewContext context = ViewECS.CreateContext();

VECS.ViewEntity entity = context.CreateEntity();
context.SetGameObject(entity, gameObject);

var transform = entity.Add<ViewTransformComponent>(forceModify: true);
transform.Transform = gameObject.transform;
```

`Add<T>(forceModify: true)` 或 `Modify<T>()` 会刷新组件版本，Reactive 系统依赖这个版本判断是否需要处理。

### Group 与 ReactiveGroup

普通 Group 会遍历拥有指定组件的实体：

```csharp
var group = context.CreatGroup<ViewTransformComponent>(includeDisable: true);
while (group.MoveNext())
{
    VECS.ViewEntity entity = group.Entity;
    ViewTransformComponent component = group.Component;
}
```

`includeDisable` 默认为 `false` 时，Collector 只返回内部状态为 `Loaded` 的实体。当前公开 API 创建出的实体初始状态为 `Active`，如果项目没有额外的加载完成状态切换入口，遍历刚创建的实体时应传入 `includeDisable: true`。

Reactive 系统只处理创建系统后新增或修改过的组件：

```csharp
public class SyncTransformSystem : VECS.ReactiveUpdateSystem<ViewTransformComponent>
{
    public SyncTransformSystem(VECS.ViewContext context) : base(context, includeDisable: true)
    {
    }

    protected override void OnExecuteEntity(VECS.ViewEntity entity, ViewTransformComponent component)
    {
        // 只处理 Add 或 Modify 后的 ViewTransformComponent
    }
}
```

ViewECS 还提供 `GroupUpdateSystem<TComponent>`、`GroupLateUpdateSystem<TComponent>`、`ReactiveUpdateSystem<TComponent>` 和 `ReactiveLateUpdateSystem<TComponent>` 四种系统基类。

---

## 代码生成规则

`ECSEditor.ComponentCollector` 会扫描当前 AppDomain 中的所有程序集，并收集满足以下条件的类型：

- 类型不是 interface，且没有 `[Obsolete]`
- 类型可赋值给当前 Context 标记接口
- 类型可赋值给对应 Component / Unique / Static Component 接口
- public 非静态字段用于生成重置逻辑
- 抽象组件类型不会计入可实例化组件数量，但可参与继承层级的重置生成

组件排序规则为：普通组件在前，Unique 组件在后；同类组件按组件名排序。组件名如果以 `Component` 结尾，生成器内部会去掉该后缀作为显示名。

自定义重置可通过 `customReset` 参数处理特定字段类型：

```csharp
ECSGenerator.ViewECSGen(
    "Game.View",
    "Assets/Scripts/View/Generated",
    (fieldType, fieldName) =>
    {
        if (fieldType == typeof(UnityEngine.Vector3))
            return $"value.{fieldName} = UnityEngine.Vector3.zero;";
        return null;
    });
```

---

## 注意事项

- 组件 ID 由生成代码写入静态泛型类型，新增、删除或重命名组件后需要重新生成。
- 组件对象会被复用，移除组件后不要继续持有并读取旧组件引用。
- Flag 组件没有独立数据实例，只适合表达标签状态。
- Unique 组件同一时间只挂在一个实体上，再次添加会替换原持有实体，并对旧组件执行移除与重置逻辑。
- `AddToAllEntity<T>()` 只会对当前有效实体添加组件，不会处理已销毁且等待复用的实体槽。
- `LiteECS` 程序集配置了 `noEngineReferences: true`，核心运行时代码不依赖 UnityEngine；业务组件是否依赖 Unity 类型由业务程序集决定。
- `ViewECS` 的方法名目前保留源码中的拼写：`CreatGroup`、`CreatMatchGroup`。
