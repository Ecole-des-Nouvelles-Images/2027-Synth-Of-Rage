using UnityEngine;

using UnityEngine.Experimental.Rendering;

using UnityEngine.Rendering;

using UnityEngine.Rendering.RenderGraphModule;

using UnityEngine.Rendering.Universal;


public class Sprite3DDepthFeature : ScriptableRendererFeature
{
    // ================================================================
    // SETTINGS
    // ================================================================

    [System.Serializable]
    public class Settings
    {
        [Tooltip(
            "Only Sprite3D objects on these layers are included."
        )]
        public LayerMask layerMask =
            ~0;


        [Tooltip(
            "Must execute BEFORE transparent Sprite3D rendering."
        )]
        public RenderPassEvent injectionPoint =
            RenderPassEvent.BeforeRenderingTransparents;
    }


    public Settings settings =
        new Settings();


    private Sprite3DDepthPass pass;


    // ================================================================
    // CREATE
    // ================================================================

    public override void Create()
    {
        pass =
            new Sprite3DDepthPass(
                settings.layerMask
            );


        pass.renderPassEvent =
            settings.injectionPoint;
    }


    // ================================================================
    // ADD RENDER PASSES
    // ================================================================

    public override void AddRenderPasses(
        ScriptableRenderer renderer,

        ref RenderingData renderingData
    )
    {
        if(
            pass ==
            null
        )
        {
            return;
        }


        pass.SetLayerMask(
            settings.layerMask
        );


        pass.renderPassEvent =
            settings.injectionPoint;


        renderer.EnqueuePass(
            pass
        );
    }


    // ================================================================
    // PASS
    // ================================================================

    private class Sprite3DDepthPass :
        ScriptableRenderPass
    {
        // ============================================================
        // TAGS
        // ============================================================

        private static readonly ShaderTagId SceneDepthShaderTag =
            new ShaderTagId(
                "SpriteSceneDepth"
            );


        private static readonly ShaderTagId OutlineDepthShaderTag =
            new ShaderTagId(
                "SpriteOutlineDepth"
            );


        // ============================================================
        // GLOBAL TEXTURES
        // ============================================================

        private static readonly int SceneDepthTextureID =
            Shader.PropertyToID(
                "_Sprite3DSceneDepthTexture"
            );


        private static readonly int OutlineDepthTextureID =
            Shader.PropertyToID(
                "_Sprite3DDepthTexture"
            );


        private LayerMask layerMask;


        // ============================================================
        // PASS DATA
        // ============================================================

        private class PassData
        {
            public RendererListHandle rendererList;

            public float clearValue;
        }


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public Sprite3DDepthPass(
            LayerMask layerMask
        )
        {
            this.layerMask =
                layerMask;
        }


        // ============================================================
        // LAYER
        // ============================================================

        public void SetLayerMask(
            LayerMask newLayerMask
        )
        {
            layerMask =
                newLayerMask;
        }


        // ============================================================
        // RECORD GRAPH
        // ============================================================

        public override void RecordRenderGraph(
            RenderGraph renderGraph,

            ContextContainer frameData
        )
        {
            UniversalRenderingData renderingData =
                frameData.Get<UniversalRenderingData>();


            UniversalCameraData cameraData =
                frameData.Get<UniversalCameraData>();


            UniversalLightData lightData =
                frameData.Get<UniversalLightData>();


            // ========================================================
            // BASE DESCRIPTOR
            // ========================================================

            RenderTextureDescriptor baseDescriptor =
                cameraData.cameraTargetDescriptor;


            baseDescriptor.msaaSamples =
                1;


            baseDescriptor.depthBufferBits =
                0;


            // ========================================================
            // SCENE DEPTH TEXTURE
            // ========================================================
            //
            // IMPORTANT :
            //
            // R32 FLOAT.
            //
            // On stocke directement :
            //
            // linear eye depth en mètres.
            //
            // Ça évite :
            //
            // - quantification R16
            // - normalize / denormalize
            // - bandes / holes / faux alpha
            // ========================================================

            RenderTextureDescriptor sceneDepthDescriptor =
                baseDescriptor;


            sceneDepthDescriptor.graphicsFormat =
                GraphicsFormat.R32_SFloat;


            TextureHandle sceneDepthTexture =
                UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,

                    sceneDepthDescriptor,

                    "_Sprite3DSceneDepthTexture",

                    false,

                    FilterMode.Point,

                    TextureWrapMode.Clamp
                );


            // ========================================================
            // OUTLINE DEPTH TEXTURE
            // ========================================================
            //
            // Celle-ci reste normalisée 0-1 car notre fullscreen
            // outline actuel attend ce format.
            // ========================================================

            RenderTextureDescriptor outlineDepthDescriptor =
                baseDescriptor;


            outlineDepthDescriptor.graphicsFormat =
                GraphicsFormat.R16_SFloat;


            TextureHandle outlineDepthTexture =
                UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,

                    outlineDepthDescriptor,

                    "_Sprite3DDepthTexture",

                    false,

                    FilterMode.Point,

                    TextureWrapMode.Clamp
                );


