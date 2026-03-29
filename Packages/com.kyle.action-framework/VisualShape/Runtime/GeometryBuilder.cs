using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using Unity.Burst;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;

namespace VisualShape
{
    using static ShapeData;
    using static CommandBuilder;
    using VisualShape.Text;
    using Unity.Profiling;
    using System.Collections.Generic;
    using UnityEngine.Rendering;

    static class GeometryBuilder
    {
        public struct CameraInfo
        {
            public float3 cameraPosition;
            public quaternion cameraRotation;
            public float2 cameraDepthToPixelSize;
            public bool cameraIsOrthographic;

            public CameraInfo(Camera camera)
            {
                var tr = camera?.transform;
                cameraPosition = tr != null ? (float3)tr.position : float3.zero;
                cameraRotation = tr != null ? (quaternion)tr.rotation : quaternion.identity;
                cameraDepthToPixelSize = (camera != null ? CameraDepthToPixelSize(camera) : 0);
                cameraIsOrthographic = camera != null ? camera.orthographic : false;
            }
        }

        internal static unsafe JobHandle Build(ShapeData gizmos, ProcessedBuilderData.MeshBuffers* buffers, ref CameraInfo cameraInfo, JobHandle dependency)
        {
            // 创建新的构建器并调度它。
            // 为什么 characterInfo 是以指针和长度的形式传递而不是直接传递 NativeArray？
            //  因为传递 NativeArray 会触发安全系统，该系统会为 NativeArray 添加一些跟踪信息。
            //  通常这不是问题，但我们可能会调度数百个使用该 NativeArray 的 Job，这会导致安全检查系统有些变慢。
            //  以指针+长度方式传递可以使整个调度代码速度提高约两倍。
            return new GeometryBuilderJob
            {
                buffers = buffers,
                currentMatrix = Matrix4x4.identity,
                currentLineWidthData = new LineWidthData
                {
                    pixels = 1,
                    automaticJoins = false,
                },
                lineWidthMultiplier = ShapeManager.lineWidthMultiplier,
                currentColor = (Color32)Color.white,
                cameraPosition = cameraInfo.cameraPosition,
                cameraRotation = cameraInfo.cameraRotation,
                cameraDepthToPixelSize = cameraInfo.cameraDepthToPixelSize,
                cameraIsOrthographic = cameraInfo.cameraIsOrthographic,
                characterInfo = (SDFCharacter*)gizmos.fontData.characters.GetUnsafeReadOnlyPtr(),
                characterInfoLength = gizmos.fontData.characters.Length,
                maxPixelError = GeometryBuilderJob.MaxCirclePixelError / math.max(0.1f, gizmos.settingsRef.curveResolution),
            }.Schedule(dependency);
        }

        /// <summary>
        /// 用于确定在给定深度下一个像素有多大的辅助方法。
        /// 在距离相机 D 处，一个像素大约对应 value.x * D + value.y 个世界单位。
        /// 其中 value 是此函数的返回值。
        /// </summary>
        private static float2 CameraDepthToPixelSize(Camera camera)
        {
            if (camera.orthographic)
            {
                return new float2(0.0f, 2.0f * camera.orthographicSize / camera.pixelHeight);
            }
            else
            {
                return new float2(Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) / (0.5f * camera.pixelHeight), 0.0f);
            }
        }

        private static NativeArray<T> ConvertExistingDataToNativeArray<T>(UnsafeAppendBuffer data) where T : struct
        {
            unsafe
            {
                var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data.Ptr, data.Length / UnsafeUtility.SizeOf<T>(), Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
                return arr;
            }
        }

        internal static unsafe void BuildMesh(ShapeData gizmos, List<MeshWithType> meshes, ProcessedBuilderData.MeshBuffers* inputBuffers)
        {
            if (inputBuffers->triangles.Length > 0)
            {
                CommandBuilderSamplers.MarkerUpdateBuffer.Begin();
                var mesh = AssignMeshData<GeometryBuilderJob.Vertex>(gizmos, inputBuffers->bounds, inputBuffers->vertices, inputBuffers->triangles, MeshLayouts.MeshLayout);
                meshes.Add(new MeshWithType { mesh = mesh, type = MeshType.Lines });
                CommandBuilderSamplers.MarkerUpdateBuffer.End();
            }

            if (inputBuffers->solidTriangles.Length > 0)
            {
                var mesh = AssignMeshData<GeometryBuilderJob.Vertex>(gizmos, inputBuffers->bounds, inputBuffers->solidVertices, inputBuffers->solidTriangles, MeshLayouts.MeshLayout);
                meshes.Add(new MeshWithType { mesh = mesh, type = MeshType.Solid });
            }

            if (inputBuffers->textTriangles.Length > 0)
            {
                var mesh = AssignMeshData<GeometryBuilderJob.TextVertex>(gizmos, inputBuffers->bounds, inputBuffers->textVertices, inputBuffers->textTriangles, MeshLayouts.MeshLayoutText);
                meshes.Add(new MeshWithType { mesh = mesh, type = MeshType.Text });
            }
        }

