# VisualShape 使用手册

VisualShape 是一个 Unity 调试绘制库，用于在 **编辑器** 和 **运行时** 轻松绘制线条、形状、文本等调试可视化内容。支持 Unity 内置渲染管线与 URP。

---

## 目录

- [快速开始](#快速开始)
- [核心概念](#核心概念)
- [绘制方式](#绘制方式)
  - [静态 Draw 类](#1-静态-draw-类)
  - [继承 MonoBehaviourGizmos](#2-继承-monobehaviourgizmos)
  - [CommandBuilder（手动管理）](#3-commandbuilder手动管理)
- [基础图形](#基础图形)
  - [线条](#线条)
  - [射线](#射线)
  - [盒体](#盒体)
  - [球体](#球体)
  - [圆与弧](#圆与弧)
  - [圆柱与胶囊体](#圆柱与胶囊体)
  - [三角形与多边形](#三角形与多边形)
  - [平面](#平面)
  - [网格](#网格)
  - [箭头](#箭头)
  - [曲线](#曲线)
  - [辅助标记](#辅助标记)
  - [网格线](#网格线)
  - [折线与虚线](#折线与虚线)
- [2D 绘制](#2d-绘制)
- [文本标签](#文本标签)
- [作用域控制](#作用域控制)
  - [颜色 (WithColor)](#颜色-withcolor)
  - [矩阵变换 (WithMatrix)](#矩阵变换-withmatrix)
  - [局部空间 (InLocalSpace)](#局部空间-inlocalspace)
  - [屏幕空间 (InScreenSpace)](#屏幕空间-inscreenspace)
  - [持续时间 (WithDuration)](#持续时间-withduration)
  - [线宽 (WithLineWidth)](#线宽-withlinewidth)
- [高级功能](#高级功能)
  - [In-Game 绘制](#in-game-绘制)
  - [RedrawScope（跨帧缓存）](#redrawscope跨帧缓存)
  - [Hasher（哈希缓存）](#hasher哈希缓存)
  - [相机目标控制](#相机目标控制)
  - [Job System 集成](#job-system-集成)
- [选择上下文 (GizmoContext)](#选择上下文-gizmocontext)
- [调色板 (Palette)](#调色板-palette)
- [工具方法 (ShapeUtilities)](#工具方法-shapeutilities)
- [项目设置 (ShapeSettings)](#项目设置-shapesettings)
- [渲染管线支持](#渲染管线支持)
- [API 速查表](#api-速查表)

---

## 快速开始

### 最简示例

```csharp
using UnityEngine;
using VisualShape;

public class MyGizmo : MonoBehaviourGizmos
{
    public override void DrawGizmos()
    {
        // 在原点画一个红色线框球
        Draw.WireSphere(Vector3.zero, 1f, Color.red);

        // 画一条从当前对象到原点的线
        Draw.Line(transform.position, Vector3.zero, Color.green);
    }
}
```

将此脚本挂载到任意 GameObject 即可在 Scene 视图中看到调试绘制。

### 无需 MonoBehaviour 的用法

```csharp
void Update()
{
    // 直接使用静态 Draw 类
    Draw.WireBox(Vector3.zero, Vector3.one, Color.yellow);
}
```

---

## 核心概念

| 概念 | 说明 |
|------|------|
| **Draw** | 静态入口类，提供所有绘制方法的快捷访问 |
| **CommandBuilder** | 底层命令构建器，支持作用域控制和 Job System |
| **MonoBehaviourGizmos** | 基类，继承后重写 `DrawGizmos()` 即可自动注册 |
| **ShapeManager** | 全局单例管理器，自动创建，通常无需直接交互 |
| **RedrawScope** | 跨帧缓存机制，避免每帧重新提交不变的绘制内容 |

---

## 绘制方式

### 1. 静态 Draw 类

最简单的方式，适合快速调试。仅在编辑器中绘制（Gizmos 启用时）。

```csharp
void Update()
{
    Draw.Line(Vector3.zero, Vector3.one, Color.red);
    Draw.WireSphere(transform.position, 0.5f, Color.blue);
    Draw.Arrow(Vector3.zero, Vector3.up * 2f, Color.green);
}
```

### 2. 继承 MonoBehaviourGizmos

推荐用于需要持续显示 Gizmos 的组件。自动处理注册和生命周期。

```csharp
using VisualShape;

public class PathVisualizer : MonoBehaviourGizmos
{
    public Transform[] waypoints;

    public override void DrawGizmos()
    {
        if (waypoints == null) return;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Draw.Arrow(waypoints[i].position, waypoints[i + 1].position, Color.cyan);
        }

        // 根据选中状态改变显示
        if (GizmoContext.InSelection(this))
        {
            foreach (var wp in waypoints)
                Draw.WireSphere(wp.position, 0.3f, Color.yellow);
        }
    }
}
```

### 3. CommandBuilder（手动管理）

需要精细控制时使用，支持自定义相机目标、RedrawScope 等。

```csharp
// 使用 using 块自动提交
using (var draw = ShapeManager.GetBuilder())
{
    draw.WireBox(Vector3.zero, Vector3.one);
    draw.WireSphere(Vector3.up, 0.5f, Color.red);
}
```

---

## 基础图形

以下所有方法既可通过 `Draw.XXX()` 静态调用，也可通过 `CommandBuilder` 实例调用。每个方法都有带 `Color` 参数和不带 `Color` 参数的重载。

### 线条

```csharp
// 基础线条
Draw.Line(float3 a, float3 b);
Draw.Line(float3 a, float3 b, Color color);

// Vector3 重载
Draw.Line(Vector3 a, Vector3 b);
Draw.Line(Vector3 a, Vector3 b, Color color);
```

### 射线

```csharp
// 从起点沿方向绘制
Draw.Ray(float3 origin, float3 direction);
Draw.Ray(float3 origin, float3 direction, Color color);

// 从 Unity Ray 结构绘制
Draw.Ray(Ray ray, float length);
Draw.Ray(Ray ray, float length, Color color);
```

### 盒体

```csharp
// 线框盒
Draw.WireBox(float3 center, float3 size);
Draw.WireBox(float3 center, quaternion rotation, float3 size);
Draw.WireBox(Bounds bounds);

// 实心盒
Draw.SolidBox(float3 center, float3 size);
Draw.SolidBox(float3 center, quaternion rotation, float3 size);
Draw.SolidBox(Bounds bounds);
```

### 球体

```csharp
// 线框球（三个正交圆环）
Draw.WireSphere(float3 position, float radius);
Draw.WireSphere(float3 position, float radius, Color color);

// 球体轮廓（面向相机的单个圆）
Draw.SphereOutline(float3 center, float radius);
Draw.SphereOutline(float3 center, float radius, Color color);
```

### 圆与弧

```csharp
// 圆（需要指定法线方向）
Draw.Circle(float3 center, float3 normal, float radius);

// 实心圆/圆盘
Draw.SolidCircle(float3 center, float3 normal, float radius);

// 弧线（从 start 到 end，绕 center）
Draw.Arc(float3 center, float3 start, float3 end);

// 实心弧
Draw.SolidArc(float3 center, float3 start, float3 end);
```

### 圆柱与胶囊体

```csharp
// 圆柱
Draw.WireCylinder(float3 bottom, float3 top, float radius);
Draw.WireCylinder(float3 position, float3 up, float height, float radius);

// 胶囊体
Draw.WireCapsule(float3 start, float3 end, float radius);
Draw.WireCapsule(float3 position, float3 direction, float length, float radius);
```

### 三角形与多边形

```csharp
// 三角形
Draw.WireTriangle(float3 a, float3 b, float3 c);
Draw.SolidTriangle(float3 a, float3 b, float3 c);

// 正三角形（指定中心、旋转和外接圆半径）
Draw.WireTriangle(float3 center, quaternion rotation, float radius);

// 正五边形 / 正六边形
Draw.WirePentagon(float3 center, quaternion rotation, float radius);
Draw.WireHexagon(float3 center, quaternion rotation, float radius);

// 正 N 边形
Draw.WirePolygon(float3 center, int vertices, quaternion rotation, float radius);
```

### 平面

```csharp
// 线框平面
Draw.WirePlane(float3 center, float3 normal, float2 size);
Draw.WirePlane(float3 center, quaternion rotation, float2 size);

// 实心平面
Draw.SolidPlane(float3 center, float3 normal, float2 size);

// 带法线箭头的平面
Draw.PlaneWithNormal(float3 center, float3 normal, float2 size);
```

### 网格

```csharp
// 从 Unity Mesh 绘制
Draw.WireMesh(Mesh mesh);
Draw.SolidMesh(Mesh mesh);

// 从原始数据绘制
Draw.WireMesh(NativeArray<float3> vertices, NativeArray<int> triangles);
```

### 箭头

```csharp
// 基础箭头
Draw.Arrow(float3 from, float3 to);
Draw.Arrow(float3 from, float3 to, Color color);

// 自定义箭头（指定上方向和箭头大小）
Draw.Arrow(float3 from, float3 to, float3 up, float headSize);

// 相对大小箭头头部（headFraction 为头部占总长的比例）
Draw.ArrowRelativeSizeHead(float3 from, float3 to, float3 up, float headFraction);

// 仅箭头头部
Draw.Arrowhead(float3 center, float3 direction, float radius);
Draw.Arrowhead(float3 center, float3 direction, float3 up, float radius);

// 弧形箭头
Draw.ArrowheadArc(float3 origin, float3 direction, float offset);
Draw.ArrowheadArc(float3 origin, float3 direction, float offset, float width);
```

### 曲线

```csharp
// 三次贝塞尔曲线
Draw.Bezier(float3 p0, float3 p1, float3 p2, float3 p3);

// Catmull-Rom 样条曲线（单段）
Draw.CatmullRom(float3 p0, float3 p1, float3 p2, float3 p3);

// Catmull-Rom 样条曲线（多点）
Draw.CatmullRom(List<Vector3> points);
```

### 辅助标记

```csharp
// 3D 十字标记
Draw.Cross(float3 position);
Draw.Cross(float3 position, float size);
Draw.Cross(float3 position, float size, Color color);
```

### 网格线

```csharp
// 3D 网格
Draw.WireGrid(float3 center, quaternion rotation, int2 cells, float2 totalSize);
```

### 折线与虚线

```csharp
// 折线（多段线）
Draw.Polyline(List<Vector3> points, bool cycle);
Draw.Polyline(Vector3[] points, bool cycle);
Draw.Polyline(NativeArray<float3> points, bool cycle);

// 虚线
Draw.DashedLine(float3 a, float3 b, float dash, float gap);

// 虚线折线
Draw.DashedPolyline(List<Vector3> points, float dash, float gap);
```

---

## 2D 绘制

通过 `Draw.xy` 或 `Draw.xz` 访问 2D 绘制 API。`xy` 在 XY 平面绘制，`xz` 在 XZ 平面绘制。

```csharp
// XZ 平面（俯视图常用）
Draw.xz.Circle(new float2(0, 0), radius: 5f);
Draw.xz.Line(new float2(0, 0), new float2(1, 1));
Draw.xz.WireRectangle(float3.zero, new float2(2, 3));

// XY 平面
Draw.xy.Circle(new float2(0, 0), radius: 5f);
Draw.xy.SolidCircle(new float2(0, 0), radius: 2f);
```

### 2D 专有图形

```csharp
// 2D 圆/弧（支持起止角度，弧度制）
Draw.xz.Circle(float2 center, float radius, float startAngle = 0, float endAngle = 2π);
Draw.xz.SolidCircle(float2 center, float radius, float startAngle, float endAngle);

// 2D 胶囊/药丸形
Draw.xz.WirePill(float2 a, float2 b, float radius);

// 2D 矩形
Draw.xz.WireRectangle(float3 center, float2 size);
Draw.xz.WireRectangle(Rect rect);
Draw.xz.SolidRectangle(Rect rect);

// 2D 十字
Draw.xz.Cross(float2 position, float size);

// 2D 折线
Draw.xz.Polyline(List<Vector2> points, bool cycle);

// 2D 网格
Draw.xz.WireGrid(float2 center, int2 cells, float2 totalSize);
```

### 2D 作用域

2D 绘制同样支持所有作用域控制：

```csharp
var draw2d = Draw.xz;
using (draw2d.WithColor(Color.red))
{
    draw2d.Circle(float2.zero, 1f);
    draw2d.WireRectangle(float3.zero, new float2(2, 2));
}
```

---

## 文本标签

### 2D 标签（屏幕空间大小）

文本始终面向相机，大小以像素为单位。

```csharp
// 基础用法
Draw.Label2D(float3 position, "Hello World", 14f);

// 带颜色
Draw.Label2D(float3 position, "Score: 100", 16f, Color.white);

// 带对齐方式
Draw.Label2D(float3 position, "居中文本", 14f, LabelAlignment.Center);
Draw.Label2D(float3 position, "左上对齐", 14f, LabelAlignment.TopLeft, Color.yellow);
```

### 3D 标签（世界空间大小）

文本在世界空间中，会随距离缩放。

```csharp
// 基础用法
Draw.Label3D(float3 position, quaternion.identity, "3D文本", 1f);

// 带颜色和对齐
Draw.Label3D(float3 position, quaternion.identity, "标记", 0.5f, LabelAlignment.Center, Color.green);
```

### 对齐方式 (LabelAlignment)

| 预设 | 说明 |
|------|------|
| `LabelAlignment.TopLeft` | 左上角 |
| `LabelAlignment.TopCenter` | 顶部居中 |
| `LabelAlignment.TopRight` | 右上角 |
| `LabelAlignment.MiddleLeft` | 中部左对齐 |
| `LabelAlignment.Center` | 完全居中 |
| `LabelAlignment.MiddleRight` | 中部右对齐 |
| `LabelAlignment.BottomLeft` | 左下角 |
| `LabelAlignment.BottomCenter` | 底部居中 |
| `LabelAlignment.BottomRight` | 右下角 |

```csharp
// 带像素偏移的对齐
var alignment = LabelAlignment.Center.withPixelOffset(10f, 5f);
Draw.Label2D(position, "偏移文本", 14f, alignment);
```

### Burst 兼容的固定字符串

在 Burst Job 中使用 `FixedString` 类型：

```csharp
Draw.Label2D(position, new FixedString32Bytes("Value"), 14f);
Draw.Label3D(position, rotation, new FixedString64Bytes("Label"), 1f);
```

---

## 作用域控制

作用域使用 `using` 块或 `Push/Pop` 方法对来管理状态。**推荐使用 `using` 块**，因为它能自动处理配对。

### 颜色 (WithColor)

```csharp
// using 块方式（推荐）
using (Draw.WithColor(Color.red))
{
    Draw.Line(a, b);           // 红色
    Draw.WireSphere(c, 1f);   // 红色

    using (Draw.WithColor(Color.blue))
    {
        Draw.Line(d, e);       // 蓝色（嵌套覆盖）
    }

    Draw.WireBox(f, g);       // 红色（恢复）
}

// Push/Pop 方式
Draw.PushColor(Color.green);
Draw.Line(a, b);   // 绿色
Draw.PopColor();
```

### 矩阵变换 (WithMatrix)

```csharp
// 应用自定义变换矩阵
using (Draw.WithMatrix(Matrix4x4.TRS(position, rotation, scale)))
{
    // 在变换空间内绘制
    Draw.WireBox(Vector3.zero, Vector3.one);  // 变换后的盒体
}
```

### 局部空间 (InLocalSpace)

```csharp
// 在 Transform 的局部空间中绘制
using (Draw.InLocalSpace(transform))
{
    // 坐标相对于此 Transform
    Draw.WireBox(Vector3.zero, Vector3.one);      // 在对象中心绘制
    Draw.WireSphere(Vector3.up, 0.5f);            // 在对象上方绘制
}
```

### 屏幕空间 (InScreenSpace)

```csharp
// 在屏幕空间绘制（坐标为像素）
using (Draw.InScreenSpace(camera))
{
    Draw.Line(new Vector3(0, 0, 0), new Vector3(100, 100, 0));
    Draw.Label2D(new Vector3(50, 50, 0), "屏幕文本", 20f);
}
```

### 持续时间 (WithDuration)

使绘制内容在指定秒数内持续显示，无需每帧重新绘制。

```csharp
// 在按下按钮时绘制，持续 2 秒
if (Input.GetKeyDown(KeyCode.Space))
{
    using (Draw.WithDuration(2f))
    {
        Draw.WireSphere(transform.position, 1f, Color.red);
        Draw.Label2D(transform.position, "Hit!", 20f, Color.red);
    }
}
```

### 线宽 (WithLineWidth)

```csharp
// 设置线宽（像素）
using (Draw.WithLineWidth(3f))
{
    Draw.Line(a, b);            // 3 像素宽
    Draw.WireBox(c, d);         // 3 像素宽
}

// 禁用自动线段连接
using (Draw.WithLineWidth(2f, automaticJoins: false))
{
    Draw.Polyline(points, cycle: true);
}
```

### 作用域嵌套

所有作用域可自由嵌套：

```csharp
using (Draw.WithColor(Color.red))
using (Draw.WithMatrix(Matrix4x4.Translate(Vector3.up * 5)))
using (Draw.WithDuration(1f))
using (Draw.WithLineWidth(2f))
{
    Draw.WireBox(Vector3.zero, Vector3.one);
}
```

---

## 高级功能

### In-Game 绘制

默认情况下，`Draw` 类的绘制仅在编辑器启用 Gizmos 时可见。使用 `Draw.ingame` 可在运行时（包括构建后的游戏）绘制。

```csharp
void Update()
{
    // 仅在编辑器中显示（需启用 Gizmos）
    Draw.WireSphere(Vector3.zero, 1f, Color.red);

    // 在编辑器和构建版本中都显示
    Draw.ingame.WireSphere(Vector3.up * 3, 1f, Color.green);
}
```

也可通过 `ShapeManager.GetBuilder` 实现：

```csharp
using (var draw = ShapeManager.GetBuilder(renderInGame: true))
{
    draw.WireBox(Vector3.zero, Vector3.one);
}
```

### RedrawScope（跨帧缓存）

当绘制内容在多帧之间不变时，使用 RedrawScope 避免每帧重新提交命令。

```csharp
private RedrawScope redrawScope;

void Start()
{
    // 创建重绘作用域
    redrawScope = ShapeManager.GetRedrawScope();

    // 提交绘制命令（只需一次）
    using (var draw = ShapeManager.GetBuilder(redrawScope))
    {
        draw.WireSphere(Vector3.zero, 1f, Color.red);
        draw.WireBox(Vector3.up, Vector3.one, Color.blue);
    }
}

void OnDestroy()
{
    // 释放时自动清理
    redrawScope.Dispose();
}
```

需要更新内容时使用 `Rewind()`：

```csharp
void RefreshVisualization()
{
    redrawScope.Rewind();
    using (var draw = ShapeManager.GetBuilder(redrawScope))
    {
        // 提交新的绘制命令
        draw.WireSphere(newPosition, 1f, Color.green);
    }
}
```

### Hasher（哈希缓存）

当输入数据不变时，自动跳过重复的绘制提交，进一步优化性能。

```csharp
void DrawPath(Vector3[] path)
{
    var hasher = ShapeData.Hasher.Create(this);
    for (int i = 0; i < path.Length; i++)
        hasher.Add(path[i]);

    // 如果哈希与上次相同，则自动跳过内部绘制
    using (var draw = ShapeManager.GetBuilder(hasher, redrawScope))
    {
        for (int i = 0; i < path.Length - 1; i++)
            draw.Line(path[i], path[i + 1], Color.white);
    }
}
```

### 相机目标控制

控制绘制内容渲染到哪些相机。

```csharp
using (var draw = ShapeManager.GetBuilder())
{
    // 仅渲染到指定相机
    draw.cameraTargets = new Camera[] { myCamera };
    draw.WireBox(Vector3.zero, Vector3.one);
}

// 全局设置：允许渲染到 RenderTexture
ShapeManager.allowRenderToRenderTextures = true;

// 全局设置：渲染到所有相机
ShapeManager.drawToAllCameras = true;

// 全局设置：线宽倍率（适用于高分辨率截图）
ShapeManager.lineWidthMultiplier = 2f;
```

### Job System 集成

CommandBuilder 支持在 Unity Job System 中使用。

```csharp
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

struct DrawJob : IJob
{
    public CommandBuilder draw;
    [ReadOnly] public NativeArray<float3> positions;

    public void Execute()
    {
        for (int i = 0; i < positions.Length; i++)
        {
            draw.WireSphere(positions[i], 0.5f);
        }
    }
}

void ScheduleDrawing()
{
    var draw = ShapeManager.GetBuilder();
    var job = new DrawJob
    {
        draw = draw,
        positions = myPositions
    };
    var handle = job.Schedule();

    // Job 完成后自动提交并释放
    draw.DisposeAfter(handle, AllowedDelay.EndOfFrame);
}
```

`AllowedDelay` 选项：

| 值 | 说明 |
|----|------|
| `AllowedDelay.EndOfFrame` | 在帧结束前等待 Job 完成 |
| `AllowedDelay.Infinite` | 无限期等待 Job 完成 |

---

## 选择上下文 (GizmoContext)

在 `DrawGizmos()` 方法中查询编辑器选择状态，实现选中时高亮等效果。

```csharp
public override void DrawGizmos()
{
    // 当前选中物体数量
    int count = GizmoContext.selectionSize;

    // 检查是否被选中（支持深度选择：父级被选中时子级也返回 true）
    if (GizmoContext.InSelection(this))
    {
        Draw.WireBox(transform.position, Vector3.one * 2, Color.yellow);
    }

    // 检查是否为当前 Inspector 显示的对象
    if (GizmoContext.InActiveSelection(this))
    {
        Draw.WireSphere(transform.position, 1.5f, Color.white);
    }
}
```

> **注意**：`GizmoContext` 仅可在 `DrawGizmos()` 回调内使用。

---

## 调色板 (Palette)

内置预定义颜色集合，方便快速使用。

### 纯色 (Palette.Pure)

```csharp
using Palette = VisualShape.Palette.Pure;

Draw.Line(a, b, Palette.Red);
Draw.Line(c, d, Palette.Cyan);
```

可用颜色：`Red`, `Green`, `Blue`, `Yellow`, `Cyan`, `Magenta`, `White`, `Black`, `Grey`, `Clear`

### Colorbrewer 配色 (Palette.Colorbrewer)

基于 [ColorBrewer](http://colorbrewer2.org/) 的科学配色方案。

```csharp
// Set1 定性配色
using Palette = VisualShape.Palette.Colorbrewer.Set1;

Draw.Line(a, b, Palette.Red);
Draw.Line(c, d, Palette.Orange);
Draw.Line(e, f, Palette.Purple);
```

Set1 可用颜色：`Red`, `Blue`, `Green`, `Purple`, `Orange`, `Yellow`, `Brown`, `Pink`, `Grey`

```csharp
// Blues 顺序配色（支持 1-9 个分类）
Color color = Palette.Colorbrewer.Blues.GetColor(classes: 5, index: 2);
```

---

## 工具方法 (ShapeUtilities)

```csharp
// 计算 GameObject 的包围盒（包含所有子对象的碰撞体和渲染器）
Bounds bounds = ShapeUtilities.BoundsFrom(gameObject);
Bounds bounds = ShapeUtilities.BoundsFrom(transform);

// 从点集计算包围盒
Bounds bounds = ShapeUtilities.BoundsFrom(pointsList);        // List<Vector3>
Bounds bounds = ShapeUtilities.BoundsFrom(pointsArray);       // Vector3[]
Bounds bounds = ShapeUtilities.BoundsFrom(nativePoints);      // NativeArray<float3>
```

---

## 项目设置 (ShapeSettings)

设置资产位于 `Assets/Settings/Resources/VisualShape.asset`，首次使用时自动创建。

| 设置项 | 默认值 | 说明 |
|--------|--------|------|
| `lineOpacity` | 1.0 | 线条在物体前方时的不透明度 |
| `solidOpacity` | 0.55 | 实体在物体前方时的不透明度 |
| `textOpacity` | 1.0 | 文本在物体前方时的不透明度 |
| `lineOpacityBehindObjects` | 0.12 | 线条在物体后方时的不透明度乘数 |
| `solidOpacityBehindObjects` | 0.45 | 实体在物体后方时的不透明度乘数 |
| `textOpacityBehindObjects` | 0.9 | 文本在物体后方时的不透明度乘数 |
| `curveResolution` | 1.0 | 曲线分辨率倍数（基于相机距离自动调整） |

可在 Inspector 中编辑设置资产，或通过代码访问：

```csharp
var settings = ShapeSettings.GetSettingsAsset();
settings.settings.lineOpacity = 0.8f;
```

---

## 渲染管线支持

| 管线 | 支持情况 |
|------|----------|
| Unity 内置渲染管线 | 通过 `Camera.onPostRender` 自动集成 |
| URP | 通过 `VisualShapeURPRenderPassFeature` 自动注入渲染通道 |
| 自定义 SRP | 通过 `endCameraRendering` 回调尽力支持 |

**URP 注意事项**：VisualShape 会自动检测 URP 并注入渲染通道，无需手动配置 Renderer Feature。渲染时机为 `BeforeRenderingPostProcessing - 1`，确保在后处理之前完成绘制。

---

## API 速查表

### 图形绘制

| 方法 | 说明 |
|------|------|
| `Line` | 直线 |
| `Ray` | 射线 |
| `Arrow` / `Arrowhead` / `ArrowheadArc` | 箭头 |
| `WireBox` / `SolidBox` | 盒体 |
| `WireSphere` / `SphereOutline` | 球体 |
| `Circle` / `SolidCircle` | 圆 |
| `Arc` / `SolidArc` | 弧 |
| `WireCylinder` | 圆柱 |
| `WireCapsule` | 胶囊体 |
| `WireTriangle` / `SolidTriangle` | 三角形 |
| `WirePentagon` / `WireHexagon` / `WirePolygon` | 正多边形 |
| `WirePlane` / `SolidPlane` / `PlaneWithNormal` | 平面 |
| `WireMesh` / `SolidMesh` | 网格 |
| `Bezier` / `CatmullRom` | 曲线 |
| `Cross` | 十字标记 |
| `WireGrid` | 网格线 |
| `Polyline` / `DashedLine` / `DashedPolyline` | 折线/虚线 |
| `Label2D` / `Label3D` | 文本标签 |

### 作用域控制

| 方法 | 说明 |
|------|------|
| `WithColor` / `PushColor` + `PopColor` | 颜色 |
| `WithMatrix` / `PushMatrix` + `PopMatrix` | 矩阵变换 |
| `InLocalSpace` | 局部空间 |
| `InScreenSpace` | 屏幕空间 |
| `WithDuration` / `PushDuration` + `PopDuration` | 持续时间 |
| `WithLineWidth` / `PushLineWidth` + `PopLineWidth` | 线宽 |

### 入口点

| 入口 | 说明 |
|------|------|
| `Draw.XXX()` | 编辑器 Gizmos 绘制 |
| `Draw.ingame.XXX()` | 运行时可见的绘制 |
| `Draw.xy.XXX()` | XY 平面 2D 绘制 |
| `Draw.xz.XXX()` | XZ 平面 2D 绘制 |
| `Draw.editor` | 访问编辑器 CommandBuilder 引用 |
| `ShapeManager.GetBuilder()` | 获取独立 CommandBuilder |
