
Shader "Custom/SOR/ParticlesLit"
{
    Properties
    {
        // ============================================================
        // MAIN TEXTURE
        // ============================================================

        [MainTexture]
        _BaseMap("Particle Texture", 2D) = "white" {}

        [MainColor]
        _BaseColor("Particle Color", Color) = (1,1,1,1)


        // ============================================================
        // LIGHTING
        // ============================================================

        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5

        [Toggle(_RECEIVE_SHADOWS)]
        _ReceiveShadows("Receive Shadows", Float) = 1

        [Toggle(_PARTICLE_VIEW_NORMALS)]
        _UseViewFacingNormals("Use View-Facing Normals (Billboard)", Float) = 0


        // ============================================================
        // SOFT PARTICLES
        // ============================================================

        [Toggle(_SOFT_PARTICLES)]
        _SoftParticlesEnabled("Enable Soft Particles", Float) = 1

        _SoftParticleDistance("Softness Distance", Range(0.01, 5)) = 1


        // ============================================================
        // SPRITE 3D OCCLUSION
        // ============================================================

        [Header(Sprite 3D Occlusion)]

        [Toggle]
        _Sprite3DOcclusionEnabled("Enable Sprite 3D Occlusion", Float) = 1

        _SpriteDepthSortEpsilon(
            "Sprite Depth Sort Epsilon",
            Range(0.0001, 0.1)
        ) = 0.005


        // ============================================================
        // EMISSION
        // ============================================================

        [Toggle(_EMISSION)]
        _EmissionEnabled("Enable Emission", Float) = 0

        [HDR]
        _EmissionColor("Emission Color", Color) = (1,1,1,1)

        _EmissionMap("Emission Texture", 2D) = "white" {}

        _EmissionMask("Emission Mask (Red Channel)", 2D) = "white" {}

        _EmissionStrength("Emission Strength", Range(0,10)) = 1

        [Toggle]
        _EmissionUseParticleColor(
            "Multiply Emission By Particle Color",
            Float
        ) = 0


        // ============================================================
        // BLENDING
        // ============================================================

        [Enum(Alpha,0,Additive,1,Premultiply,2)]
        _BlendMode("Blend Mode", Float) = 0


        // ============================================================
        // INTERNAL RENDERING
        // ============================================================

        [HideInInspector]
        _Surface("__surface", Float) = 1

        [HideInInspector]
        _Blend("__blend", Float) = 0

        [HideInInspector]
        _Cull("__cull", Float) = 0
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "IgnoreProjector" = "True"
            "UniversalMaterialType" = "Lit"
        }

        LOD 300


        // ============================================================
        // SHARED HLSL
        // ============================================================

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


        // ============================================================
        // TEXTURES
        // ============================================================

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        TEXTURE2D(_EmissionMap);
        SAMPLER(sampler_EmissionMap);

        TEXTURE2D(_EmissionMask);
        SAMPLER(sampler_EmissionMask);


        // ============================================================
        // SPRITE 3D SCENE DEPTH
        // ============================================================

        // Texture générée par Sprite3D / SpriteSceneDepth.
        // Elle contient la profondeur linéaire des sprites 3D.

        Texture2D<float> _Sprite3DSceneDepthTexture;


        // ============================================================
        // MATERIAL
        // ============================================================

        CBUFFER_START(UnityPerMaterial)

            float4 _BaseMap_ST;

            float4 _EmissionMap_ST;
            float4 _EmissionMask_ST;

            half4 _BaseColor;
            half4 _EmissionColor;

            half _Metallic;
            half _Smoothness;

            half _SoftParticleDistance;

            half _EmissionStrength;
            half _EmissionUseParticleColor;

            half _BlendMode;

            half _Sprite3DOcclusionEnabled;
            float _SpriteDepthSortEpsilon;

        CBUFFER_END


        // ============================================================
        // STRUCTURES
        // ============================================================

        struct Attributes
        {
            float4 positionOS : POSITION;

            float3 normalOS : NORMAL;

            float2 uv : TEXCOORD0;

            half4 color : COLOR;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };


        struct Varyings
        {
            float4 positionCS : SV_POSITION;

            float3 positionWS : TEXCOORD0;

            float3 normalWS : TEXCOORD1;

            float2 uv : TEXCOORD2;

            float2 emissionUV : TEXCOORD3;

            float2 emissionMaskUV : TEXCOORD4;

            half4 color : COLOR;

            float4 shadowCoord : TEXCOORD5;

            half fogFactor : TEXCOORD6;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };


        // ============================================================
        // VERTEX
        // ============================================================

        Varyings Vert(Attributes IN)
        {
            Varyings OUT;

            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_TRANSFER_INSTANCE_ID(IN, OUT);


            // --------------------------------------------------------
            // POSITION
            // --------------------------------------------------------

            VertexPositionInputs positionInputs =
                GetVertexPositionInputs(IN.positionOS.xyz);

            VertexNormalInputs normalInputs =
                GetVertexNormalInputs(IN.normalOS);


            OUT.positionCS =
                positionInputs.positionCS;

            OUT.positionWS =
                positionInputs.positionWS;

            OUT.normalWS =
                NormalizeNormalPerVertex(normalInputs.normalWS);


            // --------------------------------------------------------
            // UV
            // --------------------------------------------------------

            OUT.uv =
                TRANSFORM_TEX(IN.uv, _BaseMap);

            OUT.emissionUV =
                TRANSFORM_TEX(IN.uv, _EmissionMap);

            OUT.emissionMaskUV =
                TRANSFORM_TEX(IN.uv, _EmissionMask);


            // --------------------------------------------------------
            // PARTICLE COLOR
            // --------------------------------------------------------

            OUT.color =
                IN.color * _BaseColor;


            // --------------------------------------------------------
            // SHADOWS
            // --------------------------------------------------------

            OUT.shadowCoord =
                GetShadowCoord(positionInputs);


            // --------------------------------------------------------
            // FOG
            // --------------------------------------------------------

            OUT.fogFactor =
                ComputeFogFactor(OUT.positionCS.z);


            return OUT;
        }


        // ============================================================
        // SOFT PARTICLE FADE
        // ============================================================

        half CalculateSoftParticleFade(Varyings IN)
        {
            #if defined(_SOFT_PARTICLES)

                float2 screenUV =
                    GetNormalizedScreenSpaceUV(IN.positionCS);

                float rawSceneDepth =
                    SampleSceneDepth(screenUV);

                float sceneEyeDepth =
                    LinearEyeDepth(
                        rawSceneDepth,
                        _ZBufferParams
                    );

                float particleEyeDepth =
                    -TransformWorldToView(IN.positionWS).z;

                float depthDifference =
                    sceneEyeDepth - particleEyeDepth;

                return saturate(
                    depthDifference /
                    max(_SoftParticleDistance, 0.001)
                );

            #else

                return 1.0;

            #endif
        }


        // ============================================================
        // SPRITE 3D DEPTH OCCLUSION
        // ============================================================

        void ApplySprite3DOcclusion(Varyings IN)
        {
            // --------------------------------------------------------
            // ENABLE CHECK
            // --------------------------------------------------------

            if (_Sprite3DOcclusionEnabled < 0.5)
                return;


            // --------------------------------------------------------
            // TEXTURE DIMENSIONS
            // --------------------------------------------------------

            uint textureWidth;
            uint textureHeight;

            _Sprite3DSceneDepthTexture.GetDimensions(
                textureWidth,
                textureHeight
            );

            if (textureWidth == 0 || textureHeight == 0)
                return;


            // --------------------------------------------------------
            // SCREEN PIXEL COORDINATES
            // --------------------------------------------------------

            // SV_POSITION contient les coordonnées écran du fragment.
            // Elles correspondent aux coordonnées utilisées par
            // Sprite3D pour consulter sa texture de profondeur.

            int2 pixelCoord = int2(IN.positionCS.xy);

            pixelCoord.x = clamp(
                pixelCoord.x,
                0,
                (int)textureWidth - 1
            );

            pixelCoord.y = clamp(
                pixelCoord.y,
                0,
                (int)textureHeight - 1
            );


            // --------------------------------------------------------
            // SPRITE DEPTH
            // --------------------------------------------------------

            float spriteEyeDepth =
                _Sprite3DSceneDepthTexture.Load(
                    int3(pixelCoord, 0)
                );


            // --------------------------------------------------------
            // EMPTY PIXEL CHECK
            // --------------------------------------------------------

            // Une profondeur supérieure au plan éloigné signifie
            // qu'aucun Sprite 3D ne couvre ce pixel.

            if (spriteEyeDepth > _ProjectionParams.z)
                return;


            // --------------------------------------------------------
            // PARTICLE DEPTH
            // --------------------------------------------------------

            float particleEyeDepth =
                -TransformWorldToView(IN.positionWS).z;


            // --------------------------------------------------------
            // OCCLUSION TEST
            // --------------------------------------------------------

            // Valeur positive :
            // Le Sprite 3D est devant la particule.
            //
            // Valeur négative :
            // La particule est devant le Sprite 3D.

            float depthDifference =
                particleEyeDepth - spriteEyeDepth;


            // --------------------------------------------------------
            // DISCARD OCCLUDED PARTICLES
            // --------------------------------------------------------

            // Si le sprite est suffisamment devant la particule,
            // on supprime entièrement ce fragment.

            clip(
                _SpriteDepthSortEpsilon - depthDifference
            );
        }


        // ============================================================
        // SHARED PARTICLE FRAGMENT
        // ============================================================

        half4 FragLit(Varyings IN)
        {
            UNITY_SETUP_INSTANCE_ID(IN);


            // ========================================================
            // PARTICLE COLOR
            // ========================================================

            half4 particleColor =
                IN.color;


            // ========================================================
            // MAIN TEXTURE
            // ========================================================

            half4 baseTex =
                SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    IN.uv
                );

            half4 baseColor =
                baseTex * particleColor;


            // ========================================================
            // SOFT PARTICLES
            // ========================================================

            half softFade =
                CalculateSoftParticleFade(IN);

            half finalAlpha =
                saturate(baseColor.a * softFade);


            // ========================================================
            // SPRITE 3D OCCLUSION
            // ========================================================

            // Important :
            // Ce test est indépendant des Soft Particles.
            //
            // Il fonctionne également avec les particules additives
            // et premultiplied.

            ApplySprite3DOcclusion(IN);


            // ========================================================
            // NORMAL
            // ========================================================

            half3 normalWS =
                SafeNormalize(IN.normalWS);

            half3 viewDir =
                GetWorldSpaceNormalizeViewDir(IN.positionWS);


            #if defined(_PARTICLE_VIEW_NORMALS)

                normalWS = viewDir;

            #endif


            // ========================================================
            // INPUT DATA
            // ========================================================

            InputData inputData =
                (InputData)0;

            inputData.positionWS =
                IN.positionWS;

            inputData.normalWS =
                normalWS;

            inputData.viewDirectionWS =
                viewDir;

            inputData.shadowCoord =
                IN.shadowCoord;

            inputData.fogCoord =
                IN.fogFactor;

            inputData.vertexLighting =
                VertexLighting(
                    IN.positionWS,
                    normalWS
                );

            inputData.bakedGI =
                SampleSH(normalWS);

            inputData.normalizedScreenSpaceUV =
                GetNormalizedScreenSpaceUV(IN.positionCS);

            inputData.shadowMask =
                half4(1,1,1,1);


            // ========================================================
            // SURFACE DATA
            // ========================================================

            SurfaceData surfaceData =
                (SurfaceData)0;

            surfaceData.albedo =
                baseColor.rgb;

            surfaceData.metallic =
                _Metallic;

            surfaceData.specular =
                half3(0.5,0.5,0.5);

            surfaceData.smoothness =
                _Smoothness;

            surfaceData.normalTS =
                half3(0,0,1);

            surfaceData.occlusion =
                1.0;

            surfaceData.alpha =
                finalAlpha;


            // ========================================================
            // SHADOW RECEIVING
            // ========================================================

            #if !defined(_RECEIVE_SHADOWS)

                inputData.shadowCoord = 0;

            #endif


            // ========================================================
            // EMISSION
            // ========================================================

            half3 emission = 0;


            #if defined(_EMISSION)

                half3 emissionTex =
                    SAMPLE_TEXTURE2D(
                        _EmissionMap,
                        sampler_EmissionMap,
                        IN.emissionUV
                    ).rgb;


                half emissionMask =
                    SAMPLE_TEXTURE2D(
                        _EmissionMask,
                        sampler_EmissionMask,
                        IN.emissionMaskUV
                    ).r;


                half3 emissionColor =
                    _EmissionColor.rgb *
                    emissionTex *
                    emissionMask *
                    _EmissionStrength;


                half3 particleEmissionColor =
                    lerp(
                        half3(1,1,1),
                        particleColor.rgb,
                        saturate(_EmissionUseParticleColor)
                    );


                emission =
                    emissionColor * particleEmissionColor;

            #endif


            surfaceData.emission =
                emission;


            // ========================================================
            // PBR LIGHTING
            // ========================================================

            half4 finalColor =
                UniversalFragmentPBR(
                    inputData,
                    surfaceData
                );


            // ========================================================
            // FOG
            // ========================================================

            finalColor.rgb =
                MixFog(
                    finalColor.rgb,
                    IN.fogFactor
                );


            // ========================================================
            // FINAL ALPHA
            // ========================================================

            finalColor.a =
                finalAlpha;


            return finalColor;
        }


        // ============================================================
        // ALPHA BLEND FRAGMENT
        // ============================================================

        half4 FragAlpha(Varyings IN) : SV_Target
        {
            if (_BlendMode >= 0.5)
                return half4(0,0,0,0);

            return FragLit(IN);
        }


        // ============================================================
        // ADDITIVE BLEND FRAGMENT
        // ============================================================

        half4 FragAdditive(Varyings IN) : SV_Target
        {
            if (_BlendMode < 0.5 || _BlendMode >= 1.5)
                return half4(0,0,0,0);

            return FragLit(IN);
        }


        // ============================================================
        // PREMULTIPLY BLEND FRAGMENT
        // ============================================================

        half4 FragPremultiply(Varyings IN) : SV_Target
        {
            if (_BlendMode < 1.5)
                return half4(0,0,0,0);

            half4 color =
                FragLit(IN);

            color.rgb *= color.a;

            return color;
        }

        ENDHLSL


        // ============================================================
        // PASS 1 : ALPHA
        // ============================================================

        Pass
        {
            Name "ForwardAlpha"

            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite Off
            ZTest LEqual
            Cull Off


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment FragAlpha

            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS
            #pragma shader_feature_local_fragment _SOFT_PARTICLES
            #pragma shader_feature_local_fragment _PARTICLE_VIEW_NORMALS

            // URP 17.7 - Mutually exclusive main light variants.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            // Additional lights.
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            // Forward+ / Deferred+.
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            // Soft shadows.
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            ENDHLSL
        }


        // ============================================================
        // PASS 2 : ADDITIVE
        // ============================================================

        Pass
        {
            Name "ForwardAdditive"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Blend SrcAlpha One

            ZWrite Off
            ZTest LEqual
            Cull Off


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment FragAdditive

            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS
            #pragma shader_feature_local_fragment _SOFT_PARTICLES
            #pragma shader_feature_local_fragment _PARTICLE_VIEW_NORMALS

            // URP 17.7 - Mutually exclusive main light variants.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            // Additional lights.
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            // Forward+ / Deferred+.
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            // Soft shadows.
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            ENDHLSL
        }


        // ============================================================
        // PASS 3 : PREMULTIPLY
        // ============================================================

        Pass
        {
            Name "ForwardPremultiply"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Blend One OneMinusSrcAlpha

            ZWrite Off
            ZTest LEqual
            Cull Off


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment FragPremultiply

            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS
            #pragma shader_feature_local_fragment _SOFT_PARTICLES
            #pragma shader_feature_local_fragment _PARTICLE_VIEW_NORMALS

            // URP 17.7 - Mutually exclusive main light variants.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            // Additional lights.
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            // Forward+ / Deferred+.
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            // Soft shadows.
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            ENDHLSL
        }
    }

    FallBack Off
}
