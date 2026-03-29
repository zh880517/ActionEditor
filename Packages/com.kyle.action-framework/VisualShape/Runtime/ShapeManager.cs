#pragma warning disable 649 // Field `VisualShape.GizmoContext.activeTransform' is never assigned to, and will always have its default value `null'. Not used outside of the unity editor.
using UnityEngine;
using System.Collections;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Unity.Profiling;
using UnityEngine.Rendering.Universal;

namespace VisualShape
{
    /// <summary>编辑器中当前选择的信息</summary>
    public static class GizmoContext
    {
#if UNITY_EDITOR
        static Transform activeTransform;
#endif

        static HashSet<Transform> selectedTransforms = new HashSet<Transform>();

        static internal bool drawingGizmos;
        static internal bool dirty;
        private static int selectionSizeInternal;

        /// <summary>选中的顶层 Transform 数量</summary>
        public static int selectionSize
        {
            get
            {
                Refresh();
                return selectionSizeInternal;
            }
            private set
            {
                selectionSizeInternal = value;
            }
        }

        internal static void SetDirty()
        {
            dirty = true;
        }

        private static void Refresh()
        {
#if UNITY_EDITOR
            if (!drawingGizmos) throw new System.Exception("Can only be used inside the VisualShape library's gizmo drawing functions.");
            if (dirty)
            {
                dirty = false;
                ShapeManager.MarkerRefreshSelectionCache.Begin();
                activeTransform = Selection.activeTransform;
                selectedTransforms.Clear();
                var topLevel = Selection.transforms;
                for (int i = 0; i < topLevel.Length; i++) selectedTransforms.Add(topLevel[i]);
                selectionSize = topLevel.Length;
                ShapeManager.MarkerRefreshSelectionCache.End();
            }
#endif
        }

        /// <summary>
        /// 如果组件被选中则为 true。
        /// 这是深度选择：即使选中 Transform 的子对象也被视为选中。
        /// </summary>
        public static bool InSelection(Component c)
        {
            return InSelection(c.transform);
        }

        /// <summary>
        /// 如果 Transform 被选中则为 true。
        /// 这是深度选择：即使选中 Transform 的子对象也被视为选中。
        /// </summary>
        public static bool InSelection(Transform tr)
        {
            Refresh();
            var leaf = tr;
            while (tr != null)
            {
                if (selectedTransforms.Contains(tr))
                {
                    selectedTransforms.Add(leaf);
                    return true;
                }
                tr = tr.parent;
            }
            return false;
        }

        /// <summary>
        /// 如果组件在检查器中显示则为 true。
        /// 活动选择是当前在检查器中可见的 GameObject。
        /// </summary>
        public static bool InActiveSelection(Component c)
        {
            return InActiveSelection(c.transform);
        }

        /// <summary>
        /// 如果 Transform 在检查器中显示则为 true。
        /// 活动选择是当前在检查器中可见的 GameObject。
        /// </summary>
        public static bool InActiveSelection(Transform tr)
        {
#if UNITY_EDITOR
            Refresh();
            return tr.transform == activeTransform;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// 每个想要绘制 Gizmos 的对象都应实现此接口。
    /// See: <see cref="VisualShape.MonoBehaviourGizmos"/>
    /// </summary>
    public interface IDrawGizmos
    {
        void DrawGizmos();
    }

    public enum DetectedRenderPipeline
    {
        BuiltInOrCustom,
        URP
    }

    /// <summary>
    /// 绘制调试项和 Gizmos 的全局脚本。
    /// 如果使用了 Draw.* 方法，或场景中有任何继承自 <see cref="VisualShape.MonoBehaviourGizmos"/> 的脚本，则会创建此脚本的实例
    /// 并放置在隐藏的 GameObject 上。
    ///
    /// 它将在所有渲染的相机中注入绘制逻辑。
    ///
    /// 通常无需与此类交互。
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("")]
    public class ShapeManager : MonoBehaviour
    {
        public ShapeData gizmos;
        static List<IDrawGizmos> gizmoDrawers = new List<IDrawGizmos>();
        static Dictionary<System.Type, bool> gizmoDrawerTypes = new Dictionary<System.Type, bool>();
        static ShapeManager _instance;
        bool framePassed;
        int lastFrameCount = int.MinValue;
        float lastFrameTime = -float.NegativeInfinity;
        int lastFilterFrame;
#if UNITY_EDITOR
        bool builtGizmos;
#endif