        private static Mesh AssignMeshData<VertexType>(ShapeData gizmos, Bounds bounds, UnsafeAppendBuffer vertices, UnsafeAppendBuffer triangles, VertexAttributeDescriptor[] layout) where VertexType : struct
        {
            CommandBuilderSamplers.MarkerConvert.Begin();
            var verticesView = ConvertExistingDataToNativeArray<VertexType>(vertices);
            var trianglesView = ConvertExistingDataToNativeArray<int>(triangles);
            CommandBuilderSamplers.MarkerConvert.End();
            var mesh = gizmos.GetMesh(verticesView.Length);

            CommandBuilderSamplers.MarkerSetLayout.Begin();
            // 必要时调整顶点缓冲区大小
            // 注意：当顶点缓冲区明显大于所需时也会调整。
            //       因为在执行命令缓冲区时，Unity 似乎会对整个缓冲区做某些操作（在 Profiler 中显示为 Mesh.CreateMesh）
            // TODO: 如果每帧使用多个大小不同的 Mesh，可能会导致问题。
            // 应该查询已有合适大小缓冲区的 Mesh。
            // if (mesh.vertexCount < verticesView.Length || mesh.vertexCount > verticesView.Length * 2) {

            // }
            // TODO: 当 Mesh.GetVertexBuffer/Mesh.GetIndexBuffer 不再有 Bug 时切换使用。
            // 目前调整大小后似乎无法正确刷新（2022.2.0b1）
            mesh.SetVertexBufferParams(math.ceilpow2(verticesView.Length), layout);
            mesh.SetIndexBufferParams(math.ceilpow2(trianglesView.Length), IndexFormat.UInt32);
            CommandBuilderSamplers.MarkerSetLayout.End();

            CommandBuilderSamplers.MarkerUpdateVertices.Begin();
            // 更新网格数据
            mesh.SetVertexBufferData(verticesView, 0, 0, verticesView.Length);
            CommandBuilderSamplers.MarkerUpdateVertices.End();
            CommandBuilderSamplers.MarkerUpdateIndices.Begin();
            // 更新索引缓冲区并假定所有索引正确
            mesh.SetIndexBufferData(trianglesView, 0, 0, trianglesView.Length, MeshUpdateFlags.DontValidateIndices);
            CommandBuilderSamplers.MarkerUpdateIndices.End();


            CommandBuilderSamplers.MarkerSubmesh.Begin();
            mesh.subMeshCount = 1;
            var submesh = new SubMeshDescriptor(0, trianglesView.Length, MeshTopology.Triangles)
            {
                vertexCount = verticesView.Length,
                bounds = bounds
            };
            mesh.SetSubMesh(0, submesh, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
            mesh.bounds = bounds;
            CommandBuilderSamplers.MarkerSubmesh.End();
            return mesh;
        }
    }

    /// <summary>某些静态字段需要在单独的类中，因为 Burst 不支持它们</summary>
    static class MeshLayouts
    {
        internal static readonly VertexAttributeDescriptor[] MeshLayout = {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        internal static readonly VertexAttributeDescriptor[] MeshLayoutText = {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };
    }

    /// <summary>
    /// 从渲染命令流构建几何体的 Job。
    ///
    /// See: <see cref="CommandBuilder"/>
    /// </summary>
    // 注意：将 FloatMode 设为 Fast 会导致绘制圆时出现视觉缺陷。
    // 我认为这是因为 math.sin(float4) 对输入的每个分量
    // 产生了略有不同的结果。
    [BurstCompile(FloatMode = FloatMode.Default)]
    internal struct GeometryBuilderJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe ProcessedBuilderData.MeshBuffers* buffers;

        [NativeDisableUnsafePtrRestriction]
        public unsafe SDFCharacter* characterInfo;
        public int characterInfoLength;

        public Color32 currentColor;
        public float4x4 currentMatrix;
        public LineWidthData currentLineWidthData;
        public float lineWidthMultiplier;
        float3 minBounds;
        float3 maxBounds;
        public float3 cameraPosition;
        public quaternion cameraRotation;
        public float2 cameraDepthToPixelSize;
        public float maxPixelError;
        public bool cameraIsOrthographic;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct Vertex
        {
            public float3 position;
            public float3 uv2;
            public Color32 color;
            public float2 uv;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct TextVertex
        {
            public float3 position;
            public Color32 color;
            public float2 uv;
        }

        static unsafe void Add<T>(UnsafeAppendBuffer* buffer, T value) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();
            // 我们知道缓冲区有足够容量，所以可以直接写入而无需
            // 为溢出情况添加分支（像 buffer->Add 那样）。
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            UnityEngine.Assertions.Assert.IsTrue(buffer->Length + size <= buffer->Capacity);
#endif
            *(T*)(buffer->Ptr + buffer->Length) = value;
            buffer->Length = buffer->Length + size;
        }

        static unsafe void Reserve(UnsafeAppendBuffer* buffer, int size)
        {
            var newSize = buffer->Length + size;

            if (newSize > buffer->Capacity)
            {
                buffer->SetCapacity(math.max(newSize, buffer->Capacity * 2));
            }
        }

        internal static float3 PerspectiveDivide(float4 p)
        {
            return p.xyz * math.rcp(p.w);
        }

        unsafe void AddText(System.UInt16* text, TextData textData, Color32 color)
        {
            var pivot = PerspectiveDivide(math.mul(currentMatrix, new float4(textData.center, 1.0f)));

            AddTextInternal(
                text,
                pivot,
                math.mul(cameraRotation, new float3(1, 0, 0)),
                math.mul(cameraRotation, new float3(0, 1, 0)),
                textData.alignment,
                textData.sizeInPixels,
                true,
                textData.numCharacters,
                color
                );
        }

        unsafe void AddText3D(System.UInt16* text, TextData3D textData, Color32 color)
        {
            var pivot = PerspectiveDivide(math.mul(currentMatrix, new float4(textData.center, 1.0f)));
            var m = math.mul(currentMatrix, new float4x4(textData.rotation, float3.zero));

            AddTextInternal(
                text,
                pivot,
                m.c0.xyz,
                m.c1.xyz,
                textData.alignment,
                textData.size,
                false,
                textData.numCharacters,
                color
                );
        }


        unsafe void AddTextInternal(System.UInt16* text, float3 pivot, float3 right, float3 up, LabelAlignment alignment, float size, bool sizeIsInPixels, int numCharacters, Color32 color)
        {
            var distance = math.abs(math.dot(pivot - cameraPosition, math.mul(cameraRotation, new float3(0, 0, 1))));
            var pixelSize = cameraDepthToPixelSize.x * distance + cameraDepthToPixelSize.y;
            float fontWorldSize = size;

            if (sizeIsInPixels) fontWorldSize *= pixelSize;

            right *= fontWorldSize;
            up *= fontWorldSize;

            // 计算文本的总宽度（以像素除以字体大小表示）
            float maxWidth = 0;
            float currentWidth = 0;
            float numLines = 1;

            for (int i = 0; i < numCharacters; i++)
            {
                var characterInfoIndex = text[i];
                if (characterInfoIndex == SDFLookupData.Newline)
                {
                    maxWidth = math.max(maxWidth, currentWidth);
                    currentWidth = 0;
                    numLines++;
                }
                else
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (characterInfoIndex >= characterInfoLength) throw new System.Exception("Invalid character. No info exists. This is a bug.");
#endif
                    currentWidth += characterInfo[characterInfoIndex].advance;
                }
            }
            maxWidth = math.max(maxWidth, currentWidth);

