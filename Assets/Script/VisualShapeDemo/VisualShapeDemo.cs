using UnityEngine;
using VisualShape;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
/// VisualShape 可视化演示脚本。
/// 挂载到任意 GameObject，在 Scene 视图中展示所有绘制功能。
/// 支持在 Inspector 中切换绘制目标（Editor / InGame）和 2D 平面（XY / XZ）。
/// </summary>
[ExecuteInEditMode]
public class VisualShapeDemo : MonoBehaviourGizmos
{
    public enum DrawTarget
    {
        Editor,
        InGame,
    }

    public enum Plane2D
    {
        XZ,
        XY,
    }

    [Header("绘制目标")]
    [Tooltip("Editor: 仅编辑器 Gizmos 启用时可见\nInGame: 编辑器和构建版本中都可见")]
    public DrawTarget drawTarget = DrawTarget.Editor;

    [Tooltip("2D 绘制使用的平面")]
    public Plane2D plane2D = Plane2D.XZ;

    [Header("演示开关")]
    public bool showLines = true;
    public bool showBoxes = true;
    public bool showSpheres = true;
    public bool showCirclesAndArcs = true;
    public bool showCylindersAndCapsules = true;
    public bool showPolygons = true;
    public bool showPlanes = true;
    public bool showArrows = true;
    public bool showCurves = true;
    public bool showGridAndCross = true;
    public bool show2D = true;
    public bool showLabels = true;
    public bool showScopes = true;
    public bool showPalette = true;
    public bool showMesh = true;

    [Header("设置")]
    [Tooltip("各组之间的间距")]
    public float groupSpacing = 6f;

    // ─── 当前帧使用的 builder 引用 ───
    CommandBuilder cmd;

    /// <summary>获取当前 2D 绘制器</summary>
    CommandBuilder2D Cmd2D => plane2D == Plane2D.XY ? cmd.xy : cmd.xz;

    float3 GroupOrigin(int index) => new float3(index * groupSpacing, 0, 0) + (float3)transform.position;

    void DrawGroupLabel(int index, string text)
    {
        var pos = GroupOrigin(index) + new float3(0, -1.5f, 0);
        cmd.Label3D(pos, quaternion.identity, text, 0.3f, LabelAlignment.TopCenter, Color.white);
    }

    public override void DrawGizmos()
    {
        // 根据 drawTarget 选择 builder
        cmd = drawTarget == DrawTarget.InGame ? Draw.ingame : Draw.editor;

        int group = 0;

        if (showLines)                  DrawLines(group++);
        if (showBoxes)                  DrawBoxes(group++);
        if (showSpheres)                DrawSpheres(group++);
        if (showCirclesAndArcs)         DrawCirclesAndArcs(group++);
        if (showCylindersAndCapsules)   DrawCylindersAndCapsules(group++);
        if (showPolygons)               DrawPolygons(group++);
        if (showPlanes)                 DrawPlanes(group++);
        if (showArrows)                 DrawArrows(group++);
        if (showCurves)                 DrawCurves(group++);
        if (showGridAndCross)           DrawGridAndCross(group++);
        if (show2D)                     Draw2D(group++);
        if (showLabels)                 DrawLabels(group++);
        if (showScopes)                 DrawScopes(group++);
        if (showPalette)                DrawPaletteColors(group++);
        if (showMesh)                   DrawMeshDemo(group++);

        // 选中高亮
        if (GizmoContext.InSelection(this))
        {
            cmd.WireBox(transform.position + Vector3.up * 3f, new Vector3(group * groupSpacing + 2, 6, 4), new Color(1, 1, 0, 0.3f));
        }
    }

    // ─────────────────────────────────────────
    // 1. 线条
    // ─────────────────────────────────────────
    void DrawLines(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Lines");

        cmd.Line(o + new float3(-1, 0, 0), o + new float3(1, 2, 0), Color.red);
        cmd.Ray(o + new float3(-1, 0, 1), new float3(1, 1, 0), Color.green);
        cmd.DashedLine(o + new float3(-1, 0, -1), o + new float3(1, 2, -1), 0.2f, 0.1f, Color.cyan);

        var polyPoints = new List<Vector3>
        {
            (Vector3)(o + new float3(-0.5f, 2.5f, 0)),
            (Vector3)(o + new float3(0.5f, 3f, 0.5f)),
            (Vector3)(o + new float3(0.5f, 3.5f, -0.5f)),
            (Vector3)(o + new float3(-0.5f, 4f, 0))
        };
        cmd.Polyline(polyPoints, cycle: true, color: Color.yellow);
    }

