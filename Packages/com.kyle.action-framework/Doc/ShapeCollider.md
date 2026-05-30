# ShapeCollider 使用手册

ShapeCollider 是一组轻量级几何碰撞工具，用于在不依赖 Unity Physics 的场景中做简单体积检测、射线检测和 Scene Gizmos 调试绘制。模块位于 `Packages/com.kyle.action-framework/ShapeCollider/Runtime/`，运行时与编辑器均可使用。

---

## 核心类型

| 类型 | 说明 |
| --- | --- |
| `ShapeSphere` | 球体，包含 `Position` 与 `Radius` |
| `ShapeCapsule` | 任意方向胶囊体，`Position` 为起点球心，`Direction` 为轴向，`Length` 为两端球心距离 |
| `ShapeSegment` | 有限线段，包含 `Start` 与 `End` |
| `ShapeAABB` | 轴对齐包围盒，`Center` 为中心，`Extents` 为半尺寸 |
| `ShapeCylinder` | 竖直圆柱，`Position` 表示底面中心，`Height` 沿世界 Y 轴向上 |
| `ShapeBox` | 绕世界 Y 轴旋转的盒体，`Position` 为中心，`Extern` 为半尺寸 |
| `ShapePie` | 带高度的 3D 扇形柱，`Position` 表示底面圆心，`YDegree` 控制朝向 |
| `ShapeRay` | 有限长度射线，`Position` 为起点，`Direction` 为方向，`Length` 为世界距离 |

> `ShapeRay.Direction` 会在碰撞检测和 Gizmos 绘制中归一化处理，调用侧应把 `Length` 当作实际世界长度，而不是方向向量的缩放倍数。
> `ShapeCapsule.Direction` 同样会被归一化，`Length` 表示胶囊中轴上两个半球球心之间的距离。`Length` 为 `0` 时可退化为球体。

---

## 碰撞检测

碰撞入口集中在 `ColliderOverlapUtil.Overlap(...)`，通过重载支持以下组合：

| 组合 | 说明 |
| --- | --- |
| 球体 / 球体 | 三维球体距离检测 |
| 球体 / 胶囊体 | 球心到胶囊中轴线段的距离检测 |
| 球体 / 线段 | 球心到线段最近点的距离检测 |
| 球体 / AABB | 球心到轴对齐包围盒的距离检测 |
| 球体 / 圆柱 | 先按高度裁剪球体截面，再做 XZ 平面圆形检测 |
| 球体 / 盒体 | 高度裁剪后使用有向盒 SDF 做 XZ 平面检测 |
| 球体 / 扇形柱 | 高度裁剪后使用扇形 SDF 做 XZ 平面检测 |
| 盒体 / 盒体 | 高度区间和 XZ 平面 OBB 分离轴检测 |
| 盒体 / 圆柱 | 高度区间和有向盒 SDF 检测 |
| 盒体 / 胶囊体 | 胶囊中轴线段到盒体的最近距离检测 |
| 盒体 / 线段 | 线段转到盒体局部空间后做 AABB 检测 |
| 盒体 / AABB | AABB 视为未旋转盒体后做 OBB 检测 |
| 盒体 / 扇形柱 | 高度区间和 XZ 平面扇形 / OBB 检测 |
| 盒体 / 射线 | 射线转到盒体局部空间后做 AABB 检测 |
| 圆柱 / 圆柱 | 高度区间和 XZ 平面圆形检测 |
| 圆柱 / 扇形柱 | 高度区间和扇形 SDF 检测 |
| 圆柱 / 胶囊体 | 胶囊端点球体与中轴线段对扩展圆柱检测 |
| 圆柱 / 线段 | 线段作为有限射线复用圆柱射线检测 |
| 圆柱 / AABB | 高度区间和 XZ 圆 / AABB 矩形距离检测 |
| 扇形柱 / AABB | 高度区间和 XZ 平面扇形 / 矩形检测 |
| 扇形柱 / 胶囊体 | 胶囊端点球体与中轴关键点对扇形柱检测 |
| 扇形柱 / 线段 | 高度区间和 XZ 平面线段 / 扇形检测 |
| 扇形柱 / 扇形柱 | 高度区间和 XZ 平面扇形边界检测 |
| 胶囊体 / 胶囊体 | 两条胶囊中轴线段的最近距离检测 |
| 胶囊体 / 线段 | 线段到胶囊中轴线段的最近距离检测 |
| 胶囊体 / AABB | 胶囊中轴线段到 AABB 的最近距离检测 |
| 线段 / 线段 | 两条线段距离为 `0` 时视为相交 |
| 线段 / AABB | 线段作为有限射线做 AABB 检测 |
| 线段 / 射线 | 线段与有限射线线段的最近距离检测 |
| AABB / AABB | 三轴投影区间检测 |
| 球体 / 射线 | 有限射线命中球体，返回首次命中距离 `t` |
| 圆柱 / 射线 | 有限射线命中竖直圆柱，返回首次命中距离 `t` |
| 胶囊体 / 射线 | 有限射线命中胶囊体，返回首次命中距离 `t` |
| AABB / 射线 | 有限射线命中轴对齐包围盒，返回首次命中距离 `t` |

