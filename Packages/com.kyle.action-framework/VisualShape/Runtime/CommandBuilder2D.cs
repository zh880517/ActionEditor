using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static VisualShape.CommandBuilder;

namespace VisualShape
{
    /// <summary>
    /// <see cref="CommandBuilder"/> 的 2D 封装。
    ///
    /// <code>
    /// var p1 = new Vector2(0, 1);
    /// var p2 = new Vector2(5, 7);
    ///
    /// // 在 XY 平面绘制
    /// Draw.xy.Line(p1, p2);
    ///
    /// // 在 XZ 平面绘制
    /// Draw.xz.Line(p1, p2);
    /// </code>
    ///
    /// See: 2d-drawing (view in online documentation for working links)
    /// See: <see cref="Draw.xy"/>
    /// See: <see cref="Draw.xz"/>
    /// </summary>
    public partial struct CommandBuilder2D
    {
        /// <summary>封装的命令构建器</summary>
        private CommandBuilder draw;
        /// <summary>如果在 XY 平面绘制为 true，在 XZ 平面绘制为 false</summary>
        bool xy;

        static readonly float3 XY_UP = new float3(0, 0, 1);
        static readonly float3 XZ_UP = new float3(0, 1, 0);
        static readonly quaternion XY_TO_XZ_ROTATION = quaternion.RotateX(-math.PI * 0.5f);
        static readonly quaternion XZ_TO_XZ_ROTATION = quaternion.identity;
        static readonly float4x4 XZ_TO_XY_MATRIX = new float4x4(new float4(1, 0, 0, 0), new float4(0, 0, 1, 0), new float4(0, 1, 0, 0), new float4(0, 0, 0, 1));

        public CommandBuilder2D(CommandBuilder draw, bool xy)
        {
            this.draw = draw;
            this.xy = xy;
        }

        /// <summary>
        /// 在两点之间绘制线条。
        ///
        /// [Open online documentation to see images]
        ///
        /// <code>
        /// void Update () {
        ///     Draw.Line(Vector3.zero, Vector3.up);
        /// }
        /// </code>
        /// </summary>
        public void Line(float2 a, float2 b)
        {
            draw.Reserve<LineData>();
            // Add(Command.Line);
            // Add(new LineData { a = a, b = b });

            // 下面的代码等同于上面被注释掉的代码。
            // 但绘制线条是最常见的操作，所以需要非常快。
            // 硬编码可以将线条渲染性能提高约 8%。
            unsafe
            {
                var buffer = draw.buffer;
                var bufferSize = buffer->Length;
                var newLen = bufferSize + 4 + 24;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                UnityEngine.Assertions.Assert.IsTrue(newLen <= buffer->Capacity);
#endif
                var ptr = (byte*)buffer->Ptr + bufferSize;
                *(Command*)ptr = Command.Line;
                var lineData = (LineData*)(ptr + 4);
                if (xy)
                {
                    lineData->a = new float3(a, 0);
                    lineData->b = new float3(b, 0);
                }
                else
                {
                    lineData->a = new float3(a.x, 0, a.y);
                    lineData->b = new float3(b.x, 0, b.y);
                }
                buffer->Length = newLen;
            }
        }

        /// <summary>
        /// 在两点之间绘制线条。
        ///
        /// [Open online documentation to see images]
        ///
        /// <code>
        /// void Update () {
        ///     Draw.Line(Vector3.zero, Vector3.up);
        /// }
        /// </code>
        /// </summary>
        public void Line(float2 a, float2 b, Color color)
        {
            draw.Reserve<Color32, LineData>();
            // Add(Command.Line);
            // Add(new LineData { a = a, b = b });

            // 下面的代码等同于上面被注释掉的代码。
            // 但绘制线条是最常见的操作，所以需要非常快。
            // 硬编码可以将线条渲染性能提高约 8%。
            unsafe
            {
                var buffer = draw.buffer;
                var bufferSize = buffer->Length;
                var newLen = bufferSize + 4 + 24 + 4;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                UnityEngine.Assertions.Assert.IsTrue(newLen <= buffer->Capacity);
#endif
                var ptr = (byte*)buffer->Ptr + bufferSize;
                *(Command*)ptr = Command.Line | Command.PushColorInline;
                *(uint*)(ptr + 4) = CommandBuilder.ConvertColor(color);
                var lineData = (LineData*)(ptr + 8);
                if (xy)
                {
                    lineData->a = new float3(a, 0);
                    lineData->b = new float3(b, 0);
                }
                else
                {
                    lineData->a = new float3(a.x, 0, a.y);
                    lineData->b = new float3(b.x, 0, b.y);
                }
                buffer->Length = newLen;
            }
        }

