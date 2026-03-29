using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace VisualShape
{
    /// <summary>Custom Universal Render Pipeline Render Pass for VisualShape</summary>
    public class VisualShapeURPRenderPassFeature : ScriptableRendererFeature
    {
        /// <summary>Custom Universal Render Pipeline Render Pass for VisualShape</summary>
        public class VisualShapeURPRenderPass : ScriptableRenderPass
        {
            /// <summary>This method is called before executing the render pass</summary>
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                ShapeManager.instance.ExecuteCustomRenderPass(context, renderingData.cameraData.camera);
            }

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
                        // We need this to be able to disable wireframe rendering in the scene view
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

            // URP's post processing actually happens in BeforeRenderingPostProcessing, not after BeforeRenderingPostProcessing as one would expect.
            // Use BeforeRenderingPostProcessing-1 to ensure this pass gets executed before post processing effects.
            m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing - 1;
        }

        /// <summary>This method is called when setting up the renderer once per-camera</summary>
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