        /// <summary>如果此实例已调用 OnEnable 且未调用 OnDisable 则为 true</summary>
        [SerializeField]
        bool actuallyEnabled;

        RedrawScope previousFrameRedrawScope;

        /// <summary>
        /// 允许渲染到使用 RenderTexture 的相机。
        /// 默认情况下不会渲染到使用 RenderTexture 的相机。
        /// 如果需要可以启用此选项。
        ///
        /// See: <see cref="VisualShape.CommandBuilder.cameraTargets"/>
        /// See: advanced (view in online documentation for working links)
        /// </summary>
        public static bool allowRenderToRenderTextures = false;
        public static bool drawToAllCameras = false;

        /// <summary>
        /// 将所有线宽乘以此值。
        /// 可用于使线条更粗或更细。
        ///
        /// 这主要在生成截图时有用，当你想以更高分辨率渲染后再缩小图像时。
        ///
        /// 仅在相机渲染时读取。因此不能用于按项目更改线条粗细。
        /// 请使用 <see cref="Draw.WithLineWidth"/>。
        /// </summary>
        public static float lineWidthMultiplier = 1.0f;

        CommandBuffer commandBuffer;

        [System.NonSerialized]
        DetectedRenderPipeline detectedRenderPipeline = DetectedRenderPipeline.BuiltInOrCustom;
        HashSet<ScriptableRenderer> scriptableRenderersWithPass = new HashSet<ScriptableRenderer>();
        VisualShapeURPRenderPassFeature renderPassFeature;

        private static readonly ProfilerMarker MarkerALINE = new ProfilerMarker("VisualShape");
        private static readonly ProfilerMarker MarkerCommandBuffer = new ProfilerMarker("Executing command buffer");
        private static readonly ProfilerMarker MarkerFrameTick = new ProfilerMarker("Frame Tick");
        private static readonly ProfilerMarker MarkerFilterDestroyedObjects = new ProfilerMarker("Filter destroyed objects");
        internal static readonly ProfilerMarker MarkerRefreshSelectionCache = new ProfilerMarker("Refresh Selection Cache");
        private static readonly ProfilerMarker MarkerGizmosAllowed = new ProfilerMarker("GizmosAllowed");
        private static readonly ProfilerMarker MarkerDrawGizmos = new ProfilerMarker("DrawGizmos");
        private static readonly ProfilerMarker MarkerSubmitGizmos = new ProfilerMarker("Submit Gizmos");

        public static ShapeManager instance
        {
            get
            {
                if (_instance == null) Init();
                return _instance;
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        public static void Init()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (Unity.Jobs.LowLevel.Unsafe.JobsUtility.IsExecutingJob) throw new System.Exception("Draw.* methods cannot be called from inside a job. See the documentation for info about how to use drawing functions from the Unity Job System.");
#endif
            if (_instance != null) return;

            // 这里可能会尝试查找尚未启用的现有类实例。
            // 但事实证明这很棘手。
            // Resources.FindObjectsOfTypeAll<T>() 是唯一包含 HideInInspector 对象的调用。
            // 但很难区分永远不会启用的内部对象和将要启用的对象。
            // 检查 .gameObject.scene.isLoaded 不可靠（即使 isLoaded 为 false 对象也可能正常工作）
            // 检查 .gameObject.scene.isValid 不可靠（即使 isValid 为 false 对象也可能正常工作）

            // 所以我们总是创建新实例。这不是特别耗费的操作，且每次游戏只发生一次。
            // OnEnable 调用会清理重复的管理器（如果有的话）。

            var go = new GameObject("RetainedGizmos")
            {
                hideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInInspector | HideFlags.HideInHierarchy
            };
            _instance = go.AddComponent<ShapeManager>();
            if (Application.isPlaying) DontDestroyOnLoad(go);
        }

        /// <summary>检测正在使用的渲染管线并配置它们进行渲染</summary>
        void RefreshRenderPipelineMode()
        {
            var pipelineType = RenderPipelineManager.currentPipeline != null ? RenderPipelineManager.currentPipeline.GetType() : null;

            if (pipelineType == typeof(UniversalRenderPipeline)) {
                detectedRenderPipeline = DetectedRenderPipeline.URP;
                return;
            }
            detectedRenderPipeline = DetectedRenderPipeline.BuiltInOrCustom;
        }

#if UNITY_EDITOR
        void DelayedDestroy()
        {
            EditorApplication.update -= DelayedDestroy;
            // 检查对象是否仍然存在（它可能已被其他方式销毁）。
            if (gameObject) GameObject.DestroyImmediate(gameObject);
        }

        void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode || change == PlayModeStateChange.ExitingPlayMode)
            {
                gizmos.OnChangingPlayMode();
            }
        }
#endif