        /// <summary>
        /// 在两点之间绘制线条。
        ///
        /// [Open online documentation to see images]
        ///
        /// <code>
        /// void Update () {
        ///     Draw.Line(Vector3.zero, Vector3.up);
        /// }
        /// </code>
        /// </summary>
        public void Line(float3 a, float3 b)
        {
            draw.Line(a, b);
        }

        /// <summary>
        /// 绘制圆。
        ///
        /// 可以通过提供 startAngle 和 endAngle 参数来绘制圆弧。
        ///
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Circle(float3,float3,float)"/>
        /// See: <see cref="Arc(float3,float3,float3)"/>
        /// </summary>
        /// <param name="center">圆或弧的中心。</param>
        /// <param name="radius">圆或弧的半径。</param>
        /// <param name="startAngle">起始角度（弧度）。0 对应正 X 轴。</param>
        /// <param name="endAngle">结束角度（弧度）。</param>
        public void Circle(float2 center, float radius, float startAngle = 0f, float endAngle = 2 * math.PI)
        {
            Circle(xy ? new float3(center, 0) : new float3(center.x, 0, center.y), radius, startAngle, endAngle);
        }

        /// <summary>
        /// 绘制圆。
        ///
        /// 可以通过提供 startAngle 和 endAngle 参数来绘制圆弧。
        ///
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Circle(float3,float3,float)"/>
        /// See: <see cref="Arc(float3,float3,float3)"/>
        /// </summary>
        /// <param name="center">圆或弧的中心。</param>
        /// <param name="radius">圆或弧的半径。</param>
        /// <param name="startAngle">起始角度（弧度）。0 对应正 X 轴。</param>
        /// <param name="endAngle">结束角度（弧度）。</param>
        public void Circle(float3 center, float radius, float startAngle = 0f, float endAngle = 2 * math.PI)
        {
            if (xy)
            {
                draw.PushMatrix(XZ_TO_XY_MATRIX);
                draw.CircleXZInternal(new float3(center.x, center.z, center.y), radius, startAngle, endAngle);
                draw.PopMatrix();
            }
            else
            {
                draw.CircleXZInternal(center, radius, startAngle, endAngle);
            }
        }

        /// <summary>\copydocref{SolidCircle(float3,float,float,float)}</summary>
        public void SolidCircle(float2 center, float radius, float startAngle = 0f, float endAngle = 2 * math.PI)
        {
            SolidCircle(xy ? new float3(center, 0) : new float3(center.x, 0, center.y), radius, startAngle, endAngle);
        }

        /// <summary>
        /// 绘制圆盘。
        ///
        /// 可以通过提供 startAngle 和 endAngle 参数来绘制圆弧。
        ///
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Draw.SolidCircle(float3,float3,float)"/>
        /// See: <see cref="SolidArc(float3,float3,float3)"/>
        /// </summary>
        /// <param name="center">圆盘或实心弧的中心。</param>
        /// <param name="radius">圆盘或实心弧的半径。</param>
        /// <param name="startAngle">起始角度（弧度）。0 对应正 X 轴。</param>
        /// <param name="endAngle">结束角度（弧度）。</param>
        public void SolidCircle(float3 center, float radius, float startAngle = 0f, float endAngle = 2 * math.PI)
        {
            if (xy) draw.PushMatrix(XZ_TO_XY_MATRIX);
            draw.SolidCircleXZInternal(new float3(center.x, -center.z, center.y), radius, startAngle, endAngle);
            if (xy) draw.PopMatrix();
        }