    // ─────────────────────────────────────────
    // 2. 盒体
    // ─────────────────────────────────────────
    void DrawBoxes(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Boxes");

        cmd.WireBox(o + new float3(0, 0.5f, -1), new float3(1, 1, 1), Color.green);
        cmd.SolidBox(o + new float3(0, 0.5f, 1), new float3(0.8f, 0.8f, 0.8f), new Color(0, 0.5f, 1, 0.5f));
        cmd.WireBox(o + new float3(0, 2.5f, 0), quaternion.Euler(0.5f, 0.5f, 0), new float3(1, 1, 1), Color.magenta);
    }

    // ─────────────────────────────────────────
    // 3. 球体
    // ─────────────────────────────────────────
    void DrawSpheres(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Spheres");

        cmd.WireSphere(o + new float3(0, 1, -1), 0.8f, Color.cyan);
        cmd.SphereOutline(o + new float3(0, 1, 1), 0.8f, Color.yellow);
        cmd.WireSphere(o + new float3(0, 3, 0), 0.5f, Color.red);
    }

    // ─────────────────────────────────────────
    // 4. 圆与弧
    // ─────────────────────────────────────────
    void DrawCirclesAndArcs(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Circles & Arcs");

        cmd.Circle(o + new float3(0, 1, 0), new float3(0, 1, 0), 0.8f, Color.green);
        cmd.SolidCircle(o + new float3(0, 1, 0), new float3(0, 1, 0), 0.5f, new Color(0, 1, 0, 0.3f));

        var arcCenter = o + new float3(0, 2.5f, 0);
        var arcStart = new float3(0.8f, 0, 0);
        var arcEnd = new float3(0, 0, 0.8f);
        cmd.Arc(arcCenter, arcCenter + arcStart, arcCenter + arcEnd, Color.red);
        cmd.SolidArc(arcCenter + new float3(0, 1, 0), arcCenter + new float3(0, 1, 0) + arcStart, arcCenter + new float3(0, 1, 0) + arcEnd, new Color(1, 0, 0, 0.3f));
    }

    // ─────────────────────────────────────────
    // 5. 圆柱与胶囊
    // ─────────────────────────────────────────
    void DrawCylindersAndCapsules(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Cylinders & Capsules");

        cmd.WireCylinder(o + new float3(-0.8f, 0, 0), o + new float3(-0.8f, 2, 0), 0.4f, Color.blue);
        cmd.WireCapsule(o + new float3(0.8f, 0, 0), o + new float3(0.8f, 2, 0), 0.4f, Color.yellow);
    }

    // ─────────────────────────────────────────
    // 6. 三角形与多边形
    // ─────────────────────────────────────────
    void DrawPolygons(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Polygons");

        cmd.WireTriangle(
            o + new float3(-0.5f, 0, 0),
            o + new float3(0.5f, 0, 0),
            o + new float3(0, 1, 0),
            Color.red
        );

        cmd.SolidTriangle(
            o + new float3(-0.5f, 0, 1),
            o + new float3(0.5f, 0, 1),
            o + new float3(0, 0.8f, 1),
            new Color(1, 0, 0, 0.4f)
        );

        cmd.WirePentagon(o + new float3(0, 2, 0), quaternion.identity, 0.6f, Color.cyan);
        cmd.WireHexagon(o + new float3(0, 3.5f, 0), quaternion.identity, 0.6f, Color.green);
    }

    // ─────────────────────────────────────────
    // 7. 平面
    // ─────────────────────────────────────────
    void DrawPlanes(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Planes");

        cmd.WirePlane(o + new float3(0, 0.5f, -1), new float3(0, 1, 0), new float2(1.5f, 1.5f), Color.green);
        cmd.SolidPlane(o + new float3(0, 0.5f, 1), new float3(0, 1, 0), new float2(1.5f, 1.5f), new Color(0, 1, 0, 0.3f));
        cmd.PlaneWithNormal(o + new float3(0, 2.5f, 0), new float3(0, 1, 0.3f), new float2(1.5f, 1.5f), Color.yellow);
    }