        void OnEnable()
        {
            if (_instance == null) _instance = this;

            // 确保没有重复的管理器
            if (_instance != this)
            {
                // 无法在启用过程中销毁对象，需要稍微延迟
#if UNITY_EDITOR
                // 这仅在编辑器中重要，以避免旧管理器堆积。
                // 在实际游戏中最多有 1 个（实际上为零）旧管理器残留。
                // 最好使用协程，但不幸的是它们不适用于标记为 HideAndDontSave 的对象。
                EditorApplication.update += DelayedDestroy;
#endif
                return;
            }

            actuallyEnabled = true;
            if (gizmos == null) gizmos = new ShapeData();
            gizmos.frameRedrawScope = new RedrawScope(gizmos);
            Draw.builder = gizmos.GetBuiltInBuilder(false);
            Draw.ingame_builder = gizmos.GetBuiltInBuilder(true);
            commandBuffer = new CommandBuffer();
            commandBuffer.name = "VisualShape Gizmos";

            // 使用内置渲染管线时的回调
            Camera.onPostRender += PostRender;
            // 使用可脚本化渲染管线时的回调
            UnityEngine.Rendering.RenderPipelineManager.beginContextRendering += BeginContextRendering;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += EndCameraRendering;
#if UNITY_EDITOR
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        void BeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            RefreshRenderPipelineMode();
        }

        void BeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            RefreshRenderPipelineMode();
        }