        /// <summary>
        /// 在 2D 中绘制线框药丸形。
        ///
        /// <code>
        /// Draw.xy.WirePill(new float2(-0.5f, -0.5f), new float2(0.5f, 0.5f), 0.5f, color);
        /// </code>
        ///
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="WirePill(float2,float2,float,float)"/>
        /// </summary>
        /// <param name="a">胶囊体第一个圆的中心。</param>
        /// <param name="b">胶囊体第二个圆的中心。</param>
        /// <param name="radius">胶囊体的半径。</param>
        public void WirePill(float2 a, float2 b, float radius)
        {
            WirePill(a, b - a, math.length(b - a), radius);
        }

        /// <summary>
        /// 在 2D 中绘制线框药丸形。
        ///
        /// <code>
        /// Draw.xy.WirePill(new float2(-0.5f, -0.5f), new float2(1, 1), 1, 0.5f, color);
        /// </code>
        ///
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="WirePill(float2,float2,float)"/>
        /// </summary>
        /// <param name="position">胶囊体第一个圆的中心。</param>
        /// <param name="direction">胶囊体的主轴。不需要归一化。如果为零，将绘制圆。</param>
        /// <param name="length">胶囊体主轴长度，从圆心到圆心。如果为零，将绘制圆。</param>
        /// <param name="radius">胶囊体的半径。</param>
        public void WirePill(float2 position, float2 direction, float length, float radius)
        {
            direction = math.normalizesafe(direction);

            if (radius <= 0)
            {
                Line(position, position + direction * length);
            }
            else if (length <= 0 || math.all(direction == 0))
            {
                Circle(position, radius);
            }
            else
            {
                float4x4 m;
                if (xy)
                {
                    m = new float4x4(
                        new float4(direction, 0, 0),
                        new float4(math.cross(new float3(direction, 0), XY_UP), 0),
                        new float4(0, 0, 1, 0),
                        new float4(position, 0, 1)
                        );
                }
                else
                {
                    m = new float4x4(
                        new float4(direction.x, 0, direction.y, 0),
                        new float4(0, 1, 0, 0),
                        new float4(math.cross(new float3(direction.x, 0, direction.y), XZ_UP), 0),
                        new float4(position.x, 0, position.y, 1)
                        );
                }
                draw.PushMatrix(m);
                Circle(new float2(0, 0), radius, 0.5f * math.PI, 1.5f * math.PI);
                Line(new float2(0, -radius), new float2(length, -radius));
                Circle(new float2(length, 0), radius, -0.5f * math.PI, 0.5f * math.PI);
                Line(new float2(0, radius), new float2(length, radius));
                draw.PopMatrix();
            }
        }