            // ========================================================
            // ALL SPRITE3D SCENE DEPTH
            // ========================================================

            float sceneDepthClearValue =
                cameraData.camera.farClipPlane +
                1.0f;


            RecordDepthPass(
                renderGraph,

                renderingData,

                cameraData,

                lightData,

                SceneDepthShaderTag,

                sceneDepthTexture,

                SceneDepthTextureID,

                "Sprite3D Scene Depth",

                sceneDepthClearValue
            );


            // ========================================================
            // OUTLINE-ENABLED SPRITE3D DEPTH
            // ========================================================

            RecordDepthPass(
                renderGraph,

                renderingData,

                cameraData,

                lightData,

                OutlineDepthShaderTag,

                outlineDepthTexture,

                OutlineDepthTextureID,

                "Sprite3D Outline Depth",

                1.0f
            );
        }


        // ================================================================
        // RECORD DEPTH PASS
        // ================================================================

        private void RecordDepthPass(
            RenderGraph renderGraph,

            UniversalRenderingData renderingData,

            UniversalCameraData cameraData,

            UniversalLightData lightData,

            ShaderTagId shaderTag,

            TextureHandle targetTexture,

            int globalTextureID,

            string passName,

            float clearValue
        )
        {
            // ========================================================
            // SORT
            // ========================================================

            SortingCriteria sortingCriteria =
                SortingCriteria.CommonTransparent;


            // ========================================================
            // DRAW SETTINGS
            // ========================================================

            DrawingSettings drawingSettings =
                RenderingUtils.CreateDrawingSettings(
                    shaderTag,

                    renderingData,

                    cameraData,

                    lightData,

                    sortingCriteria
                );


            // ========================================================
            // FILTER
            // ========================================================

            FilteringSettings filteringSettings =
                new FilteringSettings(
                    RenderQueueRange.transparent,

                    layerMask
                );


            // ========================================================
            // RENDERER LIST
            // ========================================================

            RendererListParams rendererListParams =
                new RendererListParams(
                    renderingData.cullResults,

                    drawingSettings,

                    filteringSettings
                );


            // ========================================================
            // RENDER GRAPH PASS
            // ========================================================

            using(
                var builder =
                    renderGraph.AddRasterRenderPass<PassData>(
                        passName,

                        out var passData
                    )
            )
            {
                passData.rendererList =
                    renderGraph.CreateRendererList(
                        rendererListParams
                    );


                passData.clearValue =
                    clearValue;


                builder.UseRendererList(
                    passData.rendererList
                );


                builder.SetRenderAttachment(
                    targetTexture,

                    0,

                    AccessFlags.Write
                );


                builder.SetGlobalTextureAfterPass(
                    targetTexture,

                    globalTextureID
                );


                builder.AllowPassCulling(
                    false
                );


                builder.SetRenderFunc(
                    static(
                        PassData data,

                        RasterGraphContext context
                    ) =>
                    {
                        // =============================================
                        // CLEAR
                        // =============================================

                        Color clearColor =
                            new Color(
                                data.clearValue,

                                0.0f,

                                0.0f,

                                1.0f
                            );


                        context.cmd.ClearRenderTarget(
                            false,

                            true,

                            clearColor
                        );


                        // =============================================
                        // DRAW
                        // =============================================

                        context.cmd.DrawRendererList(
                            data.rendererList
                        );
                    }
                );
            }
        }
    }
}