using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace VisualShape
{
    using static ShapeData;
    using BitPackedMeta = ShapeData.BuilderData.BitPackedMeta;
    using VisualShape.Text;
    using Unity.Profiling;

    /// <summary>
    /// 指定相对于锚点的文本对齐方式。
    ///
    /// <code>
    /// Draw.Label2D(transform.position, "Hello World", 14, LabelAlignment.TopCenter);
    /// </code>
    /// <code>
    /// // 在物体下方 20 像素处绘制标签
    /// Draw.Label2D(transform.position, "Hello World", 14, LabelAlignment.TopCenter.withPixelOffset(0, -20));
    /// </code>
    ///
    /// See: <see cref="Draw.Label2D"/>
    /// See: <see cref="Draw.Label3D"/>
    /// </summary>
    public struct LabelAlignment
    {
        /// <summary>
        /// 文本包围盒上的锚点位置。
        ///
        /// 锚点使用相对坐标指定，其中 (0,0) 是左下角，(1,1) 是右上角。
        /// </summary>
        public float2 relativePivot;
        /// <summary>在屏幕空间中移动文本的量</summary>
        public float2 pixelOffset;

        public static readonly LabelAlignment TopLeft = new LabelAlignment { relativePivot = new float2(0.0f, 1.0f), pixelOffset = new float2(0, 0) };
        public static readonly LabelAlignment MiddleLeft = new LabelAlignment { relativePivot = new float2(0.0f, 0.5f), pixelOffset = new float2(0, 0) };
        public static readonly LabelAlignment BottomLeft = new LabelAlignment { relativePivot = new float2(0.0f, 0.0f), pixelOffset = new float2(0, 0) };
        public static readonly LabelAlignment BottomCenter = new LabelAlignment { relativePivot = new float2(0.5f, 0.0f), pixelOffset = new float2(0, 0) };
        public static readonly LabelAlignment BottomRight = new LabelAlignment { relativePivot = new float2(1.0f, 0.0f), pixelOffset = new float2(0, 0) };
        public static readonly LabelAlignment MiddleRight = new LabelAlignment { relativePivot = new float2(1.0f, 0.5f), pixelOffset = new float2(0, 0) };
        public static readonly LabelAlignment TopRight = new LabelAlignment { relativePivot = new float2(1.0f, 1.0f), pixelOffset = new float2(0, 0) };
        public static readonly LabelAlignment TopCenter = new LabelAlignment { relativePivot = new float2(0.5f, 1.0f), pixelOffset = new float2(0, 0) };
        public static readonly LabelAlignment Center = new LabelAlignment { relativePivot = new float2(0.5f, 0.5f), pixelOffset = new float2(0, 0) };

        /// <summary>
        /// 按指定的像素量在屏幕空间中移动文本。
        ///
        /// <code>
        /// // 在物体下方 20 像素处绘制标签
        /// Draw.Label2D(transform.position, "Hello World", 14, LabelAlignment.TopCenter.withPixelOffset(0, -20));
        /// </code>
        /// </summary>
        public LabelAlignment withPixelOffset(float x, float y)
        {
            return new LabelAlignment
            {
                relativePivot = this.relativePivot,
                pixelOffset = new float2(x, y),
            };
        }
    }

    /// <summary>绘制到命令缓冲区的 Job 的最大允许延迟</summary>
    public enum AllowedDelay
    {
        /// <summary>
        /// 如果 Job 在帧结束时未完成，绘制将阻塞直到完成。
        /// 建议用于大多数预期在单帧内完成的 Job。
        /// </summary>
        EndOfFrame,
        /// <summary>
        /// 无限等待 Job 完成，仅在完成后提交渲染结果。
        /// 建议用于可能需要多帧才能完成的长时间运行的 Job。
        /// </summary>
        Infinite,
    }

    /// <summary>某些静态字段需要在单独的类中，因为 Burst 不支持它们</summary>
    static class CommandBuilderSamplers
    {
        internal static readonly ProfilerMarker MarkerConvert = new ProfilerMarker("Convert");
        internal static readonly ProfilerMarker MarkerSetLayout = new ProfilerMarker("SetLayout");
        internal static readonly ProfilerMarker MarkerUpdateVertices = new ProfilerMarker("UpdateVertices");
        internal static readonly ProfilerMarker MarkerUpdateIndices = new ProfilerMarker("UpdateIndices");
        internal static readonly ProfilerMarker MarkerSubmesh = new ProfilerMarker("Submesh");
        internal static readonly ProfilerMarker MarkerUpdateBuffer = new ProfilerMarker("UpdateComputeBuffer");

        internal static readonly ProfilerMarker MarkerProcessCommands = new ProfilerMarker("Commands");
        internal static readonly ProfilerMarker MarkerCreateTriangles = new ProfilerMarker("CreateTriangles");
    }

    /// <summary>
    /// 绘制命令的构建器。
    /// 可以使用此类排队多个绘制命令。调用 Dispose 方法时命令将排队渲染。
    /// 建议使用 <a href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement">using 语句</a>，它会自动调用 Dispose 方法。
    ///
    /// <code>
    /// // 创建一个新的 CommandBuilder
    /// using (var draw = ShapeManager.GetBuilder()) {
    ///     // 使用与全局 Draw 类完全相同的 API
    ///     draw.WireBox(Vector3.zero, Vector3.one);
    /// }
    /// </code>
    ///
    /// 警告：使用完此对象后必须调用 <see cref="Dispose"/> 或 <see cref="DiscardAndDispose"/> 以避免内存泄漏。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile]
    public partial struct CommandBuilder : IDisposable
    {
        // 注意：许多字段/方法被显式标记为 private。因为 doxygen 默认认为它们是 public 的（就像 C++ 中的结构体成员）

        [NativeDisableUnsafePtrRestriction]
        internal unsafe UnsafeAppendBuffer* buffer;

        private GCHandle gizmos;

        [NativeSetThreadIndex]
        private int threadIndex;

        private ShapeData.BuilderData.BitPackedMeta uniqueID;

        internal unsafe CommandBuilder(UnsafeAppendBuffer* buffer, GCHandle gizmos, int threadIndex, ShapeData.BuilderData.BitPackedMeta uniqueID)
        {
            this.buffer = buffer;
            this.gizmos = gizmos;
            this.threadIndex = threadIndex;
            this.uniqueID = uniqueID;
        }


        internal CommandBuilder(ShapeData gizmos, Hasher hasher, RedrawScope frameRedrawScope, RedrawScope customRedrawScope, bool isGizmos, bool isBuiltInCommandBuilder, int sceneModeVersion)
        {
            // 我们需要使用 GCHandle 而不是普通引用，以便将此对象传递给 Burst 编译的函数指针。
            // 遗憾的是 NativeSetClassTypeToNullOnSchedule 只能与 Job 系统配合使用，不能与原始函数一起使用。
            this.gizmos = GCHandle.Alloc(gizmos, GCHandleType.Normal);

            threadIndex = 0;
            uniqueID = gizmos.data.Reserve(isBuiltInCommandBuilder);
            gizmos.data.Get(uniqueID).Init(hasher, frameRedrawScope, customRedrawScope, isGizmos, gizmos.GetNextDrawOrderIndex(), sceneModeVersion);
            unsafe
            {
                buffer = gizmos.data.Get(uniqueID).bufferPtr;
            }
        }

        internal unsafe int BufferSize
        {
            get
            {
                return buffer->Length;
            }
            set
            {
                buffer->Length = value;
            }
        }

        /// <summary>
        /// 在 XY 平面绘制的包装器。
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
        /// See: <see cref="Draw.xz"/>
        /// </summary>
        public CommandBuilder2D xy => new CommandBuilder2D(this, true);

        /// <summary>
        /// 在 XZ 平面绘制的包装器。
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
        /// </summary>
        public CommandBuilder2D xz => new CommandBuilder2D(this, false);

        static readonly float3 DEFAULT_UP = new float3(0, 1, 0);

        /// <summary>
        /// 可以设置为专门渲染到这些相机。
        /// 如果将此属性设置为相机数组，则此命令构建器仅会渲染
        /// 到指定的相机。设置此属性将绕过 <see cref="VisualShape.ShapeManager.allowRenderToRenderTextures"/>。
        /// 即使相机渲染到 RenderTexture 也会被渲染。
        ///
        /// null 值表示应渲染到所有有效相机。这是默认值。
        ///
        /// <code>
        /// var draw = ShapeManager.GetBuilder(true);
        ///
        /// draw.cameraTargets = new Camera[] { myCamera };
        /// // 此球体仅会渲染到 myCamera
        /// draw.WireSphere(Vector3.zero, 0.5f, Color.black);
        /// draw.Dispose();
        /// </code>
        ///
        /// See: advanced (view in online documentation for working links)
        /// </summary>
        public Camera[] cameraTargets
        {
            get
            {
                if (gizmos.IsAllocated && gizmos.Target != null)
                {
                    var target = gizmos.Target as ShapeData;
                    if (target.data.StillExists(uniqueID))
                    {
                        return target.data.Get(uniqueID).meta.cameraTargets;
                    }
                }
                throw new System.Exception("Cannot get cameraTargets because the command builder has already been disposed or does not exist.");
            }
            set
            {
                if (uniqueID.isBuiltInCommandBuilder) throw new System.Exception("You cannot set the camera targets for a built-in command builder. Create a custom command builder instead.");
                if (gizmos.IsAllocated && gizmos.Target != null)
                {
                    var target = gizmos.Target as ShapeData;
                    if (!target.data.StillExists(uniqueID))
                    {
                        throw new System.Exception("Cannot set cameraTargets because the command builder has already been disposed or does not exist.");
                    }
                    target.data.Get(uniqueID).meta.cameraTargets = value;
                }
            }
        }

        /// <summary>提交此命令构建器进行渲染</summary>
        public void Dispose()
        {
            if (uniqueID.isBuiltInCommandBuilder) throw new System.Exception("You cannot dispose a built-in command builder");
            DisposeInternal();
        }

        /// <summary>
        /// 在给定的 Job 完成后释放此命令构建器。
        ///
        /// 如果你使用 Unity 的 ECS/Burst 且不确定 Job 何时完成，这很方便。
        ///
        /// 你将无法再在主线程使用此命令构建器。
        ///
        /// See: job-system (view in online documentation for working links)
        /// </summary>
        /// <param name="dependency">必须在此命令构建器释放前完成的 Job。</param>
        /// <param name="allowedDelay">是否在渲染当前帧前阻塞等待此依赖完成。
        ///    如果 Job 预计在单帧内完成，保持默认的 \reflink{AllowedDelay.EndOfFrame}。
        ///    但如果 Job 预计需要多帧完成，可以设置为 \reflink{AllowedDelay.Infinite}。</param>
        public void DisposeAfter(JobHandle dependency, AllowedDelay allowedDelay = AllowedDelay.EndOfFrame)
        {
            if (!gizmos.IsAllocated) throw new System.Exception("You cannot dispose an invalid command builder. Are you trying to dispose it twice?");
            try
            {
                if (gizmos.IsAllocated && gizmos.Target != null)
                {
                    var target = gizmos.Target as ShapeData;
                    if (!target.data.StillExists(uniqueID))
                    {
                        throw new System.Exception("Cannot dispose the command builder because the drawing manager has been destroyed");
                    }
                    target.data.Get(uniqueID).SubmitWithDependency(gizmos, dependency, allowedDelay);
                }
            }
            finally
            {
                this = default;
            }
        }

        internal void DisposeInternal()
        {
            if (!gizmos.IsAllocated) throw new System.Exception("You cannot dispose an invalid command builder. Are you trying to dispose it twice?");
            try
            {
                if (gizmos.IsAllocated && gizmos.Target != null)
                {
                    var target = gizmos.Target as ShapeData;
                    if (!target.data.StillExists(uniqueID))
                    {
                        throw new System.Exception("Cannot dispose the command builder because the drawing manager has been destroyed");
                    }
                    target.data.Get(uniqueID).Submit(gizmos.Target as ShapeData);
                }
            }
            finally
            {
                gizmos.Free();
                this = default;
            }
        }

        /// <summary>
        /// 丢弃此命令构建器的内容而不渲染任何东西。
        /// 如果你不打算绘制任何内容（即不调用 <see cref="Dispose"/> 方法），则必须调用此方法以避免
        /// 内存泄漏。
        /// </summary>
        public void DiscardAndDispose()
        {
            if (uniqueID.isBuiltInCommandBuilder) throw new System.Exception("You cannot dispose a built-in command builder");
            DiscardAndDisposeInternal();
        }

        internal void DiscardAndDisposeInternal()
        {
            try
            {
                if (gizmos.IsAllocated && gizmos.Target != null)
                {
                    var target = gizmos.Target as ShapeData;
                    if (!target.data.StillExists(uniqueID))
                    {
                        throw new System.Exception("Cannot dispose the command builder because the drawing manager has been destroyed");
                    }
                    target.data.Release(uniqueID);
                }
            }
            finally
            {
                if (gizmos.IsAllocated) gizmos.Free();
                this = default;
            }
        }

        /// <summary>
        /// 预分配内部缓冲区额外的字节数。
        /// 如果你绘制大量内容，这可以提供轻微的性能提升。
        ///
        /// 注意：仅为当前线程调整缓冲区大小。
        /// </summary>
        public void Preallocate(int size)
        {
            Reserve(size);
        }

        /// <summary>内部渲染命令</summary>
        [System.Flags]
        internal enum Command
        {
            PushColorInline = 1 << 8,
            PushColor = 0,
            PopColor,
            PushMatrix,
            PushSetMatrix,
            PopMatrix,
            Line,
            Circle,
            CircleXZ,
            Disc,
            DiscXZ,
            SphereOutline,
            Box,
            WirePlane,
            WireBox,
            SolidTriangle,
            PushPersist,
            PopPersist,
            Text,
            Text3D,
            PushLineWidth,
            PopLineWidth,
            CaptureState,
        }

        internal struct TriangleData
        {
            public float3 a, b, c;
        }

        /// <summary>存储线条的渲染数据</summary>
        internal struct LineData
        {
            public float3 a, b;
        }

        internal struct LineDataV3
        {
            public Vector3 a, b;
        }

        /// <summary>存储圆的渲染数据</summary>
        internal struct CircleXZData
        {
            public float3 center;
            public float radius, startAngle, endAngle;
        }

        /// <summary>存储圆的渲染数据</summary>
        internal struct CircleData
        {
            public float3 center;
            public float3 normal;
            public float radius;
        }

        /// <summary>存储球体的渲染数据</summary>
        internal struct SphereData
        {
            public float3 center;
            public float radius;
        }

        /// <summary>存储方盒的渲染数据</summary>
        internal struct BoxData
        {
            public float3 center;
            public float3 size;
        }

        internal struct PlaneData
        {
            public float3 center;
            public quaternion rotation;
            public float2 size;
        }

        internal struct PersistData
        {
            public float endTime;
        }

        internal struct LineWidthData
        {
            public float pixels;
            public bool automaticJoins;
        }



        internal struct TextData
        {
            public float3 center;
            public LabelAlignment alignment;
            public float sizeInPixels;
            public int numCharacters;
        }

        internal struct TextData3D
        {
            public float3 center;
            public quaternion rotation;
            public LabelAlignment alignment;
            public float size;
            public int numCharacters;
        }

        /// <summary>确保缓冲区至少还有 N 字节的空间</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void Reserve(int additionalSpace)
        {
            unsafe
            {
                if (Unity.Burst.CompilerServices.Hint.Unlikely(threadIndex >= 0))
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (threadIndex < 0 || threadIndex >= JobsUtility.MaxJobThreadCount) throw new System.Exception("Thread index outside the expected range");
                    if (threadIndex > 0 && uniqueID.isBuiltInCommandBuilder) throw new System.Exception("You should use a custom command builder when using the Unity Job System. Take a look at the documentation for more info.");
                    if (buffer == null) throw new System.Exception("CommandBuilder does not have a valid buffer. Is it properly initialized?");

                    // 利用此包绘制 Gizmos 后缓冲区将为空的事实
                    // 下一个任务是 Unity 将渲染其自身的内部 Gizmos。
                    // 因此我们可以轻松地（且不会有太大性能开销）
                    // 捕获来自 OnDrawGizmos 函数的意外 Draw.* 调用
                    // 通过在第一次 Reserve 调用时进行此检查。
                    AssertNotRendering();
#endif

                    buffer += threadIndex;
                    threadIndex = -1;
                }

                var newLength = buffer->Length + additionalSpace;
                if (newLength > buffer->Capacity)
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    // 理论上每次访问缓冲区都应执行此检查...但那样会有点慢
                    // 此代码最终会捕获错误。
                    AssertBufferExists();
                    const int MAX_BUFFER_SIZE = 1024 * 1024 * 256; // 256 MB
                    if (buffer->Length * 2 > MAX_BUFFER_SIZE)
                    {
                        throw new System.Exception("CommandBuilder buffer is very large. Are you trying to draw things in an infinite loop?");
                    }
#endif
                    buffer->SetCapacity(math.max(newLength, buffer->Length * 2));
                }
            }
        }

        [BurstDiscard]
        private void AssertBufferExists()
        {
            if (!gizmos.IsAllocated || gizmos.Target == null || !(gizmos.Target as ShapeData).data.StillExists(uniqueID))
            {
                // 此命令构建器无效，清除所有数据以防止再次使用
                this = default;
                throw new System.Exception("This command builder no longer exists. Are you trying to draw to a command builder which has already been disposed?");
            }
        }

        [BurstDiscard]
        static void AssertNotRendering()
        {
            // 检查是否从 OnDrawGizmos 内部进行绘制
            // 此检查相对较快（约 0.05 毫秒），但出于性能考虑仅每 128 帧执行一次
            if (!GizmoContext.drawingGizmos && !JobsUtility.IsExecutingJob && (Time.renderedFrameCount & 127) == 0)
            {
                // 检查堆栈跟踪以提供更有帮助的错误信息
                var st = StackTraceUtility.ExtractStackTrace();
                if (st.Contains("OnDrawGizmos"))
                {
                    throw new System.Exception("You are trying to use Draw.* functions from within Unity's OnDrawGizmos function. Use this package's gizmo callbacks instead (see the documentation).");
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void Reserve<A>() where A : struct
        {
            Reserve(UnsafeUtility.SizeOf<Command>() + UnsafeUtility.SizeOf<A>());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void Reserve<A, B>() where A : struct where B : struct
        {
            Reserve(UnsafeUtility.SizeOf<Command>() * 2 + UnsafeUtility.SizeOf<A>() + UnsafeUtility.SizeOf<B>());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void Reserve<A, B, C>() where A : struct where B : struct where C : struct
        {
            Reserve(UnsafeUtility.SizeOf<Command>() * 3 + UnsafeUtility.SizeOf<A>() + UnsafeUtility.SizeOf<B>() + UnsafeUtility.SizeOf<C>());
        }

        /// <summary>
        /// 将 Color 转换为 Color32。
        /// 此方法比 Unity 的原生颜色转换更快，尤其在使用 Burst 时。
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static unsafe uint ConvertColor(Color color)
        {
            // 如果支持 SSE2（基本上所有 X86 CPU 都支持）
            // 则可以使用更快的 Color 到 Color32 转换。
            // 这仅在 Burst 内部可行。
            if (Unity.Burst.Intrinsics.X86.Sse2.IsSse2Supported)
            {
                // 从 0-1 浮点范围转换到 0-255 整数范围
                var ci = (int4)(255 * new float4(color.r, color.g, color.b, color.a) + 0.5f);
                var v32 = new Unity.Burst.Intrinsics.v128(ci.x, ci.y, ci.z, ci.w);
                // 将四个 32 位数转换为四个 16 位数
                var v16 = Unity.Burst.Intrinsics.X86.Sse2.packs_epi32(v32, v32);
                // 将四个 16 位数转换为四个 8 位数
                var v8 = Unity.Burst.Intrinsics.X86.Sse2.packus_epi16(v16, v16);
                return v8.UInt0;
            }
            else
            {
                // 如果没有 SSE2（很可能不在 Burst 内运行），
                // 则手动进行 Color 到 Color32 的转换。
                // 这比直接转换为 Color32 快得多。
                var r = (uint)Mathf.Clamp((int)(color.r * 255f + 0.5f), 0, 255);
                var g = (uint)Mathf.Clamp((int)(color.g * 255f + 0.5f), 0, 255);
                var b = (uint)Mathf.Clamp((int)(color.b * 255f + 0.5f), 0, 255);
                var a = (uint)Mathf.Clamp((int)(color.a * 255f + 0.5f), 0, 255);
                return (a << 24) | (b << 16) | (g << 8) | r;
            }
        }

        internal unsafe void Add<T>(T value) where T : struct
        {
            int num = UnsafeUtility.SizeOf<T>();
            var buffer = this.buffer;
            var bufferSize = buffer->Length;
            // 我们假设这一点，因为 Reserve 函数已经处理了。
            // 这在 Burst 运行时从汇编中移除了一些分支。
            Unity.Burst.CompilerServices.Hint.Assume(buffer->Ptr != null);
            Unity.Burst.CompilerServices.Hint.Assume(buffer->Ptr + bufferSize != null);

            unsafe
            {
                UnsafeUtility.CopyStructureToPtr(ref value, (void*)((byte*)buffer->Ptr + bufferSize));
                buffer->Length = bufferSize + num;
            }
        }

        public struct ScopeMatrix : IDisposable
        {
            internal CommandBuilder builder;
            public void Dispose()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!builder.gizmos.IsAllocated || !(builder.gizmos.Target is ShapeData data) || !data.data.StillExists(builder.uniqueID)) throw new System.InvalidOperationException("The drawing instance this matrix scope belongs to no longer exists. Matrix scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a matrix scope inside a coroutine?");
#endif
                unsafe
                {
                    builder.PopMatrix();
                    builder.buffer = null;
                }
            }
        }

        public struct ScopeColor : IDisposable
        {
            internal CommandBuilder builder;
            public void Dispose()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!builder.gizmos.IsAllocated || !(builder.gizmos.Target is ShapeData data) || !data.data.StillExists(builder.uniqueID)) throw new System.InvalidOperationException("The drawing instance this color scope belongs to no longer exists. Color scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a color scope inside a coroutine?");
#endif
                unsafe
                {
                    builder.PopColor();
                    builder.buffer = null;
                }
            }
        }

        public struct ScopePersist : IDisposable
        {
            internal CommandBuilder builder;
            public void Dispose()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!builder.gizmos.IsAllocated || !(builder.gizmos.Target is ShapeData data) || !data.data.StillExists(builder.uniqueID)) throw new System.InvalidOperationException("The drawing instance this persist scope belongs to no longer exists. Persist scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a persist scope inside a coroutine?");
#endif
                unsafe
                {
                    builder.PopDuration();
                    builder.buffer = null;
                }
            }
        }

        /// <summary>
        /// 不执行任何操作的作用域。
        /// 用于独立构建中的优化。
        /// </summary>
        public struct ScopeEmpty : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public struct ScopeLineWidth : IDisposable
        {
            internal CommandBuilder builder;
            public void Dispose()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!builder.gizmos.IsAllocated || !(builder.gizmos.Target is ShapeData data) || !data.data.StillExists(builder.uniqueID)) throw new System.InvalidOperationException("The drawing instance this line width scope belongs to no longer exists. Line width scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a line width scope inside a coroutine?");