            // 根据相机和文本对齐方式计算文本的世界空间位置
            var pos = pivot;
            pos -= right * maxWidth * alignment.relativePivot.x;
            // 使用当前字体时，字符大小占整行的比例
            const float FontCharacterFractionOfLine = 0.75f;
            // 假设从 y=0 开始书写，文本上下部分的位置
            var lower = 1 - numLines;
            var upper = FontCharacterFractionOfLine;
            var yAdjustment = math.lerp(lower, upper, alignment.relativePivot.y);
            pos -= up * yAdjustment;
            pos += math.mul(cameraRotation, new float3(1, 0, 0)) * (pixelSize * alignment.pixelOffset.x);
            pos += math.mul(cameraRotation, new float3(0, 1, 0)) * (pixelSize * alignment.pixelOffset.y);

            var textVertices = &buffers->textVertices;
            var textTriangles = &buffers->textTriangles;

            // 预先分配所有缓冲区空间
            Reserve(textVertices, numCharacters * VerticesPerCharacter * UnsafeUtility.SizeOf<TextVertex>());
            Reserve(textTriangles, numCharacters * TrianglesPerCharacter * UnsafeUtility.SizeOf<int>());

            var lineStart = pos;

            for (int i = 0; i < numCharacters; i++)
            {
                var characterInfoIndex = text[i];

                if (characterInfoIndex == SDFLookupData.Newline)
                {
                    lineStart -= up;
                    pos = lineStart;
                    continue;
                }

                // 从字体获取字符渲染信息
                SDFCharacter ch = characterInfo[characterInfoIndex];

                int vertexIndexStart = textVertices->Length / UnsafeUtility.SizeOf<TextVertex>();

                float3 v;

                v = pos + ch.vertexTopLeft.x * right + ch.vertexTopLeft.y * up;
                minBounds = math.min(minBounds, v);
                maxBounds = math.max(maxBounds, v);
                Add(textVertices, new TextVertex
                {
                    position = v,
                    uv = ch.uvTopLeft,
                    color = color,
                });

                v = pos + ch.vertexTopRight.x * right + ch.vertexTopRight.y * up;
                minBounds = math.min(minBounds, v);
                maxBounds = math.max(maxBounds, v);
                Add(textVertices, new TextVertex
                {
                    position = v,
                    uv = ch.uvTopRight,
                    color = color,
                });

                v = pos + ch.vertexBottomRight.x * right + ch.vertexBottomRight.y * up;
                minBounds = math.min(minBounds, v);
                maxBounds = math.max(maxBounds, v);
                Add(textVertices, new TextVertex
                {
                    position = v,
                    uv = ch.uvBottomRight,
                    color = color,
                });

                v = pos + ch.vertexBottomLeft.x * right + ch.vertexBottomLeft.y * up;
                minBounds = math.min(minBounds, v);
                maxBounds = math.max(maxBounds, v);
                Add(textVertices, new TextVertex
                {
                    position = v,
                    uv = ch.uvBottomLeft,
                    color = color,
                });

                Add(textTriangles, vertexIndexStart + 0);
                Add(textTriangles, vertexIndexStart + 1);
                Add(textTriangles, vertexIndexStart + 2);

                Add(textTriangles, vertexIndexStart + 0);
                Add(textTriangles, vertexIndexStart + 2);
                Add(textTriangles, vertexIndexStart + 3);

                // 推进字符位置
                pos += right * ch.advance;
            }
        }

        float3 lastNormalizedLineDir;
        float lastLineWidth;

        public const float MaxCirclePixelError = 0.5f;

        public const int VerticesPerCharacter = 4;
        public const int TrianglesPerCharacter = 6;

