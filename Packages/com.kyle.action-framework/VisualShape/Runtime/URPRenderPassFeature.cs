using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace VisualShape
{
    /// <summary>VisualShape 的自定义通用渲染管线渲染通道</summary>
    public class VisualShapeURPRenderPassFeature : ScriptableRendererFeature
    {
        /// <summary>VisualShape 的自定义通用渲染管线渲染通道</summary>
        public class VisualShapeURPRenderPass : ScriptableRenderPass
        {

            public VisualShapeURPRenderPass() : base()
            {
                profilingSampler = new ProfilingSampler("VisualShape");
            }

            private class PassData
            {
                public Camera camera;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("VisualShape", out PassData passData, profilingSampler))
                {
                    bool allowDisablingWireframe = false;

                    if (Application.isEditor && (cameraData.cameraType & (CameraType.SceneView | CameraType.Preview)) != 0)
                    {
                        // 需要此项以便在场景视图中禁用线框渲染
                        builder.AllowGlobalStateModification(true);
                        allowDisablingWireframe = true;
                    }

                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                    passData.camera = cameraData.camera;

                    builder.SetRenderFunc<PassData>(
                        (PassData data, RasterGraphContext context) =>
                        {
                            ShapeManager.instance.ExecuteCustomRenderGraphPass(new ShapeData.CommandBufferWrapper { cmd2 = context.cmd, allowDisablingWireframe = allowDisablingWireframe }, data.camera);
                        }
                        );
                }
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
            }
        }

        VisualShapeURPRenderPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new VisualShapeURPRenderPass();

            // URP 的后处理实际上发生在 BeforeRenderingPostProcessing 期间，而不是在其之后。
            // 使用 BeforeRenderingPostProcessing-1 以确保此通道在后处理效果之前执行。
            m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing - 1;
        }

        /// <summary>此方法在每个相机设置渲染器时调用</summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            AddRenderPasses(renderer);
        }

        public void AddRenderPasses(ScriptableRenderer renderer)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
