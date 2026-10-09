Shader "Custom/SOR/Sprite3D"
{
    Properties
    {
        // ============================================================
        // SPRITE
        // ============================================================

        [PerRendererData]
        [MainTexture]
        _MainTex("Sprite Texture", 2D) = "white" {}

        [MainColor]
        _Color("Color", Color) = (1,1,1,1)


        // ============================================================
        // LIGHTING
        // ============================================================

        [Toggle]
        _ReceiveShadows("Receive Shadows", Float) = 1

        [Toggle]
        _CastShadows("Cast Shadows", Float) = 1

        _ShadowAlphaClip(
            "Shadow Alpha Clip",
            Range(0,1)
        ) = 0.01

        _AmbientStrength(
            "Ambient Strength",
            Range(0,1)
        ) = 0.1


        // ============================================================
        // SPRITE EDGE LIGHT
        // ============================================================

        _EdgeWidth(
            "Edge Width",
            Range(0.0005,0.05)
        ) = 0.005

        _EdgeSensitivity(
            "Edge Sensitivity",
            Range(0,10)
        ) = 2

        _EdgeLightBoost(
            "Edge Light Boost",
            Range(0,10)
        ) = 3


        // ============================================================
        // 3D DEPTH SORTING
        // ============================================================

        [Header(3D Depth Sorting)]

        _SpriteDepthSortEpsilon(
            "Depth Sort Epsilon",
            Range(0.0001,0.1)
        ) = 0.005


        // ============================================================
        // DEPTH OCCLUSION ALPHA
        // ============================================================
        //
        // IMPORTANT :
        //
        // Ce seuil ne change PAS la transparence visible.
        //
        // Il indique seulement à partir de quel alpha
        // le pixel est suffisamment opaque pour cacher
        // les autres Sprite3D derrière lui.
        //
        // Exemple :
        //
        // alpha 1.0  -> bloque derrière
        // alpha 0.8  -> selon threshold
        // alpha 0.3  -> ne bloque pas derrière
        //
        // ============================================================

        _DepthOcclusionAlphaCutoff(
            "Depth Occlusion Alpha Cutoff",
            Range(0,1)
        ) = 0.9


        // ============================================================
        // PER INSTANCE OUTLINE
        // ============================================================

        [PerRendererData]
        _SpriteOutlineEnabled(
            "Sprite Outline Enabled",
            Float
        ) = 1
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"

            "Queue" = "Transparent"

            "RenderType" = "Transparent"
        }


        // ============================================================
        // FORWARD LIT
        // ============================================================
        //
        // Deferred / Deferred+ renderer.
        //
        // Le Sprite3D reste transparent :
        // il est donc rendu en UniversalForwardOnly.
        // ============================================================

        Pass
        {
            Name "ForwardLit"


            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }


            Blend SrcAlpha OneMinusSrcAlpha

            Cull Off

            ZWrite Off

            ZTest LEqual


            // ========================================================
            // STENCIL
            // ========================================================

            Stencil
            {
                Ref 8

                ReadMask 8

                WriteMask 8

                Comp Always

                Pass Replace

                Fail Keep

                ZFail Keep
            }


            HLSLPROGRAM


            #pragma target 4.5

            #pragma vertex Vert

            #pragma fragment Frag


            // ========================================================
            // LIGHTING
            // ========================================================

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #pragma multi_compile_fragment _ _SHADOWS_SOFT


            // ========================================================
            // INCLUDES
            // ========================================================

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Clustering.hlsl"


            // ========================================================
            // STRUCTURES
            // ========================================================

            struct Attributes
            {
                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;

                half4 color : COLOR;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float2 uv : TEXCOORD1;

                half4 color : COLOR;

                float4 shadowCoord : TEXCOORD2;

                float eyeDepth : TEXCOORD3;
            };


            // ========================================================
            // SPRITE
            // ========================================================

            Texture2D<float4> _MainTex;

            SamplerState sampler_MainTex;


            // ========================================================
            // SPRITE 3D SCENE DEPTH
            // ========================================================
            //
            // R32_SFloat
            //
            // Profondeur linéaire directement en unités Unity.
            // ========================================================

            Texture2D<float> _Sprite3DSceneDepthTexture;


            // ========================================================
            // MATERIAL
            // ========================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                half4 _Color;

                half _ReceiveShadows;

                half _CastShadows;

                half _ShadowAlphaClip;

                half _AmbientStrength;

                half _EdgeWidth;

                half _EdgeSensitivity;

                half _EdgeLightBoost;

                float _SpriteDepthSortEpsilon;

                float _DepthOcclusionAlphaCutoff;

            CBUFFER_END


            // ========================================================
            // UV
            // ========================================================

            float2 TransformMainUV(
                float2 uv
            )
            {
                return
                    uv *
                    _MainTex_ST.xy
                    +
                    _MainTex_ST.zw;
            }


            // ========================================================
            // VERTEX
            // ========================================================

            Varyings Vert(
                Attributes input
            )
            {
                Varyings output;


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                float3 positionVS =
                    TransformWorldToView(
                        positionInputs.positionWS
                    );


                output.positionCS =
                    positionInputs.positionCS;


                output.positionWS =
                    positionInputs.positionWS;


                output.uv =
                    TransformMainUV(
                        input.uv
                    );


                output.color =
                    input.color *
                    _Color;


                output.shadowCoord =
                    GetShadowCoord(
                        positionInputs
                    );


                output.eyeDepth =
                    max(
                        -positionVS.z,

                        0.0
                    );


                return output;
            }


            // ========================================================
            // 3D SPRITE DEPTH SORT
            // ========================================================

            void ApplySprite3DDepthSorting(
                Varyings input
            )
            {
                uint textureWidth;

                uint textureHeight;


                _Sprite3DSceneDepthTexture.GetDimensions(
                    textureWidth,

                    textureHeight
                );


                if(
                    textureWidth == 0
                    ||
                    textureHeight == 0
                )
                {
                    return;
                }


                // ====================================================
                // EXACT SCREEN PIXEL
                // ====================================================

                int2 pixelCoord =
                    int2(
                        input.positionCS.xy
                    );


                pixelCoord.x =
                    clamp(
                        pixelCoord.x,

                        0,

                        (int)textureWidth - 1
                    );


                pixelCoord.y =
                    clamp(
                        pixelCoord.y,

                        0,

                        (int)textureHeight - 1
                    );


                // ====================================================
                // CLOSEST OPAQUE-ENOUGH SPRITE
                // ====================================================

                float nearestEyeDepth =
                    _Sprite3DSceneDepthTexture.Load(
                        int3(
                            pixelCoord,

                            0
                        )
                    );


                // ====================================================
                // EMPTY
                // ====================================================

                if(
                    nearestEyeDepth >
                    _ProjectionParams.z
                )
                {
                    return;
                }


                // ====================================================
                // CURRENT SPRITE BEHIND AN OPAQUE PIXEL
                // ====================================================

                float depthDifference =
                    input.eyeDepth -
                    nearestEyeDepth;


                clip(
                    _SpriteDepthSortEpsilon -
                    depthDifference
                );
            }


            // ========================================================
            // EDGE DETECTION
            // ========================================================

            half GetEdgeMask(
                float2 uv
            )
            {
                float2 offset =
                    float2(
                        _EdgeWidth,

                        _EdgeWidth
                    );


                float alphaLeft =
                    _MainTex.Sample(
                        sampler_MainTex,

                        uv +
                        float2(
                            -offset.x,

                            0
                        )
                    ).a;


                float alphaRight =
                    _MainTex.Sample(
                        sampler_MainTex,

                        uv +
                        float2(
                            offset.x,

                            0
                        )
                    ).a;


                float alphaUp =
                    _MainTex.Sample(
                        sampler_MainTex,

                        uv +
                        float2(
                            0,

                            offset.y
                        )
                    ).a;


                float alphaDown =
                    _MainTex.Sample(
                        sampler_MainTex,

                        uv +
                        float2(
                            0,

                            -offset.y
                        )
                    ).a;


                float2 gradient;


                gradient.x =
                    alphaRight -
                    alphaLeft;


                gradient.y =
                    alphaUp -
                    alphaDown;


                return saturate(
                    length(
                        gradient
                    )
                    *
                    _EdgeSensitivity
                );
            }


            // ========================================================
            // LIGHT
            // ========================================================

            half3 EvaluateLight(
                Light light,

                float3 normalWS
            )
            {
                float NdotL =
                    saturate(
                        abs(
                            dot(
                                normalWS,

                                light.direction
                            )
                        )
                    );


                half shadow =
                    lerp(
                        1.0h,

                        light.shadowAttenuation,

                        _ReceiveShadows
                    );


                return
                    light.color
                    *
                    NdotL
                    *
                    light.distanceAttenuation
                    *
                    shadow;
            }


            // ========================================================
            // FRAGMENT
            // ========================================================

            half4 Frag(
                Varyings input
            ) : SV_Target
            {
                // ====================================================
                // TEXTURE
                // ====================================================

                float4 tex =
                    _MainTex.Sample(
                        sampler_MainTex,

                        input.uv
                    );


                half4 color =
                    (half4)tex *
                    input.color;


                // ====================================================
                // FULLY TRANSPARENT PIXELS
                // ====================================================

                clip(
                    color.a -
                    0.001
                );


                // ====================================================
                // 3D DEPTH SORT
                // ====================================================

                ApplySprite3DDepthSorting(
                    input
                );


                // ====================================================
                // NORMAL
                // ====================================================

                float3 normalWS =
                    TransformObjectToWorldNormal(
                        float3(
                            0,

                            0,

                            -1
                        )
                    );


                normalWS =
                    normalize(
                        normalWS
                    );


                // ====================================================
                // INPUT DATA
                // ====================================================

                InputData inputData =
                    (InputData)0;


                inputData.positionWS =
                    input.positionWS;


                inputData.normalWS =
                    normalWS;


                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(
                        input.positionWS
                    );


                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(
                        input.positionCS
                    );


                // ====================================================
                // LIGHTING
                // ====================================================

                half3 lighting =
                    0;


                // ====================================================
                // MAIN LIGHT
                // ====================================================

                Light mainLight =
                    GetMainLight(
                        input.shadowCoord
                    );


                lighting +=
                    EvaluateLight(
                        mainLight,

                        normalWS
                    );


                // ====================================================
                // ADDITIONAL LIGHTS
                // ====================================================

                #if defined(_ADDITIONAL_LIGHTS)


                    // =================================================
                    // CLUSTER / DEFERRED+
                    // =================================================

                    #if USE_CLUSTER_LIGHT_LOOP


                        [loop]

                        for(
                            uint lightIndex = 0;

                            lightIndex <
                            min(
                                uint(
                                    URP_FP_DIRECTIONAL_LIGHTS_COUNT
                                ),

                                uint(
                                    MAX_VISIBLE_LIGHTS
                                )
                            );

                            lightIndex++
                        )
                        {
                            Light light =
                                GetAdditionalLight(
                                    lightIndex,

                                    inputData.positionWS,

                                    half4(
                                        1,

                                        1,

                                        1,

                                        1
                                    )
                                );


                            lighting +=
                                EvaluateLight(
                                    light,

                                    normalWS
                                );
                        }


                        // =============================================
                        // CLUSTERED POINT / SPOT
                        // =============================================

                        ClusterIterator clusterIterator =
                            ClusterInit(
                                inputData.normalizedScreenSpaceUV,

                                inputData.positionWS,

                                0
                            );


                        uint clusterLightIndex;


                        [loop]

                        while(
                            ClusterNext(
                                clusterIterator,

                                clusterLightIndex
                            )
                        )
                        {
                            clusterLightIndex +=
                                URP_FP_DIRECTIONAL_LIGHTS_COUNT;


                            Light light =
                                GetAdditionalLight(
                                    clusterLightIndex,

                                    inputData.positionWS,

                                    half4(
                                        1,

                                        1,

                                        1,

                                        1
                                    )
                                );


                            lighting +=
                                EvaluateLight(
                                    light,

                                    normalWS
                                );
                        }


                    // =================================================
                    // CLASSIC
                    // =================================================

                    #else


                        uint pixelLightCount =
                            GetAdditionalLightsCount();


                        [loop]

                        for(
                            uint lightIndex = 0;

                            lightIndex <
                            pixelLightCount;

                            lightIndex++
                        )
                        {
                            Light light =
                                GetAdditionalLight(
                                    lightIndex,

                                    inputData.positionWS,

                                    half4(
                                        1,

                                        1,

                                        1,

                                        1
                                    )
                                );


                            lighting +=
                                EvaluateLight(
                                    light,

                                    normalWS
                                );
                        }


                    #endif


                #endif


                // ====================================================
                // AMBIENT
                // ====================================================

                half3 ambient =
                    SampleSH(
                        normalWS
                    );


                lighting +=
                    ambient *
                    _AmbientStrength;


                // ====================================================
                // EDGE LIGHT
                // ====================================================

                half edgeMask =
                    GetEdgeMask(
                        input.uv
                    );


                half edgeBoost =
                    1.0h
                    +
                    edgeMask *
                    _EdgeLightBoost;


                lighting *=
                    edgeBoost;


                // ====================================================
                // FINAL
                // ====================================================

                color.rgb *=
                    lighting;


                return color;
            }


            ENDHLSL
        }


        // ============================================================
        // SPRITE SCENE DEPTH
        // ============================================================
        //
        // C'est ici que la correction principale se trouve.
        //
        // AVANT :
        //
        // clip(alpha - 0.001)
        //
        // Un alpha 0.01 bloquait donc les autres sprites.
        //
        //
        // MAINTENANT :
        //
        // clip(alpha - _DepthOcclusionAlphaCutoff)
        //
        // Seuls les pixels suffisamment opaques participent
        // à l'occlusion 3D.
        //
        // ============================================================

        Pass
        {
            Name "SpriteSceneDepth"


            Tags
            {
                "LightMode" = "SpriteSceneDepth"
            }


            Cull Off

            ZWrite Off

            ZTest Always


            Blend One One

            BlendOp Min

            ColorMask R


            HLSLPROGRAM


            #pragma target 4.5

            #pragma vertex SpriteSceneDepthVert

            #pragma fragment SpriteSceneDepthFrag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            // ========================================================
            // STRUCTURES
            // ========================================================

            struct Attributes
            {
                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;

                half4 color : COLOR;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;

                half4 color : COLOR;

                float eyeDepth : TEXCOORD1;
            };


            // ========================================================
            // TEXTURE
            // ========================================================

            Texture2D<float4> _MainTex;

            SamplerState sampler_MainTex;


            // ========================================================
            // MATERIAL
            // ========================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                half4 _Color;

                half _ReceiveShadows;

                half _CastShadows;

                half _ShadowAlphaClip;

                half _AmbientStrength;

                half _EdgeWidth;

                half _EdgeSensitivity;

                half _EdgeLightBoost;

                float _SpriteDepthSortEpsilon;

                float _DepthOcclusionAlphaCutoff;

            CBUFFER_END


            // ========================================================
            // UV
            // ========================================================

            float2 TransformMainUV(
                float2 uv
            )
            {
                return
                    uv *
                    _MainTex_ST.xy
                    +
                    _MainTex_ST.zw;
            }


            // ========================================================
            // VERTEX
            // ========================================================

            Varyings SpriteSceneDepthVert(
                Attributes input
            )
            {
                Varyings output;


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                float3 positionVS =
                    TransformWorldToView(
                        positionInputs.positionWS
                    );


                output.positionCS =
                    positionInputs.positionCS;


                output.uv =
                    TransformMainUV(
                        input.uv
                    );


                output.color =
                    input.color *
                    _Color;


                output.eyeDepth =
                    max(
                        -positionVS.z,

                        0.0
                    );


                return output;
            }


            // ========================================================
            // FRAGMENT
            // ========================================================

            float4 SpriteSceneDepthFrag(
                Varyings input
            ) : SV_Target
            {
                float4 tex =
                    _MainTex.Sample(
                        sampler_MainTex,

                        input.uv
                    );


                float alpha =
                    tex.a *
                    input.color.a;


                // ====================================================
                // IMPORTANT
                // ====================================================
                //
                // Le pixel n'occulte les autres Sprite3D
                // QUE s'il est suffisamment opaque.
                // ====================================================

                clip(
                    alpha -
                    _DepthOcclusionAlphaCutoff
                );


                // ====================================================
                // LINEAR EYE DEPTH
                // ====================================================

                return float4(
                    input.eyeDepth,

                    0,

                    0,

                    1
                );
            }


            ENDHLSL
        }


        // ============================================================
        // SPRITE OUTLINE DEPTH
        // ============================================================
        //
        // Ici on conserve toute la silhouette visible.
        //
        // Un pixel semi-transparent peut donc toujours participer
        // à la silhouette de l'outline.
        // ============================================================

        Pass
        {
            Name "SpriteOutlineDepth"


            Tags
            {
                "LightMode" = "SpriteOutlineDepth"
            }


            Cull Off

            ZWrite Off

            ZTest Always


            Blend One One

            BlendOp Min

            ColorMask R


            HLSLPROGRAM


            #pragma target 4.5

            #pragma vertex SpriteOutlineDepthVert

            #pragma fragment SpriteOutlineDepthFrag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;

                half4 color : COLOR;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;

                half4 color : COLOR;

                float eyeDepth : TEXCOORD1;
            };


            Texture2D<float4> _MainTex;

            SamplerState sampler_MainTex;


            // ========================================================
            // INSTANCE OUTLINE
            // ========================================================

            float _SpriteOutlineEnabled;


            // ========================================================
            // MATERIAL
            // ========================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                half4 _Color;

                half _ReceiveShadows;

                half _CastShadows;

                half _ShadowAlphaClip;

                half _AmbientStrength;

                half _EdgeWidth;

                half _EdgeSensitivity;

                half _EdgeLightBoost;

                float _SpriteDepthSortEpsilon;

                float _DepthOcclusionAlphaCutoff;

            CBUFFER_END


            float2 TransformMainUV(
                float2 uv
            )
            {
                return
                    uv *
                    _MainTex_ST.xy
                    +
                    _MainTex_ST.zw;
            }


            Varyings SpriteOutlineDepthVert(
                Attributes input
            )
            {
                Varyings output;


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                float3 positionVS =
                    TransformWorldToView(
                        positionInputs.positionWS
                    );


                output.positionCS =
                    positionInputs.positionCS;


                output.uv =
                    TransformMainUV(
                        input.uv
                    );


                output.color =
                    input.color *
                    _Color;


                output.eyeDepth =
                    max(
                        -positionVS.z,

                        0.0
                    );


                return output;
            }


            float4 SpriteOutlineDepthFrag(
                Varyings input
            ) : SV_Target
            {
                // ====================================================
                // INSTANCE SWITCH
                // ====================================================

                clip(
                    _SpriteOutlineEnabled -
                    0.5
                );


                float4 tex =
                    _MainTex.Sample(
                        sampler_MainTex,

                        input.uv
                    );


                float alpha =
                    tex.a *
                    input.color.a;


                // ====================================================
                // OUTLINE SILHOUETTE
                // ====================================================

                clip(
                    alpha -
                    0.001
                );


                float farPlane =
                    max(
                        _ProjectionParams.z,

                        0.001
                    );


                float depth01 =
                    saturate(
                        input.eyeDepth /
                        farPlane
                    );


                return float4(
                    depth01,

                    0,

                    0,

                    1
                );
            }


            ENDHLSL
        }


        // ============================================================
        // DEPTH NORMALS
        // ============================================================
        //
        // On utilise aussi le nouveau alpha cutoff ici.
        //
        // Les zones réellement translucides ne doivent pas devenir
        // artificiellement opaques dans la depth/normals caméra.
        // ============================================================

        Pass
        {
            Name "DepthNormals"


            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }


            Cull Off

            ZWrite On


            HLSLPROGRAM


            #pragma target 4.5

            #pragma vertex DepthNormalsVert

            #pragma fragment DepthNormalsFrag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;
            };


            Texture2D<float4> _MainTex;

            SamplerState sampler_MainTex;


            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                half4 _Color;

                half _ReceiveShadows;

                half _CastShadows;

                half _ShadowAlphaClip;

                half _AmbientStrength;

                half _EdgeWidth;

                half _EdgeSensitivity;

                half _EdgeLightBoost;

                float _SpriteDepthSortEpsilon;

                float _DepthOcclusionAlphaCutoff;

            CBUFFER_END


            float2 TransformMainUV(
                float2 uv
            )
            {
                return
                    uv *
                    _MainTex_ST.xy
                    +
                    _MainTex_ST.zw;
            }


            Varyings DepthNormalsVert(
                Attributes input
            )
            {
                Varyings output;


                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );


                output.uv =
                    TransformMainUV(
                        input.uv
                    );


                return output;
            }


            half4 DepthNormalsFrag(
                Varyings input
            ) : SV_Target
            {
                float alpha =
                    _MainTex.Sample(
                        sampler_MainTex,

                        input.uv
                    ).a;


                clip(
                    alpha -
                    _DepthOcclusionAlphaCutoff
                );


                float3 normalWS =
                    TransformObjectToWorldNormal(
                        float3(
                            0,

                            0,

                            -1
                        )
                    );


                normalWS =
                    normalize(
                        normalWS
                    );


                return half4(
                    normalWS *
                    0.5h +
                    0.5h,

                    0
                );
            }


            ENDHLSL
        }


        // ============================================================
        // SHADOW CASTER
        // ============================================================

        Pass
        {
            Name "ShadowCaster"


            Tags
            {
                "LightMode" = "ShadowCaster"
            }


            Cull Off

            ZWrite On

            ZTest LEqual

            ColorMask 0


            HLSLPROGRAM


            #pragma target 4.5

            #pragma vertex ShadowVert

            #pragma fragment ShadowFrag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;
            };


            Texture2D<float4> _MainTex;

            SamplerState sampler_MainTex;


            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                half4 _Color;

                half _ReceiveShadows;

                half _CastShadows;

                half _ShadowAlphaClip;

                half _AmbientStrength;

                half _EdgeWidth;

                half _EdgeSensitivity;

                half _EdgeLightBoost;

                float _SpriteDepthSortEpsilon;

                float _DepthOcclusionAlphaCutoff;

            CBUFFER_END


            float2 TransformMainUV(
                float2 uv
            )
            {
                return
                    uv *
                    _MainTex_ST.xy
                    +
                    _MainTex_ST.zw;
            }


            Varyings ShadowVert(
                Attributes input
            )
            {
                Varyings output;


                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );


                output.uv =
                    TransformMainUV(
                        input.uv
                    );


                return output;
            }


            half4 ShadowFrag(
                Varyings input
            ) : SV_Target
            {
                if(
                    _CastShadows <
                    0.5h
                )
                {
                    discard;
                }


                float alpha =
                    _MainTex.Sample(
                        sampler_MainTex,

                        input.uv
                    ).a;


                // Ombres toujours indépendantes de l'occlusion 3D.

                clip(
                    alpha -
                    _ShadowAlphaClip
                );


                return 0;
            }


            ENDHLSL
        }


        // ============================================================
        // DEPTH ONLY
        // ============================================================

        Pass
        {
            Name "DepthOnly"


            Tags
            {
                "LightMode" = "DepthOnly"
            }


            Cull Off

            ZWrite On

            ColorMask 0


            HLSLPROGRAM


            #pragma target 4.5

            #pragma vertex DepthVert

            #pragma fragment DepthFrag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;
            };


            Texture2D<float4> _MainTex;

            SamplerState sampler_MainTex;


            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                half4 _Color;

                half _ReceiveShadows;

                half _CastShadows;

                half _ShadowAlphaClip;

                half _AmbientStrength;

                half _EdgeWidth;

                half _EdgeSensitivity;

                half _EdgeLightBoost;

                float _SpriteDepthSortEpsilon;

                float _DepthOcclusionAlphaCutoff;

            CBUFFER_END


            float2 TransformMainUV(
                float2 uv
            )
            {
                return
                    uv *
                    _MainTex_ST.xy
                    +
                    _MainTex_ST.zw;
            }


            Varyings DepthVert(
                Attributes input
            )
            {
                Varyings output;


                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );


                output.uv =
                    TransformMainUV(
                        input.uv
                    );


                return output;
            }


            half4 DepthFrag(
                Varyings input
            ) : SV_Target
            {
                float alpha =
                    _MainTex.Sample(
                        sampler_MainTex,

                        input.uv
                    ).a;


                clip(
                    alpha -
                    _DepthOcclusionAlphaCutoff
                );


                return 0;
            }


            ENDHLSL
        }
    }
    CustomEditor "SynthOfRage.Editor.SORShaderGUI"


    FallBack Off
}