        void AddLine(LineData line)
        {
            // 将线段方向存储在顶点中。
            // 一条线由 4 个顶点组成。线段方向将用于
            // 偏移顶点以创建固定像素厚度的线条
            var a = PerspectiveDivide(math.mul(currentMatrix, new float4(line.a, 1.0f)));
            var b = PerspectiveDivide(math.mul(currentMatrix, new float4(line.b, 1.0f)));

            float lineWidth = currentLineWidthData.pixels;
            var normalizedLineDir = math.normalizesafe(b - a);

            if (math.any(math.isnan(normalizedLineDir))) throw new Exception("Nan line coordinates");
            if (lineWidth <= 0)
            {
                return;
            }

            // 更新包围盒
            minBounds = math.min(minBounds, math.min(a, b));
            maxBounds = math.max(maxBounds, math.max(a, b));

            unsafe
            {
                var outlineVertices = &buffers->vertices;

                // 确保有足够的分配容量容纳 4 个更多顶点
                Reserve(outlineVertices, 4 * UnsafeUtility.SizeOf<Vertex>());

                // 插入 4 个顶点
                // 使用指针操作更快，这是整个 Gizmo
                // 绘制过程中最热的代码。
                var ptr = (Vertex*)((byte*)outlineVertices->Ptr + outlineVertices->Length);

                var startLineDir = normalizedLineDir * lineWidth;
                var endLineDir = normalizedLineDir * lineWidth;

                // 如果 dot(上一方向, 当前方向) >= 0 => 使用连接
                if (lineWidth > 1 && currentLineWidthData.automaticJoins && outlineVertices->Length > 2 * UnsafeUtility.SizeOf<Vertex>())
                {
                    // 有前一个顶点
                    Vertex* lastVertex1 = (Vertex*)(ptr - 1);
                    Vertex* lastVertex2 = (Vertex*)(ptr - 2);

                    var cosAngle = math.dot(normalizedLineDir, lastNormalizedLineDir);
                    if (math.all(lastVertex2->position == a) && lastLineWidth == lineWidth && cosAngle >= -0.6f)
                    {
                        // 安全：tangent 不可能为 0 因为 cosAngle > -1
                        var tangent = normalizedLineDir + lastNormalizedLineDir;
                        // 由余弦定理得
                        // tangent.magnitude = sqrt(2)*sqrt(1+cosAngle)

                        // 创建连接！
                        // 三角函数给出
                        // joinRadius = lineWidth / (2*cos(alpha / 2))
                        // 使用余弦半角公式得
                        // joinRadius = lineWidth / (sqrt(2)*sqrt(1 + cos(alpha))
                        // 由于 tangent 已经包含大部分相同因子，可以简化计算
                        // normalize(tangent) * joinRadius * 2
                        // = tangent / (sqrt(2)*sqrt(1+cosAngle)) * joinRadius * 2
                        // = tangent * lineWidth / (1 + cos(alpha)
                        var joinLineDir = tangent * lineWidth / (1 + cosAngle);

                        startLineDir = joinLineDir;
                        lastVertex1->uv2 = startLineDir;
                        lastVertex2->uv2 = startLineDir;
                    }
                }

                outlineVertices->Length = outlineVertices->Length + 4 * UnsafeUtility.SizeOf<Vertex>();
                *ptr++ = new Vertex
                {
                    position = a,
                    color = currentColor,
                    uv = new float2(0, 0),
                    uv2 = startLineDir,
                };
                *ptr++ = new Vertex
                {
                    position = a,
                    color = currentColor,
                    uv = new float2(1, 0),
                    uv2 = startLineDir,
                };

                *ptr++ = new Vertex
                {
                    position = b,
                    color = currentColor,
                    uv = new float2(0, 1),
                    uv2 = endLineDir,
                };
                *ptr++ = new Vertex
                {
                    position = b,
                    color = currentColor,
                    uv = new float2(1, 1),
                    uv2 = endLineDir,
                };

                lastNormalizedLineDir = normalizedLineDir;
                lastLineWidth = lineWidth;
            }
        }

        /// <summary>计算在指定点和半径处绘制圆时，为使像素误差小于指定值所需的步数。</summary>
        internal static int CircleSteps(float3 center, float radius, float maxPixelError, ref float4x4 currentMatrix, float2 cameraDepthToPixelSize, float3 cameraPosition)
        {
            var centerv4 = math.mul(currentMatrix, new float4(center, 1.0f));

            if (math.abs(centerv4.w) < 0.0000001f) return 3;
            var cc = PerspectiveDivide(centerv4);
            // 取 3 个轴中最大的缩放因子。
            // 如果当前矩阵是均匀缩放则它们都相同。
            var maxScaleFactor = math.sqrt(math.max(math.max(math.lengthsq(currentMatrix.c0.xyz), math.lengthsq(currentMatrix.c1.xyz)), math.lengthsq(currentMatrix.c2.xyz))) / centerv4.w;
            var realWorldRadius = radius * maxScaleFactor;
            var distance = math.length(cc - cameraPosition);

            var pixelSize = cameraDepthToPixelSize.x * distance + cameraDepthToPixelSize.y;
            // realWorldRadius += pixelSize * this.currentLineWidthData.pixels * 0.5f;
            var cosAngle = 1 - (maxPixelError * pixelSize) / realWorldRadius;
            int steps = cosAngle < 0 ? 3 : (int)math.ceil(math.PI / (math.acos(cosAngle)));
            return steps;
        }

        void AddCircle(CircleData circle)
        {
            // 如果圆的法线为零则忽略
            if (math.all(circle.normal == 0)) return;

            circle.normal = math.normalize(circle.normal);
            // 规范化
            if (circle.normal.y < 0) circle.normal = -circle.normal;

            float3 tangent1;
            if (math.all(math.abs(circle.normal - new float3(0, 1, 0)) < 0.001f))
            {
                // 法线（几乎）等于 (0, 1, 0)
                tangent1 = new float3(0, 0, 1);
            }
            else
            {
                // 常见情况
                tangent1 = math.normalizesafe(math.cross(circle.normal, new float3(0, 1, 0)));
            }

            var ex = tangent1;
            var ey = circle.normal;
            var ez = math.cross(ey, ex);
            var oldMatrix = currentMatrix;

            currentMatrix = math.mul(currentMatrix, new float4x4(
                new float4(ex, 0) * circle.radius,
                new float4(ey, 0) * circle.radius,
                new float4(ez, 0) * circle.radius,
                new float4(circle.center, 1)
                ));

            AddCircle(new CircleXZData
            {
                center = new float3(0, 0, 0),
                radius = 1,
                startAngle = 0,
                endAngle = 2 * math.PI,
            });

            currentMatrix = oldMatrix;
        }

        void AddDisc(CircleData circle)
        {
            // 如果圆的法线为零则忽略
            if (math.all(circle.normal == 0)) return;

            var steps = CircleSteps(circle.center, circle.radius, maxPixelError, ref currentMatrix, cameraDepthToPixelSize, cameraPosition);

            circle.normal = math.normalize(circle.normal);
            float3 tangent1;
            if (math.all(math.abs(circle.normal - new float3(0, 1, 0)) < 0.001f))
            {
                // 法线（几乎）等于 (0, 1, 0)
                tangent1 = new float3(0, 0, 1);
            }
            else
            {
                // 常见情况
                tangent1 = math.cross(circle.normal, new float3(0, 1, 0));
            }

            float invSteps = 1.0f / steps;

            unsafe
            {
                var solidVertices = &buffers->solidVertices;
                var solidTriangles = &buffers->solidTriangles;
                Reserve(solidVertices, steps * UnsafeUtility.SizeOf<Vertex>());
                Reserve(solidTriangles, 3 * (steps - 2) * UnsafeUtility.SizeOf<int>());

                var matrix = math.mul(currentMatrix, Matrix4x4.TRS(circle.center, Quaternion.LookRotation(circle.normal, tangent1), new Vector3(circle.radius, circle.radius, circle.radius)));

                var mn = minBounds;
                var mx = maxBounds;
                int vertexCount = solidVertices->Length / UnsafeUtility.SizeOf<Vertex>();

                for (int i = 0; i < steps; i++)
                {
                    var t = math.lerp(0, 2 * Mathf.PI, i * invSteps);
                    math.sincos(t, out float sin, out float cos);

                    var p = PerspectiveDivide(math.mul(matrix, new float4(cos, sin, 0, 1)));
                    // 更新包围盒
                    mn = math.min(mn, p);
                    mx = math.max(mx, p);

                    Add(solidVertices, new Vertex
                    {
                        position = p,
                        color = currentColor,
                        uv = new float2(0, 0),
                        uv2 = new float3(0, 0, 0),
                    });
                }

                minBounds = mn;
                maxBounds = mx;

                for (int i = 0; i < steps - 2; i++)
                {
                    Add(solidTriangles, vertexCount);
                    Add(solidTriangles, vertexCount + i + 1);
                    Add(solidTriangles, vertexCount + i + 2);
                }
            }
        }

