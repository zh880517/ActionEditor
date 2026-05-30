# ShapeCollider 使用手册

ShapeCollider 是一组轻量级几何碰撞工具，用于在不依赖 Unity Physics 的场景中做简单体积检测、射线检测和 Scene Gizmos 调试绘制。模块位于 `Packages/com.kyle.action-framework/ShapeCollider/Runtime/`，运行时与编辑器均可使用。

---

## 核心类型

| 类型 | 说明 |
| --- | --- |
| `ShapeSphere` | 球体，包含 `Position` 与 `Radius` |
| `ShapeCylinder` | 竖直圆柱，`Position` 表示底面中心，`Height` 沿世界 Y 轴向上 |
| `ShapeBox` | 绕世界 Y 轴旋转的盒体，`Position` 为中心，`Extern` 为半尺寸 |
| `ShapePie` | 带高度的 3D 扇形柱，`Position` 表示底面圆心，`YDegree` 控制朝向 |
| `ShapeRay` | 有限长度射线，`Position` 为起点，`Direction` 为方向，`Length` 为世界距离 |

> `ShapeRay.Direction` 会在碰撞检测和 Gizmos 绘制中归一化处理，调用侧应把 `Length` 当作实际世界长度，而不是方向向量的缩放倍数。

---

## 碰撞检测

碰撞入口集中在 `ColliderOverlapUtil.Overlap(...)`，通过重载支持以下组合：

| 组合 | 说明 |
| --- | --- |
| 球体 / 球体 | 三维球体距离检测 |
| 球体 / 圆柱 | 先按高度裁剪球体截面，再做 XZ 平面圆形检测 |
| 球体 / 盒体 | 高度裁剪后使用有向盒 SDF 做 XZ 平面检测 |
| 球体 / 扇形柱 | 高度裁剪后使用扇形 SDF 做 XZ 平面检测 |
| 圆柱 / 圆柱 | 高度区间和 XZ 平面圆形检测 |
| 圆柱 / 盒体 | 高度区间和有向盒 SDF 检测 |
| 圆柱 / 扇形柱 | 高度区间和扇形 SDF 检测 |
| 球体 / 射线 | 有限射线命中球体，返回首次命中距离 `t` |
| 圆柱 / 射线 | 有限射线命中竖直圆柱，返回首次命中距离 `t` |

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
}
```

`ColliderDebug` 是一个调试组件，可挂到 GameObject 上快速查看当前形状，并可指定一个球体或圆柱体目标，用颜色显示是否重叠。

---

## 使用约定

- 所有圆柱和扇形柱都使用世界 Y 轴作为高度方向。
- `ShapeCylinder.Position` 和 `ShapePie.Position` 表示底部中心，`ShapeBox.Position` 和 `ShapeSphere.Position` 表示几何中心。
- `Radius`、`Height`、`Length` 建议传入非负值；射线检测会把负 `Length` 按 `0` 处理。
- 当前模块只覆盖简单体积组合，不包含盒体 / 盒体、扇形 / 扇形等复杂组合。