    // ─────────────────────────────────────────
    // 8. 箭头
    // ─────────────────────────────────────────
    void DrawArrows(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Arrows");

        cmd.Arrow(o + new float3(-1, 0, 0), o + new float3(1, 1, 0), Color.red);
        cmd.Arrow(o + new float3(-1, 1.5f, 0), o + new float3(1, 2.5f, 0), new float3(0, 1, 0), 0.3f, Color.green);
        cmd.Arrowhead(o + new float3(0, 3.5f, 0), new float3(0, 1, 0), 0.4f, Color.blue);
        cmd.ArrowheadArc(o + new float3(0, 3.5f, 0), new float3(1, 0, 0), 0.8f, Color.magenta);
    }

    // ─────────────────────────────────────────
    // 9. 曲线
    // ─────────────────────────────────────────
    void DrawCurves(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Curves");

        // 贝塞尔曲线
        cmd.Bezier(
            o + new float3(-1, 0, 0),
            o + new float3(-0.5f, 2, 0),
            o + new float3(0.5f, 0, 0),
            o + new float3(1, 2, 0),
            Color.cyan
        );

        // Catmull-Rom 样条
        var splinePoints = new List<Vector3>
        {
            (Vector3)(o + new float3(-1, 2.5f, 0)),
            (Vector3)(o + new float3(-0.3f, 3.5f, 0)),
            (Vector3)(o + new float3(0.3f, 2.5f, 0)),
            (Vector3)(o + new float3(1, 3.5f, 0))
        };
        cmd.CatmullRom(splinePoints, Color.yellow);
    }

    // ─────────────────────────────────────────
    // 10. 网格与十字
    // ─────────────────────────────────────────
    void DrawGridAndCross(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Grid & Cross");

        cmd.WireGrid(o + new float3(0, 0.01f, 0), quaternion.identity, new int2(4, 4), new float2(2, 2), new Color(0.5f, 0.5f, 0.5f));
        cmd.Cross(o + new float3(0, 2, 0), 0.5f, Color.red);
        cmd.Cross(o + new float3(-0.8f, 2.5f, 0), 0.3f, Color.green);
        cmd.Cross(o + new float3(0.8f, 2.5f, 0), 0.3f, Color.blue);
    }

    // ─────────────────────────────────────────
    // 11. 2D 绘制
    // ─────────────────────────────────────────
    void Draw2D(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, plane2D == Plane2D.XY ? "2D (XY)" : "2D (XZ)");

