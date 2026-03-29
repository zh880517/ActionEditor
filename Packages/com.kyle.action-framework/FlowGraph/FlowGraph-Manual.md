# FlowGraph 用户手册

## 目录

1. [概述](#概述)
2. [核心概念](#核心概念)
3. [节点系统](#节点系统)
4. [创建自定义节点](#创建自定义节点)
5. [编辑器使用](#编辑器使用)
6. [代码生成](#代码生成)
7. [运行时系统](#运行时系统)
8. [子图（SubGraph）](#子图subgraph)
9. [调试系统](#调试系统)
10. [完整示例](#完整示例)
11. [API 参考](#api-参考)

---

## 概述

FlowGraph 是 ActionEditor 框架中的可视化脚本系统，支持在 Unity 编辑器中以节点图方式编写逻辑，并在运行时高效执行。

**主要特性：**

- 可视化节点编辑，所见即所得
- 基于属性（Attribute）的节点注册，零侵入式扩展
- 支持数据流（Data Flow）与控制流（Control Flow）分离
- 支持多帧执行节点（Updateable）、条件分支、动态输出
- 嵌套子图（SubGraph）
- 自动代码生成，减少手动样板代码
- 可选运行时调试追踪（需开启 `FLOW_GRAPH_DEBUG`）

**模块结构：**

```
FlowGraph/
├── Runtime/
│   ├── Asset/       # 图资产定义（FlowGraph、FlowNode、边）
│   ├── Attribute/   # 节点注册属性
│   ├── Data/        # 运行时数据结构
│   ├── Debug/       # 调试追踪
│   └── Executor/    # 节点执行引擎
└── Editor/
    ├── CodeGen/     # 代码生成
    ├── Context/     # 编辑器上下文管理
    ├── ElementView/ # 图编辑器 UI
    └── Window/      # 编辑器窗口
```

---

## 核心概念

### 控制流（Flow）与数据流（Data）

FlowGraph 中有两种边：

| 类型 | 用途 | 对应类 |
|------|------|--------|
| **控制流边（Flow Edge）** | 决定节点执行顺序 | `FlowEdge` |
| **数据流边（Data Edge）** | 在节点间传递数据值 | `FlowDataEdge` |

- 控制流从上一个节点的输出端口流向下一个节点的输入端口
- 数据流将某节点的输出字段连接到另一节点的输入字段

### 节点类型

| 接口 | 说明 |
|------|------|
| `IFlowEntry` | 图的入口节点，每个图只有一个 |
| `IFlowDataProvider` | 纯数据节点，无控制流端口，仅提供数据 |
| `IFlowInputable` | 有控制流输入端口 |
| `IFlowOutputable` | 有控制流输出端口 |
| `IFlowUpdateable` | 跨帧执行，有输入输出端口 |
| `IFlowConditionable` | 条件节点，有 True（索引 0）和 False（索引 1）两个输出 |
| `IFlowDynamicOutputable` | 动态输出端口数量 |

### EdgeID

每条数据流边有唯一的 `ulong` 类型 EdgeID，用于在运行时上下文的变量字典中存取数据值。

---

## 节点系统

### 节点定义结构

一个完整的节点由以下部分构成：

```
Struct（数据定义）  →  Node 包装类（编辑器/资产）  →  Executor（运行时逻辑）
```

#### Struct — 节点数据定义

节点数据是一个 **可序列化的 C# struct**，通过实现节点接口和标注属性来声明行为：

```csharp
[FlowNodePath("分类/子分类")]   // 右键菜单路径
[Alias("显示名称")]             // 编辑器中显示的名称
[System.Serializable]
public struct MyNode : IMyNodeInterface
{
    [Inputable]                 // 标记为数据输入字段（可连接数据流）
    public int InputA;

    [Inputable]
    public int InputB;

    public OutputData<int> Result;  // 数据输出
}
```

#### 节点接口分类

项目通常为每个 Tag 分组定义一套接口，继承自 FlowGraph 核心接口：

```csharp
[FlowTag("Common")]                          // 分组标签
public interface ICommonNode : IFlowNode { }

public interface ICommonNormalNode   : ICommonNode, IFlowInputable, IFlowOutputable { }
public interface ICommonConditionable: ICommonNode, IFlowConditionable { }
public interface ICommonDataProvider : ICommonNode, IFlowDataProvider { }
public interface ICommonUpdateable   : ICommonNode, IFlowUpdateable { }
public interface ICommonDynamicOutput: ICommonNode, IFlowDynamicOutputable { }
```

#### 节点属性说明

| 属性 | 应用目标 | 说明 |
|------|---------|------|
| `[FlowNodePath("路径")]` | Struct | 右键菜单中的创建路径，支持多级，如 `"数学/整数"` |
| `[Alias("名称")]` | Struct | 编辑器显示名称 |
| `[FlowTag("标签")]` | 接口/Struct/Class | 将节点归入指定分组，用于编辑器过滤和代码生成 |
| `[Inputable]` | 字段 | 标记该字段可接收数据流连接 |
| `[DynamicOutput]` | `List<T>` 字段 | 标记该列表字段为动态输出端口，列表长度决定输出端口数 |

---

## 创建自定义节点

### 普通节点（Normal Node）

单帧执行，有一个输入端口和一个输出端口。

**Step 1：定义 Struct**

```csharp
// Assets/Runtime/Nodes/IntAdd.cs
[FlowNodePath("数学/整数加法")]
[Alias("整数加法")]
[System.Serializable]
public struct IntAdd : ICommonNormalNode
{
    [Inputable] public int A;
    [Inputable] public int B;

    public OutputData<int> Result;
}
```

**Step 2：创建 Node 包装类（Editor 目录）**

```csharp
// Assets/Editor/Nodes/IntAddNode.cs
public class IntAddNode : Flow.TFlowNode<IntAdd> { }
```

**Step 3：实现 Executor**

运行时逻辑（可通过代码生成工具自动生成模板）：

```csharp
// Assets/Runtime/Executors/IntAddExecutor.cs
public partial class IntAddExecutor
{
    protected override void OnExecute(MyContext context, IntAdd data)
    {
        // 数据输入已由基类自动填充
        int sum = data.A + data.B;
        context.SetOutputValue(data.Result, sum);
    }
}
```

**Step 4：注册 Executor**

```csharp
// 在游戏启动时调用（通常由代码生成工具自动生成注册代码）
CommonExecutorInit.Init();
```

---

### 条件节点（Condition Node）

输出两个控制流端口：索引 0 为 True，索引 1 为False。

```csharp
[FlowNodePath("逻辑/整数比较")]
[System.Serializable]
public struct IntCompare : ICommonConditionable
{
    [Inputable] public int A;
    [Inputable] public int B;
}

// Executor
public partial class IntCompareExecutor
{
    protected override bool OnCondition(MyContext context, IntCompare data)
    {
        return data.A > data.B;  // true → 走 True 端口
    }
}
```

---

### 动态输出节点（Dynamic Output Node）

输出端口数量由运行时数据决定。

```csharp
[FlowNodePath("逻辑/整数分支")]
[System.Serializable]
public struct IntBranch : ICommonDynamicOutputable
{
    [Inputable] public int Value;

    [DynamicOutput]
    public List<int> OutPort;   // 列表长度 = 输出端口数量
}

// Executor
public partial class IntBranchExecutor
{
    protected override int Select(MyContext context, IntBranch data)
    {
        // 返回要激活的输出端口索引
        return data.Value % data.OutPort.Count;
    }
}
```

---

### 跨帧节点（Updateable Node）

可跨越多个 `Update` 帧执行，适合等待、动画播放等异步操作。

```csharp
[FlowNodePath("计时/等待")]
[Alias("等待")]
[System.Serializable]
public struct Wait : ICommonUpdateable
{
    [Inputable] public float Duration;
}

// 自定义节点状态（可选）
public class WaitNodeContext : Flow.UpdateNodeContext
{
    public float ElapsedTime;
}

// Executor
public partial class WaitExecutor : Flow.TUpdateableExecutor<Wait, WaitRuntimeData, MyContext>
{
    protected override Flow.UpdateNodeContext CreateNodeContext()
    {
        return new WaitNodeContext();
    }

    protected override void OnEnter(MyContext context, Wait data, Flow.UpdateNodeContext nodeCtx)
    {
        ((WaitNodeContext)nodeCtx).ElapsedTime = 0f;
    }

    protected override bool OnUpdate(MyContext context, Wait data, Flow.UpdateNodeContext nodeCtx)
    {
        var ctx = (WaitNodeContext)nodeCtx;
        ctx.ElapsedTime += Time.deltaTime;
        return ctx.ElapsedTime < data.Duration;  // true = 继续等待
    }

    protected override void OnExit(MyContext context, Wait data, Flow.UpdateNodeContext nodeCtx) { }
}
```

---

### 纯数据节点（Data Provider Node）

无控制流端口，只提供数据值。每帧可选择是否重新计算。

```csharp
[FlowNodePath("数学/常量整数")]
[System.Serializable]
public struct ConstInt : ICommonDataProvider
{
    public int Value;

    public OutputData<int> Output;
    public bool IsRealTimeData => false;   // false = 只计算一次并缓存
}

// Executor
public partial class ConstIntExecutor
{
    protected override void OnExecute(MyContext context, ConstInt data)
    {
        context.SetOutputValue(data.Output, data.Value);
    }
}
```

---

## 编辑器使用

### 打开 FlowGraph 编辑器

在 Project 窗口中双击任意 `FlowGraph` 资产文件，编辑器窗口将自动打开。

编辑器支持同时打开最多 20 个图（超出时自动关闭最早打开的标签页）。

### 编辑器界面

```
┌─────────────────────────────────────────────┐
│  [图名称标签页] [图名称标签页] ...            │
├───────────────┬─────────────────────────────┤
│               │                             │
│  节点属性面板  │       图编辑区域             │
│  (选中节点时  │   (节点 + 连线)             │
│   显示字段)   │                             │
│               │                             │
└───────────────┴─────────────────────────────┘
```

### 基本操作

| 操作 | 方式 |
|------|------|
| 创建节点 | 在图编辑区域右键 → 选择节点类型 |
| 连接控制流 | 从节点输出端口拖拽到另一节点输入端口 |
| 连接数据流 | 从数据输出端口拖拽到标有 `[Inputable]` 的数据输入端口 |
| 选中节点 | 左键单击节点 |
| 移动节点 | 拖拽节点 |
| 删除节点/边 | 选中后按 `Delete` 键 |
| 框选 | 在空白区域拖拽 |
| 复制/粘贴 | `Ctrl+C` / `Ctrl+V` |
| 缩放 | 鼠标滚轮 |
| 平移画布 | 按住中键拖拽 或 按住 `Alt` + 拖拽 |

### 端口类型

| 端口颜色/形状 | 类型 | 说明 |
|-------------|------|------|
| 圆形（控制流） | Flow In / Flow Out | 决定执行顺序 |
| 菱形（数据流） | Data Out | 节点输出的数据值 |
| 三角形（数据流）| Data In（Inputable）| 可接收数据连接的输入字段 |

### 动态输出端口

对于 `[DynamicOutput]` 标注的 `List<T>` 字段，可在节点上通过 `+` / `-` 按钮动态添加/删除输出端口。

---

## 代码生成

FlowGraph 提供代码生成工具，可自动生成 RuntimeData、Executor 基类等样板代码。

### 配置代码生成

```csharp
// 通常放在 Editor/ 目录下
public class MyCodeGen
{
    [MenuItem("Tools/生成 FlowGraph 代码")]
    public static void Generate()
    {
        var ctx = new FlowCodeGenContext();

        ctx.AddSetting(new FlowCodeGenSetting
        {
            Tag = "Common",                           // 节点分组标签
            RuntimeContextType = typeof(MyContext),   // 运行时上下文类型
            Namespace = "MyGame.Nodes",               // 生成代码的命名空间
            NodeScriptRoot    = "Assets/Gen/Nodes",   // Node 包装类输出目录
            RuntimeDataDefineFile = "Assets/Gen/CommonRuntimeData.cs",   // RuntimeData 汇总文件
            ExecutorDefineFile    = "Assets/Gen/CommonExecutorDefine.cs", // Executor 基类汇总文件
            ExecutorScriptRoot    = "Assets/Executors/Common",            // Executor 实现类输出目录
        });

        ctx.Generate();
    }
}
```

### 生成内容说明

对每个带有指定 Tag 的节点类型，代码生成器会生成以下文件：

**1. RuntimeData 类（汇总在一个文件）**

```csharp
// 自动生成，请勿手动编辑
public class IntAddRuntimeData : Flow.TFlowNodeRuntimeData<IntAdd> { }
```

**2. Executor 基类（汇总在一个文件）**

```csharp
// 自动生成，请勿手动编辑
public partial class IntAddExecutor : Flow.TNormalExecutor<IntAdd, IntAddRuntimeData, MyContext>
{
    protected override bool HasInput => true;

    protected override void FillInputs(MyContext context, int nodeId, ref IntAdd data)
    {
        // 自动生成的输入填充代码
        context.TryGetInputValue(nodeId, <hashOf("A")>, ref data.A);
        context.TryGetInputValue(nodeId, <hashOf("B")>, ref data.B);
    }
}

// Executor 注册
public static class CommonExecutorInit
{
    public static void Init()
    {
        Flow.FlowNodeExecutor<IntAdd>.Executor = new IntAddExecutor();
        // ... 其他注册
    }
}
```

**3. Executor 实现模板（每个类型一个文件，可手动编辑）**

```csharp
// 此文件可手动修改
public partial class IntAddExecutor
{
    protected override void OnExecute(MyContext context, IntAdd data)
    {
        throw new System.NotImplementedException("IntAddExecutor.OnExecute 未实现");
    }
}
```

**4. Node 包装类（每个类型一个文件）**

```csharp
// 自动生成，请勿手动编辑
public partial class IntAddNode : Flow.TFlowNode<IntAdd>
{
    protected override Flow.FlowNodeRuntimeData CreateExport()
    {
        return new IntAddRuntimeData();
    }
}
```

> **注意**：已存在的 Executor 实现文件不会被覆盖，确保手写逻辑安全。

---

## 运行时系统

### 运行时上下文（FlowGraphRuntimeContext）

运行时上下文管理图的完整执行状态：

```csharp
// 创建上下文
var context = new MyContext();  // 继承自 FlowGraphRuntimeContext

// 启动执行
context.Start(runtimeData);

// 每帧驱动（在 MonoBehaviour.Update 中调用）
void Update()
{
    if (context.IsRunning)
    {
        context.Update();
    }
}

// 手动停止
context.Stop();
```

### 自定义上下文

通过继承 `FlowGraphRuntimeContext` 注入游戏数据：

```csharp
public class MyContext : FlowGraphRuntimeContext
{
    // 注入游戏对象引用
    public Transform TargetTransform;
    public Animator CharacterAnimator;
    public float GameTime => Time.time;

    // Executor 可通过 context.TargetTransform 等直接访问这些字段
}
```

### 数据读写 API

在 Executor 的 `OnExecute` 方法中读写数据：

```csharp
// 读取输入（通常由代码生成的 FillInputs 自动处理）
context.TryGetInputValue(nodeId, paramHash, ref data.InputField);

// 写出输出
context.SetOutputValue(data.OutputField, value);
```

### 导出图到运行时数据

在将图投入运行前，需要先将编辑器资产导出为运行时数据：

```csharp
var runtimeData = new FlowGraphRuntimeData();
FlowGraphExport.ExportToRuntimeData(myFlowGraph, runtimeData);
```

### 子图提供器

若图中使用了子图节点，需要注册 `FlowSubGraphProvider`：

```csharp
public class MySubGraphProvider : FlowSubGraphProvider
{
    private Dictionary<string, FlowGraphRuntimeData> _subGraphs;

    public override FlowGraphRuntimeData GetSubGraphData(string name)
    {
        return _subGraphs.TryGetValue(name, out var data) ? data : null;
    }
}

// 注册
FlowSubGraphProvider.Instance = new MySubGraphProvider();
```

---

## 子图（SubGraph）

子图允许将可复用的逻辑封装为独立图，并在主图中以节点形式引用。

### 创建子图

1. 创建一个继承自 `FlowSubGraph` 的资产类：

```csharp
[CreateAssetMenu(menuName = "FlowGraph/子图")]
public class MySubGraph : FlowSubGraph { }
```

2. 在 Project 窗口创建子图资产，在编辑器中定义子图的输入/输出端口及内部逻辑。

3. 子图的输入输出端口在 `FlowSubGraph.InputPorts` 和 `OutputPorts` 中定义，可通过编辑器添加。

### 在主图中使用子图

1. 在主图编辑器中右键 → 选择 `SubGraph` 节点
2. 在节点属性面板中指定引用的子图资产
3. 子图的输入/输出端口会自动出现在节点上，可连接控制流和数据流

### 子图执行机制

- 子图节点执行时，运行时系统创建 `SubGraphRuntimeContext`
- 子图**同步**执行（会阻塞父图，直至子图完成）
- 父图的数据通过端口映射传入子图；子图输出数据在结束时写回父图

---

## 调试系统

FlowGraph 支持运行时执行追踪，需开启编译宏 `FLOW_GRAPH_DEBUG`。

### 开启调试

在 Unity 项目的 `Player Settings → Scripting Define Symbols` 中添加：

```
FLOW_GRAPH_DEBUG
```

### 实现调试提供器

```csharp
public class MyFlowDebuger : IFlowDebuger
{
    public void OnNodeStart(int nodeId, int frameIndex)
    {
        Debug.Log($"[Frame {frameIndex}] 节点 {nodeId} 开始执行");
    }

    public void OnNodeOutput(int nodeId, int outputIndex, int frameIndex)
    {
        Debug.Log($"[Frame {frameIndex}] 节点 {nodeId} 输出端口 {outputIndex}");
    }

    public void OnDataNode(int nodeId, int frameIndex)
    {
        Debug.Log($"[Frame {frameIndex}] 数据节点 {nodeId} 执行");
    }

    public void OnNodeParamChange(int nodeId, ulong edgeId, object value, int frameIndex)
    {
        Debug.Log($"[Frame {frameIndex}] 节点 {nodeId} 参数 {edgeId} = {value}");
    }
}

// 注册提供器
FlowDebugContext.Provider = new MyFlowDebugProvider();
```

### 节点 UID 映射

`FlowGraphRuntimeData.NodeUIDs` 保存了运行时节点索引到编辑器 UID 的映射，可用于将运行时调试事件关联到编辑器中对应的节点：

```csharp
// 查找运行时节点对应的编辑器 UID
if (runtimeData.NodeUIDs.TryGetValue(nodeId, out long uid))
{
    // uid 对应编辑器中 FlowNode.UID 字段
}
```

---

## 完整示例

以下示例展示一个完整的 FlowGraph 集成流程。

### 1. 定义节点接口分组

```csharp
// Assets/Runtime/IGameFlowNode.cs
[FlowTag("Game")]
public interface IGameNode : IFlowNode { }

public interface IGameNormalNode    : IGameNode, IFlowInputable, IFlowOutputable { }
public interface IGameConditionable : IGameNode, IFlowConditionable { }
public interface IGameDataProvider  : IGameNode, IFlowDataProvider { }
public interface IGameUpdateable    : IGameNode, IFlowUpdateable { }
```

### 2. 定义节点 Struct

```csharp
// Assets/Runtime/Nodes/PrintLog.cs
[FlowNodePath("调试/打印日志")]
[Alias("打印日志")]
[System.Serializable]
public struct PrintLog : IGameNormalNode
{
    [Inputable] public string Message;
}

// Assets/Runtime/Nodes/CheckHealth.cs
[FlowNodePath("角色/检查生命值")]
[Alias("检查生命值")]
[System.Serializable]
public struct CheckHealth : IGameConditionable
{
    [Inputable] public float Threshold;
}
```

### 3. 自定义运行时上下文

```csharp
// Assets/Runtime/GameFlowContext.cs
public class GameFlowContext : FlowGraphRuntimeContext
{
    public PlayerController Player;
    public EnemyAI Enemy;
}
```

### 4. 实现 Executor

```csharp
// Assets/Runtime/Executors/PrintLogExecutor.cs
public partial class PrintLogExecutor
{
    protected override void OnExecute(GameFlowContext context, PrintLog data)
    {
        Debug.Log(data.Message);
    }
}

// Assets/Runtime/Executors/CheckHealthExecutor.cs
public partial class CheckHealthExecutor
{
    protected override bool OnCondition(GameFlowContext context, CheckHealth data)
    {
        return context.Player.CurrentHealth > data.Threshold;
    }
}
```

### 5. 定义图资产

```csharp
// Assets/Editor/TestFlowGraph.cs（放在 Editor 目录）
[CreateAssetMenu(menuName = "Game/GameFlowGraph")]
public class GameFlowGraph : FlowMainGraph { }

// 注册编辑器上下文
public class GameFlowGraphEditorContext : TFlowGraphEditorContext<GameFlowGraph>
{
    protected override string AssetRootPath => "Assets/FlowGraphs";
}
```

### 6. 运行时驱动

```csharp
// Assets/Runtime/GameFlowRunner.cs
public class GameFlowRunner : MonoBehaviour
{
    [SerializeField] private GameFlowGraph _graph;

    private GameFlowRuntimeData _runtimeData;
    private GameFlowContext _context;

    void Start()
    {
        // 注册 Executors
        GameExecutorInit.Init();

        // 导出运行时数据
        _runtimeData = new GameFlowRuntimeData();
        FlowGraphExport.ExportToRuntimeData(_graph, _runtimeData);

        // 创建上下文并绑定游戏对象
        _context = new GameFlowContext
        {
            Player = FindObjectOfType<PlayerController>(),
            Enemy  = FindObjectOfType<EnemyAI>()
        };

        // 启动执行
        _context.Start(_runtimeData);
    }

    void Update()
    {
        if (_context.IsRunning)
            _context.Update();
    }

    void OnDestroy()
    {
        _context.Stop();
    }
}
```

---

## API 参考

### 核心属性

| 属性 | 命名空间 | 说明 |
|------|---------|------|
| `[FlowNodePath(path)]` | Runtime | 节点在编辑器菜单中的路径 |
| `[FlowTag(tag)]` | Runtime | 节点分组标签 |
| `[Inputable]` | Runtime | 标记字段为数据输入端口 |
| `[DynamicOutput]` | Runtime | 标记 `List<T>` 字段为动态输出端口 |

### 核心接口

| 接口 | 说明 |
|------|------|
| `IFlowNode` | 所有节点的基接口 |
| `IFlowEntry` | 图入口节点 |
| `IFlowInputable` | 有控制流输入端口 |
| `IFlowOutputable` | 有控制流输出端口 |
| `IFlowDataProvider` | 纯数据节点 |
| `IFlowUpdateable` | 跨帧执行节点 |
| `IFlowConditionable` | 条件分支节点（True/False） |
| `IFlowDynamicOutputable` | 动态输出端口节点 |

### 核心类

| 类 | 说明 |
|---|------|
| `TFlowNode<T>` | 节点包装类基类 |
| `TFlowNodeRuntimeData<T>` | 运行时节点数据基类 |
| `FlowGraphRuntimeContext` | 运行时执行上下文基类 |
| `FlowGraphRuntimeData` | 运行时图数据 |
| `FlowGraphExport` | 图导出工具（静态类） |
| `OutputData<T>` | 节点数据输出字段类型 |
| `FlowNodeResult` | 节点执行结果（`Running`、`Next`、`True`、`False`） |
| `UpdateNodeContext` | 跨帧节点状态基类 |
| `FlowSubGraphProvider` | 子图数据提供器基类 |

### Executor 基类

| 基类 | 对应节点类型 | 需实现方法 |
|------|------------|-----------|
| `TNormalExecutor<T, D, C>` | 普通节点 | `OnExecute()` |
| `TConditionExecutor<T, D, C>` | 条件节点 | `OnCondition()` |
| `TDynamicOutputExecutor<T, D, C>` | 动态输出节点 | `Select()` |
| `TUpdateableExecutor<T, D, C>` | 跨帧节点 | `OnEnter()`, `OnUpdate()`, `OnExit()` |
| `TConditionUpdateableExecutor<T, D, C>` | 条件+跨帧节点 | `OnUpdate()` 返回 `ResultType` |
| `TDynamicOutputUpdateableExecutor<T, D, C>` | 动态+跨帧节点 | `OnUpdate()` 返回端口索引（-1=继续） |

> 类型参数：`T` = 节点 struct，`D` = RuntimeData 类，`C` = RuntimeContext 类

### FlowNodeResult 常量

| 常量 | 说明 |
|------|------|
| `FlowNodeResult.Running` | 节点仍在执行（跨帧） |
| `FlowNodeResult.Next` | 正常完成，激活输出端口 0 |
| `FlowNodeResult.True` | 条件为真，激活输出端口 0 |
| `FlowNodeResult.False` | 条件为假，激活输出端口 1 |
