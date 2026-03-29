using UnityEngine;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Rendering;
using System.Diagnostics;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Profiling;
using System.Linq;

namespace VisualShape
{
    using VisualShape.Text;
    using Unity.Profiling;

    public static class SharedShapeData
    {
        /// <summary>
        /// 与 Time.time 相同，但更新频率较低。
        /// 因为 Burst Job 无法访问 Time.time 所以使用此变量。
        /// </summary>
        public static readonly Unity.Burst.SharedStatic<float> BurstTime = Unity.Burst.SharedStatic<float>.GetOrCreate<ShapeManager, BurstTimeKey>(4);

        private class BurstTimeKey { }
    }

    /// <summary>
    /// 用于跨多帧缓存绘制数据。
    /// 当你在多个连续帧中绘制相同内容时，这是一种有用的性能优化。
    ///
    /// <code>
    /// private RedrawScope redrawScope;
    ///
    /// void Start () {
    ///     redrawScope = ShapeManager.GetRedrawScope();
    ///     using (var builder = ShapeManager.GetBuilder(redrawScope)) {
    ///         builder.WireSphere(Vector3.zero, 1.0f, Color.red);
    ///     }
    /// }
    ///
    /// void OnDestroy () {
    ///     redrawScope.Dispose();
    /// }
    /// </code>
    ///
    /// See: <see cref="ShapeManager.GetRedrawScope"/>
    /// </summary>
    public struct RedrawScope : System.IDisposable
    {
        // 存储为 GCHandle 以允许在非托管 ECS 组件或系统中存储此结构体
        internal System.Runtime.InteropServices.GCHandle gizmos;
        /// <summary>
        /// 作用域的 ID。
        /// 零表示无效或不存在的作用域。
        /// </summary>
        internal int id;

        static int idCounter = 1;

        internal RedrawScope(ShapeData gizmos, int id)
        {
            this.gizmos = gizmos.gizmosHandle;
            this.id = id;
        }

        internal RedrawScope(ShapeData gizmos)
        {
            this.gizmos = gizmos.gizmosHandle;
            // 40 亿个 ID 在回绕前应该足够了。
            id = idCounter++;
        }

        /// <summary>
        /// 使用此作用域渲染的且不超过一帧的所有内容将重新绘制。
        /// 如果由于某种原因无法在帧内绘制某些项（例如某个异步过程正在修改内容），这很有用
        /// 但你仍然想绘制与上一帧相同的内容以至少绘制*一些东西*。
        ///
        /// 注意：项的年龄将被重置。因此下一帧你可以调用
        /// 此方法再次绘制这些项。
        /// </summary>
        internal void Draw()
        {
            if (gizmos.IsAllocated)
            {
                if (gizmos.Target is ShapeData gizmosTarget) gizmosTarget.Draw(this);
            }
        }

        /// <summary>
        /// 停止保持所有之前渲染的项存活，并开始一个新作用域。
        /// 等同于先在旧作用域上调用 Dispose 然后创建新的。
        /// </summary>
        public void Rewind()
        {
            Dispose();
            this = ShapeManager.GetRedrawScope();
        }

        internal void DrawUntilDispose()
        {
            if (gizmos.Target is ShapeData gizmosTarget) gizmosTarget.DrawUntilDisposed(this);
        }

        /// <summary>
        /// 释放重绘作用域以停止渲染这些项。
        ///
        /// 使用完作用域后必须执行此操作，即使它从未用于实际渲染任何内容。
        /// 这些项将立即停止渲染：下一个渲染的相机将不会渲染这些项，除非以其他方式保持存活。
        /// 例如项至少会被渲染一次。
        /// </summary>
        public void Dispose()
        {
            if (gizmos.IsAllocated)
            {
                if (gizmos.Target is ShapeData gizmosTarget) gizmosTarget.DisposeRedrawScope(this);
            }
            gizmos = default;
        }
    };