        void AddSphereOutline(SphereData circle)
        {
            var centerv4 = math.mul(currentMatrix, new float4(circle.center, 1.0f));

            if (math.abs(centerv4.w) < 0.0000001f) return;
            var center = PerspectiveDivide(centerv4);
            // 计算经过所有矩阵变换后球体的实际半径。
            // 非均匀缩放时，选择最大半径
            var maxScaleFactor = math.sqrt(math.max(math.max(math.lengthsq(currentMatrix.c0.xyz), math.lengthsq(currentMatrix.c1.xyz)), math.lengthsq(currentMatrix.c2.xyz))) / centerv4.w;
            var realWorldRadius = circle.radius * maxScaleFactor;

            if (cameraIsOrthographic)
            {
                var prevMatrix = this.currentMatrix;
                this.currentMatrix = float4x4.identity;
                AddCircle(new CircleData
                {
                    center = center,
                    normal = math.mul(this.cameraRotation, new float3(0, 0, 1)),
                    radius = realWorldRadius,
                });
                this.currentMatrix = prevMatrix;
            }
            else
            {
                var dist = math.length(this.cameraPosition - center);
                // 相机在球体内部，无法绘制
                if (dist <= realWorldRadius) return;

                var offsetTowardsCamera = realWorldRadius * realWorldRadius / dist;
                var outlineRadius = math.sqrt(realWorldRadius * realWorldRadius - offsetTowardsCamera * offsetTowardsCamera);
                var normal = math.normalize(this.cameraPosition - center);
                var prevMatrix = this.currentMatrix;
                this.currentMatrix = float4x4.identity;
                AddCircle(new CircleData
                {
                    center = center + normal * offsetTowardsCamera,
                    normal = normal,
                    radius = outlineRadius,
                });
                this.currentMatrix = prevMatrix;
            }
        }

        void AddCircle(CircleXZData circle)
        {
            circle.endAngle = math.clamp(circle.endAngle, circle.startAngle - Mathf.PI * 2, circle.startAngle + Mathf.PI * 2);

            unsafe
            {
                var m = math.mul(currentMatrix, new float4x4(
                    new float4(circle.radius, 0, 0, 0),
                    new float4(0, circle.radius, 0, 0),
                    new float4(0, 0, circle.radius, 0),
                    new float4(circle.center, 1)
                    ));
                var steps = CircleSteps(float3.zero, 1.0f, maxPixelError, ref m, cameraDepthToPixelSize, cameraPosition);
                var lineWidth = currentLineWidthData.pixels;
                if (lineWidth < 0) return;

                var byteSize = steps * 4 * UnsafeUtility.SizeOf<Vertex>();
                Reserve(&buffers->vertices, byteSize);
                var ptr = (Vertex*)(buffers->vertices.Ptr + buffers->vertices.Length);
                buffers->vertices.Length += byteSize;
                math.sincos(circle.startAngle, out float sin0, out float cos0);
                var prev = PerspectiveDivide(math.mul(m, new float4(cos0, 0, sin0, 1)));
                var prevTangent = math.normalizesafe(math.mul(m, new float4(-sin0, 0, cos0, 0)).xyz) * lineWidth;
                var invSteps = math.rcp(steps);

                for (int i = 1; i <= steps; i++)
                {
                    var t = math.lerp(circle.startAngle, circle.endAngle, i * invSteps);
                    math.sincos(t, out float sin, out float cos);
                    var next = PerspectiveDivide(math.mul(m, new float4(cos, 0, sin, 1)));
                    var tangent = math.normalizesafe(math.mul(m, new float4(-sin, 0, cos, 0)).xyz) * lineWidth;
                    *ptr++ = new Vertex
                    {
                        position = prev,
                        color = currentColor,
                        uv = new float2(0, 0),
                        uv2 = prevTangent,
                    };
                    *ptr++ = new Vertex
                    {
                        position = prev,
                        color = currentColor,
                        uv = new float2(1, 0),
                        uv2 = prevTangent,
                    };
                    *ptr++ = new Vertex
                    {
                        position = next,
                        color = currentColor,
                        uv = new float2(0, 1),
                        uv2 = tangent,
                    };
                    *ptr++ = new Vertex
                    {
                        position = next,
                        color = currentColor,
                        uv = new float2(1, 1),
                        uv2 = tangent,
                    };

                    prev = next;
                    prevTangent = tangent;
                }

                // 用圆的包围盒更新全局边界
                var b0 = PerspectiveDivide(math.mul(m, new float4(-1, 0, 0, 1)));
                var b1 = PerspectiveDivide(math.mul(m, new float4(0, -1, 0, 1)));
                var b2 = PerspectiveDivide(math.mul(m, new float4(+1, 0, 0, 1)));
                var b3 = PerspectiveDivide(math.mul(m, new float4(0, +1, 0, 1)));
                minBounds = math.min(math.min(math.min(math.min(b0, b1), b2), b3), minBounds);
                maxBounds = math.max(math.max(math.max(math.max(b0, b1), b2), b3), maxBounds);
            }
        }

