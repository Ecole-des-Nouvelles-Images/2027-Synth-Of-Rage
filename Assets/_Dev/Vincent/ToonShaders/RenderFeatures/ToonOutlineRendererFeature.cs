
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace _Dev.Vincent.ToonShaders.RenderFeatures
{
    public class ToonOutlineRendererFeature : ScriptableRendererFeature
    {
        // ================================================================
        // SETTINGS
        // ================================================================

        [System.Serializable]
        public class Settings
        {
            [Header("Outline")]
            public Material outlineMaterial;

            [Min(0)]
            public int materialPassIndex = 0;

            [Header("Rendering")]
            public RenderPassEvent injectionPoint =
                RenderPassEvent.BeforeRenderingPostProcessing;
        }

        [SerializeField]
        private Settings settings = new Settings();

        private ToonOutlineRenderPass outlinePass;

        // ================================================================
        // SHADER GLOBAL TEXTURES
        // ================================================================

        private static readonly int Sprite3DDepthTextureID =
            Shader.PropertyToID("_Sprite3DDepthTexture");

        private static readonly int Sprite3DOutlineOccluderDepthTextureID =
            Shader.PropertyToID("_Sprite3DOutlineOccluderDepthTexture");

        // ================================================================
        // CREATE
        // ================================================================

        public override void Create()
        {
            outlinePass = new ToonOutlineRenderPass();

            outlinePass.renderPassEvent =
                settings.injectionPoint;
        }

        // ================================================================
        // ADD RENDER PASSES
        // ================================================================

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (outlinePass == null)
                return;

            CameraData cameraData = renderingData.cameraData;

            // Ignore preview and reflection cameras.
            if (cameraData.cameraType == CameraType.Preview ||
                cameraData.cameraType == CameraType.Reflection)
            {
                return;
            }

            if (settings.outlineMaterial == null)
                return;

            if (settings.materialPassIndex < 0 ||
                settings.materialPassIndex >= settings.outlineMaterial.passCount)
            {
                Debug.LogWarning(
                    "[ToonOutlineRendererFeature] Invalid material pass index.",
                    this);

                return;
            }

            outlinePass.Setup(
                settings.outlineMaterial,
                settings.materialPassIndex
            );

            outlinePass.renderPassEvent =
                settings.injectionPoint;

            // The Outline shader samples the camera depth texture.
            outlinePass.ConfigureInput(
                ScriptableRenderPassInput.Depth
            );

            // Request an intermediate color texture.
            outlinePass.requiresIntermediateTexture = true;

            renderer.EnqueuePass(outlinePass);
        }

        // ================================================================
        // RENDER PASS
        // ================================================================

        private sealed class ToonOutlineRenderPass : ScriptableRenderPass
        {
            private const string CopyPassName =
                "Toon Outline - Copy Camera Color";

            private const string OutlinePassName =
                "Toon Outline - Combined Mesh Sprite Depth Outline";

            private Material outlineMaterial;
            private int materialPassIndex;

            // ============================================================
            // PASS DATA
            // ============================================================

            private sealed class PassData
            {
                public TextureHandle source;
                public Material material;
                public int materialPassIndex;
            }

            // ============================================================
            // SETUP
            // ============================================================

            public void Setup(
                Material material,
                int passIndex)
            {
                outlineMaterial = material;
                materialPassIndex = passIndex;
            }

            // ============================================================
            // RECORD RENDER GRAPH
            // ============================================================

            public override void RecordRenderGraph(
                RenderGraph renderGraph,
                ContextContainer frameData)
            {
                if (outlineMaterial == null)
                    return;

                UniversalResourceData resourcesData =
                    frameData.Get<UniversalResourceData>();

                // --------------------------------------------------------
                // CAMERA COLOR
                // --------------------------------------------------------

                TextureHandle source =
                    resourcesData.activeColorTexture;

                if (!source.IsValid())
                    return;

                // --------------------------------------------------------
                // CAMERA DEPTH
                //
                // Used by SampleSceneDepth() in the Outline shader.
                // --------------------------------------------------------

                TextureHandle cameraDepth =
                    resourcesData.cameraDepthTexture;

                if (!cameraDepth.IsValid())
                    return;

                // --------------------------------------------------------
                // TEMPORARY COLOR TEXTURE
                // --------------------------------------------------------

                var descriptor =
                    renderGraph.GetTextureDesc(source);

                descriptor.name =
                    "_CameraColor_ToonOutline";

                descriptor.clearBuffer = false;

                TextureHandle copiedColor =
                    renderGraph.CreateTexture(descriptor);

                // Copy the current camera color.
                renderGraph.AddCopyPass(
                    source,
                    copiedColor,
                    passName: CopyPassName
                );

                // --------------------------------------------------------
                // OUTLINE RASTER PASS
                // --------------------------------------------------------

                using (var builder =
                    renderGraph.AddRasterRenderPass<PassData>(
                        OutlinePassName,
                        out PassData passData))
                {
                    passData.source = copiedColor;
                    passData.material = outlineMaterial;
                    passData.materialPassIndex = materialPassIndex;

                    // ----------------------------------------------------
                    // SOURCE COLOR
                    // ----------------------------------------------------

                    builder.UseTexture(
                        copiedColor,
                        AccessFlags.Read
                    );

                    // ----------------------------------------------------
                    // CAMERA DEPTH
                    // ----------------------------------------------------

                    builder.UseTexture(
                        cameraDepth,
                        AccessFlags.Read
                    );

                    // ----------------------------------------------------
                    // SPRITE3D OUTLINE DEPTH
                    //
                    // R16 normalized eye depth.
                    // ----------------------------------------------------

                    builder.UseGlobalTexture(
                        Sprite3DDepthTextureID,
                        AccessFlags.Read
                    );

                    // ----------------------------------------------------
                    // SPRITE3D OCCLUDER DEPTH
                    //
                    // R32 linear eye depth.
                    // ----------------------------------------------------

                    builder.UseGlobalTexture(
                        Sprite3DOutlineOccluderDepthTextureID,
                        AccessFlags.Read
                    );

                    // ----------------------------------------------------
                    // DESTINATION CAMERA COLOR
                    // ----------------------------------------------------

                    builder.SetRenderAttachment(
                        resourcesData.activeColorTexture,
                        0,
                        AccessFlags.Write
                    );

                    // ----------------------------------------------------
                    // RENDER FUNCTION
                    // ----------------------------------------------------

                    builder.SetRenderFunc(
                        (PassData data, RasterGraphContext context) =>
                        {
                            Blitter.BlitTexture(
                                context.cmd,
                                data.source,
                                new Vector4(1f, 1f, 0f, 0f),
                                data.material,
                                data.materialPassIndex
                            );
                        }
                    );
                }
            }
        }
    }
}
