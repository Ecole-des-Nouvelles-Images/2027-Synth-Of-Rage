
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace _Dev.Vincent.ToonShaders.RenderFeatures
{
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
            public LayerMask layerMask = ~0;

            [Tooltip(
                "Must execute BEFORE transparent Sprite3D rendering."
            )]
            public RenderPassEvent injectionPoint =
                RenderPassEvent.BeforeRenderingTransparents;
        }

        public Settings settings = new Settings();

        private Sprite3DDepthPass pass;

        // ================================================================
        // CREATE
        // ================================================================

        public override void Create()
        {
            pass = new Sprite3DDepthPass(
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
            if (pass == null)
                return;

            pass.SetLayerMask(
                settings.layerMask
            );

            pass.renderPassEvent =
                settings.injectionPoint;

            renderer.EnqueuePass(pass);
        }

        // ================================================================
        // PASS
        // ================================================================

        private class Sprite3DDepthPass : ScriptableRenderPass
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

            private static readonly ShaderTagId OutlineOccluderDepthShaderTag =
                new ShaderTagId(
                    "SpriteOutlineOccluderDepth"
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

            private static readonly int OutlineOccluderDepthTextureID =
                Shader.PropertyToID(
                    "_Sprite3DOutlineOccluderDepthTexture"
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
                this.layerMask = layerMask;
            }

            // ============================================================
            // LAYER
            // ============================================================

            public void SetLayerMask(
                LayerMask newLayerMask
            )
            {
                layerMask = newLayerMask;
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

                baseDescriptor.msaaSamples = 1;
                baseDescriptor.depthBufferBits = 0;

                // ========================================================
                // SCENE DEPTH TEXTURE
                // ========================================================
                //
                // R32 FLOAT.
                //
                // Stocke directement la profondeur linéaire en unités
                // Unity pour le tri des Sprite3D.
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
                // R16 FLOAT.
                //
                // Cette texture reste normalisée 0-1 car le shader
                // Outline existant attend ce format.
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
                // OUTLINE OCCLUDER DEPTH TEXTURE
                // ========================================================
                //
                // R32 FLOAT.
                //
                // Stocke la profondeur linéaire des Sprite3D autorisés
                // à masquer les outlines situés derrière eux.
                //
                // Cette texture est indépendante de la texture
                // Sprite3DDepthTexture utilisée pour générer les
                // outlines des sprites.
                // ========================================================

                RenderTextureDescriptor outlineOccluderDescriptor =
                    baseDescriptor;

                outlineOccluderDescriptor.graphicsFormat =
                    GraphicsFormat.R32_SFloat;

                TextureHandle outlineOccluderDepthTexture =
                    UniversalRenderer.CreateRenderGraphTexture(
                        renderGraph,
                        outlineOccluderDescriptor,
                        "_Sprite3DOutlineOccluderDepthTexture",
                        false,
                        FilterMode.Point,
                        TextureWrapMode.Clamp
                    );

                // ========================================================
                // CLEAR VALUES
                // ========================================================

                float sceneDepthClearValue =
                    cameraData.camera.farClipPlane + 1.0f;

                float outlineOccluderClearValue =
                    cameraData.camera.farClipPlane + 1.0f;

                // ========================================================
                // ALL SPRITE3D SCENE DEPTH
                // ========================================================

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
                //
                // Seuls les Sprite3D ayant _SpriteOutlineEnabled = 1
                // participent à cette texture.
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

                // ========================================================
                // SPRITE3D OUTLINE OCCLUDER DEPTH
                // ========================================================
                //
                // Indépendant de l'activation de leur propre outline.
                //
                // Les Sprite3D ayant :
                //
                // _SpriteOutlineOcclusionEnabled = 1
                //
                // peuvent masquer les outlines situés derrière eux.
                //
                // Le shader applique également le seuil alpha configuré.
                // ========================================================

                RecordDepthPass(
                    renderGraph,
                    renderingData,
                    cameraData,
                    lightData,
                    OutlineOccluderDepthShaderTag,
                    outlineOccluderDepthTexture,
                    OutlineOccluderDepthTextureID,
                    "Sprite3D Outline Occluder Depth",
                    outlineOccluderClearValue
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

                using (
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

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(
                        static (
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
}