    /// <summary>
    /// 以高性能方式绘制 Gizmos 的辅助类。
    /// 这是 Unity Gizmos 类的替代方案，因为该类在绘制大量几何体时性能不佳
    /// （例如大型网格图）。
    /// 这些 Gizmos 可以是持久的，如果数据不变，Gizmos
    /// 不需要更新。
    ///
    /// 使用方法
    /// - 创建 Hasher 对象并哈希你将用于绘制 Gizmos 的数据
    ///      例如顶点的位置等。只要
    ///      Gizmos 变化时哈希也会变化即可。
    /// - 检查该哈希是否存在缓存的网格
    /// - 如果没有，创建 Builder 对象并调用绘制方法直到完成
    ///      然后使用 Gizmos 类的引用和之前计算的哈希调用 Finalize。
    /// - 使用哈希调用 gizmos.Draw。
    /// - 当此帧的 Gizmos 绘制完成后，调用 gizmos.FinalizeDraw
    ///
    /// <code>
    /// var a = Vector3.zero;
    /// var b = Vector3.one;
    /// var color = Color.red;
    /// var hasher = ShapeData.Hasher.Create(this);
    ///
    /// hasher.Add(a);
    /// hasher.Add(b);
    /// hasher.Add(color);
    /// var gizmos = ShapeManager.instance.gizmos;
    /// if (!gizmos.Draw(hasher)) {
    ///     using (var builder = gizmos.GetBuilder(hasher)) {
    ///         // 最好是非常复杂的内容，而不仅仅是一条线
    ///         builder.Line(a, b, color);
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class ShapeData
    {
        /// <summary>将多个哈希值组合为单个哈希值</summary>
        public struct Hasher : IEquatable<Hasher>
        {
            ulong hash;

            public static Hasher NotSupplied => new Hasher { hash = ulong.MaxValue };

            public static Hasher Create<T>(T init)
            {
                var h = new Hasher();

                h.Add(init);
                return h;
            }

            public void Add<T>(T hash)
            {
                // 普通的哈希函数。+ 12289 是为了确保哈希零值不会只产生零（以及一般情况下哈希 X 不会产生 X 的哈希）
                // （结构体无法提供默认初始化）
                this.hash = (1572869UL * this.hash) ^ (ulong)hash.GetHashCode() + 12289;
            }

            public ulong Hash
            {
                get
                {
                    return hash;
                }
            }

            public override int GetHashCode()
            {
                return (int)hash;
            }

            public bool Equals(Hasher other)
            {
                return hash == other.hash;
            }
        }

        internal struct ProcessedBuilderData
        {
            public enum Type
            {
                Invalid = 0,
                Static,
                Dynamic,
                Persistent,
            }

            public Type type;
            public BuilderData.Meta meta;
            bool submitted;

            // MeshBuffers 结构体的单个实例。
            // 需要存储在 NativeArray 中因为我们将用作指针
            // 并且需要保证在内存中保持不变的位置。
            public NativeArray<MeshBuffers> temporaryMeshBuffers;
            JobHandle buildJob, splitterJob;
            public List<MeshWithType> meshes;

            public bool isValid => type != Type.Invalid;

            public struct CapturedState
            {
                public Matrix4x4 matrix;
                public Color color;
            }

            public struct MeshBuffers
            {
                public UnsafeAppendBuffer splitterOutput, vertices, triangles, solidVertices, solidTriangles, textVertices, textTriangles, capturedState;
                public Bounds bounds;

                public MeshBuffers(Allocator allocator)
                {
                    splitterOutput = new UnsafeAppendBuffer(0, 4, allocator);
                    vertices = new UnsafeAppendBuffer(0, 4, allocator);
                    triangles = new UnsafeAppendBuffer(0, 4, allocator);
                    solidVertices = new UnsafeAppendBuffer(0, 4, allocator);
                    solidTriangles = new UnsafeAppendBuffer(0, 4, allocator);
                    textVertices = new UnsafeAppendBuffer(0, 4, allocator);
                    textTriangles = new UnsafeAppendBuffer(0, 4, allocator);
                    capturedState = new UnsafeAppendBuffer(0, 4, allocator);
                    bounds = new Bounds();
                }

                public void Dispose()
                {
                    splitterOutput.Dispose();
                    vertices.Dispose();
                    triangles.Dispose();
                    solidVertices.Dispose();
                    solidTriangles.Dispose();
                    textVertices.Dispose();
                    textTriangles.Dispose();
                    capturedState.Dispose();
                }

                static void DisposeIfLarge(ref UnsafeAppendBuffer ls)
                {
                    if (ls.Length * 3 < ls.Capacity && ls.Capacity > 1024)
                    {
                        var alloc = ls.Allocator;
                        ls.Dispose();
                        ls = new UnsafeAppendBuffer(0, 4, alloc);
                    }
                }

                public void DisposeIfLarge()
                {
                    DisposeIfLarge(ref splitterOutput);
                    DisposeIfLarge(ref vertices);
                    DisposeIfLarge(ref triangles);
                    DisposeIfLarge(ref solidVertices);
                    DisposeIfLarge(ref solidTriangles);
                    DisposeIfLarge(ref textVertices);
                    DisposeIfLarge(ref textTriangles);
                    DisposeIfLarge(ref capturedState);
                }
            }

            public unsafe UnsafeAppendBuffer* splitterOutputPtr => &((MeshBuffers*)temporaryMeshBuffers.GetUnsafePtr())->splitterOutput;

            public void Init(Type type, BuilderData.Meta meta)
            {
                submitted = false;
                this.type = type;
                this.meta = meta;

                if (meshes == null) meshes = new List<MeshWithType>();
                if (!temporaryMeshBuffers.IsCreated)
                {
                    temporaryMeshBuffers = new NativeArray<MeshBuffers>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    temporaryMeshBuffers[0] = new MeshBuffers(Allocator.Persistent);
                }
            }

            static int SubmittedJobs = 0;

            public void SetSplitterJob(ShapeData gizmos, JobHandle splitterJob)
            {
                this.splitterJob = splitterJob;
                if (type == Type.Static)
                {
                    var cameraInfo = new GeometryBuilder.CameraInfo(null);
                    unsafe
                    {
                        buildJob = GeometryBuilder.Build(gizmos, (MeshBuffers*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(temporaryMeshBuffers), ref cameraInfo, splitterJob);
                    }

                    SubmittedJobs++;
                    // ScheduleBatchedJobs 开销大，所以只偶尔执行
                    if (SubmittedJobs % 8 == 0)
                    {
                        MarkerScheduleJobs.Begin();
                        JobHandle.ScheduleBatchedJobs();
                        MarkerScheduleJobs.End();
                    }
                }
            }

            public void SchedulePersistFilter(int version, int lastTickVersion, float time, int sceneModeVersion)
            {
                if (type != Type.Persistent) throw new System.InvalidOperationException();

                // 如果数据来自不同的游戏模式则不应继续存活。
                // 例如编辑器模式 => 游戏模式
                if (meta.sceneModeVersion != sceneModeVersion)
                {
                    meta.version = -1;
                    return;
                }

                // 保证所有绘制命令至少存活一帧
                // 至少给它们一次绘制机会后再过滤。
                // （它们可能实际未被绘制因为可能没有活动相机）
                if (meta.version < lastTickVersion || submitted)
                {
                    splitterJob.Complete();
                    meta.version = version;

                    // 如果命令缓冲区为空则此实例不应继续存活
                    var splitterOutput = temporaryMeshBuffers[0].splitterOutput;
                    if (splitterOutput.Length == 0)
                    {
                        meta.version = -1;
                        return;
                    }

                    buildJob.Complete();
                    unsafe
                    {
                        splitterJob = new PersistentFilterJob
                        {
                            buffer = &((MeshBuffers*)NativeArrayUnsafeUtility.GetUnsafePtr(temporaryMeshBuffers))->splitterOutput,
                            time = time,
                        }.Schedule(splitterJob);
                    }
                }
            }

            public bool IsValidForCamera(Camera camera, bool allowGizmos, bool allowCameraDefault)
            {
                if (!allowGizmos && meta.isGizmos) return false;

                if (meta.cameraTargets != null)
                {
                    return meta.cameraTargets.Contains(camera);
                }
                else
                {
                    return allowCameraDefault;
                }
            }

            public void Schedule(ShapeData gizmos, ref GeometryBuilder.CameraInfo cameraInfo)
            {
                // 静态的 Job 已在 SetSplitterJob 中调度
                if (type != Type.Static)
                {
                    unsafe
                    {
                        buildJob = GeometryBuilder.Build(gizmos, (MeshBuffers*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(temporaryMeshBuffers), ref cameraInfo, splitterJob);
                    }
                }
            }

            public void BuildMeshes(ShapeData gizmos)
            {
                if (type == Type.Static && submitted) return;
                buildJob.Complete();
                unsafe
                {
                    GeometryBuilder.BuildMesh(gizmos, meshes, (MeshBuffers*)temporaryMeshBuffers.GetUnsafePtr());
                }
                submitted = true;
            }

            public void CollectMeshes(List<RenderedMeshWithType> meshes)
            {
                var itemMeshes = this.meshes;
                var customMeshIndex = 0;
                var capturedState = temporaryMeshBuffers[0].capturedState;
                var maxCustomMeshes = capturedState.Length / UnsafeUtility.SizeOf<CapturedState>();

                for (int i = 0; i < itemMeshes.Count; i++)
                {
                    Color color;
                    Matrix4x4 matrix;
                    int drawOrderIndex;
                    if ((itemMeshes[i].type & MeshType.Custom) != 0)
                    {
                        UnityEngine.Assertions.Assert.IsTrue(customMeshIndex < maxCustomMeshes);

                        // 自定义网格的颜色和方向存储在捕获状态数组中。
                        // 它与 #meshes 列表中的自定义网格顺序相同。
                        unsafe
                        {
                            var state = *((CapturedState*)capturedState.Ptr + customMeshIndex);
                            color = state.color;
                            matrix = state.matrix;
                            customMeshIndex += 1;
                        }
                        // 自定义网格在所有类似构建器*之后*渲染。
                        // 实际上这意味着所有自定义网格在所有动态项之后绘制。
                        drawOrderIndex = meta.drawOrderIndex + 1;
                    }
                    else
                    {
                        // 所有其他网格使用默认颜色和单位矩阵
                        // 因为它们的数据已烘焙到顶点颜色和位置中
                        color = Color.white;
                        matrix = Matrix4x4.identity;
                        drawOrderIndex = meta.drawOrderIndex;
                    }
                    meshes.Add(new RenderedMeshWithType
                    {
                        mesh = itemMeshes[i].mesh,
                        type = itemMeshes[i].type,
                        drawingOrderIndex = drawOrderIndex,
                        color = color,
                        matrix = matrix,
                    });
                }
            }

            void PoolMeshes(ShapeData gizmos, bool includeCustom)
            {
                if (!isValid) throw new System.InvalidOperationException();
                var outIndex = 0;
                for (int i = 0; i < meshes.Count; i++)
                {
                    // 仅当设置了 Pool 标志时才回收自定义网格。
                    // 否则它们由用户提供，如何处理由用户决定。
                    if ((meshes[i].type & MeshType.Custom) == 0 || (includeCustom && (meshes[i].type & MeshType.Pool) != 0))
                    {
                        gizmos.PoolMesh(meshes[i].mesh);
                    }
                    else
                    {
                        // 保留自定义网格
                        meshes[outIndex] = meshes[i];
                        outIndex += 1;
                    }
                }
                meshes.RemoveRange(outIndex, meshes.Count - outIndex);
            }

            public void PoolDynamicMeshes(ShapeData gizmos)
            {
                if (type == Type.Static && submitted) return;
                PoolMeshes(gizmos, false);
            }

            public void Release(ShapeData gizmos)
            {
                if (!isValid) throw new System.InvalidOperationException();
                PoolMeshes(gizmos, true);
                // 也清除自定义网格
                meshes.Clear();
                type = Type.Invalid;
                splitterJob.Complete();
                buildJob.Complete();
                var bufs = this.temporaryMeshBuffers[0];
                bufs.DisposeIfLarge();
                this.temporaryMeshBuffers[0] = bufs;
            }

            public void Dispose()
            {
                if (isValid) throw new System.InvalidOperationException();
                splitterJob.Complete();
                buildJob.Complete();
                if (temporaryMeshBuffers.IsCreated)
                {
                    temporaryMeshBuffers[0].Dispose();
                    temporaryMeshBuffers.Dispose();
                }
            }
        }

        internal struct SubmittedMesh
        {
            public Mesh mesh;
            public bool temporary;
        }

        [BurstCompile]
        internal struct BuilderData : IDisposable
        {
            public enum State
            {
                Free,
                Reserved,
                Initialized,
                WaitingForSplitter,
                WaitingForUserDefinedJob,
            }

            public struct Meta
            {
                public Hasher hasher;
                public RedrawScope redrawScope1;
                public RedrawScope redrawScope2;
                public int version;
                public bool isGizmos;
                /// <summary>用于在场景模式变化时使 Gizmos 失效</summary>
                public int sceneModeVersion;
                public int drawOrderIndex;
                public Camera[] cameraTargets;
            }

            public struct BitPackedMeta
            {
                uint flags;

                const int UniqueIDBitshift = 17;
                const int IsBuiltInFlagIndex = 16;
                const int IndexMask = (1 << IsBuiltInFlagIndex) - 1;
                const int MaxDataIndex = IndexMask;
                public const int UniqueIdMask = (1 << (32 - UniqueIDBitshift)) - 1;


                public BitPackedMeta(int dataIndex, int uniqueID, bool isBuiltInCommandBuilder)
                {
                    // 确保位打包不会冲突很重要
                    if (dataIndex > MaxDataIndex) throw new System.Exception("Too many command builders active. Are some command builders not being disposed?");
                    UnityEngine.Assertions.Assert.IsTrue(uniqueID <= UniqueIdMask && uniqueID >= 0);

                    flags = (uint)(dataIndex | uniqueID << UniqueIDBitshift | (isBuiltInCommandBuilder ? 1 << IsBuiltInFlagIndex : 0));
                }

                public int dataIndex
                {
                    get
                    {
                        return (int)(flags & IndexMask);
                    }
                }

                public int uniqueID
                {
                    get
                    {
                        return (int)(flags >> UniqueIDBitshift);
                    }
                }

                public bool isBuiltInCommandBuilder
                {
                    get
                    {
                        return (flags & (1 << IsBuiltInFlagIndex)) != 0;
                    }
                }

                public static bool operator ==(BitPackedMeta lhs, BitPackedMeta rhs)
                {
                    return lhs.flags == rhs.flags;
                }

                public static bool operator !=(BitPackedMeta lhs, BitPackedMeta rhs)
                {
                    return lhs.flags != rhs.flags;
                }

                public override bool Equals(object obj)
                {
                    if (obj is BitPackedMeta meta)
                    {
                        return flags == meta.flags;
                    }
                    return false;
                }

                public override int GetHashCode()
                {
                    return (int)flags;
                }
            }

            public BitPackedMeta packedMeta;
            public List<SubmittedMesh> meshes;
            public NativeArray<UnsafeAppendBuffer> commandBuffers;
            public State state { get; private set; }
            // TODO?
            public bool preventDispose;
            JobHandle splitterJob;
            JobHandle disposeDependency;
            AllowedDelay disposeDependencyDelay;
            System.Runtime.InteropServices.GCHandle disposeGCHandle;
            public Meta meta;

            public void Reserve(int dataIndex, bool isBuiltInCommandBuilder)
            {
                if (state != State.Free) throw new System.InvalidOperationException();
                state = BuilderData.State.Reserved;
                packedMeta = new BitPackedMeta(dataIndex, (UniqueIDCounter++) & BitPackedMeta.UniqueIdMask, isBuiltInCommandBuilder);
            }

            static int UniqueIDCounter = 0;

            public void Init(Hasher hasher, RedrawScope frameRedrawScope, RedrawScope customRedrawScope, bool isGizmos, int drawOrderIndex, int sceneModeVersion)
            {
                if (state != State.Reserved) throw new System.InvalidOperationException();

                meta = new Meta
                {
                    hasher = hasher,
                    redrawScope1 = frameRedrawScope,
                    redrawScope2 = customRedrawScope,
                    isGizmos = isGizmos,
                    version = 0, // Will be filled in later
                    drawOrderIndex = drawOrderIndex,
                    sceneModeVersion = sceneModeVersion,
                    cameraTargets = null,
                };

                if (meshes == null) meshes = new List<SubmittedMesh>();
                if (!commandBuffers.IsCreated)
                {
                    commandBuffers = new NativeArray<UnsafeAppendBuffer>(JobsUtility.ThreadIndexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    for (int i = 0; i < commandBuffers.Length; i++)
                        commandBuffers[i] = new UnsafeAppendBuffer(0, 4, Allocator.Persistent);
                }

                state = State.Initialized;
            }

            public unsafe UnsafeAppendBuffer* bufferPtr
            {
                get
                {
                    return (UnsafeAppendBuffer*)commandBuffers.GetUnsafePtr();
                }
            }

            [BurstCompile]
            [AOT.MonoPInvokeCallback(typeof(AnyBuffersWrittenToDelegate))]
            unsafe static bool AnyBuffersWrittenTo(UnsafeAppendBuffer* buffers, int numBuffers)
            {
                bool any = false;

                for (int i = 0; i < numBuffers; i++)
                {
                    any |= buffers[i].Length > 0;
                }
                return any;
            }

            [BurstCompile]
            [AOT.MonoPInvokeCallback(typeof(AnyBuffersWrittenToDelegate))]
            unsafe static void ResetAllBuffers(UnsafeAppendBuffer* buffers, int numBuffers)
            {
                for (int i = 0; i < numBuffers; i++)
                {
                    buffers[i].Reset();
                }
            }

            unsafe delegate bool AnyBuffersWrittenToDelegate(UnsafeAppendBuffer* buffers, int numBuffers);
            private readonly unsafe static AnyBuffersWrittenToDelegate AnyBuffersWrittenToInvoke = BurstCompiler.CompileFunctionPointer<AnyBuffersWrittenToDelegate>(AnyBuffersWrittenTo).Invoke;
            unsafe delegate void ResetAllBuffersToDelegate(UnsafeAppendBuffer* buffers, int numBuffers);
            private readonly unsafe static ResetAllBuffersToDelegate ResetAllBuffersToInvoke = BurstCompiler.CompileFunctionPointer<ResetAllBuffersToDelegate>(ResetAllBuffers).Invoke;

            public void SubmitWithDependency(System.Runtime.InteropServices.GCHandle gcHandle, JobHandle dependency, AllowedDelay allowedDelay)
            {
                state = State.WaitingForUserDefinedJob;
                disposeDependency = dependency;
                disposeDependencyDelay = allowedDelay;
                disposeGCHandle = gcHandle;
            }

            public void Submit(ShapeData gizmos)
            {
                if (state != State.Initialized) throw new System.InvalidOperationException();

                unsafe
                {
                    // 大约有 128 个缓冲区需要检查，使用 Burst 更快
                    if (meshes.Count == 0 && !AnyBuffersWrittenToInvoke((UnsafeAppendBuffer*)commandBuffers.GetUnsafeReadOnlyPtr(), commandBuffers.Length))
                    {
                        // 如果没有缓冲区被写入则直接丢弃此构建器
                        Release();
                        return;
                    }
                }

                meta.version = gizmos.version;

                // 命令流
                // 分为静态、动态和持久
                // 渲染静态
                // 按相机渲染动态
                // 按相机渲染持久
                const int PersistentDrawOrderOffset = 1000000;
                var tmpMeta = meta;
                // 预留一些缓冲区。
                // 需要设置确定性的绘制顺序以避免闪烁。
                // 着色器大部分时间使用 Z 缓冲区，但仍有
                // 不与顺序无关的内容。
                // 静态内容先绘制
                tmpMeta.drawOrderIndex = meta.drawOrderIndex * 3 + 0;
                int staticBuffer = gizmos.processedData.Reserve(ProcessedBuilderData.Type.Static, tmpMeta);
                // 动态内容紧接在静态内容之后绘制
                // 注意任何自定义网格的绘制顺序索引会 + 1。
                tmpMeta.drawOrderIndex = meta.drawOrderIndex * 3 + 1;
                int dynamicBuffer = gizmos.processedData.Reserve(ProcessedBuilderData.Type.Dynamic, tmpMeta);
                // 持久内容始终在所有其他内容之后绘制
                tmpMeta.drawOrderIndex = meta.drawOrderIndex + PersistentDrawOrderOffset;
                int persistentBuffer = gizmos.processedData.Reserve(ProcessedBuilderData.Type.Persistent, tmpMeta);

                unsafe
                {
                    splitterJob = new StreamSplitter
                    {
                        inputBuffers = commandBuffers,
                        staticBuffer = gizmos.processedData.Get(staticBuffer).splitterOutputPtr,
                        dynamicBuffer = gizmos.processedData.Get(dynamicBuffer).splitterOutputPtr,
                        persistentBuffer = gizmos.processedData.Get(persistentBuffer).splitterOutputPtr,
                    }.Schedule();
                }

                gizmos.processedData.Get(staticBuffer).SetSplitterJob(gizmos, splitterJob);
                gizmos.processedData.Get(dynamicBuffer).SetSplitterJob(gizmos, splitterJob);
                gizmos.processedData.Get(persistentBuffer).SetSplitterJob(gizmos, splitterJob);

                if (meshes.Count > 0)
                {
                    // Custom meshes may be affected by matrices and colors that are set in the command builders.
                    // Matrices may in theory be dynamic per camera (though this functionality is not used at the moment).
                    // The Command.CaptureState commands are marked as Dynamic so captured state will be written to
                    // the meshBuffers.capturedState array in the #dynamicBuffer.
                    var customMeshes = gizmos.processedData.Get(dynamicBuffer).meshes;

                    // 复制要渲染的网格
                    for (int i = 0; i < meshes.Count; i++) customMeshes.Add(new MeshWithType { mesh = meshes[i].mesh, type = MeshType.Solid | MeshType.Custom | (meshes[i].temporary ? MeshType.Pool : 0) });
                    meshes.Clear();
                }

                // TODO: 分配 3 个输出对象并将分离器输出管道连接到它们

                // 仅提交了对所有相机有效的网格。
                // 依赖特定相机的网格将在渲染前提交
                // 该相机。线条绘制取决于具体相机。
                // 特别是绘制圆时，不同数量的段
                // 取决于到相机的距离。
                state = State.WaitingForSplitter;
            }

            public void CheckJobDependency(ShapeData gizmos, bool allowBlocking)
            {
                if (state == State.WaitingForUserDefinedJob && (disposeDependency.IsCompleted || (allowBlocking && disposeDependencyDelay == AllowedDelay.EndOfFrame)))
                {
                    disposeDependency.Complete();
                    disposeDependency = default;
                    disposeGCHandle.Free();
                    state = State.Initialized;
                    Submit(gizmos);
                }
            }

            public void Release()
            {
                if (state == State.Free) throw new System.InvalidOperationException();
                state = BuilderData.State.Free;
                ClearData();
            }

            void ClearData()
            {
                // 等待可能正在运行的任何 Job
                // 这对于避免内存损坏错误很重要
                disposeDependency.Complete();
                splitterJob.Complete();
                meta = default;
                disposeDependency = default;
                preventDispose = false;
                meshes.Clear();
                unsafe
                {
                    // 大约有 128 个缓冲区需要重置，使用 Burst 更快
                    ResetAllBuffers((UnsafeAppendBuffer*)commandBuffers.GetUnsafePtr(), commandBuffers.Length);
                }
            }

            public void Dispose()
            {
                if (state == State.WaitingForUserDefinedJob)
                {
                    disposeDependency.Complete();
                    disposeGCHandle.Free();
                    // 这里本应调用 Submit，但反正要删除数据，所以无所谓。
                    state = State.WaitingForSplitter;
                }

                if (state == State.Reserved || state == State.Initialized || state == State.WaitingForUserDefinedJob)
                {
                    UnityEngine.Debug.LogError("Drawing data is being destroyed, but a drawing instance is still active. Are you sure you have called Dispose on all drawing instances? This will cause a memory leak!");
                    return;
                }

                splitterJob.Complete();
                if (commandBuffers.IsCreated)
                {
                    for (int i = 0; i < commandBuffers.Length; i++)
                    {
                        commandBuffers[i].Dispose();
                    }
                    commandBuffers.Dispose();
                }
            }
        }

        internal struct BuilderDataContainer : IDisposable
        {
            BuilderData[] data;

            public int memoryUsage
            {
                get
                {
                    int sum = 0;
                    if (data != null)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            var cmds = data[i].commandBuffers;
                            for (int j = 0; j < cmds.Length; j++)
                            {
                                sum += cmds[j].Capacity;
                            }
                            unsafe
                            {
                                sum += data[i].commandBuffers.Length * sizeof(UnsafeAppendBuffer);
                            }
                        }
                    }
                    return sum;
                }
            }


            public BuilderData.BitPackedMeta Reserve(bool isBuiltInCommandBuilder)
            {
                if (data == null) data = new BuilderData[1];
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].state == BuilderData.State.Free)
                    {
                        data[i].Reserve(i, isBuiltInCommandBuilder);
                        return data[i].packedMeta;
                    }
                }

                var newData = new BuilderData[data.Length * 2];
                data.CopyTo(newData, 0);
                data = newData;
                return Reserve(isBuiltInCommandBuilder);
            }