        /// <summary>\copydocref{CommandBuilder.Polyline(List&lt;Vector3&gt;,bool)}</summary>
        [BurstDiscard]
        public void Polyline(List<Vector2> points, bool cycle = false)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line(points[i], points[i + 1]);
            }
            if (cycle && points.Count > 1) Line(points[points.Count - 1], points[0]);
        }

        /// <summary>\copydocref{CommandBuilder.Polyline(Vector3[],bool)}</summary>
        [BurstDiscard]
        public void Polyline(Vector2[] points, bool cycle = false)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Line(points[i], points[i + 1]);
            }
            if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
        }

        /// <summary>\copydocref{CommandBuilder.Polyline(float3[],bool)}</summary>
        [BurstDiscard]
        public void Polyline(float2[] points, bool cycle = false)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Line(points[i], points[i + 1]);
            }
            if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
        }

        /// <summary>\copydocref{CommandBuilder.Polyline(NativeArray&lt;float3&gt;,bool)}</summary>
        public void Polyline(NativeArray<float2> points, bool cycle = false)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Line(points[i], points[i + 1]);
            }
            if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
        }

        /// <summary>
        /// 绘制 2D 十字。
        ///
        /// <code>
        /// Draw.xz.Cross(float3.zero, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Draw.Cross"/>
        /// </summary>
        public void Cross(float2 position, float size = 1)
        {
            size *= 0.5f;
            Line(position - new float2(size, 0), position + new float2(size, 0));
            Line(position - new float2(0, size), position + new float2(0, size));
        }

        /// <summary>
        /// 绘制矩形轮廓。
        /// 矩形将沿旋转的 X 和 Z 轴方向排列。
        ///
        /// <code>
        /// Draw.xz.WireRectangle(new Vector3(0f, 0, 0), new Vector2(1, 1), Color.black);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// 这与 <see cref="Draw.WirePlane(float3,quaternion,float2)"/> 相同，但为了一致性添加了此名称。
        ///
        /// See: <see cref="Draw.WirePolygon"/>
        /// </summary>
        public void WireRectangle(float3 center, float2 size)
        {
            draw.WirePlane(center, xy ? XY_TO_XZ_ROTATION : XZ_TO_XZ_ROTATION, size);
        }

        /// <summary>
        /// 绘制矩形轮廓。
        /// 与 <see cref="InScreenSpace"/> 结合使用时特别有用。
        ///
        /// <code>
        /// using (Draw.InScreenSpace(Camera.main)) {
        ///     Draw.xy.WireRectangle(new Rect(10, 10, 100, 100), Color.black);
        /// }
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Draw.WireRectangle(float3,quaternion,float2)"/>
        /// See: <see cref="Draw.WirePolygon"/>
        /// </summary>
        public void WireRectangle(Rect rect)
        {
            float2 min = rect.min;
            float2 max = rect.max;

            Line(new float2(min.x, min.y), new float2(max.x, min.y));
            Line(new float2(max.x, min.y), new float2(max.x, max.y));
            Line(new float2(max.x, max.y), new float2(min.x, max.y));
            Line(new float2(min.x, max.y), new float2(min.x, min.y));
        }

        /// <summary>
        /// 绘制实心矩形。
        /// 与 <see cref="InScreenSpace"/> 结合使用时特别有用。
        ///
        /// 底层实现使用 <see cref="Draw.SolidPlane"/>。
        ///
        /// <code>
        /// using (Draw.InScreenSpace(Camera.main)) {
        ///     Draw.xy.SolidRectangle(new Rect(10, 10, 100, 100), Color.black);
        /// }
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="WireRectangle"/>
        /// See: <see cref="Draw.WireRectangle(float3,quaternion,float2)"/>
        /// See: <see cref="Draw.SolidBox"/>
        /// </summary>
        public void SolidRectangle(Rect rect)
        {
            draw.SolidPlane(new float3(rect.center.x, rect.center.y, 0.0f), xy ? XY_TO_XZ_ROTATION : XZ_TO_XZ_ROTATION, new float2(rect.width, rect.height));
        }

        /// <summary>
        /// 绘制线条网格。
        ///
        /// <code>
        /// Draw.xz.WireGrid(Vector3.zero, new int2(3, 3), new float2(1, 1), color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Draw.WireGrid"/>
        /// </summary>
        /// <param name="center">网格中心</param>
        /// <param name="cells">网格单元数。应大于 0。</param>
        /// <param name="totalSize">网格沿 X 和 Z 轴的总大小。</param>
        public void WireGrid(float2 center, int2 cells, float2 totalSize)
        {
            draw.WireGrid(xy ? new float3(center, 0) : new float3(center.x, 0, center.y), xy ? XY_TO_XZ_ROTATION : XZ_TO_XZ_ROTATION, cells, totalSize);
        }

        /// <summary>
        /// 绘制线条网格。
        ///
        /// <code>
        /// Draw.xz.WireGrid(Vector3.zero, new int2(3, 3), new float2(1, 1), color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Draw.WireGrid"/>
        /// </summary>
        /// <param name="center">网格中心</param>
        /// <param name="cells">网格单元数。应大于 0。</param>
        /// <param name="totalSize">网格沿 X 和 Z 轴的总大小。</param>
        public void WireGrid(float3 center, int2 cells, float2 totalSize)
        {
            draw.WireGrid(center, xy ? XY_TO_XZ_ROTATION : XZ_TO_XZ_ROTATION, cells, totalSize);
        }
    }
}