        void AddDisc(CircleXZData circle)
        {
            var steps = CircleSteps(circle.center, circle.radius, maxPixelError, ref currentMatrix, cameraDepthToPixelSize, cameraPosition);

            circle.endAngle = math.clamp(circle.endAngle, circle.startAngle - Mathf.PI * 2, circle.startAngle + Mathf.PI * 2);

            float invSteps = 1.0f / steps;

            unsafe
            {
                var solidVertices = &buffers->solidVertices;
                var solidTriangles = &buffers->solidTriangles;
                Reserve(solidVertices, (2 + steps) * UnsafeUtility.SizeOf<Vertex>());
                Reserve(solidTriangles, 3 * steps * UnsafeUtility.SizeOf<int>());

                var matrix = math.mul(currentMatrix, Matrix4x4.Translate(circle.center) * Matrix4x4.Scale(new Vector3(circle.radius, circle.radius, circle.radius)));

                var worldCenter = PerspectiveDivide(math.mul(matrix, new float4(0, 0, 0, 1)));
                Add(solidVertices, new Vertex
                {
                    position = worldCenter,
                    color = currentColor,
                    uv = new float2(0, 0),
                    uv2 = new float3(0, 0, 0),
                });

                var mn = math.min(minBounds, worldCenter);
                var mx = math.max(maxBounds, worldCenter);
                int vertexCount = solidVertices->Length / UnsafeUtility.SizeOf<Vertex>();

                for (int i = 0; i <= steps; i++)
                {
                    var t = math.lerp(circle.startAngle, circle.endAngle, i * invSteps);
                    math.sincos(t, out float sin, out float cos);

                    var p = PerspectiveDivide(math.mul(matrix, new float4(cos, 0, sin, 1)));
                    // 更新包围盒
                    mn = math.min(mn, p);
                    mx = math.max(mx, p);

                    Add(solidVertices, new Vertex
                    {
                        position = p,
                        color = currentColor,
                        uv = new float2(0, 0),
                        uv2 = new float3(0, 0, 0),
                    });
                }

                minBounds = mn;
                maxBounds = mx;

                for (int i = 0; i < steps; i++)
                {
                    // 中心顶点
                    Add(solidTriangles, vertexCount - 1);
                    Add(solidTriangles, vertexCount + i + 0);
                    Add(solidTriangles, vertexCount + i + 1);
                }
            }
        }

        void AddSolidTriangle(TriangleData triangle)
        {
            unsafe
            {
                var solidVertices = &buffers->solidVertices;
                var solidTriangles = &buffers->solidTriangles;
                Reserve(solidVertices, 3 * UnsafeUtility.SizeOf<Vertex>());
                Reserve(solidTriangles, 3 * UnsafeUtility.SizeOf<int>());
                var matrix = currentMatrix;
                var a = PerspectiveDivide(math.mul(matrix, new float4(triangle.a, 1)));
                var b = PerspectiveDivide(math.mul(matrix, new float4(triangle.b, 1)));
                var c = PerspectiveDivide(math.mul(matrix, new float4(triangle.c, 1)));
                int startVertex = solidVertices->Length / UnsafeUtility.SizeOf<Vertex>();

                minBounds = math.min(math.min(math.min(minBounds, a), b), c);
                maxBounds = math.max(math.max(math.max(maxBounds, a), b), c);

                Add(solidVertices, new Vertex
                {
                    position = a,
                    color = currentColor,
                    uv = new float2(0, 0),
                    uv2 = new float3(0, 0, 0),
                });
                Add(solidVertices, new Vertex
                {
                    position = b,
                    color = currentColor,
                    uv = new float2(0, 0),
                    uv2 = new float3(0, 0, 0),
                });
                Add(solidVertices, new Vertex
                {
                    position = c,
                    color = currentColor,
                    uv = new float2(0, 0),
                    uv2 = new float3(0, 0, 0),
                });

                Add(solidTriangles, startVertex + 0);
                Add(solidTriangles, startVertex + 1);
                Add(solidTriangles, startVertex + 2);
            }
        }

        void AddWireBox(BoxData box)
        {
            var min = box.center - box.size * 0.5f;
            var max = box.center + box.size * 0.5f;
            AddLine(new LineData { a = new float3(min.x, min.y, min.z), b = new float3(max.x, min.y, min.z) });
            AddLine(new LineData { a = new float3(max.x, min.y, min.z), b = new float3(max.x, min.y, max.z) });
            AddLine(new LineData { a = new float3(max.x, min.y, max.z), b = new float3(min.x, min.y, max.z) });
            AddLine(new LineData { a = new float3(min.x, min.y, max.z), b = new float3(min.x, min.y, min.z) });

            AddLine(new LineData { a = new float3(min.x, max.y, min.z), b = new float3(max.x, max.y, min.z) });
            AddLine(new LineData { a = new float3(max.x, max.y, min.z), b = new float3(max.x, max.y, max.z) });
            AddLine(new LineData { a = new float3(max.x, max.y, max.z), b = new float3(min.x, max.y, max.z) });
            AddLine(new LineData { a = new float3(min.x, max.y, max.z), b = new float3(min.x, max.y, min.z) });

            AddLine(new LineData { a = new float3(min.x, min.y, min.z), b = new float3(min.x, max.y, min.z) });
            AddLine(new LineData { a = new float3(max.x, min.y, min.z), b = new float3(max.x, max.y, min.z) });
            AddLine(new LineData { a = new float3(max.x, min.y, max.z), b = new float3(max.x, max.y, max.z) });
            AddLine(new LineData { a = new float3(min.x, min.y, max.z), b = new float3(min.x, max.y, max.z) });
        }

        void AddPlane(PlaneData plane)
        {
            var oldMatrix = currentMatrix;

            currentMatrix = math.mul(currentMatrix, float4x4.TRS(plane.center, plane.rotation, new float3(plane.size.x * 0.5f, 1, plane.size.y * 0.5f)));

            AddLine(new LineData { a = new float3(-1, 0, -1), b = new float3(1, 0, -1) });
            AddLine(new LineData { a = new float3(1, 0, -1), b = new float3(1, 0, 1) });
            AddLine(new LineData { a = new float3(1, 0, 1), b = new float3(-1, 0, 1) });
            AddLine(new LineData { a = new float3(-1, 0, 1), b = new float3(-1, 0, -1) });

            currentMatrix = oldMatrix;
        }