#endif
                unsafe
                {
                    builder.PopLineWidth();
                    builder.buffer = null;
                }
            }
        }

        /// <summary>
        /// 使用隐式矩阵变换绘制多个内容的作用域。
        /// 作用域内绘制的所有项的坐标将乘以该矩阵。
        /// 如果 WithMatrix 作用域嵌套，则坐标按顺序乘以所有嵌套矩阵。
        ///
        /// <code>
        /// using (Draw.InLocalSpace(transform)) {
        ///     // 在相对于当前物体的 (0,0,0) 处绘制方盒
        ///     // 这意味着它将显示在物体的位置
        ///     Draw.WireBox(Vector3.zero, Vector3.one);
        /// }
        ///
        /// // 使用底层 WithMatrix 作用域的等效代码
        /// using (Draw.WithMatrix(transform.localToWorldMatrix)) {
        ///     Draw.WireBox(Vector3.zero, Vector3.one);
        /// }
        /// </code>
        ///
        /// See: <see cref="InLocalSpace"/>
        /// </summary>
        [BurstDiscard]
        public ScopeMatrix WithMatrix(Matrix4x4 matrix)
        {
            PushMatrix(matrix);
            // TODO: 跟踪存活的作用域，除非所有作用域都已释放，否则阻止释放
            unsafe
            {
                return new ScopeMatrix { builder = this };
            }
        }

        /// <summary>
        /// 使用隐式矩阵变换绘制多个内容的作用域。
        /// 作用域内绘制的所有项的坐标将乘以该矩阵。
        /// 如果 WithMatrix 作用域嵌套，则坐标按顺序乘以所有嵌套矩阵。
        ///
        /// <code>
        /// using (Draw.InLocalSpace(transform)) {
        ///     // 在相对于当前物体的 (0,0,0) 处绘制方盒
        ///     // 这意味着它将显示在物体的位置
        ///     Draw.WireBox(Vector3.zero, Vector3.one);
        /// }
        ///
        /// // 使用底层 WithMatrix 作用域的等效代码
        /// using (Draw.WithMatrix(transform.localToWorldMatrix)) {
        ///     Draw.WireBox(Vector3.zero, Vector3.one);
        /// }
        /// </code>
        ///
        /// See: <see cref="InLocalSpace"/>
        /// </summary>
        [BurstDiscard]
        public ScopeMatrix WithMatrix(float3x3 matrix)
        {
            PushMatrix(new float4x4(matrix, float3.zero));
            // TODO: 跟踪存活的作用域，除非所有作用域都已释放，否则阻止释放
            unsafe
            {
                return new ScopeMatrix { builder = this };
            }
        }

        /// <summary>
        /// 使用相同颜色绘制多个内容的作用域。
        ///
        /// <code>
        /// void Update () {
        ///     using (Draw.WithColor(Color.red)) {
        ///         Draw.Line(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        ///         Draw.Line(new Vector3(0, 0, 0), new Vector3(0, 1, 2));
        ///     }
        /// }
        /// </code>
        ///
        /// 传入显式颜色参数的命令将覆盖此颜色。
        /// 如果在此作用域内嵌套另一个颜色作用域，则该作用域将覆盖此颜色。
        /// </summary>
        [BurstDiscard]
        public ScopeColor WithColor(Color color)
        {
            PushColor(color);
            unsafe
            {
                return new ScopeColor { builder = this };
            }
        }

        /// <summary>
        /// 使绘制内容持续更长时间的作用域。
        ///
        /// 通常绘制的项仅渲染一帧。
        /// 使用持续作用域可以使项绘制任意时长。
        ///
        /// <code>
        /// void Update () {
        ///     using (Draw.WithDuration(1.0f)) {
        ///         var offset = Time.time;
        ///         Draw.Line(new Vector3(offset, 0, 0), new Vector3(offset, 0, 1));
        ///     }
        /// }
        /// </code>
        ///
        /// 注意：在非播放模式下，持续时间以 Unity 的 Time.realtimeSinceStartup 衡量。
        ///
        /// 警告：不建议在 DrawGizmos 回调内使用，因为 DrawGizmos 每帧都会被调用。
        /// </summary>
        /// <param name="duration">绘制项应持续的秒数。</param>

        [BurstDiscard]
        public ScopePersist WithDuration(float duration)
        {
            PushDuration(duration);
            unsafe
            {
                return new ScopePersist { builder = this };
            }
        }

        /// <summary>
        /// 使用指定线宽绘制多个内容的作用域。
        ///
        /// 请注意，线条连接算法是一个为速度优化的简单算法。它通常在 2D 平面上看起来不错，但如果折线在 3D 空间中弯曲很大，
        /// 从某些角度看可能会显得奇怪。
        ///
        /// [Open online documentation to see images]
        ///
        /// 图片中上排启用了 automaticJoins，下排则禁用。
        /// </summary>
        /// <param name="pixels">线宽（像素）</param>
        /// <param name="automaticJoins">如果为 true，相邻线段将在顶点处自动连接。通常可产生更美观的折线，避免奇怪的间隙。</param>
        [BurstDiscard]
        public ScopeLineWidth WithLineWidth(float pixels, bool automaticJoins = true)
        {
            PushLineWidth(pixels, automaticJoins);
            unsafe
            {
                return new ScopeLineWidth { builder = this };
            }
        }

        /// <summary>
        /// 相对于 Transform 对象绘制多个内容的作用域。
        /// 作用域内绘制的所有项的坐标将乘以 transform 的 localToWorldMatrix。
        ///
        /// <code>
        /// void Update () {
        ///     using (Draw.InLocalSpace(transform)) {
        ///         // 在相对于当前物体的 (0,0,0) 处绘制方盒
        ///         // 这意味着它将显示在物体的位置
        ///         // 方盒也会随 transform 旋转和缩放
        ///         Draw.WireBox(Vector3.zero, Vector3.one);
        ///     }
        /// }
        /// </code>
        ///
        /// [Open online documentation to see videos]
        /// </summary>
        [BurstDiscard]
        public ScopeMatrix InLocalSpace(Transform transform)
        {
            return WithMatrix(transform.localToWorldMatrix);
        }

        /// <summary>
        /// 在相机屏幕空间中绘制多个内容的作用域。
        /// 如果绘制 2D 坐标（即 (x,y,0)），它们将被投影到相机前方约 [2*近裁剪面] 世界单位的平面上（保证在近远平面之间）。
        ///
        /// 相机左下角为 (0,0,0)，右上角为 (camera.pixelWidth, camera.pixelHeight, 0)
        ///
        /// 注意：因此像素中心偏移 0.5。例如左上角像素的中心在 (0.5, 0.5, 0)。
        /// 因此如果想在屏幕空间绘制 1 像素宽的线条，可能需要将坐标偏移 0.5 像素。
        ///
        /// See: <see cref="InLocalSpace"/>
        /// See: <see cref="WithMatrix"/>
        /// </summary>
        [BurstDiscard]
        public ScopeMatrix InScreenSpace(Camera camera)
        {
            return WithMatrix(camera.cameraToWorldMatrix * camera.nonJitteredProjectionMatrix.inverse * Matrix4x4.TRS(new Vector3(-1.0f, -1.0f, 0), Quaternion.identity, new Vector3(2.0f / camera.pixelWidth, 2.0f / camera.pixelHeight, 1)));
        }

        /// <summary>
        /// 将给定矩阵乘以所有坐标直到下一个 PopMatrix。
        /// 与 <see cref="PushSetMatrix"/> 不同，此方法与所有先前推入的矩阵叠加，而 <see cref="PushSetMatrix"/> 不会。
        /// </summary>
        public void PushMatrix(Matrix4x4 matrix)
        {
            Reserve<float4x4>();
            Add(Command.PushMatrix);
            Add(matrix);
        }

        /// <summary>
        /// 将给定矩阵乘以所有坐标直到下一个 PopMatrix。
        /// 与 <see cref="PushSetMatrix"/> 不同，此方法与所有先前推入的矩阵叠加，而 <see cref="PushSetMatrix"/> 不会。
        /// </summary>
        public void PushMatrix(float4x4 matrix)
        {
            Reserve<float4x4>();
            Add(Command.PushMatrix);
            Add(matrix);
        }

        /// <summary>
        /// 将给定矩阵乘以所有坐标直到下一个 PopMatrix。
        /// 与 <see cref="PushMatrix"/> 不同，此方法直接设置当前矩阵，而 <see cref="PushMatrix"/> 与所有先前推入的矩阵叠加。
        /// </summary>
        public void PushSetMatrix(Matrix4x4 matrix)
        {
            Reserve<float4x4>();
            Add(Command.PushSetMatrix);
            Add((float4x4)matrix);
        }

        /// <summary>
        /// 将给定矩阵乘以所有坐标直到下一个 PopMatrix。
        /// 与 <see cref="PushMatrix"/> 不同，此方法直接设置当前矩阵，而 <see cref="PushMatrix"/> 与所有先前推入的矩阵叠加。
        /// </summary>
        public void PushSetMatrix(float4x4 matrix)
        {
            Reserve<float4x4>();
            Add(Command.PushSetMatrix);
            Add(matrix);
        }

        /// <summary>从栈中弹出矩阵</summary>
        public void PopMatrix()
        {
            Reserve(4);
            Add(Command.PopMatrix);
        }

        /// <summary>
        /// 使用给定颜色绘制直到下一个 PopColor。
        /// 传入显式颜色参数的命令将覆盖此颜色。
        /// 如果在此作用域内嵌套另一个颜色作用域，则该作用域将覆盖此颜色。
        /// </summary>
        public void PushColor(Color color)
        {
            Reserve<Color32>();
            Add(Command.PushColor);
            Add(ConvertColor(color));
        }

        /// <summary>从栈中弹出颜色</summary>
        public void PopColor()
        {
            Reserve(4);
            Add(Command.PopColor);
        }

        /// <summary>
        /// 绘制持续指定秒数直到下一个 PopDuration。
        /// 警告：不建议在 DrawGizmos 回调内使用，因为 DrawGizmos 每帧都会被调用。
        /// </summary>
        public void PushDuration(float duration)
        {
            Reserve<PersistData>();
            Add(Command.PushPersist);
            // 我们必须使用更新频率低于 Time.time 的 BurstTime 变量。
            // 这是必要的，因为此代码可能从 Burst Job 或不同线程调用。
            // Time.time 只能在主线程访问。
            Add(new PersistData { endTime = SharedShapeData.BurstTime.Data + duration });
        }

        /// <summary>从栈中弹出持续时间作用域</summary>
        public void PopDuration()
        {
            Reserve(4);
            Add(Command.PopPersist);
        }


        /// <summary>
        /// 使用给定的像素线宽绘制所有线条直到下一个 PopLineWidth。
        ///
        /// 请注意，线条连接算法是一个为速度优化的简单算法。它通常在 2D 平面上看起来不错，但如果折线在 3D 空间中弯曲很大，
        /// 从某些角度看可能会显得奇怪。
        ///
        /// [Open online documentation to see images]
        ///
        /// 图片中上排启用了 automaticJoins，下排则禁用。
        /// </summary>
        /// <param name="pixels">线宽（像素）</param>
        /// <param name="automaticJoins">如果为 true，相邻线段将在顶点处自动连接。通常可产生更美观的折线，避免奇怪的间隙。</param>
        public void PushLineWidth(float pixels, bool automaticJoins = true)
        {
            if (pixels < 0) throw new System.ArgumentOutOfRangeException("pixels", "Line width must be positive");

            Reserve<LineWidthData>();
            Add(Command.PushLineWidth);
            Add(new LineWidthData { pixels = pixels, automaticJoins = automaticJoins });
        }

        /// <summary>从栈中弹出线宽作用域</summary>
        public void PopLineWidth()
        {
            Reserve(4);
            Add(Command.PopLineWidth);
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
            Reserve<LineData>();
            Add(Command.Line);
            Add(new LineData { a = a, b = b });
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
        public void Line(Vector3 a, Vector3 b)
        {
            Reserve<LineData>();
            // Add(Command.Line);
            // Add(new LineDataV3 { a = a, b = b });

            // 下面的代码等同于上面被注释掉的代码。
            // 但绘制线条是最常见的操作，所以需要非常快。
            // 硬编码可以将线条渲染性能提高约 8%。
            var bufferSize = BufferSize;

            unsafe
            {
                var newLen = bufferSize + 4 + 24;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                UnityEngine.Assertions.Assert.IsTrue(newLen <= buffer->Capacity);
#endif
                var ptr = (byte*)buffer->Ptr + bufferSize;
                *(Command*)ptr = Command.Line;
                var lineData = (LineDataV3*)(ptr + 4);
                lineData->a = a;
                lineData->b = b;
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
        public void Line(Vector3 a, Vector3 b, Color color)
        {
            Reserve<Color32, LineData>();
            // Add(Command.Line | Command.PushColorInline);
            // Add(ConvertColor(color));
            // Add(new LineDataV3 { a = a, b = b });

            // 下面的代码等同于上面被注释掉的代码。
            // 但绘制线条是最常见的操作，所以需要非常快
            // 硬编码可以将线条渲染性能提高约 8%。
            var bufferSize = BufferSize;

            unsafe
            {
                var newLen = bufferSize + 4 + 24 + 4;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                UnityEngine.Assertions.Assert.IsTrue(newLen <= buffer->Capacity);
#endif
                var ptr = (byte*)buffer->Ptr + bufferSize;
                *(Command*)ptr = Command.Line | Command.PushColorInline;
                *(uint*)(ptr + 4) = ConvertColor(color);
                var lineData = (LineDataV3*)(ptr + 8);
                lineData->a = a;
                lineData->b = b;
                buffer->Length = newLen;
            }
        }

        /// <summary>
        /// 从一个点开始沿给定方向绘制射线。
        /// 射线将在 origin + direction 处结束。
        ///
        /// [Open online documentation to see images]
        ///
        /// <code>
        /// Draw.Ray(Vector3.zero, Vector3.up);
        /// </code>
        /// </summary>
        public void Ray(float3 origin, float3 direction)
        {
            Line(origin, origin + direction);
        }

        /// <summary>
        /// 绘制给定长度的射线。
        ///
        /// [Open online documentation to see images]
        ///
        /// <code>
        /// Draw.Ray(Camera.main.ScreenPointToRay(Vector3.zero), 10);
        /// </code>
        /// </summary>
        public void Ray(Ray ray, float length)
        {
            Line(ray.origin, ray.origin + ray.direction * length);
        }

        /// <summary>
        /// 在两点之间绘制圆弧。
        ///
        /// 渲染的弧是两点之间的最短弧。
        /// 弧的半径等于圆心到起点的距离。
        ///
        /// [Open online documentation to see images]
        /// <code>
        /// float a1 = Mathf.PI*0.9f;
        /// float a2 = Mathf.PI*0.1f;
        /// var arcStart = new float3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
        /// var arcEnd = new float3(Mathf.Cos(a2), 0, Mathf.Sin(a2));
        /// Draw.Arc(new float3(0, 0, 0), arcStart, arcEnd, color);
        /// </code>
        ///
        /// See: <see cref="CommandBuilder2D.Circle(float3,float,float,float)"/>
        /// </summary>
        /// <param name="center">弧所属虚拟圆的圆心。</param>
        /// <param name="start">弧的起点。</param>
        /// <param name="end">弧的终点。</param>
        public void Arc(float3 center, float3 start, float3 end)
        {
            var d1 = start - center;
            var d2 = end - center;
            var normal = math.cross(d2, d1);

            if (math.any(normal != 0) && math.all(math.isfinite(normal)))
            {
                var m = Matrix4x4.TRS(center, Quaternion.LookRotation(d1, normal), Vector3.one);
                var angle = Vector3.SignedAngle(d1, d2, normal) * Mathf.Deg2Rad;
                PushMatrix(m);
                CircleXZInternal(float3.zero, math.length(d1), 90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad - angle);
                PopMatrix();
            }
        }

        internal void CircleXZInternal(float3 center, float radius, float startAngle = 0f, float endAngle = 2 * Mathf.PI)
        {
            Reserve<CircleXZData>();
            Add(Command.CircleXZ);
            Add(new CircleXZData { center = center, radius = radius, startAngle = startAngle, endAngle = endAngle });
        }

        internal void CircleXZInternal(float3 center, float radius, float startAngle, float endAngle, Color color)
        {
            Reserve<Color32, CircleXZData>();
            Add(Command.CircleXZ | Command.PushColorInline);
            Add(ConvertColor(color));
            Add(new CircleXZData { center = center, radius = radius, startAngle = startAngle, endAngle = endAngle });
        }

        internal static readonly float4x4 XZtoXYPlaneMatrix = float4x4.RotateX(-math.PI * 0.5f);
        internal static readonly float4x4 XZtoYZPlaneMatrix = float4x4.RotateZ(math.PI * 0.5f);


        /// <summary>
        /// 绘制圆。
        ///
        /// [Open online documentation to see images]
        ///
        /// 注意：此重载不支持绘制圆弧。请改用 <see cref="Arc"/>、<see cref="CircleXY"/> 或 <see cref="CircleXZ"/>。
        /// </summary>
        public void Circle(float3 center, float3 normal, float radius)
        {
            Reserve<CircleData>();
            Add(Command.Circle);
            Add(new CircleData { center = center, normal = normal, radius = radius });
        }

        /// <summary>
        /// 在两点之间绘制实心圆弧。
        ///
        /// 渲染的弧是两点之间的最短弧。
        /// 弧的半径等于圆心到起点的距离。
        ///
        /// [Open online documentation to see images]
        /// <code>
        /// float a1 = Mathf.PI*0.9f;
        /// float a2 = Mathf.PI*0.1f;
        /// var arcStart = new float3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
        /// var arcEnd = new float3(Mathf.Cos(a2), 0, Mathf.Sin(a2));
        /// Draw.SolidArc(new float3(0, 0, 0), arcStart, arcEnd, color);
        /// </code>
        ///
        /// See: <see cref="CommandBuilder2D.SolidCircle(float3,float,float,float)"/>
        /// </summary>
        /// <param name="center">弧所属虚拟圆的圆心。</param>
        /// <param name="start">弧的起点。</param>
        /// <param name="end">弧的终点。</param>
        public void SolidArc(float3 center, float3 start, float3 end)
        {
            var d1 = start - center;
            var d2 = end - center;
            var normal = math.cross(d2, d1);

            if (math.any(normal))
            {
                var m = Matrix4x4.TRS(center, Quaternion.LookRotation(d1, normal), Vector3.one);
                var angle = Vector3.SignedAngle(d1, d2, normal) * Mathf.Deg2Rad;
                PushMatrix(m);
                SolidCircleXZInternal(float3.zero, math.length(d1), 90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad - angle);
                PopMatrix();
            }
        }


        internal void SolidCircleXZInternal(float3 center, float radius, float startAngle = 0f, float endAngle = 2 * Mathf.PI)
        {
            Reserve<CircleXZData>();
            Add(Command.DiscXZ);
            Add(new CircleXZData { center = center, radius = radius, startAngle = startAngle, endAngle = endAngle });
        }

        internal void SolidCircleXZInternal(float3 center, float radius, float startAngle, float endAngle, Color color)
        {
            Reserve<Color32, CircleXZData>();
            Add(Command.DiscXZ | Command.PushColorInline);
            Add(ConvertColor(color));
            Add(new CircleXZData { center = center, radius = radius, startAngle = startAngle, endAngle = endAngle });
        }


        /// <summary>
        /// 绘制圆盘。
        ///
        /// [Open online documentation to see images]
        ///
        /// 注意：此重载不支持绘制圆弧。请改用 <see cref="SolidArc"/> 或 <see cref="CommandBuilder2D.SolidCircle(float3,float,float,float)"/>。
        /// </summary>
        public void SolidCircle(float3 center, float3 normal, float radius)
        {
            Reserve<CircleData>();
            Add(Command.Disc);
            Add(new CircleData { center = center, normal = normal, radius = radius });
        }

        /// <summary>
        /// 在球体周围绘制圆形轮廓。
        ///
        /// 视觉上是一个始终面向相机的圆，并自动调整大小以适应球体。
        ///
        /// [Open online documentation to see images]
        /// </summary>
        public void SphereOutline(float3 center, float radius)
        {
            Reserve<SphereData>();
            Add(Command.SphereOutline);
            Add(new SphereData { center = center, radius = radius });
        }

        /// <summary>
        /// 绘制圆柱体。
        /// 圆柱体的底部圆以 bottom 参数为中心，顶部圆类似。
        ///
        /// <code>
        /// // 在点 (0,0,0) 和 (1,1,1) 之间绘制半径为 0.5 的倾斜圆柱体
        /// Draw.WireCylinder(Vector3.zero, Vector3.one, 0.5f, Color.black);
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        public void WireCylinder(float3 bottom, float3 top, float radius)
        {
            WireCylinder(bottom, top - bottom, math.length(top - bottom), radius);
        }

        /// <summary>
        /// 绘制圆柱体。
        ///
        /// <code>
        /// // 在世界原点绘制高 2 米、半径 0.5 的圆柱体
        /// Draw.WireCylinder(Vector3.zero, Vector3.up, 2, 0.5f, Color.black);
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="position">圆柱体"底部"圆的中心。</param>
        /// <param name="up">圆柱体的主轴。不需要归一化。如果为零，则不绘制。</param>
        /// <param name="height">沿主轴测量的圆柱体长度。</param>
        /// <param name="radius">圆柱体的半径。</param>
        public void WireCylinder(float3 position, float3 up, float height, float radius)
        {
            up = math.normalizesafe(up);
            if (math.all(up == 0) || math.any(math.isnan(up)) || math.isnan(height) || math.isnan(radius)) return;

            OrthonormalBasis(up, out var basis1, out var basis2);

            PushMatrix(new float4x4(
                new float4(basis1 * radius, 0),
                new float4(up * height, 0),
                new float4(basis2 * radius, 0),
                new float4(position, 1)
                ));

            CircleXZInternal(float3.zero, 1);
            if (height > 0)
            {
                CircleXZInternal(new float3(0, 1, 0), 1);
                Line(new float3(1, 0, 0), new float3(1, 1, 0));
                Line(new float3(-1, 0, 0), new float3(-1, 1, 0));
                Line(new float3(0, 0, 1), new float3(0, 1, 1));
                Line(new float3(0, 0, -1), new float3(0, 1, -1));
            }
            PopMatrix();
        }

        /// <summary>
        /// 从单个法向量构建正交基。
        ///
        /// 这类似于 math.orthonormal_basis，但更努力保持输入的连续性。
        /// 相比之下，math.orthonormal_basis 即使法线微小变化也容易跳变。
        ///
        /// 不过速度不如 math.orthonormal_basis。
        /// </summary>
        static void OrthonormalBasis(float3 normal, out float3 basis1, out float3 basis2)
        {
            basis1 = math.cross(normal, new float3(1, 1, 1));
            if (math.all(basis1 == 0)) basis1 = math.cross(normal, new float3(-1, 1, 1));
            basis1 = math.normalizesafe(basis1);
            basis2 = math.cross(normal, basis1);
        }

        /// <summary>
        /// 使用 (start,end) 参数化绘制胶囊体。
        ///
        /// 此方法的行为与常见的 Unity API（如 Physics.CheckCapsule）一致。
        ///
        /// <code>
        /// // 在点 (0,0,0) 和 (1,1,1) 之间绘制半径为 0.5 的倾斜胶囊体
        /// Draw.WireCapsule(Vector3.zero, Vector3.one, 0.5f, Color.black);
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="start">胶囊体起始半球的中心。</param>
        /// <param name="end">胶囊体终止半球的中心。</param>
        /// <param name="radius">胶囊体的半径。</param>
        public void WireCapsule(float3 start, float3 end, float radius)
        {
            var dir = end - start;
            var length = math.length(dir);

            if (length < 0.0001)
            {
                // 端点相同，无法绘制胶囊体因为不知道其方向。
                // 回退绘制球体
                WireSphere(start, radius);
            }
            else
            {
                var normalized_dir = dir / length;

                WireCapsule(start - normalized_dir * radius, normalized_dir, length + 2 * radius, radius);
            }
        }

        // TODO: 改为 center, up, height 参数化
        /// <summary>
        /// 使用 (position,direction/length) 参数化绘制胶囊体。
        ///
        /// <code>
        /// // 绘制接触 y=0 平面的胶囊体，高 2 米，半径 0.5
        /// Draw.WireCapsule(Vector3.zero, Vector3.up, 2.0f, 0.5f, Color.black);
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="position">胶囊体的一个端点。位于胶囊体边缘，而非半球中心。</param>
        /// <param name="direction">胶囊体的主轴。不需要归一化。如果为零，则不绘制。</param>
        /// <param name="length">胶囊体两端点之间的距离。长度将被钳制为至少 2*radius。</param>
        /// <param name="radius">胶囊体的半径。</param>
        public void WireCapsule(float3 position, float3 direction, float length, float radius)
        {
            direction = math.normalizesafe(direction);
            if (math.all(direction == 0) || math.any(math.isnan(direction)) || math.isnan(length) || math.isnan(radius)) return;

            if (radius <= 0)
            {
                Line(position, position + direction * length);
            }
            else
            {
                length = math.max(length, radius * 2);
                OrthonormalBasis(direction, out var basis1, out var basis2);

                PushMatrix(new float4x4(
                    new float4(basis1, 0),
                    new float4(direction, 0),
                    new float4(basis2, 0),
                    new float4(position, 1)
                    ));
                CircleXZInternal(new float3(0, radius, 0), radius);
                PushMatrix(XZtoXYPlaneMatrix);
                CircleXZInternal(new float3(0, 0, radius), radius, Mathf.PI, 2 * Mathf.PI);
                PopMatrix();
                PushMatrix(XZtoYZPlaneMatrix);
                CircleXZInternal(new float3(radius, 0, 0), radius, Mathf.PI * 0.5f, Mathf.PI * 1.5f);
                PopMatrix();
                if (length > 0)
                {
                    var upperY = length - radius;
                    var lowerY = radius;
                    CircleXZInternal(new float3(0, upperY, 0), radius);
                    PushMatrix(XZtoXYPlaneMatrix);
                    CircleXZInternal(new float3(0, 0, upperY), radius, 0, Mathf.PI);
                    PopMatrix();
                    PushMatrix(XZtoYZPlaneMatrix);
                    CircleXZInternal(new float3(upperY, 0, 0), radius, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);
                    PopMatrix();
                    Line(new float3(radius, lowerY, 0), new float3(radius, upperY, 0));
                    Line(new float3(-radius, lowerY, 0), new float3(-radius, upperY, 0));
                    Line(new float3(0, lowerY, radius), new float3(0, upperY, radius));
                    Line(new float3(0, lowerY, -radius), new float3(0, upperY, -radius));
                }
                PopMatrix();
            }
        }

        /// <summary>
        /// 绘制线框球体。
        ///
        /// [Open online documentation to see images]
        ///
        /// <code>
        /// // 在原点绘制半径为 0.5 的线框球体
        /// Draw.WireSphere(Vector3.zero, 0.5f, Color.black);
        /// </code>
        ///
        /// See: <see cref="Circle"/>
        /// </summary>
        public void WireSphere(float3 position, float radius)
        {
            SphereOutline(position, radius);
            Circle(position, new float3(1, 0, 0), radius);
            Circle(position, new float3(0, 1, 0), radius);
            Circle(position, new float3(0, 0, 1), radius);
        }

        /// <summary>
        /// 通过一系列点绘制线条。
        ///
        /// [Open online documentation to see images]
        /// <code>
        /// // 绘制正方形
        /// Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
        /// </code>
        /// </summary>
        /// <param name="points">绘制线条经过的点序列</param>
        /// <param name="cycle">如果为 true，将从序列最后一个点绘制线条回到第一个点。</param>
        [BurstDiscard]
        public void Polyline(List<Vector3> points, bool cycle = false)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line(points[i], points[i + 1]);
            }
            if (cycle && points.Count > 1) Line(points[points.Count - 1], points[0]);
        }

        /// <summary>
        /// 通过一系列点绘制线条。
        ///
        /// [Open online documentation to see images]
        /// <code>
        /// // 绘制正方形
        /// Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
        /// </code>
        /// </summary>
        /// <param name="points">绘制线条经过的点序列</param>
        /// <param name="cycle">如果为 true，将从序列最后一个点绘制线条回到第一个点。</param>
        public void Polyline<T>(T points, bool cycle = false) where T : IReadOnlyList<float3>
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line(points[i], points[i + 1]);
            }
            if (cycle && points.Count > 1) Line(points[points.Count - 1], points[0]);
        }

        /// <summary>
        /// 通过一系列点绘制线条。
        ///
        /// [Open online documentation to see images]
        /// <code>
        /// // 绘制正方形
        /// Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
        /// </code>
        /// </summary>
        /// <param name="points">绘制线条经过的点序列</param>
        /// <param name="cycle">如果为 true，将从序列最后一个点绘制线条回到第一个点。</param>
        [BurstDiscard]
        public void Polyline(Vector3[] points, bool cycle = false)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Line(points[i], points[i + 1]);
            }
            if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
        }

        /// <summary>
        /// 通过一系列点绘制线条。
        ///
        /// [Open online documentation to see images]
        /// <code>
        /// // 绘制正方形
        /// Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
        /// </code>
        /// </summary>
        /// <param name="points">绘制线条经过的点序列</param>
        /// <param name="cycle">如果为 true，将从序列最后一个点绘制线条回到第一个点。</param>
        [BurstDiscard]
        public void Polyline(float3[] points, bool cycle = false)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Line(points[i], points[i + 1]);
            }
            if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
        }

        /// <summary>
        /// 通过一系列点绘制线条。
        ///
        /// [Open online documentation to see images]
        /// <code>
        /// // 绘制正方形
        /// Draw.Polyline(new [] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, true);
        /// </code>
        /// </summary>
        /// <param name="points">绘制线条经过的点序列</param>
        /// <param name="cycle">如果为 true，将从序列最后一个点绘制线条回到第一个点。</param>
        public void Polyline(NativeArray<float3> points, bool cycle = false)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Line(points[i], points[i + 1]);
            }
            if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
        }

        /// <summary>确定用于 <see cref="PolylineWithSymbol"/> 的符号</summary>
        public enum SymbolDecoration
        {
            /// <summary>
            /// 无符号。
            ///
            /// 仍会保留空间，但不会绘制符号。
            /// 可用于绘制虚线。
            ///
            /// [Open online documentation to see images]
            /// </summary>
            None,
            /// <summary>
            /// 箭头符号。
            ///
            /// [Open online documentation to see images]
            /// </summary>
            ArrowHead,
            /// <summary>
            /// 圆形符号。
            ///
            /// [Open online documentation to see images]
            /// </summary>
            Circle,
        }

        /// <summary>
        /// 在两点之间绘制虚线。
        ///
        /// <code>
        /// Draw.DashedPolyline(points, 0.1f, 0.1f, color);
        /// </code>
        ///
        /// [Open online documentation to see images]
        ///
        /// 警告：每个虚线段都会绘制一个单独的线段。这意味着如果虚线+间隔距离太小，性能可能会下降。
        /// 但对于大多数使用场景，性能无需担心。
        ///
        /// See: <see cref="DashedPolyline"/>
        /// See: <see cref="PolylineWithSymbol"/>
        /// </summary>
        public void DashedLine(float3 a, float3 b, float dash, float gap)
        {
            var p = new PolylineWithSymbol(SymbolDecoration.None, gap, 0, dash + gap);
            p.MoveTo(ref this, a);
            p.MoveTo(ref this, b);
        }

        /// <summary>
        /// 通过一系列点绘制虚线。
        ///
        /// <code>
        /// Draw.DashedPolyline(points, 0.1f, 0.1f, color);
        /// </code>
        ///
        /// [Open online documentation to see images]
        ///
        /// 警告：每个虚线段都会绘制一个单独的线段。这意味着如果虚线+间隔距离太小，性能可能会下降。
        /// 但对于大多数使用场景，性能无需担心。
        ///
        /// 如果你使用不同的集合类型，或者没有将点放在集合中，可以直接使用 <see cref="PolylineWithSymbol"/> 结构体。
        ///
        /// <code>
        /// using (Draw.WithColor(color)) {
        ///     var dash = 0.1f;
        ///     var gap = 0.1f;
        ///     var p = new CommandBuilder.PolylineWithSymbol(CommandBuilder.SymbolDecoration.None, gap, 0, dash + gap);
        ///     for (int i = 0; i < points.Count; i++) {
        ///         p.MoveTo(ref Draw.editor, points[i]);
        ///     }
        /// }
        /// </code>
        ///
        /// See: <see cref="DashedLine"/>
        /// See: <see cref="PolylineWithSymbol"/>
        /// </summary>
        public void DashedPolyline(List<Vector3> points, float dash, float gap)
        {
            var p = new PolylineWithSymbol(SymbolDecoration.None, gap, 0, dash + gap);
            for (int i = 0; i < points.Count; i++)
            {
                p.MoveTo(ref this, points[i]);
            }
        }

        /// <summary>
        /// 在固定间隔处绘制带符号折线的辅助工具。
        ///
        /// <code>
        /// var generator = new CommandBuilder.PolylineWithSymbol(CommandBuilder.SymbolDecoration.Circle, 0.2f, 0.0f, 0.47f);
        /// generator.MoveTo(ref Draw.editor, new float3(-0.5f, 0, -0.5f));
        /// generator.MoveTo(ref Draw.editor, new float3(0.5f, 0, 0.5f));
        /// </code>
        ///
        /// [Open online documentation to see images]
        ///
        /// [Open online documentation to see images]
        ///
        /// 你也可以使用此结构体绘制虚线，但常见情况下可以改用 <see cref="DashedPolyline"/> 辅助函数。
        ///
        /// <code>
        /// using (Draw.WithColor(color)) {
        ///     var dash = 0.1f;
        ///     var gap = 0.1f;
        ///     var p = new CommandBuilder.PolylineWithSymbol(CommandBuilder.SymbolDecoration.None, gap, 0, dash + gap);
        ///     for (int i = 0; i < points.Count; i++) {
        ///         p.MoveTo(ref Draw.editor, points[i]);
        ///     }
        /// }
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        public struct PolylineWithSymbol
        {
            float3 prev;
            float offset;
            readonly float symbolSize;
            readonly float symbolSpacing;
            readonly float symbolPadding;
            readonly float symbolOffset;
            readonly SymbolDecoration symbol;
            readonly bool reverseSymbols;
            bool odd;

            /// <summary>创建新的带符号折线生成器。</summary>
            /// <param name="symbol">使用的符号</param>
            /// <param name="symbolSize">符号大小。对于圆形，这是直径。</param>
            /// <param name="symbolPadding">符号两侧在符号与线条之间的间距。</param>
            /// <param name="symbolSpacing">符号之间的间距。这是符号中心之间的距离。</param>
            /// <param name="reverseSymbols">如果为 true，符号将被反转。对圆形无效，但箭头符号会被反转。</param>
            public PolylineWithSymbol(SymbolDecoration symbol, float symbolSize, float symbolPadding, float symbolSpacing, bool reverseSymbols = false)
            {
                if (symbolSpacing <= math.FLT_MIN_NORMAL) throw new System.ArgumentOutOfRangeException(nameof(symbolSpacing), "Symbol spacing must be greater than zero");
                if (symbolSize <= math.FLT_MIN_NORMAL) throw new System.ArgumentOutOfRangeException(nameof(symbolSize), "Symbol size must be greater than zero");
                if (symbolPadding < 0) throw new System.ArgumentOutOfRangeException(nameof(symbolPadding), "Symbol padding must non-negative");

                this.prev = float3.zero;
                this.symbol = symbol;
                this.symbolSize = symbolSize;
                this.symbolPadding = symbolPadding;
                this.symbolSpacing = math.max(0, symbolSpacing - symbolPadding * 2f - symbolSize);
                this.reverseSymbols = reverseSymbols;
                symbolOffset = symbol == SymbolDecoration.ArrowHead ? -0.25f * symbolSize : 0;
                if (reverseSymbols)
                {
                    symbolOffset = -symbolOffset;
                }
                symbolOffset += 0.5f * symbolSize;
                offset = -1;
                odd = false;
            }

            /// <summary>
            /// 移动到新点。
            ///
            /// 这将在前一个点和新点之间绘制符号和线段。
            /// </summary>
            /// <param name="draw">要绘制到的命令构建器。可以使用内置构建器如 \reflink{Draw.editor} 或 \reflink{Draw.ingame}，也可以使用自定义构建器。</param>
            /// <param name="next">折线中要移动到的下一个点。</param>
            public void MoveTo(ref CommandBuilder draw, float3 next)
            {
                if (offset == -1)
                {
                    offset = this.symbolSpacing * 0.5f;
                    prev = next;
                    return;
                }
                var len = math.length(next - prev);
                var invLen = math.rcp(len);
                var dir = next - prev;
                float3 up = default;
                if (symbol != SymbolDecoration.None)
                {
                    up = math.normalizesafe(math.cross(dir, math.cross(dir, new float3(0, 1, 0))));
                    if (math.all(up == 0f))
                    {
                        up = new float3(0, 0, 1);
                    }
                }
                if (reverseSymbols) dir = -dir;
                if (offset > 0 && !odd)
                {
                    draw.Line(prev, math.lerp(prev, next, math.min(offset * invLen, 1)));
                }
                while (offset < len)
                {
                    if (odd)
                    {
                        var pLast = math.lerp(prev, next, offset * invLen);
                        offset += symbolSpacing;
                        var p = math.lerp(prev, next, math.min(offset * invLen, 1));
                        draw.Line(pLast, p);
                        offset += symbolPadding;
                    }
                    else
                    {
                        var p = math.lerp(prev, next, (offset + symbolOffset) * invLen);
                        switch (symbol)
                        {
                            case SymbolDecoration.None:
                                break;
                            case SymbolDecoration.ArrowHead:
                                draw.Arrowhead(p, dir, up, symbolSize);
                                break;
                            case SymbolDecoration.Circle:
                            default:
                                draw.Circle(p, up, symbolSize * 0.5f);
                                break;
                        }
                        offset += symbolSize + symbolPadding;
                    }
                    odd = !odd;
                }
                offset -= len;
                prev = next;
            }
        }

        /// <summary>
        /// 绘制轴对齐方盒的轮廓。
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">方盒中心</param>
        /// <param name="size">方盒沿各维度的宽度</param>
        public void WireBox(float3 center, float3 size)
        {
            Reserve<BoxData>();
            Add(Command.WireBox);
            Add(new BoxData { center = center, size = size });
        }

        /// <summary>
        /// 绘制方盒的轮廓。
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">方盒中心</param>
        /// <param name="rotation">方盒旋转</param>
        /// <param name="size">方盒沿各维度的宽度</param>
        public void WireBox(float3 center, quaternion rotation, float3 size)
        {
            PushMatrix(float4x4.TRS(center, rotation, size));
            WireBox(float3.zero, new float3(1, 1, 1));
            PopMatrix();
        }

        /// <summary>
        /// 绘制方盒的轮廓。
        ///
        /// [Open online documentation to see images]
        /// </summary>
        public void WireBox(Bounds bounds)
        {
            WireBox(bounds.center, bounds.size);
        }

        /// <summary>
        /// 绘制线框网格。
        /// 网格的每条边都将使用 <see cref="Line"/> 命令绘制。
        ///
        /// <code>
        /// var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        /// go.transform.position = new Vector3(0, 0, 0);
        /// using (Draw.InLocalSpace(go.transform)) {
        ///     Draw.WireMesh(go.GetComponent<MeshFilter>().sharedMesh, color);
        /// }
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="SolidMesh(Mesh)"/>
        ///
        /// 版本：需要 Unity 2020.1 或更高版本。
        /// </summary>
        public void WireMesh(Mesh mesh)
        {
            if (mesh == null) throw new System.ArgumentNullException();

            // 使用 Burst 编译的函数绘制线条
            // 这比纯 C# 快很多（约 5 倍）。
            var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            var meshData = meshDataArray[0];

            JobWireMesh.JobWireMeshFunctionPointer(ref meshData, ref this);
            meshDataArray.Dispose();
        }

        /// <summary>
        /// 绘制线框网格。
        /// 网格的每条边都将使用 <see cref="Line"/> 命令绘制。
        ///
        /// <code>
        /// var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        /// go.transform.position = new Vector3(0, 0, 0);
        /// using (Draw.InLocalSpace(go.transform)) {
        ///     Draw.WireMesh(go.GetComponent<MeshFilter>().sharedMesh, color);
        /// }
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="SolidMesh(Mesh)"/>
        ///
        /// 版本：需要 Unity 2020.1 或更高版本。
        /// </summary>
        public void WireMesh(NativeArray<float3> vertices, NativeArray<int> triangles)
        {
            unsafe
            {
                JobWireMesh.WireMesh((float3*)vertices.GetUnsafeReadOnlyPtr(), (int*)triangles.GetUnsafeReadOnlyPtr(), vertices.Length, triangles.Length, ref this);
            }
        }

        /// <summary>Helper job for <see cref="WireMesh"/></summary>
        [BurstCompile]
        class JobWireMesh
        {
            public delegate void JobWireMeshDelegate(ref Mesh.MeshData rawMeshData, ref CommandBuilder draw);

            public static readonly JobWireMeshDelegate JobWireMeshFunctionPointer = BurstCompiler.CompileFunctionPointer<JobWireMeshDelegate>(Execute).Invoke;

            [BurstCompile]
            public static unsafe void WireMesh(float3* verts, int* indices, int vertexCount, int indexCount, ref CommandBuilder draw)
            {
                // 忽略 NativeHashMap 在早期 collections 包版本中被标记为过时的警告。
                // 它工作正常，在后续版本中 NativeHashMap 不再过时。
#pragma warning disable 618
                var seenEdges = new NativeHashMap<int2, bool>(indexCount, Allocator.Temp);
#pragma warning restore 618
                for (int i = 0; i < indexCount; i += 3)
                {
                    var a = indices[i];
                    var b = indices[i + 1];
                    var c = indices[i + 2];
                    if (a < 0 || b < 0 || c < 0 || a >= vertexCount || b >= vertexCount || c >= vertexCount)
                    {
                        throw new Exception("Invalid vertex index. Index out of bounds");
                    }
                    int v1, v2;

                    // 绘制三角形的每条边。
                    // 检查以避免重复绘制边。
                    v1 = math.min(a, b);
                    v2 = math.max(a, b);
                    if (!seenEdges.ContainsKey(new int2(v1, v2)))
                    {
                        seenEdges.Add(new int2(v1, v2), true);
                        draw.Line(verts[v1], verts[v2]);
                    }

                    v1 = math.min(b, c);
                    v2 = math.max(b, c);
                    if (!seenEdges.ContainsKey(new int2(v1, v2)))
                    {
                        seenEdges.Add(new int2(v1, v2), true);
                        draw.Line(verts[v1], verts[v2]);
                    }

                    v1 = math.min(c, a);
                    v2 = math.max(c, a);
                    if (!seenEdges.ContainsKey(new int2(v1, v2)))
                    {
                        seenEdges.Add(new int2(v1, v2), true);
                        draw.Line(verts[v1], verts[v2]);
                    }
                }
            }

            [BurstCompile]
            [AOT.MonoPInvokeCallback(typeof(JobWireMeshDelegate))]
            static void Execute(ref Mesh.MeshData rawMeshData, ref CommandBuilder draw)
            {
                int maxIndices = 0;
                for (int subMeshIndex = 0; subMeshIndex < rawMeshData.subMeshCount; subMeshIndex++)
                {
                    maxIndices = math.max(maxIndices, rawMeshData.GetSubMesh(subMeshIndex).indexCount);
                }
                var tris = new NativeArray<int>(maxIndices, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var verts = new NativeArray<Vector3>(rawMeshData.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                rawMeshData.GetVertices(verts);

                for (int subMeshIndex = 0; subMeshIndex < rawMeshData.subMeshCount; subMeshIndex++)
                {
                    var submesh = rawMeshData.GetSubMesh(subMeshIndex);
                    rawMeshData.GetIndices(tris, subMeshIndex);
                    unsafe
                    {
                        WireMesh((float3*)verts.GetUnsafeReadOnlyPtr(), (int*)tris.GetUnsafeReadOnlyPtr(), verts.Length, submesh.indexCount, ref draw);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制实心网格。
        /// 网格将以纯色绘制。
        ///
        /// <code>
        /// var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        /// go.transform.position = new Vector3(0, 0, 0);
        /// using (Draw.InLocalSpace(go.transform)) {
        ///     Draw.SolidMesh(go.GetComponent<MeshFilter>().sharedMesh, color);
        /// }
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// 注意：此方法非线程安全，不能在 Unity Job 系统中使用。
        /// TODO: 矩阵是否被处理？
        ///
        /// See: <see cref="WireMesh(Mesh)"/>
        /// </summary>
        public void SolidMesh(Mesh mesh)
        {
            SolidMeshInternal(mesh, false);
        }

        void SolidMeshInternal(Mesh mesh, bool temporary, Color color)
        {
            PushColor(color);
            SolidMeshInternal(mesh, temporary);
            PopColor();
        }


        void SolidMeshInternal(Mesh mesh, bool temporary)
        {
            var g = gizmos.Target as ShapeData;

            g.data.Get(uniqueID).meshes.Add(new SubmittedMesh
            {
                mesh = mesh,
                temporary = temporary,
            });
            // 内部需要确保捕获当前状态
            // （包括当前矩阵和颜色）以便
            // 将其应用于网格。
            Reserve(4);
            Add(Command.CaptureState);
        }

        /// <summary>
        /// 使用给定顶点绘制实心网格。
        ///
        /// [Open online documentation to see images]
        ///
        /// 注意：此方法非线程安全，不能在 Unity Job 系统中使用。
        /// TODO: 矩阵是否被处理？
        /// </summary>
        [BurstDiscard]
        public void SolidMesh(List<Vector3> vertices, List<int> triangles, List<Color> colors)
        {
            if (vertices.Count != colors.Count) throw new System.ArgumentException("Number of colors must be the same as the number of vertices");

            // TODO: 这个网格会被回收吗？
            var g = gizmos.Target as ShapeData;
            var mesh = g.GetMesh(vertices.Count);

            // 设置网格上的所有数据
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            // 上传所有数据
            mesh.UploadMeshData(false);
            SolidMeshInternal(mesh, true);
        }

        /// <summary>
        /// 使用给定顶点绘制实心网格。
        ///
        /// [Open online documentation to see images]
        ///
        /// 注意：此方法非线程安全，不能在 Unity Job 系统中使用。
        /// TODO: 矩阵是否被处理？
        /// </summary>
        [BurstDiscard]
        public void SolidMesh(Vector3[] vertices, int[] triangles, Color[] colors, int vertexCount, int indexCount)
        {
            if (vertices.Length != colors.Length) throw new System.ArgumentException("Number of colors must be the same as the number of vertices");

            // TODO: 这个网格会被回收吗？
            var g = gizmos.Target as ShapeData;
            var mesh = g.GetMesh(vertices.Length);

            // 设置网格上的所有数据
            mesh.Clear();
            mesh.SetVertices(vertices, 0, vertexCount);
            mesh.SetTriangles(triangles, 0, indexCount, 0);
            mesh.SetColors(colors, 0, vertexCount);
            // 上传所有数据
            mesh.UploadMeshData(false);
            SolidMeshInternal(mesh, true);
        }

        /// <summary>
        /// 绘制 3D 十字。
        ///
        /// [Open online documentation to see images]
        /// </summary>
        public void Cross(float3 position, float size = 1)
        {
            size *= 0.5f;
            Line(position - new float3(size, 0, 0), position + new float3(size, 0, 0));
            Line(position - new float3(0, size, 0), position + new float3(0, size, 0));
            Line(position - new float3(0, 0, size), position + new float3(0, 0, size));
        }


        /// <summary>返回三次贝塞尔曲线上的点。t 被钳制在 0 和 1 之间</summary>
        public static float3 EvaluateCubicBezier(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            t = math.clamp(t, 0, 1);
            float tr = 1 - t;
            return tr * tr * tr * p0 + 3 * tr * tr * t * p1 + 3 * tr * t * t * p2 + t * t * t * p3;
        }

        /// <summary>
        /// 绘制三次贝塞尔曲线。
        ///
        /// [Open online documentation to see images]
        ///
        /// [Open online documentation to see images]
        ///
        /// TODO: 目前使用固定的 20 段分辨率。分辨率应取决于到相机的距离。
        ///
        /// See: https://en.wikipedia.org/wiki/Bezier_curve
        /// </summary>
        /// <param name="p0">起点</param>
        /// <param name="p1">第一个控制点</param>
        /// <param name="p2">第二个控制点</param>
        /// <param name="p3">终点</param>
        public void Bezier(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            float3 prev = p0;

            for (int i = 1; i <= 20; i++)
            {
                float t = i / 20.0f;
                float3 p = EvaluateCubicBezier(p0, p1, p2, p3, t);
                Line(prev, p);
                prev = p;
            }
        }

        /// <summary>
        /// 通过一组点绘制平滑曲线。
        ///
        /// Catmull-Rom 样条等价于由算法确定控制点的贝塞尔曲线。
        /// 实际上，此包通过先将 Catmull-Rom 样条转换为贝塞尔曲线来显示。
        ///
        /// [Open online documentation to see images]
        ///
        /// See: https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline
        /// See: <see cref="CatmullRom(float3,float3,float3,float3)"/>
        /// </summary>
        /// <param name="points">曲线将按顺序平滑通过列表中的每个点。</param>
        public void CatmullRom(List<Vector3> points)
        {
            if (points.Count < 2) return;

            if (points.Count == 2)
            {
                Line(points[0], points[1]);
            }
            else
            {
                // count >= 3
                var count = points.Count;
                // 绘制第一条曲线，这很特殊因为前两个控制点相同
                CatmullRom(points[0], points[0], points[1], points[2]);
                for (int i = 0; i + 3 < count; i++)
                {
                    CatmullRom(points[i], points[i + 1], points[i + 2], points[i + 3]);
                }
                // 绘制最后一条曲线
                CatmullRom(points[count - 3], points[count - 2], points[count - 1], points[count - 1]);
            }
        }

        /// <summary>
        /// 绘制向心 Catmull-Rom 样条。
        ///
        /// 曲线从 p1 开始在 p2 结束。
        ///
        /// [Open online documentation to see images]
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="CatmullRom(List<Vector3>)"/>
        /// </summary>
        /// <param name="p0">第一个控制点</param>
        /// <param name="p1">第二个控制点。曲线起点。</param>
        /// <param name="p2">第三个控制点。曲线终点。</param>
        /// <param name="p3">第四个控制点。</param>
        public void CatmullRom(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            // 使用的参考资料：
            // p.266 GemsV1
            //
            // tension 通常设为 0.5 但可以使用任何合理值：
            // http://www.cs.cmu.edu/~462/projects/assn2/assn2/catmullRom.pdf
            //
            // bias 和 tension 控制：
            // http://local.wasp.uwa.edu.au/~pbourke/miscellaneous/interpolation/

            // 为简单起见，我们将 Catmull-Rom 样条转换为贝塞尔曲线。
            // 最终结果是一个转换矩阵，将 Catmull-Rom 控制点
            // 转换为等效的贝塞尔曲线控制点。

            // 转换矩阵
            // =================

            // 向心 Catmull-Rom 样条可以分解为以下项：
            // 1 * p1 +
            // t * (-0.5 * p0 + 0.5*p2) +
            // t*t * (p0 - 2.5*p1  + 2.0*p2 + 0.5*t2) +
            // t*t*t * (-0.5*p0 + 1.5*p1 - 1.5*p2 + 0.5*p3)
            //
            // 矩阵形式：
            // 1     t   t^2 t^3
            // {0, -1/2, 1, -1/2}
            // {1, 0, -5/2, 3/2}
            // {0, 1/2, 2, -3/2}
            // {0, 0, -1/2, 1/2}

            // 转置矩阵：
            // M_1 = {{0, 1, 0, 0}, {-1/2, 0, 1/2, 0}, {1, -5/2, 2, -1/2}, {-1/2, 3/2, -3/2, 1/2}}

            // 贝塞尔样条可以分解为以下项：
            // (-t^3 + 3 t^2 - 3 t + 1) * c0 +
            // (3t^3 - 6*t^2 + 3t) * c1 +
            // (3t^2 - 3t^3) * c2 +
            // t^3 * c3
            //
            // 矩阵形式：
            // 1  t  t^2  t^3
            // {1, -3, 3, -1}
            // {0, 3, -6, 3}
            // {0, 0, 3, -3}
            // {0, 0, 0, 1}

            // 转置矩阵：
            // M_2 = {{1, 0, 0, 0}, {-3, 3, 0, 0}, {3, -6, 3, 0}, {-1, 3, -3, 1}}

            // 因此贝塞尔曲线可以用以下表达式求值
            // output1 = T * M_1 * c
            // 其中 T = [1, t, t^2, t^3]，c 为控制点 c = [c0, c1, c2, c3]^T
            //
            // Catmull-Rom 样条可以用以下方式求值
            //
            // output2 = T * M_2 * p
            // 其中 T = 同上，p = [p0, p1, p2, p3]^T
            //
            // 我们可以在 output1 = output2 中求解 c
            // T * M_1 * c = T * M_2 * p
            // M_1 * c = M_2 * p
            // c = M_1^(-1) * M_2 * p
            // 因此从 p 到 c 的转换矩阵为 M_1^(-1) * M_2
            // 计算后的结果为以下矩阵：
            //
            // {0, 1, 0, 0}
            // {-1/6, 1, 1/6, 0}
            // {0, 1/6, 1, -1/6}
            // {0, 0, 1, 0}
            // ------------------------------------------------------------------
            //
            // 使用此矩阵计算 c = M_1^(-1) * M_2 * p
            var c0 = p1;
            var c1 = (-p0 + 6 * p1 + 1 * p2) * (1 / 6.0f);
            var c2 = (p1 + 6 * p2 - p3) * (1 / 6.0f);
            var c3 = p2;

            // 最后绘制等效于所需 Catmull-Rom 样条的贝塞尔曲线
            Bezier(c0, c1, c2, c3);
        }

        /// <summary>
        /// 在两点之间绘制箭头。
        ///
        /// 箭头大小默认为箭头长度的 20%。
        ///
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="ArrowheadArc"/>
        /// See: <see cref="Arrow(float3,float3,float3,float)"/>
        /// See: <see cref="ArrowRelativeSizeHead"/>
        /// </summary>
        /// <param name="from">箭头的底部。</param>
        /// <param name="to">箭头的头部。</param>
        public void Arrow(float3 from, float3 to)
        {
            ArrowRelativeSizeHead(from, to, DEFAULT_UP, 0.2f);
        }

        /// <summary>
        /// 在两点之间绘制箭头。
        ///
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="ArrowRelativeSizeHead"/>
        /// See: <see cref="ArrowheadArc"/>
        /// </summary>
        /// <param name="from">箭头的底部。</param>
        /// <param name="to">箭头的头部。</param>
        /// <param name="up">世界的向上方向，箭头平面将尽可能垂直于此方向。默认为 Vector3.up。</param>
        /// <param name="headSize">箭头在世界单位中的大小。</param>
        public void Arrow(float3 from, float3 to, float3 up, float headSize)
        {
            var length_sq = math.lengthsq(to - from);

            if (length_sq > 0.000001f)
            {
                ArrowRelativeSizeHead(from, to, up, headSize * math.rsqrt(length_sq));
            }
        }

        /// <summary>
        /// 在两点之间绘制箭头，箭头大小随箭头长度变化。
        ///
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="ArrowheadArc"/>
        /// See: <see cref="Arrow"/>
        /// </summary>
        /// <param name="from">箭头的底部。</param>
        /// <param name="to">箭头的头部。</param>
        /// <param name="up">世界的向上方向，箭头平面将尽可能垂直于此方向。</param>
        /// <param name="headFraction">箭头长度为 from 到 to 距离乘以此分数。应在 0 和 1 之间。</param>
        public void ArrowRelativeSizeHead(float3 from, float3 to, float3 up, float headFraction)
        {
            Line(from, to);
            var dir = to - from;

            var normal = math.cross(dir, up);
            // 如果方向恰好与之共线则选择不同的上方向。
            if (math.all(normal == 0)) normal = math.cross(new float3(1, 0, 0), dir);
            // 如果 up=(1,0,0) 则上面的检查会再次生成零向量，选择不同的上方向
            if (math.all(normal == 0)) normal = math.cross(new float3(0, 1, 0), dir);
            normal = math.normalizesafe(normal) * math.length(dir);

            Line(to, to - (dir + normal) * headFraction);
            Line(to, to - (dir - normal) * headFraction);
        }

        /// <summary>
        /// 在一个点处绘制箭头。
        ///
        /// <code>
        /// Draw.Arrowhead(Vector3.zero, Vector3.forward, 0.75f, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Arrow"/>
        /// See: <see cref="ArrowRelativeSizeHead"/>
        /// </summary>
        /// <param name="center">箭头中心。</param>
        /// <param name="direction">箭头指向的方向。</param>
        /// <param name="radius">从中心到箭头每个角的距离。</param>
        public void Arrowhead(float3 center, float3 direction, float radius)
        {
            Arrowhead(center, direction, DEFAULT_UP, radius);
        }

        /// <summary>
        /// 在一个点处绘制箭头。
        ///
        /// <code>
        /// Draw.Arrowhead(Vector3.zero, Vector3.forward, 0.75f, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Arrow"/>
        /// See: <see cref="ArrowRelativeSizeHead"/>
        /// </summary>
        /// <param name="center">箭头中心。</param>
        /// <param name="direction">箭头指向的方向。</param>
        /// <param name="up">世界的向上方向，箭头平面将尽可能垂直于此方向。默认为 Vector3.up。必须归一化。</param>
        /// <param name="radius">从中心到箭头每个角的距离。</param>
        public void Arrowhead(float3 center, float3 direction, float3 up, float radius)
        {
            if (math.all(direction == 0)) return;
            direction = math.normalizesafe(direction);
            var normal = math.cross(direction, up);
            const float SinPiOver3 = 0.866025f;
            const float CosPiOver3 = 0.5f;
            var circleCenter = center - radius * (1 - CosPiOver3) * 0.5f * direction;
            var p1 = circleCenter + radius * direction;
            var p2 = circleCenter - radius * CosPiOver3 * direction + radius * SinPiOver3 * normal;
            var p3 = circleCenter - radius * CosPiOver3 * direction - radius * SinPiOver3 * normal;
            Line(p1, p2);
            Line(p2, circleCenter);
            Line(circleCenter, p3);
            Line(p3, p1);
        }

        /// <summary>
        /// 绘制以圆为中心的箭头。
        ///
        /// 这可以用于例如显示角色移动的方向。
        ///
        /// [Open online documentation to see images]
        ///
        /// 注意：上图中箭头是此方法唯一绘制的部分。圆柱体仅用于提供上下文。
        ///
        /// See: <see cref="Arrow"/>
        /// </summary>
        /// <param name="origin">弧线居中的点</param>
        /// <param name="direction">箭头指向的方向</param>
        /// <param name="offset">箭头从原点开始的距离。</param>
        /// <param name="width">箭头宽度（度），默认 60。应在 0 和 90 之间。</param>
        public void ArrowheadArc(float3 origin, float3 direction, float offset, float width = 60)
        {
            if (!math.any(direction)) return;
            if (offset < 0) throw new System.ArgumentOutOfRangeException(nameof(offset));
            if (offset == 0) return;

            var rot = Quaternion.LookRotation(direction, DEFAULT_UP);
            PushMatrix(Matrix4x4.TRS(origin, rot, Vector3.one));
            var a1 = math.PI * 0.5f - width * (0.5f * Mathf.Deg2Rad);
            var a2 = math.PI * 0.5f + width * (0.5f * Mathf.Deg2Rad);
            CircleXZInternal(float3.zero, offset, a1, a2);
            var p1 = new float3(math.cos(a1), 0, math.sin(a1)) * offset;
            var p2 = new float3(math.cos(a2), 0, math.sin(a2)) * offset;
            const float sqrt2 = 1.4142f;
            var p3 = new float3(0, 0, sqrt2 * offset);
            Line(p1, p3);
            Line(p3, p2);
            PopMatrix();
        }

        /// <summary>
        /// 绘制线条网格。
        ///
        /// <code>
        /// Draw.xz.WireGrid(Vector3.zero, new int2(3, 3), new float2(1, 1), color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">网格中心</param>
        /// <param name="rotation">网格旋转。网格将对齐到旋转的 X 和 Z 轴。</param>
        /// <param name="cells">网格单元数。应大于 0。</param>
        /// <param name="totalSize">网格沿 X 和 Z 轴的总大小。</param>
        public void WireGrid(float3 center, quaternion rotation, int2 cells, float2 totalSize)
        {
            cells = math.max(cells, new int2(1, 1));
            PushMatrix(float4x4.TRS(center, rotation, new Vector3(totalSize.x, 0, totalSize.y)));
            int w = cells.x;
            int h = cells.y;
            for (int i = 0; i <= w; i++) Line(new float3(i / (float)w - 0.5f, 0, -0.5f), new float3(i / (float)w - 0.5f, 0, 0.5f));
            for (int i = 0; i <= h; i++) Line(new float3(-0.5f, 0, i / (float)h - 0.5f), new float3(0.5f, 0, i / (float)h - 0.5f));
            PopMatrix();
        }

        /// <summary>
        /// 绘制三角形轮廓。
        ///
        /// <code>
        /// Draw.WireTriangle(new Vector3(-0.5f, 0, 0), new Vector3(0, 1, 0), new Vector3(0.5f, 0, 0), Color.black);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="Draw.WirePlane(float3,quaternion,float2)"/>
        /// See: <see cref="WirePolygon"/>
        /// See: <see cref="SolidTriangle"/>
        /// </summary>
        /// <param name="a">三角形第一个角</param>
        /// <param name="b">三角形第二个角</param>
        /// <param name="c">三角形第三个角</param>
        public void WireTriangle(float3 a, float3 b, float3 c)
        {
            Line(a, b);
            Line(b, c);
            Line(c, a);
        }


        /// <summary>
        /// 绘制矩形轮廓。
        /// 矩形将沿旋转的 X 和 Z 轴方向排列。
        ///
        /// <code>
        /// Draw.WireRectangle(new Vector3(0f, 0, 0), Quaternion.identity, new Vector2(1, 1), Color.black);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// 这与 <see cref="Draw.WirePlane(float3,quaternion,float2)"/> 相同，但为了一致性添加了此名称。
        ///
        /// See: <see cref="WirePolygon"/>
        /// </summary>
        public void WireRectangle(float3 center, quaternion rotation, float2 size)
        {
            WirePlane(center, rotation, size);
        }


        /// <summary>
        /// 绘制三角形轮廓。
        ///
        /// <code>
        /// Draw.WireTriangle(Vector3.zero, Quaternion.identity, 0.5f, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// 注意：这是 <see cref="WirePolygon(float3,int,quaternion,float)"/> 的便捷封装
        ///
        /// See: <see cref="WireTriangle(float3,float3,float3)"/>
        /// </summary>
        /// <param name="center">三角形中心。</param>
        /// <param name="rotation">三角形旋转。从旋转角度看，第一个顶点将在中心前方 radius 个单位处。</param>
        /// <param name="radius">从中心到每个顶点的距离。</param>
        public void WireTriangle(float3 center, quaternion rotation, float radius)
        {
            WirePolygon(center, 3, rotation, radius);
        }

        /// <summary>
        /// 绘制五边形轮廓。
        ///
        /// <code>
        /// Draw.WirePentagon(Vector3.zero, Quaternion.identity, 0.5f, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// 注意：这是 <see cref="WirePolygon(float3,int,quaternion,float)"/> 的便捷封装
        /// </summary>
        /// <param name="center">多边形中心。</param>
        /// <param name="rotation">多边形旋转。从旋转角度看，第一个顶点将在中心前方 radius 个单位处。</param>
        /// <param name="radius">从中心到每个顶点的距离。</param>
        public void WirePentagon(float3 center, quaternion rotation, float radius)
        {
            WirePolygon(center, 5, rotation, radius);
        }

        /// <summary>
        /// 绘制六边形轮廓。
        ///
        /// <code>
        /// Draw.WireHexagon(Vector3.zero, Quaternion.identity, 0.5f, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// 注意：这是 <see cref="WirePolygon(float3,int,quaternion,float)"/> 的便捷封装
        /// </summary>
        /// <param name="center">多边形中心。</param>
        /// <param name="rotation">多边形旋转。从旋转角度看，第一个顶点将在中心前方 radius 个单位处。</param>
        /// <param name="radius">从中心到每个顶点的距离。</param>
        public void WireHexagon(float3 center, quaternion rotation, float radius)
        {
            WirePolygon(center, 6, rotation, radius);
        }

        /// <summary>
        /// 绘制正多边形轮廓。
        ///
        /// <code>
        /// Draw.WirePolygon(new Vector3(-0.5f, 0, +0.5f), 3, Quaternion.identity, 0.4f, color);
        /// Draw.WirePolygon(new Vector3(+0.5f, 0, +0.5f), 4, Quaternion.identity, 0.4f, color);
        /// Draw.WirePolygon(new Vector3(-0.5f, 0, -0.5f), 5, Quaternion.identity, 0.4f, color);
        /// Draw.WirePolygon(new Vector3(+0.5f, 0, -0.5f), 6, Quaternion.identity, 0.4f, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="WireTriangle"/>
        /// See: <see cref="WirePentagon"/>
        /// See: <see cref="WireHexagon"/>
        /// </summary>
        /// <param name="center">多边形中心。</param>
        /// <param name="vertices">多边形的角（和边）数。</param>
        /// <param name="rotation">多边形旋转。从旋转角度看，第一个顶点将在中心前方 radius 个单位处。</param>
        /// <param name="radius">从中心到每个顶点的距离。</param>
        public void WirePolygon(float3 center, int vertices, quaternion rotation, float radius)
        {
            PushMatrix(float4x4.TRS(center, rotation, new float3(radius, radius, radius)));
            float3 prev = new float3(0, 0, 1);
            for (int i = 1; i <= vertices; i++)
            {
                float a = 2 * math.PI * (i / (float)vertices);
                var p = new float3(math.sin(a), 0, math.cos(a));
                Line(prev, p);
                prev = p;
            }
            PopMatrix();
        }


        /// <summary>
        /// 绘制实心平面。
        ///
        /// <code>
        /// Draw.SolidPlane(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">可视化平面的中心。</param>
        /// <param name="normal">垂直于平面的方向。如果为 (0,0,0) 则不会渲染。</param>
        /// <param name="size">可视化平面的宽度和高度。</param>
        public void SolidPlane(float3 center, float3 normal, float2 size)
        {
            if (math.any(normal))
            {
                SolidPlane(center, Quaternion.LookRotation(calculateTangent(normal), normal), size);
            }
        }

        /// <summary>
        /// 绘制实心平面。
        ///
        /// 平面将相对于旋转位于 XZ 平面中。
        ///
        /// <code>
        /// Draw.SolidPlane(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">可视化平面的中心。</param>
        /// <param name="size">可视化平面的宽度和高度。</param>
        public void SolidPlane(float3 center, quaternion rotation, float2 size)
        {
            PushMatrix(float4x4.TRS(center, rotation, new float3(size.x, 0, size.y)));
            Reserve<BoxData>();
            Add(Command.Box);
            Add(new BoxData { center = 0, size = 1 });
            PopMatrix();
        }

        /// <summary>返回与给定向量正交的任意向量</summary>
        private static float3 calculateTangent(float3 normal)
        {
            var tangent = math.cross(new float3(0, 1, 0), normal);

            if (math.all(tangent == 0)) tangent = math.cross(new float3(1, 0, 0), normal);
            return tangent;
        }

        /// <summary>
        /// 绘制线框平面。
        ///
        /// <code>
        /// Draw.WirePlane(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">可视化平面的中心。</param>
        /// <param name="normal">垂直于平面的方向。如果为 (0,0,0) 则不会渲染。</param>
        /// <param name="size">可视化平面的宽度和高度。</param>
        public void WirePlane(float3 center, float3 normal, float2 size)
        {
            if (math.any(normal))
            {
                WirePlane(center, Quaternion.LookRotation(calculateTangent(normal), normal), size);
            }
        }

        /// <summary>
        /// 绘制线框平面。
        ///
        /// 这与 <see cref="WireRectangle(float3,quaternion,float2)"/> 相同，为一致性而包含。
        ///
        /// <code>
        /// Draw.WirePlane(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">可视化平面的中心。</param>
        /// <param name="rotation">Rotation of the plane. 平面将相对于旋转位于 XZ 平面中。</param>
        /// <param name="size">可视化平面的宽度和高度。</param>
        public void WirePlane(float3 center, quaternion rotation, float2 size)
        {
            Reserve<PlaneData>();
            Add(Command.WirePlane);
            Add(new PlaneData { center = center, rotation = rotation, size = size });
        }

        /// <summary>
        /// 绘制平面及其法线的可视化。
        ///
        /// <code>
        /// Draw.PlaneWithNormal(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">可视化平面的中心。</param>
        /// <param name="normal">垂直于平面的方向。如果为 (0,0,0) 则不会渲染。</param>
        /// <param name="size">可视化平面的宽度和高度。</param>
        public void PlaneWithNormal(float3 center, float3 normal, float2 size)
        {
            if (math.any(normal))
            {
                PlaneWithNormal(center, Quaternion.LookRotation(calculateTangent(normal), normal), size);
            }
        }

        /// <summary>
        /// 绘制平面及其法线的可视化。
        ///
        /// <code>
        /// Draw.PlaneWithNormal(new float3(0, 0, 0), new float3(0, 1, 0), 1.0f, color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">可视化平面的中心。</param>
        /// <param name="rotation">Rotation of the plane. 平面将相对于旋转位于 XZ 平面中。</param>
        /// <param name="size">可视化平面的宽度和高度。</param>
        public void PlaneWithNormal(float3 center, quaternion rotation, float2 size)
        {
            SolidPlane(center, rotation, size);
            WirePlane(center, rotation, size);
            ArrowRelativeSizeHead(center, center + math.mul(rotation, new float3(0, 1, 0)) * 0.5f, math.mul(rotation, new float3(0, 0, 1)), 0.2f);
        }

        /// <summary>
        /// 绘制实心三角形。
        ///
        /// <code>
        /// Draw.xy.SolidTriangle(new float2(-0.43f, -0.25f), new float2(0, 0.5f), new float2(0.43f, -0.25f), color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// 注意：如果要绘制大量三角形，最好使用 <see cref="Draw.SolidMesh"/>，效率更高。
        ///
        /// See: <see cref="Draw.SolidMesh"/>
        /// See: <see cref="Draw.WireTriangle"/>
        /// </summary>
        /// <param name="a">三角形第一个角。</param>
        /// <param name="b">三角形第二个角。</param>
        /// <param name="c">三角形第三个角。</param>
        public void SolidTriangle(float3 a, float3 b, float3 c)
        {
            Reserve<TriangleData>();
            Add(Command.SolidTriangle);
            Add(new TriangleData { a = a, b = b, c = c });
        }

        /// <summary>
        /// 绘制实心方盒。
        ///
        /// <code>
        /// Draw.SolidBox(new float3(0, 0, 0), new float3(1, 1, 1), color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">方盒中心</param>
        /// <param name="size">方盒沿各维度的宽度</param>
        public void SolidBox(float3 center, float3 size)
        {
            Reserve<BoxData>();
            Add(Command.Box);
            Add(new BoxData { center = center, size = size });
        }

        /// <summary>
        /// 绘制实心方盒。
        ///
        /// <code>
        /// Draw.SolidBox(new float3(0, 0, 0), new float3(1, 1, 1), color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="bounds">方盒的包围盒</param>
        public void SolidBox(Bounds bounds)
        {
            SolidBox(bounds.center, bounds.size);
        }

        /// <summary>
        /// 绘制实心方盒。
        ///
        /// <code>
        /// Draw.SolidBox(new float3(0, 0, 0), new float3(1, 1, 1), color);
        /// </code>
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="center">方盒中心</param>
        /// <param name="rotation">方盒旋转</param>
        /// <param name="size">方盒沿各维度的宽度</param>
        public void SolidBox(float3 center, quaternion rotation, float3 size)
        {
            PushMatrix(float4x4.TRS(center, rotation, size));
            SolidBox(float3.zero, Vector3.one);
            PopMatrix();
        }

        /// <summary>
        /// 在 3D 空间中绘制标签。
        ///
        /// The default alignment is <see cref="VisualShape.LabelAlignment.MiddleLeft"/>.
        ///
        /// <code>
        /// Draw.Label3D(new float3(0.2f, -1f, 0.2f), Quaternion.Euler(45, -110, -90), "Label", 1, LabelAlignment.Center, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: Label3D(float3,quaternion,string,float,LabelAlignment)
        ///
        /// 注意：仅支持 ASCII，因为内置字体纹理仅包含 ASCII。其他字符将渲染为问号 (?)。
        /// </summary>
        /// <param name="position">3D 空间中的位置。</param>
        /// <param name="rotation">3D 空间中的旋转。</param>
        /// <param name="text">要显示的文本。</param>
        /// <param name="size">文本的世界大小。对于大尺寸使用 SDF（有符号距离场）字体，小尺寸使用普通字体纹理。</param>
        public void Label3D(float3 position, quaternion rotation, string text, float size)
        {
            Label3D(position, rotation, text, size, LabelAlignment.MiddleLeft);
        }

        /// <summary>
        /// 在 3D 空间中绘制标签。
        ///
        /// <code>
        /// Draw.Label3D(new float3(0.2f, -1f, 0.2f), Quaternion.Euler(45, -110, -90), "Label", 1, LabelAlignment.Center, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: Label3D(float3,quaternion,string,float)
        ///
        /// 注意：仅支持 ASCII，因为内置字体纹理仅包含 ASCII。其他字符将渲染为问号 (?)。
        ///
        /// 注意：此方法不能在 Burst 中使用，因为 Burst 不支持托管字符串。但可以使用接受 FixedString 的 Label3D 重载。
        /// </summary>
        /// <param name="position">3D 空间中的位置。</param>
        /// <param name="rotation">3D 空间中的旋转。</param>
        /// <param name="text">要显示的文本。</param>
        /// <param name="size">文本的世界大小。对于大尺寸使用 SDF（有符号距离场）字体，小尺寸使用普通字体纹理。</param>
        /// <param name="alignment">文本相对于给定位置的对齐方式。</param>
        public void Label3D(float3 position, quaternion rotation, string text, float size, LabelAlignment alignment)
        {
            AssertBufferExists();
            var g = gizmos.Target as ShapeData;
            Reserve<TextData3D>();
            Add(Command.Text3D);
            Add(new TextData3D { center = position, rotation = rotation, numCharacters = text.Length, size = size, alignment = alignment });

            Reserve(UnsafeUtility.SizeOf<System.UInt16>() * text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                System.UInt16 index = (System.UInt16)g.fontData.GetIndex(c);
                Add(index);
            }
        }

        /// <summary>
        /// 在 3D 空间中绘制与相机对齐的标签。
        ///
        /// The default alignment is <see cref="VisualShape.LabelAlignment.MiddleLeft"/>.
        ///
        /// <code>
        /// Draw.Label2D(Vector3.zero, "Label", 48, LabelAlignment.Center, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: Label2D(float3,string,float,LabelAlignment)
        ///
        /// 注意：仅支持 ASCII，因为内置字体纹理仅包含 ASCII。其他字符将渲染为问号 (?)。
        /// </summary>
        /// <param name="position">3D 空间中的位置。</param>
        /// <param name="text">要显示的文本。</param>
        /// <param name="sizeInPixels">文本的屏幕像素大小。对于大尺寸使用 SDF（有符号距离场）字体，小尺寸使用普通字体纹理。</param>
        public void Label2D(float3 position, string text, float sizeInPixels = 14)
        {
            Label2D(position, text, sizeInPixels, LabelAlignment.MiddleLeft);
        }

        /// <summary>
        /// 在 3D 空间中绘制与相机对齐的标签。
        ///
        /// <code>
        /// Draw.Label2D(Vector3.zero, "Label", 48, LabelAlignment.Center, color);
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: Label2D(float3,string,float)
        ///
        /// 注意：仅支持 ASCII，因为内置字体纹理仅包含 ASCII。其他字符将渲染为问号 (?)。
        ///
        /// 注意：此方法不能在 Burst 中使用，因为 Burst 不支持托管字符串。但可以使用接受 FixedString 的 Label2D 重载。
        /// </summary>
        /// <param name="position">3D 空间中的位置。</param>
        /// <param name="text">要显示的文本。</param>
        /// <param name="sizeInPixels">文本的屏幕像素大小。对于大尺寸使用 SDF（有符号距离场）字体，小尺寸使用普通字体纹理。</param>
        /// <param name="alignment">文本相对于给定位置的对齐方式。</param>
        public void Label2D(float3 position, string text, float sizeInPixels, LabelAlignment alignment)
        {
            AssertBufferExists();
            var g = gizmos.Target as ShapeData;
            Reserve<TextData>();
            Add(Command.Text);
            Add(new TextData { center = position, numCharacters = text.Length, sizeInPixels = sizeInPixels, alignment = alignment });

            Reserve(UnsafeUtility.SizeOf<System.UInt16>() * text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                System.UInt16 index = (System.UInt16)g.fontData.GetIndex(c);
                Add(index);
            }
        }

        #region Label2DFixedString
        /// <summary>
        /// 在 3D 空间中绘制与相机对齐的标签。
        ///
        /// <code>
        /// // 这部分可以在 Burst Job 内部
        /// for (int i = 0; i < 10; i++) {
        ///     Unity.Collections.FixedString32Bytes text = $"X = {i}";
        ///     builder.Label2D(new float3(i, 0, 0), ref text, 12, LabelAlignment.Center);
        /// }
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: Label2D(float3,string,float)
        ///
        /// 注意：仅支持 ASCII，因为内置字体纹理仅包含 ASCII。其他字符将渲染为问号 (?)。
        ///
        /// 注意：此方法需要 Unity.Collections 包版本 0.8 或更高。
        /// </summary>
        /// <param name="position">3D 空间中的位置。</param>
        /// <param name="text">要显示的文本。</param>
        /// <param name="sizeInPixels">文本的屏幕像素大小。对于大尺寸使用 SDF（有符号距离场）字体，小尺寸使用普通字体纹理。</param>
        public void Label2D(float3 position, ref FixedString32Bytes text, float sizeInPixels = 14)
        {
            Label2D(position, ref text, sizeInPixels, LabelAlignment.MiddleLeft);
        }

        /// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float)}</summary>
        public void Label2D(float3 position, ref FixedString64Bytes text, float sizeInPixels = 14)
        {
            Label2D(position, ref text, sizeInPixels, LabelAlignment.MiddleLeft);
        }

        /// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float)}</summary>
        public void Label2D(float3 position, ref FixedString128Bytes text, float sizeInPixels = 14)
        {
            Label2D(position, ref text, sizeInPixels, LabelAlignment.MiddleLeft);
        }

        /// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float)}</summary>
        public void Label2D(float3 position, ref FixedString512Bytes text, float sizeInPixels = 14)
        {
            Label2D(position, ref text, sizeInPixels, LabelAlignment.MiddleLeft);
        }

        /// <summary>
        /// 在 3D 空间中绘制与相机对齐的标签。
        ///
        /// <code>
        /// // 这部分可以在 Burst Job 内部
        /// for (int i = 0; i < 10; i++) {
        ///     Unity.Collections.FixedString32Bytes text = $"X = {i}";
        ///     builder.Label2D(new float3(i, 0, 0), ref text, 12, LabelAlignment.Center);
        /// }
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: Label2D(float3,string,float)
        ///
        /// 注意：仅支持 ASCII，因为内置字体纹理仅包含 ASCII。其他字符将渲染为问号 (?)。
        ///
        /// 注意：此方法需要 Unity.Collections 包版本 0.8 或更高。
        /// </summary>
        /// <param name="position">3D 空间中的位置。</param>
        /// <param name="text">要显示的文本。</param>
        /// <param name="sizeInPixels">文本的屏幕像素大小。对于大尺寸使用 SDF（有符号距离场）字体，小尺寸使用普通字体纹理。</param>
        /// <param name="alignment">文本相对于给定位置的对齐方式。</param>
        public void Label2D(float3 position, ref FixedString32Bytes text, float sizeInPixels, LabelAlignment alignment)
        {
            unsafe
            {
                Label2D(position, text.GetUnsafePtr(), text.Length, sizeInPixels, alignment);
            }
        }

        /// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float,LabelAlignment)}</summary>
        public void Label2D(float3 position, ref FixedString64Bytes text, float sizeInPixels, LabelAlignment alignment)
        {
            unsafe
            {
                Label2D(position, text.GetUnsafePtr(), text.Length, sizeInPixels, alignment);
            }
        }

        /// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float,LabelAlignment)}</summary>
        public void Label2D(float3 position, ref FixedString128Bytes text, float sizeInPixels, LabelAlignment alignment)
        {
            unsafe
            {
                Label2D(position, text.GetUnsafePtr(), text.Length, sizeInPixels, alignment);
            }
        }

        /// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float,LabelAlignment)}</summary>
        public void Label2D(float3 position, ref FixedString512Bytes text, float sizeInPixels, LabelAlignment alignment)
        {
            unsafe
            {
                Label2D(position, text.GetUnsafePtr(), text.Length, sizeInPixels, alignment);
            }
        }

        /// <summary>\copydocref{Label2D(float3,FixedString32Bytes,float,LabelAlignment)}</summary>
        internal unsafe void Label2D(float3 position, byte* text, int byteCount, float sizeInPixels, LabelAlignment alignment)
        {
            AssertBufferExists();
            Reserve<TextData>();
            Add(Command.Text);
            Add(new TextData { center = position, numCharacters = byteCount, sizeInPixels = sizeInPixels, alignment = alignment });

            Reserve(UnsafeUtility.SizeOf<System.UInt16>() * byteCount);
            for (int i = 0; i < byteCount; i++)
            {
                // The first 128 elements in the font data are guaranteed to be laid out as ascii.
                // We use this since we cannot use the dynamic font lookup.
                System.UInt16 c = *(text + i);
                if (c >= 128) c = (System.UInt16)'?';
                if (c == (byte)'\n') c = SDFLookupData.Newline;
                // Ignore carriage return instead of printing them as '?'. Windows encodes newlines as \r\n.
                if (c == (byte)'\r') continue;
                Add(c);
            }
        }
        #endregion

        #region Label3DFixedString
        /// <summary>
        /// 在 3D 空间中绘制标签。
        ///
        /// <code>
        /// // 这部分可以在 Burst Job 内部
        /// for (int i = 0; i < 10; i++) {
        ///     Unity.Collections.FixedString32Bytes text = $"X = {i}";
        ///     builder.Label3D(new float3(i, 0, 0), quaternion.identity, ref text, 1, LabelAlignment.Center);
        /// }
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: Label3D(float3,quaternion,string,float)
        ///
        /// 注意：仅支持 ASCII，因为内置字体纹理仅包含 ASCII。其他字符将渲染为问号 (?)。
        ///
        /// 注意：此方法需要 Unity.Collections 包版本 0.8 或更高。
        /// </summary>
        /// <param name="position">3D 空间中的位置。</param>
        /// <param name="rotation">3D 空间中的旋转。</param>
        /// <param name="text">要显示的文本。</param>
        /// <param name="size">文本的世界大小。对于大尺寸使用 SDF（有符号距离场）字体，小尺寸使用普通字体纹理。</param>
        public void Label3D(float3 position, quaternion rotation, ref FixedString32Bytes text, float size)
        {
            Label3D(position, rotation, ref text, size, LabelAlignment.MiddleLeft);
        }

        /// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float)}</summary>
        public void Label3D(float3 position, quaternion rotation, ref FixedString64Bytes text, float size)
        {
            Label3D(position, rotation, ref text, size, LabelAlignment.MiddleLeft);
        }

        /// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float)}</summary>
        public void Label3D(float3 position, quaternion rotation, ref FixedString128Bytes text, float size)
        {
            Label3D(position, rotation, ref text, size, LabelAlignment.MiddleLeft);
        }

        /// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float)}</summary>
        public void Label3D(float3 position, quaternion rotation, ref FixedString512Bytes text, float size)
        {
            Label3D(position, rotation, ref text, size, LabelAlignment.MiddleLeft);
        }

        /// <summary>
        /// 在 3D 空间中绘制标签。
        ///
        /// <code>
        /// // 这部分可以在 Burst Job 内部
        /// for (int i = 0; i < 10; i++) {
        ///     Unity.Collections.FixedString32Bytes text = $"X = {i}";
        ///     builder.Label3D(new float3(i, 0, 0), quaternion.identity, ref text, 1, LabelAlignment.Center);
        /// }
        /// </code>
        /// [Open online documentation to see images]
        ///
        /// See: Label3D(float3,quaternion,string,float)
        ///
        /// 注意：仅支持 ASCII，因为内置字体纹理仅包含 ASCII。其他字符将渲染为问号 (?)。
        ///
        /// 注意：此方法需要 Unity.Collections 包版本 0.8 或更高。
        /// </summary>
        /// <param name="position">3D 空间中的位置。</param>
        /// <param name="rotation">3D 空间中的旋转。</param>
        /// <param name="text">要显示的文本。</param>
        /// <param name="size">文本的世界大小。对于大尺寸使用 SDF（有符号距离场）字体，小尺寸使用普通字体纹理。</param>
        /// <param name="alignment">文本相对于给定位置的对齐方式。</param>
        public void Label3D(float3 position, quaternion rotation, ref FixedString32Bytes text, float size, LabelAlignment alignment)
        {
            unsafe
            {
                Label3D(position, rotation, text.GetUnsafePtr(), text.Length, size, alignment);
            }
        }

        /// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float,LabelAlignment)}</summary>
        public void Label3D(float3 position, quaternion rotation, ref FixedString64Bytes text, float size, LabelAlignment alignment)
        {
            unsafe
            {
                Label3D(position, rotation, text.GetUnsafePtr(), text.Length, size, alignment);
            }
        }

        /// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float,LabelAlignment)}</summary>
        public void Label3D(float3 position, quaternion rotation, ref FixedString128Bytes text, float size, LabelAlignment alignment)
        {
            unsafe
            {
                Label3D(position, rotation, text.GetUnsafePtr(), text.Length, size, alignment);
            }
        }

        /// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float,LabelAlignment)}</summary>
        public void Label3D(float3 position, quaternion rotation, ref FixedString512Bytes text, float size, LabelAlignment alignment)
        {
            unsafe
            {
                Label3D(position, rotation, text.GetUnsafePtr(), text.Length, size, alignment);
            }
        }

        /// <summary>\copydocref{Label3D(float3,quaternion,FixedString32Bytes,float,LabelAlignment)}</summary>
        internal unsafe void Label3D(float3 position, quaternion rotation, byte* text, int byteCount, float size, LabelAlignment alignment)
        {
            AssertBufferExists();
            Reserve<TextData3D>();
            Add(Command.Text3D);
            Add(new TextData3D { center = position, rotation = rotation, numCharacters = byteCount, size = size, alignment = alignment });

            Reserve(UnsafeUtility.SizeOf<System.UInt16>() * byteCount);
            for (int i = 0; i < byteCount; i++)
            {
                // The first 128 elements in the font data are guaranteed to be laid out as ascii.
                // We use this since we cannot use the dynamic font lookup.
                System.UInt16 c = *(text + i);
                if (c >= 128) c = (System.UInt16)'?';
                if (c == (byte)'\n') c = SDFLookupData.Newline;
                Add(c);
            }
        }
        #endregion
    }
}