        using (cmd.WithMatrix(Matrix4x4.Translate((Vector3)o)))
        {
            var d = Cmd2D;
            d.Circle(new float2(0, 0), 0.8f, Color.green);
            d.WireRectangle(new float3(0, 1.5f, 0), new float2(1.5f, 1.5f), Color.blue);
            d.WirePill(new float2(-0.5f, -2), new float2(0.5f, -2), 0.3f, Color.yellow);
            d.Cross(new float2(0, -3), 0.4f, Color.red);
        }
    }

    // ─────────────────────────────────────────
    // 12. 文本标签
    // ─────────────────────────────────────────
    void DrawLabels(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Labels");

        cmd.Label2D(o + new float3(0, 0.5f, 0), "Label2D (14px)", 14f, LabelAlignment.Center, Color.white);
        cmd.Label2D(o + new float3(0, 1.2f, 0), "TopLeft", 12f, LabelAlignment.TopLeft, Color.green);
        cmd.Label2D(o + new float3(0, 1.8f, 0), "BottomRight", 12f, LabelAlignment.BottomRight, Color.cyan);

        cmd.Label3D(o + new float3(0, 2.5f, 0), quaternion.identity, "Label3D", 0.4f, LabelAlignment.Center, Color.yellow);
        cmd.Label3D(o + new float3(0, 3.5f, 0), quaternion.Euler(0, 0.5f, 0), "Rotated", 0.3f, LabelAlignment.Center, Color.magenta);
    }

    // ─────────────────────────────────────────
    // 13. 作用域演示
    // ─────────────────────────────────────────
    void DrawScopes(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Scopes");

        // WithColor
        using (cmd.WithColor(Color.red))
        {
            cmd.Line(o + new float3(-1, 0, 0), o + new float3(1, 0, 0));
            cmd.Line(o + new float3(-1, 0.2f, 0), o + new float3(1, 0.2f, 0));
        }

        // WithLineWidth
        using (cmd.WithLineWidth(1f))
        {
            cmd.Line(o + new float3(-1, 0.7f, 0), o + new float3(1, 0.7f, 0), Color.green);
        }
        using (cmd.WithLineWidth(3f))
        {
            cmd.Line(o + new float3(-1, 1.0f, 0), o + new float3(1, 1.0f, 0), Color.green);
        }
        using (cmd.WithLineWidth(6f))
        {
            cmd.Line(o + new float3(-1, 1.4f, 0), o + new float3(1, 1.4f, 0), Color.green);
        }

        // WithMatrix (缩放 + 旋转)
        var mtx = Matrix4x4.TRS((Vector3)(o + new float3(0, 2.5f, 0)), Quaternion.Euler(0, 45, 0), Vector3.one * 0.5f);
        using (cmd.WithMatrix(mtx))
        {
            cmd.WireBox(Vector3.zero, Vector3.one, Color.cyan);
        }

        // InLocalSpace
        using (cmd.InLocalSpace(transform))
        {
            cmd.WireSphere(new Vector3(index * groupSpacing, 4f, 0), 0.3f, Color.magenta);
        }

        cmd.Label2D(o + new float3(0, 0, 0.5f), "Color", 10f, LabelAlignment.Center, Color.red);
        cmd.Label2D(o + new float3(0, 1, 0.5f), "LineWidth", 10f, LabelAlignment.Center, Color.green);
        cmd.Label2D(o + new float3(0, 2.5f, 0.5f), "Matrix", 10f, LabelAlignment.Center, Color.cyan);
        cmd.Label2D(o + new float3(0, 4f, 0.5f), "LocalSpace", 10f, LabelAlignment.Center, Color.magenta);
    }

    // ─────────────────────────────────────────
    // 14. 调色板颜色展示
    // ─────────────────────────────────────────
    void DrawPaletteColors(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Palette");

        // Colorbrewer Set1
        Color[] set1 = {
            Palette.Colorbrewer.Set1.Red,
            Palette.Colorbrewer.Set1.Blue,
            Palette.Colorbrewer.Set1.Green,
            Palette.Colorbrewer.Set1.Purple,
            Palette.Colorbrewer.Set1.Orange,
            Palette.Colorbrewer.Set1.Yellow,
            Palette.Colorbrewer.Set1.Brown,
            Palette.Colorbrewer.Set1.Pink,
            Palette.Colorbrewer.Set1.Grey
        };

        string[] names = { "Red", "Blue", "Green", "Purple", "Orange", "Yellow", "Brown", "Pink", "Grey" };

        for (int i = 0; i < set1.Length; i++)
        {
            float y = i * 0.45f;
            cmd.SolidBox(o + new float3(-0.3f, y + 0.15f, 0), new float3(0.5f, 0.3f, 0.3f), set1[i]);
            cmd.Label2D(o + new float3(0.3f, y + 0.15f, 0), names[i], 10f, LabelAlignment.MiddleLeft, set1[i]);
        }

        // Blues 顺序配色
        for (int i = 0; i < 5; i++)
        {
            var color = Palette.Colorbrewer.Blues.GetColor(5, i);
            cmd.SolidBox(o + new float3(0, set1.Length * 0.45f + 0.3f + i * 0.35f, 0), new float3(1.5f, 0.25f, 0.3f), color);
        }
    }

    // ─────────────────────────────────────────
    // 15. 网格绘制
    // ─────────────────────────────────────────
    void DrawMeshDemo(int index)
    {
        var o = GroupOrigin(index);
        DrawGroupLabel(index, "Mesh");

        var mtx = Matrix4x4.TRS((Vector3)(o + new float3(0, 0.5f, 0)), Quaternion.Euler(30, 45, 0), Vector3.one * 0.8f);
        using (cmd.WithMatrix(mtx))
        {
            var mesh = GetPrimitiveMesh(PrimitiveType.Cube);
            if (mesh != null)
                cmd.WireMesh(mesh, Color.green);
        }

        mtx = Matrix4x4.TRS((Vector3)(o + new float3(0, 2.5f, 0)), Quaternion.Euler(0, Time.realtimeSinceStartup * 30, 0), Vector3.one * 0.6f);
        using (cmd.WithMatrix(mtx))
        {
            var mesh = GetPrimitiveMesh(PrimitiveType.Sphere);
            if (mesh != null)
                cmd.SolidMesh(mesh, new Color(0.2f, 0.6f, 1f, 0.4f));
        }
    }

    static Dictionary<PrimitiveType, Mesh> primitiveMeshCache = new Dictionary<PrimitiveType, Mesh>();

    static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        if (!primitiveMeshCache.TryGetValue(type, out var mesh))
        {
            var go = GameObject.CreatePrimitive(type);
            mesh = go.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(go);
            primitiveMeshCache[type] = mesh;
        }
        return mesh;
    }
}