        internal static readonly float4[] BoxVertices = {
            new float4(-1, -1, -1, 1),
            new float4(-1, -1, +1, 1),
            new float4(-1, +1, -1, 1),
            new float4(-1, +1, +1, 1),
            new float4(+1, -1, -1, 1),
            new float4(+1, -1, +1, 1),
            new float4(+1, +1, -1, 1),
            new float4(+1, +1, +1, 1),
        };

        internal static readonly int[] BoxTriangles = {
            // 底部两个三角形
            0, 1, 5,
            0, 5, 4,

            // 顶部
            7, 3, 2,
            7, 2, 6,

            // -X
            0, 1, 3,
            0, 3, 2,

            // +X
            4, 5, 7,
            4, 7, 6,

            // +Z
            1, 3, 7,
            1, 7, 5,

            // -Z
            0, 2, 6,
            0, 6, 4,
        };

        void AddBox(BoxData box)
        {
            unsafe
            {
                var solidVertices = &buffers->solidVertices;
                var solidTriangles = &buffers->solidTriangles;
                Reserve(solidVertices, BoxVertices.Length * UnsafeUtility.SizeOf<Vertex>());
                Reserve(solidTriangles, BoxTriangles.Length * UnsafeUtility.SizeOf<int>());

                var scale = box.size * 0.5f;
                var matrix = math.mul(currentMatrix, new float4x4(
                    new float4(scale.x, 0, 0, 0),
                    new float4(0, scale.y, 0, 0),
                    new float4(0, 0, scale.z, 0),
                    new float4(box.center, 1)
                    ));

                var mn = minBounds;
                var mx = maxBounds;
                int vertexOffset = solidVertices->Length / UnsafeUtility.SizeOf<Vertex>();
                var ptr = (Vertex*)(solidVertices->Ptr + solidVertices->Length);
                for (int i = 0; i < BoxVertices.Length; i++)
                {
                    var p = PerspectiveDivide(math.mul(matrix, BoxVertices[i]));
                    // 更新包围盒
                    mn = math.min(mn, p);
                    mx = math.max(mx, p);

                    *ptr++ = new Vertex
                    {
                        position = p,
                        color = currentColor,
                        uv = new float2(0, 0),
                        uv2 = new float3(0, 0, 0),
                    };
                }
                solidVertices->Length += BoxVertices.Length * UnsafeUtility.SizeOf<Vertex>();

                minBounds = mn;
                maxBounds = mx;

                var triPtr = (int*)(solidTriangles->Ptr + solidTriangles->Length);
                for (int i = 0; i < BoxTriangles.Length; i++)
                {
                    *triPtr++ = vertexOffset + BoxTriangles[i];
                }
                solidTriangles->Length += BoxTriangles.Length * UnsafeUtility.SizeOf<int>();
            }
        }

        // 使用 AggressiveInlining 因为只在一个位置调用，否则 Burst 不会内联
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Next(ref UnsafeAppendBuffer.Reader reader, ref NativeArray<float4x4> matrixStack, ref NativeArray<Color32> colorStack, ref NativeArray<LineWidthData> lineWidthStack, ref int matrixStackSize, ref int colorStackSize, ref int lineWidthStackSize)
        {
            var fullCmd = reader.ReadNext<Command>();
            var cmd = fullCmd & (Command)0xFF;
            Color32 oldColor = default;

            if ((fullCmd & Command.PushColorInline) != 0)
            {
                oldColor = currentColor;
                currentColor = reader.ReadNext<Color32>();
            }

            switch (cmd)
            {
                case Command.PushColor:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (colorStackSize >= colorStack.Length) throw new System.Exception("Too deeply nested PushColor calls");
#else
                if (colorStackSize >= colorStack.Length) colorStackSize--;
#endif
                    colorStack[colorStackSize] = currentColor;
                    colorStackSize++;
                    currentColor = reader.ReadNext<Color32>();
                    break;
                case Command.PopColor:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (colorStackSize <= 0) throw new System.Exception("PushColor and PopColor are not matched");
#else
                if (colorStackSize <= 0) break;
#endif
                    colorStackSize--;
                    currentColor = colorStack[colorStackSize];
                    break;
                case Command.PushMatrix:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (matrixStackSize >= matrixStack.Length) throw new System.Exception("Too deeply nested PushMatrix calls");
#else
                if (matrixStackSize >= matrixStack.Length) matrixStackSize--;
#endif
                    matrixStack[matrixStackSize] = currentMatrix;
                    matrixStackSize++;
                    currentMatrix = math.mul(currentMatrix, reader.ReadNext<float4x4>());
                    break;
                case Command.PushSetMatrix:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (matrixStackSize >= matrixStack.Length) throw new System.Exception("Too deeply nested PushMatrix calls");
#else
                if (matrixStackSize >= matrixStack.Length) matrixStackSize--;
#endif
                    matrixStack[matrixStackSize] = currentMatrix;
                    matrixStackSize++;
                    currentMatrix = reader.ReadNext<float4x4>();
                    break;
                case Command.PopMatrix:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (matrixStackSize <= 0) throw new System.Exception("PushMatrix and PopMatrix are not matched");
#else
                if (matrixStackSize <= 0) break;
#endif
                    matrixStackSize--;
                    currentMatrix = matrixStack[matrixStackSize];
                    break;
                case Command.PushLineWidth:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (lineWidthStackSize >= lineWidthStack.Length) throw new System.Exception("Too deeply nested PushLineWidth calls");
#else
                if (lineWidthStackSize >= lineWidthStack.Length) lineWidthStackSize--;
#endif
                    lineWidthStack[lineWidthStackSize] = currentLineWidthData;
                    lineWidthStackSize++;
                    currentLineWidthData = reader.ReadNext<LineWidthData>();
                    currentLineWidthData.pixels *= lineWidthMultiplier;
                    break;
                case Command.PopLineWidth:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (lineWidthStackSize <= 0) throw new System.Exception("PushLineWidth and PopLineWidth are not matched");
#else
                if (lineWidthStackSize <= 0) break;
#endif
                    lineWidthStackSize--;
                    currentLineWidthData = lineWidthStack[lineWidthStackSize];
                    break;
                case Command.Line:
                    AddLine(reader.ReadNext<LineData>());
                    break;
                case Command.SphereOutline:
                    AddSphereOutline(reader.ReadNext<SphereData>());
                    break;
                case Command.CircleXZ:
                    AddCircle(reader.ReadNext<CircleXZData>());
                    break;
                case Command.Circle:
                    AddCircle(reader.ReadNext<CircleData>());
                    break;
                case Command.DiscXZ:
                    AddDisc(reader.ReadNext<CircleXZData>());
                    break;
                case Command.Disc:
                    AddDisc(reader.ReadNext<CircleData>());
                    break;
                case Command.Box:
                    AddBox(reader.ReadNext<BoxData>());
                    break;
                case Command.WirePlane:
                    AddPlane(reader.ReadNext<PlaneData>());
                    break;
                case Command.WireBox:
                    AddWireBox(reader.ReadNext<BoxData>());
                    break;
                case Command.SolidTriangle:
                    AddSolidTriangle(reader.ReadNext<TriangleData>());
                    break;
                case Command.PushPersist:
                    // 此命令不需要由构建器处理
                    reader.ReadNext<PersistData>();
                    break;
                case Command.PopPersist:
                    // 此命令不需要由构建器处理
                    break;
                case Command.Text:
                    var data = reader.ReadNext<TextData>();
                    unsafe
                    {
                        System.UInt16* ptr = (System.UInt16*)reader.ReadNext(UnsafeUtility.SizeOf<System.UInt16>() * data.numCharacters);
                        AddText(ptr, data, currentColor);
                    }
                    break;
                case Command.Text3D:
                    var data2 = reader.ReadNext<TextData3D>();
                    unsafe
                    {
                        System.UInt16* ptr = (System.UInt16*)reader.ReadNext(UnsafeUtility.SizeOf<System.UInt16>() * data2.numCharacters);
                        AddText3D(ptr, data2, currentColor);
                    }
                    break;
                case Command.CaptureState:
                    unsafe
                    {
                        buffers->capturedState.Add(new ProcessedBuilderData.CapturedState
                        {
                            color = this.currentColor,
                            matrix = this.currentMatrix,
                        });
                    }
                    break;
                default:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    throw new System.Exception("Unknown command");
#else
                break;
#endif
            }

            if ((fullCmd & Command.PushColorInline) != 0)
            {
                currentColor = oldColor;
            }
        }