            public void Release(BuilderData.BitPackedMeta meta)
            {
                data[meta.dataIndex].Release();
            }

            public bool StillExists(BuilderData.BitPackedMeta meta)
            {
                int index = meta.dataIndex;

                if (data == null || index >= data.Length) return false;
                return data[index].packedMeta == meta;
            }

            public ref BuilderData Get(BuilderData.BitPackedMeta meta)
            {
                int index = meta.dataIndex;

                if (data[index].state == BuilderData.State.Free) throw new System.ArgumentException("Data is not reserved");
                if (data[index].packedMeta != meta) throw new System.ArgumentException("This command builder has already been disposed");
                return ref data[index];
            }

            public void DisposeCommandBuildersWithJobDependencies(ShapeData gizmos)
            {
                if (data == null) return;
                for (int i = 0; i < data.Length; i++) data[i].CheckJobDependency(gizmos, false);
                MarkerAwaitUserDependencies.Begin();
                for (int i = 0; i < data.Length; i++) data[i].CheckJobDependency(gizmos, true);
                MarkerAwaitUserDependencies.End();
            }

            public void ReleaseAllUnused()
            {
                if (data == null) return;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].state == BuilderData.State.WaitingForSplitter)
                    {
                        data[i].Release();
                    }
                }
            }

            public void Dispose()
            {
                if (data != null)
                {
                    for (int i = 0; i < data.Length; i++) data[i].Dispose();
                }
                // Ensures calling Dispose multiple times is a NOOP
                data = null;
            }
        }

        internal struct ProcessedBuilderDataContainer
        {
            ProcessedBuilderData[] data;
            Dictionary<ulong, List<int>> hash2index;
            Stack<int> freeSlots;
            Stack<List<int>> freeLists;

            public int memoryUsage
            {
                get
                {
                    int sum = 0;
                    if (data != null)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            var bufs = data[i].temporaryMeshBuffers;
                            for (int j = 0; j < bufs.Length; j++)
                            {
                                var psum = 0;
                                psum += bufs[j].textVertices.Capacity;
                                psum += bufs[j].textTriangles.Capacity;
                                psum += bufs[j].solidVertices.Capacity;
                                psum += bufs[j].solidTriangles.Capacity;
                                psum += bufs[j].vertices.Capacity;
                                psum += bufs[j].triangles.Capacity;
                                psum += bufs[j].capturedState.Capacity;
                                psum += bufs[j].splitterOutput.Capacity;
                                sum += psum;
                                UnityEngine.Debug.Log(i + ":" + j + " " + psum);
                            }
                        }
                    }
                    return sum;
                }
            }

            public int Reserve(ProcessedBuilderData.Type type, BuilderData.Meta meta)
            {
                if (data == null)
                {
                    data = new ProcessedBuilderData[0];
                    freeSlots = new Stack<int>();
                    freeLists = new Stack<List<int>>();
                    hash2index = new Dictionary<ulong, List<int>>();
                }
                if (freeSlots.Count == 0)
                {
                    var newData = new ProcessedBuilderData[math.max(4, data.Length * 2)];
                    data.CopyTo(newData, 0);
                    for (int i = data.Length; i < newData.Length; i++) freeSlots.Push(i);
                    data = newData;
                }
                int index = freeSlots.Pop();
                data[index].Init(type, meta);
                if (!meta.hasher.Equals(Hasher.NotSupplied))
                {
                    List<int> ls;
                    if (!hash2index.TryGetValue(meta.hasher.Hash, out ls))
                    {
                        if (freeLists.Count == 0) freeLists.Push(new List<int>());
                        ls = hash2index[meta.hasher.Hash] = freeLists.Pop();
                    }
                    ls.Add(index);
                }
                return index;
            }

            public ref ProcessedBuilderData Get(int index)
            {
                if (!data[index].isValid) throw new System.ArgumentException();
                return ref data[index];
            }

            void Release(ShapeData gizmos, int i)
            {
                var h = data[i].meta.hasher.Hash;

                if (!data[i].meta.hasher.Equals(Hasher.NotSupplied))
                {
                    if (hash2index.TryGetValue(h, out var ls))
                    {
                        ls.Remove(i);
                        if (ls.Count == 0)
                        {
                            freeLists.Push(ls);
                            hash2index.Remove(h);
                        }
                    }
                }
                data[i].Release(gizmos);
                freeSlots.Push(i);
            }

            public void SubmitMeshes(ShapeData gizmos, Camera camera, int versionThreshold, bool allowGizmos, bool allowCameraDefault)
            {
                if (data == null) return;
                MarkerSchedule.Begin();
                var cameraInfo = new GeometryBuilder.CameraInfo(camera);
                int c = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].isValid && data[i].meta.version >= versionThreshold && data[i].IsValidForCamera(camera, allowGizmos, allowCameraDefault))
                    {
                        c++;
                        data[i].Schedule(gizmos, ref cameraInfo);
                    }
                }

                MarkerSchedule.End();

                // 确保所有 Job 现在开始在工作线程上执行
                JobHandle.ScheduleBatchedJobs();

                MarkerBuild.Begin();
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].isValid && data[i].meta.version >= versionThreshold && data[i].IsValidForCamera(camera, allowGizmos, allowCameraDefault))
                    {
                        data[i].BuildMeshes(gizmos);
                    }
                }
                MarkerBuild.End();
            }

            /// <summary>
            /// 移除任何现有的动态网格，因为此帧之后不再需要它们。
            /// 不移除自定义网格或静态网格，因为它们可能在帧和相机之间保留。
            /// </summary>
            public void PoolDynamicMeshes(ShapeData gizmos)
            {
                if (data == null) return;
                MarkerPool.Begin();
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].isValid)
                    {
                        data[i].PoolDynamicMeshes(gizmos);
                    }
                }
                MarkerPool.End();
            }

            public void CollectMeshes(int versionThreshold, List<RenderedMeshWithType> meshes, Camera camera, bool allowGizmos, bool allowCameraDefault)
            {
                if (data == null) return;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].isValid && data[i].meta.version >= versionThreshold && data[i].IsValidForCamera(camera, allowGizmos, allowCameraDefault))
                    {
                        data[i].CollectMeshes(meshes);
                    }
                }
            }

            public void FilterOldPersistentCommands(int version, int lastTickVersion, float time, int sceneModeVersion)
            {
                if (data == null) return;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].isValid && data[i].type == ProcessedBuilderData.Type.Persistent)
                    {
                        data[i].SchedulePersistFilter(version, lastTickVersion, time, sceneModeVersion);
                    }
                }
            }

            public bool SetVersion(Hasher hasher, int version)
            {
                if (data == null) return false;

                if (hash2index.TryGetValue(hasher.Hash, out var indices))
                {
                    UnityEngine.Assertions.Assert.IsTrue(indices.Count > 0);
                    for (int id = 0; id < indices.Count; id++)
                    {
                        var i = indices[id];
                        UnityEngine.Assertions.Assert.IsTrue(data[i].isValid);
                        UnityEngine.Assertions.Assert.AreEqual(data[i].meta.hasher.Hash, hasher.Hash);
                        data[i].meta.version = version;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool SetVersion(RedrawScope scope, int version)
            {
                if (data == null) return false;
                bool found = false;

                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].isValid && (data[i].meta.redrawScope1.id == scope.id || data[i].meta.redrawScope2.id == scope.id))
                    {
                        data[i].meta.version = version;
                        found = true;
                    }
                }
                return found;
            }

            public bool SetCustomScope(Hasher hasher, RedrawScope scope)
            {
                if (data == null) return false;

                if (hash2index.TryGetValue(hasher.Hash, out var indices))
                {
                    UnityEngine.Assertions.Assert.IsTrue(indices.Count > 0);
                    for (int id = 0; id < indices.Count; id++)
                    {
                        var i = indices[id];
                        UnityEngine.Assertions.Assert.IsTrue(data[i].isValid);
                        UnityEngine.Assertions.Assert.AreEqual(data[i].meta.hasher.Hash, hasher.Hash);
                        data[i].meta.redrawScope2 = scope;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void ReleaseDataOlderThan(ShapeData gizmos, int version)
            {
                if (data == null) return;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].isValid && data[i].meta.version < version)
                    {
                        Release(gizmos, i);
                    }
                }
            }

            public void ReleaseAllWithHash(ShapeData gizmos, Hasher hasher)
            {
                if (data == null) return;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].isValid && data[i].meta.hasher.Hash == hasher.Hash)
                    {
                        Release(gizmos, i);
                    }
                }
            }

            public void Dispose(ShapeData gizmos)
            {
                if (data == null) return;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].isValid) Release(gizmos, i);
                    data[i].Dispose();
                }
                // Ensures calling Dispose multiple times is a NOOP
                data = null;
            }
        }

        [System.Flags]
        internal enum MeshType
        {
            Solid = 1 << 0,
            Lines = 1 << 1,
            Text = 1 << 2,
            // 如果网格不是内置网格则设置。这些可能设置了非单位矩阵。
            Custom = 1 << 3,
            // 如果为自定义网格设置此标志，网格将被回收。
            // 用于 ALINE 创建的临时自定义网格
            Pool = 1 << 4,
            BaseType = Solid | Lines | Text,
        }

        internal struct MeshWithType
        {
            public Mesh mesh;
            public MeshType type;
        }

        internal struct RenderedMeshWithType
        {
            public Mesh mesh;
            public MeshType type;
            public int drawingOrderIndex;
            // 仅当 type 包含 MeshType.Custom 时才能设为非白色
            public Color color;
            // 仅当 type 包含 MeshType.Custom 时才能设为非单位矩阵
            public Matrix4x4 matrix;
        }

        internal BuilderDataContainer data;
        internal ProcessedBuilderDataContainer processedData;
        List<RenderedMeshWithType> meshes = new List<RenderedMeshWithType>();
        List<Mesh> cachedMeshes = new List<Mesh>();
        List<Mesh> stagingCachedMeshes = new List<Mesh>();
        List<Mesh> stagingCachedMeshesDelay = new List<Mesh>();
        int lastTimeLargestCachedMeshWasUsed = 0;
        internal SDFLookupData fontData;
        int currentDrawOrderIndex = 0;

        /// <summary>
        /// 每次编辑器从播放模式切换到编辑模式或反之时递增。
        /// 用于确保没有 WithDuration 作用域在此过渡中幸存。
        ///
        /// 通常不重要，但当 Unity 的进入播放模式设置禁用了域重新加载时
        /// 会变得重要，因为此管理器会在过渡中保留。
        /// </summary>
        internal int sceneModeVersion = 0;

        /// <summary>
        /// 稍微调整的场景模式版本。
        /// 这也考虑了 `Application.isPlaying`。<see cref="sceneModeVersion"/> 可能被修改
        /// 然后在实际播放模式变化发生之前（使用旧的 Application.isPlaying 模式）绘制了某些 Gizmos。
        ///
        /// 更精确地说，没有此调整可能发生的情况是
        /// 1. EditorApplication.playModeStateChanged (PlayModeStateChange.ExitingPlayMode) 触发，递增 sceneModeVersion。
        /// 2. 最后一次更新循环以 Application.isPlaying = true 运行。
        /// 3. 在此循环中，使用新的 sceneModeVersion 和 Application.isPlaying=true 创建了新的命令构建器并使用 WithDuration 作用域绘制。
        /// 4. 播放模式切换到编辑模式。
        /// 5. WithDuration 作用域幸存了！
        ///
        /// 我们不能改为在 PlayModeStateChange.ExitedPlayMode（而非 Exiting）上递增 sceneModeVersion，因为我们想保留的某些 Gizmos 可能
        /// 在该事件触发之前被绘制。
        /// </summary>
        int adjustedSceneModeVersion
        {
            get
            {
                return sceneModeVersion + (Application.isPlaying ? 1000 : 0);
            }
        }

        internal int GetNextDrawOrderIndex()
        {
            currentDrawOrderIndex++;
            return currentDrawOrderIndex;
        }

        internal void PoolMesh(Mesh mesh)
        {
            // 注意：在此清除网格会释放顶点/索引缓冲区
            // 这对性能不好，因为下一帧可能需要重新分配（可能相同大小）
            //mesh.Clear();
            stagingCachedMeshes.Add(mesh);
        }

        void SortPooledMeshes()
        {
            // TODO: 访问顶点数是否很慢？
            cachedMeshes.Sort((a, b) => b.vertexCount - a.vertexCount);
        }

        internal Mesh GetMesh(int desiredVertexCount)
        {
            if (cachedMeshes.Count > 0)
            {
                // 二分搜索找到大于或等于所需顶点数的最小缓存网格
                // TODO: 实际上应该比较顶点缓冲区的字节大小，而不是顶点数因为
                // 顶点大小可能因网格属性布局而变化。
                int mn = 0;
                int mx = cachedMeshes.Count;
                while (mx > mn + 1)
                {
                    int mid = (mn + mx) / 2;
                    if (cachedMeshes[mid].vertexCount < desiredVertexCount)
                    {
                        mx = mid;
                    }
                    else
                    {
                        mn = mid;
                    }
                }

                var res = cachedMeshes[mn];
                if (mn == 0) lastTimeLargestCachedMeshWasUsed = version;
                cachedMeshes.RemoveAt(mn);
                return res;
            }
            else
            {
                var mesh = new Mesh
                {
                    hideFlags = HideFlags.DontSave
                };
                mesh.MarkDynamic();
                return mesh;
            }
        }

        internal void LoadFontDataIfNecessary()
        {
            if (fontData.material == null)
            {
                var font = DefaultFonts.LoadDefaultFont();
                fontData.Dispose();
                fontData = new SDFLookupData(font);
            }
        }

        static float CurrentTime
        {
            get
            {
                return Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            }
        }

        static void UpdateTime()
        {
            // Time.time 无法在 Job 系统中访问，因此创建一个*可以*访问的全局变量。
            // 更新频率不高，但仅用于 WithDuration 方法，所以应该没问题
            SharedShapeData.BurstTime.Data = CurrentTime;
        }

        /// <summary>
        /// 获取空的构建器以排队绘制命令。
        ///
        /// <code>
        /// // 创建一个新的 CommandBuilder
        /// using (var draw = ShapeManager.GetBuilder()) {
        ///     // 使用与全局 Draw 类完全相同的 API
        ///     draw.WireBox(Vector3.zero, Vector3.one);
        /// }
        /// </code>
        /// See: <see cref="VisualShape.CommandBuilder"/>
        /// </summary>
        /// <param name="renderInGame">如果为 true，此构建器将在独立游戏和编辑器中渲染，即使 Gizmos 被禁用。
        /// 如果为 false，仅在编辑器中启用 Gizmos 时渲染。</param>
        public CommandBuilder GetBuilder(bool renderInGame = false)
        {
            UpdateTime();
            return new CommandBuilder(this, Hasher.NotSupplied, frameRedrawScope, default, !renderInGame, false, adjustedSceneModeVersion);
        }

        internal CommandBuilder GetBuiltInBuilder(bool renderInGame = false)
        {
            UpdateTime();
            return new CommandBuilder(this, Hasher.NotSupplied, frameRedrawScope, default, !renderInGame, true, adjustedSceneModeVersion);
        }

        /// <summary>
        /// 获取空的构建器以排队绘制命令。
        ///
        /// See: <see cref="VisualShape.CommandBuilder"/>
        /// </summary>
        /// <param name="renderInGame">如果为 true，此构建器将在独立游戏和编辑器中渲染，即使 Gizmos 被禁用。</param>
        public CommandBuilder GetBuilder(RedrawScope redrawScope, bool renderInGame = false)
        {
            UpdateTime();
            return new CommandBuilder(this, Hasher.NotSupplied, frameRedrawScope, redrawScope, !renderInGame, false, adjustedSceneModeVersion);
        }

        /// <summary>
        /// 获取空的构建器以排队绘制命令。
        ///
        /// See: <see cref="VisualShape.CommandBuilder"/>
        /// </summary>
        /// <param name="renderInGame">如果为 true，此构建器将在独立游戏和编辑器中渲染，即使 Gizmos 被禁用。</param>
        public CommandBuilder GetBuilder(Hasher hasher, RedrawScope redrawScope = default, bool renderInGame = false)
        {
            // 用户将使用给定的哈希重建数据
            // 清除之前该哈希的数据因为我们知道不再需要。
            // 如果未给定哈希则不执行此操作。
            if (!hasher.Equals(Hasher.NotSupplied)) DiscardData(hasher);
            UpdateTime();
            return new CommandBuilder(this, hasher, frameRedrawScope, redrawScope, !renderInGame, false, adjustedSceneModeVersion);
        }

        /// <summary>用于表面的材质</summary>
        public Material surfaceMaterial;

        /// <summary>用于线条的材质</summary>
        public Material lineMaterial;

        /// <summary>用于文本的材质</summary>
        public Material textMaterial;

        public ShapeSettings settingsAsset;

        public ShapeSettings.Settings settingsRef
        {
            get
            {
                if (settingsAsset == null)
                {
                    settingsAsset = ShapeSettings.GetSettingsAsset();
                    if (settingsAsset == null)
                    {
                        throw new System.InvalidOperationException("VisualShape settings could not be found");
                    }
                }
                return settingsAsset.settings;
            }
        }

        public int version { get; private set; } = 1;
        int lastTickVersion;
        int lastTickVersion2;
        HashSet<int> persistentRedrawScopes = new HashSet<int>();