射线重载的 `out float t` 表示从射线起点沿归一化方向前进的世界距离。若射线起点已经在体积内部，`Overlap` 返回 `true` 且 `t` 为 `0`。

```csharp
using ShapeCollider;
using UnityEngine;

var body = new ShapeCylinder
{
    Position = Vector3.zero,
    Radius = 0.5f,
    Height = 2f,
};

var ray = new ShapeRay
{
    Position = new Vector3(-2f, 1f, 0f),
    Direction = Vector3.right,
    Length = 5f,
};

if (ColliderOverlapUtil.Overlap(body, ray, out float t))
{
    Vector3 hit = ray.Position + ray.Direction.normalized * t;
}
```

胶囊体适合做角色近身范围、武器扫掠或非竖直体积检测：

```csharp
var capsule = new ShapeCapsule
{
    Position = transform.position,
    Direction = transform.forward,
    Length = 1.5f,
    Radius = 0.35f,
};

var target = new ShapeSphere
{
    Position = enemy.position,
    Radius = 0.5f,
};

bool hit = ColliderOverlapUtil.Overlap(capsule, target);
```

---

## SDF 工具

`ShapeSDFUtil` 提供 XZ 平面上的辅助计算：

| 方法 | 说明 |
| --- | --- |
| `Rotate(Vector2 v, float degree)` | 按角度旋转二维向量 |
| `OrientedBoxSDF(...)` | 计算点到有向矩形的有符号距离 |
| `SectorSDF(...)` | 计算点到扇形的有符号距离 |

SDF 返回值小于等于 `0` 表示点在形状内部或边界上，大于 `0` 表示在外部。

---

## Gizmos 调试

`ColliderGizmos` 封装了 Scene 视图线框绘制：

```csharp
void OnDrawGizmos()
{
    var box = new ShapeBox
    {
        Position = transform.position,
        Extern = Vector3.one * 0.5f,
        YDegree = transform.eulerAngles.y,
    };

    ColliderGizmos.DrawBox(box);
    ColliderGizmos.DrawCapsule(new ShapeCapsule
    {
        Position = transform.position,
        Direction = transform.forward,
        Length = 2f,
        Radius = 0.4f,
    });
}
```

`ColliderDebug` 是一个调试组件，可挂到 GameObject 上快速查看当前形状，并可指定一个球体或圆柱体目标，用颜色显示是否重叠。

---

## 使用约定

- 所有圆柱和扇形柱都使用世界 Y 轴作为高度方向。
- `ShapeCylinder.Position` 和 `ShapePie.Position` 表示底部中心，`ShapeBox.Position` 和 `ShapeSphere.Position` 表示几何中心。
- `ShapeCapsule.Position` 表示起点球心，终点球心为 `Position + Direction.normalized * Length`。
- `Radius`、`Height`、`Length` 建议传入非负值；射线和胶囊体检测会把负 `Length` 按 `0` 处理。
- 当前模块不提供 `ShapePie / ShapeRay` 和 `ShapeRay / ShapeRay` 重载；这两类检测通常需要更明确的命中参数约定。