        void CreateTriangles()
        {
            // 为所有线条创建三角形
            // 一个三角形由 3 个索引组成
            // 一条线（4 个顶点）由 2 个三角形组成，即 6 个三角形索引
            unsafe
            {
                var outlineVertices = &buffers->vertices;
                var outlineTriangles = &buffers->triangles;
                var vertexCount = outlineVertices->Length / UnsafeUtility.SizeOf<Vertex>();
                // 每条线由 4 个顶点组成
                var lineCount = vertexCount / 4;
                var trianglesSizeInBytes = lineCount * 6 * UnsafeUtility.SizeOf<int>();
                if (trianglesSizeInBytes >= outlineTriangles->Capacity)
                {
                    outlineTriangles->SetCapacity(math.ceilpow2(trianglesSizeInBytes));
                }

                int* ptr = (int*)outlineTriangles->Ptr;
                for (int i = 0, vi = 0; i < lineCount; i++, vi += 4)
                {
                    // 第一个三角形
                    *ptr++ = vi + 0;
                    *ptr++ = vi + 1;
                    *ptr++ = vi + 2;

                    // 第二个三角形
                    *ptr++ = vi + 1;
                    *ptr++ = vi + 3;
                    *ptr++ = vi + 2;
                }
                outlineTriangles->Length = trianglesSizeInBytes;
            }
        }

        public const int MaxStackSize = 32;

        public void Execute()
        {
            unsafe
            {
                buffers->vertices.Reset();
                buffers->triangles.Reset();
                buffers->solidVertices.Reset();
                buffers->solidTriangles.Reset();
                buffers->textVertices.Reset();
                buffers->textTriangles.Reset();
                buffers->capturedState.Reset();
            }

            currentLineWidthData.pixels *= lineWidthMultiplier;

            minBounds = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            maxBounds = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            var matrixStack = new NativeArray<float4x4>(MaxStackSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var colorStack = new NativeArray<Color32>(MaxStackSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var lineWidthStack = new NativeArray<LineWidthData>(MaxStackSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            int matrixStackSize = 0;
            int colorStackSize = 0;
            int lineWidthStackSize = 0;

            CommandBuilderSamplers.MarkerProcessCommands.Begin();
            unsafe
            {
                var reader = buffers->splitterOutput.AsReader();
                while (reader.Offset < reader.Size) Next(ref reader, ref matrixStack, ref colorStack, ref lineWidthStack, ref matrixStackSize, ref colorStackSize, ref lineWidthStackSize);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (reader.Offset != reader.Size) throw new Exception("Didn't reach the end of the buffer");
#endif
            }
            CommandBuilderSamplers.MarkerProcessCommands.End();

            CommandBuilderSamplers.MarkerCreateTriangles.Begin();
            CreateTriangles();
            CommandBuilderSamplers.MarkerCreateTriangles.End();

            unsafe
            {
                var outBounds = &buffers->bounds;
                *outBounds = new Bounds((minBounds + maxBounds) * 0.5f, maxBounds - minBounds);

                if (math.any(math.isnan(outBounds->min)) && (buffers->vertices.Length > 0 || buffers->solidTriangles.Length > 0))
                {
                    // 回退到覆盖所有内容的包围盒
                    *outBounds = new Bounds(Vector3.zero, new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    throw new Exception("NaN bounds. A Draw.* command may have been given NaN coordinates.");
#endif
                }
            }
        }
    }
}