#if ALINE_TRACK_REDRAW_SCOPE_LEAKS
        Dictionary<int, String> persistentRedrawScopeInfos = new Dictionary<int, String>();
#endif
        internal System.Runtime.InteropServices.GCHandle gizmosHandle;

        public RedrawScope frameRedrawScope;

        struct Range
        {
            public int start;
            public int end;
        }

        Dictionary<Camera, Range> cameraVersions = new Dictionary<Camera, Range>();

        internal static readonly ProfilerMarker MarkerScheduleJobs = new ProfilerMarker("ScheduleJobs");
        internal static readonly ProfilerMarker MarkerAwaitUserDependencies = new ProfilerMarker("Await user dependencies");
        internal static readonly ProfilerMarker MarkerSchedule = new ProfilerMarker("Schedule");
        internal static readonly ProfilerMarker MarkerBuild = new ProfilerMarker("Build");
        internal static readonly ProfilerMarker MarkerPool = new ProfilerMarker("Pool");
        internal static readonly ProfilerMarker MarkerRelease = new ProfilerMarker("Release");
        internal static readonly ProfilerMarker MarkerBuildMeshes = new ProfilerMarker("Build Meshes");
        internal static readonly ProfilerMarker MarkerCollectMeshes = new ProfilerMarker("Collect Meshes");
        internal static readonly ProfilerMarker MarkerSortMeshes = new ProfilerMarker("Sort Meshes");
        internal static readonly ProfilerMarker LeakTracking = new ProfilerMarker("RedrawScope Leak Tracking");

        void DiscardData(Hasher hasher)
        {
            processedData.ReleaseAllWithHash(this, hasher);
        }

        internal void OnChangingPlayMode()
        {
            sceneModeVersion++;

#if UNITY_EDITOR
            // 在编辑器中，我们安排回调检查是否有 RedrawScope 对象未被释放。
            // OnChangingPlayMode 在场景销毁前运行。因此任何当前存活的持久重绘作用域
            // 应该很快被销毁。
            // 我们等几次更新让场景销毁后再检查泄漏。
            // EditorApplication.delayCall 可能在场景实际销毁前被调用。
            // 通常已销毁，但特别是如果用户双击播放按钮启动然后立即
            // 停止游戏，则可能在场景销毁前运行。
            var shouldBeDestroyed = this.persistentRedrawScopes.ToArray();
            UnityEditor.EditorApplication.CallbackFunction checkLeaks = null;
            int remainingFrames = 2;
            checkLeaks = () =>
            {
                if (remainingFrames > 0)
                {
                    remainingFrames--;
                    return;
                }
                UnityEditor.EditorApplication.delayCall -= checkLeaks;
                int leaked = 0;
                foreach (var v in shouldBeDestroyed)
                {
                    if (persistentRedrawScopes.Contains(v)) leaked++;
                }
                if (leaked > 0)
                {
#if ALINE_TRACK_REDRAW_SCOPE_LEAKS
                    UnityEngine.Debug.LogError(leaked + " RedrawScope objects were not disposed. Make sure to dispose them when you are done with them, otherwise this will lead to a memory leak and potentially a performance issue.");
                    foreach (var v in shouldBeDestroyed) {
                        if (persistentRedrawScopes.Contains(v)) {
                            UnityEngine.Debug.LogError("RedrawScope leaked. Allocated from:\n" + persistentRedrawScopeInfos[v]);
                        }
                    }
#else
                    UnityEngine.Debug.LogError(leaked + " RedrawScope objects were not disposed. Make sure to dispose them when you are done with them, otherwise this will lead to a memory leak and potentially a performance issue.\nEnable ALINE_TRACK_REDRAW_SCOPE_LEAKS in the scripting define symbols to track the leaks more accurately.");
#endif
                    foreach (var v in shouldBeDestroyed)
                    {
                        persistentRedrawScopes.Remove(v);
#if ALINE_TRACK_REDRAW_SCOPE_LEAKS
                        persistentRedrawScopeInfos.Remove(v);
#endif
                    }
                }
            };
            UnityEditor.EditorApplication.delayCall += checkLeaks;
#endif
        }

        /// <summary>
        /// 安排指定哈希的网格进行绘制。
        /// 返回：如果此哈希没有缓存的网格则为 False，此时你可能需要
        ///  提交一个。无论返回值如何，绘制命令都会发出。
        /// </summary>
        public bool Draw(Hasher hasher)
        {
            if (hasher.Equals(Hasher.NotSupplied)) throw new System.ArgumentException("Invalid hash value");
            return processedData.SetVersion(hasher, version);
        }

        /// <summary>
        /// 安排指定哈希的网格进行绘制。
        /// 返回：如果此哈希没有缓存的网格则为 False，此时你可能需要
        ///  提交一个。无论返回值如何，绘制命令都会发出。
        ///
        /// 此重载将绘制指定重绘作用域内的所有网格。
        /// 注意如果它们之前在另一个重绘作用域中绘制过，将从该作用域中移除。
        /// </summary>
        public bool Draw(Hasher hasher, RedrawScope scope)
        {
            if (hasher.Equals(Hasher.NotSupplied)) throw new System.ArgumentException("Invalid hash value");
            processedData.SetCustomScope(hasher, scope);
            return processedData.SetVersion(hasher, version);
        }

        /// <summary>安排上一帧使用此重绘作用域绘制的所有网格再次绘制</summary>
        internal void Draw(RedrawScope scope)
        {
            if (scope.id != 0) processedData.SetVersion(scope, version);
        }

        internal void DrawUntilDisposed(RedrawScope scope)
        {
            if (scope.id != 0)
            {
                Draw(scope);
                persistentRedrawScopes.Add(scope.id);
#if ALINE_TRACK_REDRAW_SCOPE_LEAKS && UNITY_EDITOR
                LeakTracking.Begin();
                persistentRedrawScopeInfos[scope.id] = new System.Diagnostics.StackTrace().ToString();
                LeakTracking.End();
#endif
            }
        }

        internal void DisposeRedrawScope(RedrawScope scope)
        {
            if (scope.id != 0)
            {
                processedData.SetVersion(scope, -1);
                persistentRedrawScopes.Remove(scope.id);
#if ALINE_TRACK_REDRAW_SCOPE_LEAKS && UNITY_EDITOR
                persistentRedrawScopeInfos.Remove(scope.id);
#endif
            }
        }

        public void TickFramePreRender()
        {
            data.DisposeCommandBuildersWithJobDependencies(this);
            // 移除已超时的持久命令。
            // 不在播放时持久命令不会绘制两次
            processedData.FilterOldPersistentCommands(version, lastTickVersion, CurrentTime, adjustedSceneModeVersion);
            foreach (var scopeId in persistentRedrawScopes)
            {
                processedData.SetVersion(new RedrawScope(this, scopeId), version);
            }

            // 上次 tick 和此次之间渲染的所有相机将有
            // 至少为 lastTickVersion + 1 的版本。
            // 但用户可能想复用上一帧的网格（见 Draw(Hasher)）。
            // 这要求我们多保留一帧数据，因此使用 lastTickVersion2 + 1
            // TODO: 一帧应该够了吧？
            processedData.ReleaseDataOlderThan(this, lastTickVersion2 + 1);
            lastTickVersion2 = lastTickVersion;
            lastTickVersion = version;
            currentDrawOrderIndex = 0;

            // 两帧前回收的网格现在可以使用了。
            // 人们可能认为一帧前回收的网格就可以使用。
            // 是的，Unity 允许这样做，但 GPU 可能仍在处理上一帧的网格。
            // 因此当我们尝试写入原始网格顶点缓冲区时 Unity 会阻塞直到上一
            // 帧的 GPU 工作完成，这可能需要很长时间。
            // 对每帧更新的网格使用"双缓冲"更高效。
            // 使用简化方法设置顶点/索引数据时不需要这样做
            // 因为 Unity 似乎为我们管理了上传缓冲区。
            cachedMeshes.AddRange(stagingCachedMeshesDelay);
            // 将 stagingCachedMeshes 移到 stagingCachedMeshesDelay，并使 stagingCachedMeshes 成为空列表。
            stagingCachedMeshesDelay.Clear();
            var tmp = stagingCachedMeshesDelay;
            stagingCachedMeshesDelay = stagingCachedMeshes;
            stagingCachedMeshes = tmp;
            SortPooledMeshes();

            // 如果最大的缓存网格一段时间未使用，则移除以释放内存
            if (version - lastTimeLargestCachedMeshWasUsed > 60 && cachedMeshes.Count > 0)
            {
                Mesh.DestroyImmediate(cachedMeshes[0]);
                cachedMeshes.RemoveAt(0);
                lastTimeLargestCachedMeshWasUsed = version;
            }

            // TODO: 过滤 cameraVersions 以避免内存泄漏
        }

        public void PostRenderCleanup()
        {
            MarkerRelease.Begin();
            data.ReleaseAllUnused();
            MarkerRelease.End();
            version++;
        }

        class MeshCompareByDrawingOrder : IComparer<RenderedMeshWithType>
        {
            public int Compare(RenderedMeshWithType a, RenderedMeshWithType b)
            {
                // 提取网格是 Solid/Lines/Text
                var ta = (int)a.type & 0x7;
                var tb = (int)b.type & 0x7;
                return ta != tb ? ta - tb : a.drawingOrderIndex - b.drawingOrderIndex;
            }
        }

        static readonly MeshCompareByDrawingOrder meshSorter = new MeshCompareByDrawingOrder();
        // 临时数组，缓存以避免分配
        Plane[] frustrumPlanes = new Plane[6];
        // 临时块，缓存以避免分配
        MaterialPropertyBlock customMaterialProperties = new MaterialPropertyBlock();

        int totalMemoryUsage => this.data.memoryUsage + this.processedData.memoryUsage;

        void LoadMaterials()
        {
            // 确保材质引用正确

            // 注意：首次导入包时资产数据库可能未更新，Resources.Load 可能返回 null。

            if (surfaceMaterial == null)
            {
                surfaceMaterial = Resources.Load<Material>("visualshape_surface_mat");
                if (surfaceMaterial == null)
                {
                    var shader = Shader.Find("Hidden/VisualShape/Surface");
                    if (shader != null) surfaceMaterial = new Material(shader);
                }
            }
            if (lineMaterial == null)
            {
                lineMaterial = Resources.Load<Material>("visualshape_outline_mat");
                if (lineMaterial == null)
                {
                    var shader = Shader.Find("Hidden/VisualShape/Outline");
                    if (shader != null) lineMaterial = new Material(shader);
                }
            }
            if (fontData.material == null)
            {
                var font = DefaultFonts.LoadDefaultFont();
                if (font.material == null)
                {
                    var shader = Shader.Find("Hidden/VisualShape/Font");
                    if (shader != null) font.material = new Material(shader);
                }
                fontData.Dispose();
                fontData = new SDFLookupData(font);
            }
        }

        public ShapeData()
        {
            gizmosHandle = System.Runtime.InteropServices.GCHandle.Alloc(this, System.Runtime.InteropServices.GCHandleType.Weak);
            LoadMaterials();
        }

        static int CeilLog2(int x)
        {
            // 下次提高数学包最低兼容版本时应使用 `math.ceillog2`。
            // 此变体容易出现浮点错误。
            return (int)math.ceil(math.log2(x));
        }

        /// <summary>
        /// 不同类型命令缓冲区的封装。
        ///
        /// 令人烦恼的是，它们最终都使用 CommandBuffer，但通用渲染管线将其封装在 RasterCommandBuffer 中，
        /// 且无法获取底层的 CommandBuffer。
        /// </summary>
        public struct CommandBufferWrapper
        {
            public CommandBuffer cmd;
            public bool allowDisablingWireframe;
            public RasterCommandBuffer cmd2;
            public void SetWireframe(bool enable)
            {
                if (cmd != null)
                {
                    cmd.SetWireframe(enable);
                }
                else if (cmd2 != null)
                {
                    if (allowDisablingWireframe) cmd2.SetWireframe(enable);
                }
            }

            public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass, MaterialPropertyBlock properties)
            {
                if (cmd != null)
                {
                    cmd.DrawMesh(mesh, matrix, material, submeshIndex, shaderPass, properties);
                }
                else if (cmd2 != null)
                {
                    cmd2.DrawMesh(mesh, matrix, material, submeshIndex, shaderPass, properties);
                }
            }
        }

        /// <summary>Call after all <see cref="Draw"/> commands for the frame have been done to draw everything.</summary>
        /// <param name="allowCameraDefault">指示内置命令构建器和没有自定义 CommandBuilder.cameraTargets 的自定义构建器是否应渲染到此相机。</param>
        public void Render(Camera cam, bool allowGizmos, CommandBufferWrapper commandBuffer, bool allowCameraDefault)
        {
            LoadMaterials();

            // Warn if the materials could not be found
            if (surfaceMaterial == null || lineMaterial == null)
            {
                // Note that when the package is installed Unity may start rendering things and call this method before it has initialized the Resources folder with the materials.
                // We don't want to throw exceptions in that case because once the import finishes everything will be good.
                // UnityEngine.Debug.LogWarning("Looks like you just installed VisualShape. The VisualShape package will start working after the next script recompilation.");
                return;
            }

            var planes = frustrumPlanes;
            GeometryUtility.CalculateFrustumPlanes(cam, planes);

            if (!cameraVersions.TryGetValue(cam, out Range cameraRenderingRange))
            {
                cameraRenderingRange = new Range { start = int.MinValue, end = int.MinValue };
            }

            // Check if the last time the camera was rendered
            // was during the current frame.
            if (cameraRenderingRange.end > lastTickVersion)
            {
                // In some cases a camera is rendered multiple times per frame.
                // In this case we just extend the end of the drawing range up to the current version.
                // The reasoning is that all times the camera is rendered in a frame
                // all things should be drawn.
                // If we did update the start of the range then things would only be drawn
                // the first time the camera was rendered in the frame.

                // Sometimes the scene view will be rendered twice in a single frame
                // due to some internal Unity tooltip code.
                // Without this fix the scene view camera may end up showing no gizmos
                // for a single frame.
                cameraRenderingRange.end = version + 1;
            }
            else
            {
                // This is the common case: the previous time the camera was rendered
                // it rendered all versions lower than cameraRenderingRange.end.
                // So now we start by rendering from that version.
                cameraRenderingRange = new Range { start = cameraRenderingRange.end, end = version + 1 };
            }

            // Don't show anything rendered before the last frame.
            // If the camera has been turned off for a while and then suddenly starts rendering again
            // we want to make sure that we don't render meshes from multiple frames.
            // This happens often in the unity editor as the scene view and game view often skip
            // rendering many frames when outside of play mode.
            cameraRenderingRange.start = Mathf.Max(cameraRenderingRange.start, lastTickVersion2 + 1);

            var settings = settingsRef;

            bool skipDueToWireframe = false;
            commandBuffer.SetWireframe(false);

            if (!skipDueToWireframe)
            {
                MarkerBuildMeshes.Begin();
                processedData.SubmitMeshes(this, cam, cameraRenderingRange.start, allowGizmos, allowCameraDefault);
                MarkerBuildMeshes.End();
                MarkerCollectMeshes.Begin();
                meshes.Clear();
                processedData.CollectMeshes(cameraRenderingRange.start, meshes, cam, allowGizmos, allowCameraDefault);
                processedData.PoolDynamicMeshes(this);
                MarkerCollectMeshes.End();
                MarkerSortMeshes.Begin();
                // Note that a stable sort is required as some meshes may have the same sorting index
                // but those meshes will have a consistent ordering between them in the list
                meshes.Sort(meshSorter);
                MarkerSortMeshes.End();

                int colorID = Shader.PropertyToID("_Color");
                int colorFadeID = Shader.PropertyToID("_FadeColor");
                var solidBaseColor = new Color(1, 1, 1, settings.solidOpacity);
                var solidFadeColor = new Color(1, 1, 1, settings.solidOpacityBehindObjects);
                var lineBaseColor = new Color(1, 1, 1, settings.lineOpacity);
                var lineFadeColor = new Color(1, 1, 1, settings.lineOpacityBehindObjects);
                var textBaseColor = new Color(1, 1, 1, settings.textOpacity);
                var textFadeColor = new Color(1, 1, 1, settings.textOpacityBehindObjects);

                // The meshes list is already sorted as first surfaces, then lines, then text
                for (int i = 0; i < meshes.Count;)
                {
                    int meshEndIndex = i + 1;
                    var tp = meshes[i].type & MeshType.BaseType;
                    while (meshEndIndex < meshes.Count && (meshes[meshEndIndex].type & MeshType.BaseType) == tp) meshEndIndex++;

                    Material mat;
                    customMaterialProperties.Clear();
                    switch (tp)
                    {
                        case MeshType.Solid:
                            mat = surfaceMaterial;
                            customMaterialProperties.SetColor(colorID, solidBaseColor);
                            customMaterialProperties.SetColor(colorFadeID, solidFadeColor);
                            break;
                        case MeshType.Lines:
                            mat = lineMaterial;
                            customMaterialProperties.SetColor(colorID, lineBaseColor);
                            customMaterialProperties.SetColor(colorFadeID, lineFadeColor);
                            break;
                        case MeshType.Text:
                            mat = fontData.material;
                            customMaterialProperties.SetColor(colorID, textBaseColor);
                            customMaterialProperties.SetColor(colorFadeID, textFadeColor);
                            break;
                        default:
                            throw new System.InvalidOperationException("Invalid mesh type");
                    }

                    for (int pass = 0; pass < mat.passCount; pass++)
                    {
                        for (int j = i; j < meshEndIndex; j++)
                        {
                            var mesh = meshes[j];
                            if ((mesh.type & MeshType.Custom) != 0)
                            {
                                // This mesh type may have a matrix set. So we need to handle that
                                if (GeometryUtility.TestPlanesAABB(planes, TransformBoundingBox(mesh.matrix, mesh.mesh.bounds)))
                                {
                                    // Custom meshes may have different colors
                                    customMaterialProperties.SetColor(colorID, solidBaseColor * mesh.color);
                                    commandBuffer.DrawMesh(mesh.mesh, mesh.matrix, mat, 0, pass, customMaterialProperties);
                                    customMaterialProperties.SetColor(colorID, solidBaseColor);
                                }
                            }
                            else if (GeometryUtility.TestPlanesAABB(planes, mesh.mesh.bounds))
                            {
                                // This mesh is drawn with an identity matrix
                                commandBuffer.DrawMesh(mesh.mesh, Matrix4x4.identity, mat, 0, pass, customMaterialProperties);
                            }
                        }
                    }

                    i = meshEndIndex;
                }

                meshes.Clear();
            }

            cameraVersions[cam] = cameraRenderingRange;
        }

        /// <summary>Returns a new axis aligned bounding box that contains the given bounding box after being transformed by the matrix</summary>
        static Bounds TransformBoundingBox(Matrix4x4 matrix, Bounds bounds)
        {
            var mn = bounds.min;
            var mx = bounds.max;
            // Create the bounding box from the bounding box of the transformed
            // 8 points of the original bounding box.
            var newBounds = new Bounds(matrix.MultiplyPoint(mn), Vector3.zero);

            newBounds.Encapsulate(matrix.MultiplyPoint(new Vector3(mn.x, mn.y, mx.z)));

            newBounds.Encapsulate(matrix.MultiplyPoint(new Vector3(mn.x, mx.y, mn.z)));
            newBounds.Encapsulate(matrix.MultiplyPoint(new Vector3(mn.x, mx.y, mx.z)));

            newBounds.Encapsulate(matrix.MultiplyPoint(new Vector3(mx.x, mn.y, mn.z)));
            newBounds.Encapsulate(matrix.MultiplyPoint(new Vector3(mx.x, mn.y, mx.z)));

            newBounds.Encapsulate(matrix.MultiplyPoint(new Vector3(mx.x, mx.y, mn.z)));
            newBounds.Encapsulate(matrix.MultiplyPoint(new Vector3(mx.x, mx.y, mx.z)));
            return newBounds;
        }

        /// <summary>
        /// Destroys all cached meshes.
        /// Used to make sure that no memory leaks happen in the Unity Editor.
        /// </summary>
        public void ClearData()
        {
            gizmosHandle.Free();
            data.Dispose();
            processedData.Dispose(this);

            for (int i = 0; i < cachedMeshes.Count; i++)
            {
                Mesh.DestroyImmediate(cachedMeshes[i]);
            }
            cachedMeshes.Clear();

            UnityEngine.Assertions.Assert.IsTrue(meshes.Count == 0);
            fontData.Dispose();
        }
    }
}