        void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (detectedRenderPipeline == DetectedRenderPipeline.URP)
            {
                var data = camera.GetUniversalAdditionalCameraData();
                if (data != null)
                {
                    var renderer = data.scriptableRenderer;
                    if (renderPassFeature == null)
                    {
                        renderPassFeature = ScriptableObject.CreateInstance<VisualShapeURPRenderPassFeature>();
                    }
                    renderPassFeature.AddRenderPasses(renderer);
                }
            }
        }

        void OnDisable()
        {
            if (!actuallyEnabled) return;
            actuallyEnabled = false;
            commandBuffer.Dispose();
            commandBuffer = null;
            Camera.onPostRender -= PostRender;
            UnityEngine.Rendering.RenderPipelineManager.beginContextRendering -= BeginContextRendering;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= EndCameraRendering;
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
            // 如果用户在层级中复制了此 GameObject，Gizmos 可能为 null。
            if (gizmos != null)
            {
                Draw.builder.DiscardAndDisposeInternal();
                Draw.ingame_builder.DiscardAndDisposeInternal();
                gizmos.ClearData();
            }
            if (renderPassFeature != null)
            {
                ScriptableObject.DestroyImmediate(renderPassFeature);
                renderPassFeature = null;
            }
        }

        // 进入播放模式 = 重新加载场景 & 重新加载域
        //	编辑器 => 播放模式: OnDisable -> OnEnable（同一对象）
        //  播放模式 => 编辑器: OnApplicationQuit（注意：无 OnDisable/OnEnable）
        // 进入播放模式 = 重新加载场景 & 不重新加载域
        //	编辑器 => 播放模式: 无
        //  播放模式 => 编辑器: OnApplicationQuit
        // 进入播放模式 = 不重新加载场景 & 不重新加载域
        //	编辑器 => 播放模式: 无
        //  播放模式 => 编辑器: OnApplicationQuit
        // OnDestroy 对此对象几乎不会被调用（除非 Unity 或游戏退出）

        // TODO: 应在 OnDestroy 中运行。OnApplicationQuit 在 OnDestroy 之前运行（这不是我们想要的）
        // private void OnApplicationQuit () {
        // Debug.Log("OnApplicationQuit");
        // Draw.builder.DiscardAndDisposeInternal();
        // Draw.ingame_builder.DiscardAndDisposeInternal();
        // gizmos.ClearData();
        // Draw.builder = gizmos.GetBuiltInBuilder(false);
        // Draw.ingame_builder = gizmos.GetBuiltInBuilder(true);
        // }

        const float NO_DRAWING_TIMEOUT_SECS = 10;

        void OnEditorUpdate()
        {
            framePassed = true;
            CleanupIfNoCameraRendered();
        }

        void Update()
        {
            if (actuallyEnabled) CleanupIfNoCameraRendered();
        }

        void CleanupIfNoCameraRendered()
        {
            if (Time.frameCount > lastFrameCount + 1)
            {
                // 超过一帧未更新
                // 可能根本没有相机在渲染。
                // 确保不会因为每帧排队的绘制项而产生内存泄漏。
                CheckFrameTicking();
                gizmos.PostRenderCleanup();

                // 注意：我们不总是想在这里调用上述方法
                // 因为在相机渲染完成后立即调用更好。
                // 否则在 Update/OnEditorUpdate 之前或之后排队的绘制项可能
                // 最终在不同帧中（就渲染 Gizmos 而言）
            }

            if (Time.realtimeSinceStartup - lastFrameTime > NO_DRAWING_TIMEOUT_SECS)
            {
                // 距离上次绘制帧已超过 NO_DRAWING_TIMEOUT_SECS 秒。
                // 在编辑器中，某些脚本可能在例如 EditorWindow.Update 中排队绘制命令而场景
                // 视图或任何游戏视图都没有重新渲染。如果长时间没有渲染，我们会丢弃这些命令。
                Draw.builder.DiscardAndDisposeInternal();
                Draw.ingame_builder.DiscardAndDisposeInternal();
                Draw.builder = gizmos.GetBuiltInBuilder(false);
                Draw.ingame_builder = gizmos.GetBuiltInBuilder(true);
                lastFrameTime = Time.realtimeSinceStartup;
                RemoveDestroyedGizmoDrawers();
            }

            // 避免不绘制 Gizmos 时的潜在内存泄漏
            if (Time.frameCount - lastFilterFrame > 5)
            {
                lastFilterFrame = Time.frameCount;
                RemoveDestroyedGizmoDrawers();
            }
        }

        internal void ExecuteCustomRenderPass(ScriptableRenderContext context, Camera camera)
        {
            MarkerALINE.Begin();
            commandBuffer.Clear();
            SubmitFrame(camera, new ShapeData.CommandBufferWrapper { cmd = commandBuffer }, true);
            context.ExecuteCommandBuffer(commandBuffer);
            MarkerALINE.End();
        }

        internal void ExecuteCustomRenderGraphPass(ShapeData.CommandBufferWrapper cmd, Camera camera)
        {
            MarkerALINE.Begin();
            SubmitFrame(camera, cmd, true);
            MarkerALINE.End();
        }
        private void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (detectedRenderPipeline == DetectedRenderPipeline.BuiltInOrCustom)
            {
                // 相机渲染完成后执行自定义渲染通道。
                // 对于 URP，渲染通道已经执行过了。
                // 但对于自定义渲染管线，我们在这里执行渲染代码。
                // 这只是尽力而为。不可能兼容所有自定义渲染管线。
                // 但对于大多数简单的应该可以工作。
                // 对于 Unity 内置渲染管线，EndCameraRendering 方法永远不会被调用。
                ExecuteCustomRenderPass(context, camera);
            }
        }

        void PostRender(Camera camera)
        {
            // 此方法仅在使用 Unity 内置渲染管线时调用
            commandBuffer.Clear();
            SubmitFrame(camera, new ShapeData.CommandBufferWrapper { cmd = commandBuffer }, false);
            MarkerCommandBuffer.Begin();
            Graphics.ExecuteCommandBuffer(commandBuffer);
            MarkerCommandBuffer.End();
        }

        void CheckFrameTicking()
        {
            MarkerFrameTick.Begin();
            if (Time.frameCount != lastFrameCount)
            {
                framePassed = true;
                lastFrameCount = Time.frameCount;
                lastFrameTime = Time.realtimeSinceStartup;
                previousFrameRedrawScope = gizmos.frameRedrawScope;
                gizmos.frameRedrawScope = new RedrawScope(gizmos);
                Draw.builder.DisposeInternal();
                Draw.ingame_builder.DisposeInternal();
                Draw.builder = gizmos.GetBuiltInBuilder(false);
                Draw.ingame_builder = gizmos.GetBuiltInBuilder(true);
            }
            else if (framePassed && Application.isPlaying)
            {
                // 渲染帧已过但游戏帧未过！
                // 这可能意味着游戏已暂停。
                // 游戏暂停时重绘 Gizmos。
                // 也可能只是我们正在使用多个相机渲染。
                previousFrameRedrawScope.Draw();
            }

            if (framePassed)
            {
                gizmos.TickFramePreRender();
#if UNITY_EDITOR
                builtGizmos = false;
#endif
                framePassed = false;
            }
            MarkerFrameTick.End();
        }

        internal void SubmitFrame(Camera camera, ShapeData.CommandBufferWrapper cmd, bool usingRenderPipeline)
        {
#if UNITY_EDITOR
            bool isSceneViewCamera = SceneView.currentDrawingSceneView != null && SceneView.currentDrawingSceneView.camera == camera;
#else
            bool isSceneViewCamera = false;
#endif
            // 渲染到纹理时不包含，除非是场景视图相机
            bool allowCameraDefault = allowRenderToRenderTextures || drawToAllCameras || camera.targetTexture == null || isSceneViewCamera;

            CheckFrameTicking();

            Submit(camera, cmd, usingRenderPipeline, allowCameraDefault);

            gizmos.PostRenderCleanup();
        }

#if UNITY_EDITOR
        static readonly System.Reflection.MethodInfo IsGizmosAllowedForObject = typeof(UnityEditor.EditorGUIUtility).GetMethod("IsGizmosAllowedForObject", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        readonly System.Object[] cachedObjectParameterArray = new System.Object[1];
#endif

        readonly Dictionary<System.Type, bool> typeToGizmosEnabled = new Dictionary<Type, bool>();

        bool ShouldDrawGizmos(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            // 使用反射调用 EditorGUIUtility.IsGizmosAllowedForObject（这是一个内部方法）。
            // 不过这正是我们需要的信息。
            // 如果 Unity 更改了 API 导致找不到该方法，则直接返回 true
            cachedObjectParameterArray[0] = obj;
            return IsGizmosAllowedForObject == null || (bool)IsGizmosAllowedForObject.Invoke(null, cachedObjectParameterArray);
#else
            return true;
#endif
        }

        static void RemoveDestroyedGizmoDrawers()
        {
            MarkerFilterDestroyedObjects.Begin();
            int j = 0;
            for (int i = 0; i < gizmoDrawers.Count; i++)
            {
                var v = gizmoDrawers[i];
                if (v as MonoBehaviour)
                {
                    gizmoDrawers[j] = v;
                    j++;
                }
            }
            gizmoDrawers.RemoveRange(j, gizmoDrawers.Count - j);
            MarkerFilterDestroyedObjects.End();
        }

#if UNITY_EDITOR
        void DrawGizmos(bool usingRenderPipeline)
        {
            GizmoContext.SetDirty();
            MarkerGizmosAllowed.Begin();
            typeToGizmosEnabled.Clear();

            // 用哪些类应该被绘制的信息填充 typeToGizmosEnabled 字典
            foreach (var tp in gizmoDrawerTypes)
            {
                if (GizmoUtility.TryGetGizmoInfo(tp.Key, out var gizmoInfo))
                {
                    typeToGizmosEnabled[tp.Key] = gizmoInfo.gizmoEnabled;
                }
                else
                {
                    typeToGizmosEnabled[tp.Key] = true;
                }
            }

            MarkerGizmosAllowed.End();

            // 将当前帧的重绘作用域设置为空作用域。
            // 因为 Gizmos 无论如何每帧都会渲染，所以我们永远不想重绘它们。
            // 否则帧重绘作用域在游戏暂停时使用。
            var frameRedrawScope = gizmos.frameRedrawScope;
            gizmos.frameRedrawScope = default(RedrawScope);

            var currentStage = StageUtility.GetCurrentStage();
            var isInNonMainStage = currentStage != StageUtility.GetMainStage();

            // 用 'using' 块会更美观，但内置命令构建器
            // 不能正常释放以防止用户错误。
            // try-finally 等同于 'using' 块。
            var gizmoBuilder = gizmos.GetBuiltInBuilder();
            // 将 Draw.builder 替换为仅用于 Gizmos 的自定义构建器
            var debugBuilder = Draw.builder;
            MarkerDrawGizmos.Begin();
            GizmoContext.drawingGizmos = true;
            try
            {
                Draw.builder = gizmoBuilder;
                if (usingRenderPipeline)
                {
                    for (int i = gizmoDrawers.Count - 1; i >= 0; i--)
                    {
                        var mono = gizmoDrawers[i] as MonoBehaviour;
                        // 如果场景处于隔离模式（如聚焦于单个预制体）且此对象不属于该子阶段则为 true
                        var disabledDueToIsolationMode = isInNonMainStage && StageUtility.GetStage(mono.gameObject) != currentStage;
                        var gizmosEnabled = mono.isActiveAndEnabled && typeToGizmosEnabled[gizmoDrawers[i].GetType()];
                        if (gizmosEnabled && (mono.hideFlags & HideFlags.HideInHierarchy) == 0 && !disabledDueToIsolationMode)
                        {
                            try
                            {
                                gizmoDrawers[i].DrawGizmos();
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogException(e, mono);
                            }
                        }
                    }
                }
                else
                {
                    for (int i = gizmoDrawers.Count - 1; i >= 0; i--)
                    {
                        var mono = gizmoDrawers[i] as MonoBehaviour;
                        if (mono.isActiveAndEnabled && (mono.hideFlags & HideFlags.HideInHierarchy) == 0 && typeToGizmosEnabled[gizmoDrawers[i].GetType()])
                        {
                            // 如果场景处于隔离模式（如聚焦于单个预制体）且此对象不属于该子阶段则为 true
                            var disabledDueToIsolationMode = isInNonMainStage && StageUtility.GetStage(mono.gameObject) != currentStage;
                            try
                            {
                                if (!disabledDueToIsolationMode) gizmoDrawers[i].DrawGizmos();
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogException(e, mono);
                            }
                        }
                    }
                }
            }
            finally
            {
                GizmoContext.drawingGizmos = false;
                MarkerDrawGizmos.End();
                // 恢复到原始构建器
                Draw.builder = debugBuilder;
                gizmoBuilder.DisposeInternal();
            }

            gizmos.frameRedrawScope = frameRedrawScope;

            // 调度绘制 Gizmos 时可能已安排的 Job
            JobHandle.ScheduleBatchedJobs();
        }
#endif

        /// <summary>提交相机进行渲染。</summary>
        /// <param name="allowCameraDefault">指示内置命令构建器和没有自定义 CommandBuilder.cameraTargets 的自定义构建器是否应渲染到此相机。</param>
        void Submit(Camera camera, ShapeData.CommandBufferWrapper cmd, bool usingRenderPipeline, bool allowCameraDefault)
        {
#if UNITY_EDITOR
            bool drawGizmos = Handles.ShouldRenderGizmos() || drawToAllCameras;
            // 仅在相机实际需要时构建 Gizmos。
            // 每帧仅对第一个需要它们的相机执行此操作。
            if (drawGizmos && !builtGizmos && allowCameraDefault)
            {
                RemoveDestroyedGizmoDrawers();
                lastFilterFrame = Time.frameCount;
                builtGizmos = true;
                DrawGizmos(usingRenderPipeline);
            }
#else
            bool drawGizmos = false;
#endif

            MarkerSubmitGizmos.Begin();
            Draw.builder.DisposeInternal();
            Draw.ingame_builder.DisposeInternal();
            gizmos.Render(camera, drawGizmos, cmd, allowCameraDefault);
            Draw.builder = gizmos.GetBuiltInBuilder(false);
            Draw.ingame_builder = gizmos.GetBuiltInBuilder(true);
            MarkerSubmitGizmos.End();
        }

        /// <summary>
        /// 注册对象以进行 Gizmo 绘制。
        /// 对象上的 DrawGizmos 方法将每帧调用直到其被销毁（假设有启用了 Gizmos 的相机）。
        /// </summary>
        public static void Register(IDrawGizmos item)
        {
            var tp = item.GetType();

            // 使用反射判断 DrawGizmos 方法是否未从 MonoBehaviourGizmos 类重写。
            // 如果没有重写，则此类型永远不会绘制 Gizmos，可以跳过。
            // 通过不必跟踪对象并每帧检查它们是否激活来提高性能。
            bool mayDrawGizmos;
            if (gizmoDrawerTypes.TryGetValue(tp, out mayDrawGizmos))
            {
            }
            else
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                // 先检查公共方法，然后检查显式接口实现。
                var m = tp.GetMethod("DrawGizmos", flags) ?? tp.GetMethod("VisualShape.IDrawGizmos.DrawGizmos", flags);
                if (m == null)
                {
                    throw new System.Exception("Could not find the DrawGizmos method in type " + tp.Name);
                }
                mayDrawGizmos = m.DeclaringType != typeof(MonoBehaviourGizmos);
                gizmoDrawerTypes[tp] = mayDrawGizmos;
            }
            if (!mayDrawGizmos) return;

            gizmoDrawers.Add(item);
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
        public static CommandBuilder GetBuilder(bool renderInGame = false) => instance.gizmos.GetBuilder(renderInGame);

        /// <summary>
        /// 获取空的构建器以排队绘制命令。
        ///
        /// See: <see cref="VisualShape.CommandBuilder"/>
        /// </summary>
        /// <param name="redrawScope">此命令构建器的作用域。参见 #GetRedrawScope。</param>
        /// <param name="renderInGame">如果为 true，此构建器将在独立游戏和编辑器中渲染，即使 Gizmos 被禁用。
        /// 如果为 false，仅在编辑器中启用 Gizmos 时渲染。</param>
        public static CommandBuilder GetBuilder(RedrawScope redrawScope, bool renderInGame = false) => instance.gizmos.GetBuilder(redrawScope, renderInGame);

        /// <summary>
        /// 获取空的构建器以排队绘制命令。
        /// TODO: 示例用法。
        ///
        /// See: <see cref="VisualShape.CommandBuilder"/>
        /// </summary>
        /// <param name="hasher">用于生成绘制数据的输入的哈希值。</param>
        /// <param name="redrawScope">此命令构建器的作用域。参见 #GetRedrawScope。</param>
        /// <param name="renderInGame">如果为 true，此构建器将在独立游戏和编辑器中渲染，即使 Gizmos 被禁用。</param>
        public static CommandBuilder GetBuilder(ShapeData.Hasher hasher, RedrawScope redrawScope = default, bool renderInGame = false) => instance.gizmos.GetBuilder(hasher, redrawScope, renderInGame);

        /// <summary>
        /// 可用于跨多帧绘制的作用域。
        /// 可使用 <see cref="GetBuilder(RedrawScope,bool)"/> 获取具有给定重绘作用域的构建器。
        /// 释放构建器后，可在之后任意帧调用 <see cref="VisualShape.RedrawScope.Draw"/> 再次渲染命令构建器。
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
        /// 注意：仅当每帧调用 <see cref="VisualShape.RedrawScope.Draw"/> 时数据才会保留。
        /// 如果在之后的帧中不调用 <see cref="VisualShape.RedrawScope.Draw"/>，命令构建器的数据将被清除。
        /// 清除后再调用 <see cref="VisualShape.RedrawScope.Draw"/> 将不会有任何效果。
        /// </summary>
        public static RedrawScope GetRedrawScope()
        {
            var scope = new RedrawScope(instance.gizmos);
            scope.DrawUntilDispose();
            return scope;
        }
    }
}


