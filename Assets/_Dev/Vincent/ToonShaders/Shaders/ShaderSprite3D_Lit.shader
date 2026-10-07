Shader "Custom/Sprite3D/Lit"
{
    Properties
    {
        [MainTexture]
        _MainTex("Sprite Texture", 2D) = "white" {}

        [MainColor]
        _Color("Color", Color) = (1,1,1,1)

        [Toggle]
        _ReceiveShadows("Receive Shadows", Float) = 1

        [Toggle]
        _CastShadows("Cast Shadows", Float) = 1

        _ShadowAlphaClip("Shadow Alpha Clip", Range(0,1)) = 0.01

        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.1

        _EdgeWidth("Edge Width", Range(0.0005,0.05)) = 0.005

        _EdgeSensitivity("Edge Sensitivity", Range(0,10)) = 2

        _EdgeLightBoost("Edge Light Boost", Range(0,10)) = 3
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
        // FORWARD ONLY
        //
        // The project uses Deferred.
        // Transparent objects are rendered through the Forward path.
        //
        // UniversalForwardOnly is therefore intentional.
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
            // SCREEN SPACE OUTLINE OCCLUSION
            // ========================================================
            // Marks visible sprite pixels in stencil bit 3 (value 8).
            // The fullscreen outline pass must use Comp NotEqual / Ref 8.
            // Enable Bind Depth-Stencil on the Full Screen Pass feature.
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
            // MAIN LIGHT SHADOWS
            //
            // IMPORTANT:
            // These must be ONE keyword set.
            //
            // Do NOT use three independent multi_compile directives.
            // ========================================================

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            // ========================================================
            // ADDITIONAL LIGHTS
            // ========================================================

            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            // ========================================================
            // FORWARD+
            //
            // Required for compatibility with modern URP additional
            // light handling.
            // ========================================================

            #pragma multi_compile _ _FORWARD_PLUS

            // ========================================================
            // SOFT SHADOWS
            // ========================================================

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            // ========================================================
            // URP
            // ========================================================

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"


            // ========================================================
            // STRUCTURES
            // ========================================================

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float2 uv : TEXCOORD1;

                half4 color : COLOR;

                float4 shadowCoord : TEXCOORD2;
            };


            // ========================================================
            // TEXTURE
            // ========================================================

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


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

            CBUFFER_END


            // ========================================================
            // VERTEX
            // ========================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                output.positionCS =
                    positionInputs.positionCS;


                output.positionWS =
                    positionInputs.positionWS;


                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _MainTex
                    );


                output.color =
                    input.color * _Color;


                // ====================================================
                // SHADOW COORDINATES
                //
                // GetShadowCoord() handles the appropriate URP
                // shadow mode for the active variant.
                // ====================================================

                output.shadowCoord =
                    GetShadowCoord(
                        positionInputs
                    );


                return output;
            }


            // ============================================================
            // EDGE DETECTION
            // ============================================================

            half GetEdgeMask(float2 uv)
            {
                float2 offset =
                    float2(
                        _EdgeWidth,
                        _EdgeWidth
                    );


                half alphaLeft =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(-offset.x, 0)
                    ).a;


                half alphaRight =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(offset.x, 0)
                    ).a;


                half alphaUp =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(0, offset.y)
                    ).a;


                half alphaDown =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(0, -offset.y)
                    ).a;


                float2 gradient;

                gradient.x =
                    alphaRight - alphaLeft;

                gradient.y =
                    alphaUp - alphaDown;


                half edge =
                    length(gradient);


                edge *=
                    _EdgeSensitivity;


                edge =
                    saturate(edge);


                return edge;
            }


            // ============================================================
            // FRAGMENT
            // ============================================================

            half4 Frag(Varyings input) : SV_Target
            {
                // ====================================================
                // SPRITE
                // ====================================================

                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );


                half4 color =
                    tex * input.color;


                // ====================================================
                // TRANSPARENCY
                // ====================================================

                clip(
                    color.a - 0.001
                );


                // ====================================================
                // SPRITE NORMAL
                //
                // Sprite lies on XY.
                // Base normal points toward -Z.
                //
                // abs(dot()) makes the lighting effectively
                // double-sided.
                // ====================================================

                float3 normalWS =
                    TransformObjectToWorldNormal(
                        float3(0, 0, -1)
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
                // MAIN DIRECTIONAL LIGHT
                // ====================================================

                Light mainLight =
                    GetMainLight(
                        input.shadowCoord
                    );


                float mainNdotL =
                    abs(
                        dot(
                            normalWS,
                            mainLight.direction
                        )
                    );


                mainNdotL =
                    saturate(
                        mainNdotL
                    );


                // ====================================================
                // MAIN LIGHT SHADOW
                // ====================================================

                half mainShadow =
                    lerp(
                        1.0h,
                        mainLight.shadowAttenuation,
                        _ReceiveShadows
                    );


                // ====================================================
                // MAIN LIGHT CONTRIBUTION
                // ====================================================

                lighting +=
                    mainLight.color
                    *
                    mainNdotL
                    *
                    mainLight.distanceAttenuation
                    *
                    mainShadow;


                // ====================================================
                // ADDITIONAL LIGHTS
                // ====================================================

                #if defined(_ADDITIONAL_LIGHTS)

                    // ------------------------------------------------
                    // FORWARD+
                    //
                    // In Forward+, additional directional lights
                    // are handled separately from the regular
                    // GetAdditionalLightsCount() loop.
                    // ------------------------------------------------

                    #if USE_FORWARD_PLUS

                        UNITY_LOOP
                        for (
                            uint lightIndex = 0;
                            lightIndex < min(
                                uint(URP_FP_DIRECTIONAL_LIGHTS_COUNT),
                                uint(MAX_VISIBLE_LIGHTS)
                            );
                            lightIndex++
                        )
                        {
                            Light light =
                                GetAdditionalLight(
                                    lightIndex,
                                    inputData.positionWS,
                                    half4(1, 1, 1, 1)
                                );


                            float additionalNdotL =
                                abs(
                                    dot(
                                        normalWS,
                                        light.direction
                                    )
                                );


                            additionalNdotL =
                                saturate(
                                    additionalNdotL
                                );


                            lighting +=
                                light.color
                                *
                                additionalNdotL
                                *
                                light.distanceAttenuation
                                *
                                light.shadowAttenuation;
                        }

                    #endif


                    // ------------------------------------------------
                    // REGULAR ADDITIONAL LIGHTS
                    //
                    // Point lights / spot lights are handled here.
                    // ------------------------------------------------

                    uint pixelLightCount =
                        GetAdditionalLightsCount();


                    LIGHT_LOOP_BEGIN(pixelLightCount)

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


                        float additionalNdotL =
                            abs(
                                dot(
                                    normalWS,
                                    light.direction
                                )
                            );


                        additionalNdotL =
                            saturate(
                                additionalNdotL
                            );


                        lighting +=
                            light.color
                            *
                            additionalNdotL
                            *
                            light.distanceAttenuation
                            *
                            light.shadowAttenuation;

                    LIGHT_LOOP_END

                #endif


                // ====================================================
                // AMBIENT
                // ====================================================

                half3 ambient =
                    SampleSH(
                        normalWS
                    );


                lighting +=
                    ambient
                    *
                    _AmbientStrength;


                // ====================================================
                // EDGE MASK
                // ====================================================

                half edgeMask =
                    GetEdgeMask(
                        input.uv
                    );


                // ====================================================
                // EDGE LIGHT BOOST
                // ====================================================

                half edgeBoost =
                    1.0h
                    +
                    edgeMask
                    *
                    _EdgeLightBoost;


                lighting *=
                    edgeBoost;


                // ====================================================
                // FINAL COLOR
                // ====================================================

                color.rgb *=
                    lighting;


                return color;
            }

            ENDHLSL
        }


        // ============================================================
        // DEPTH NORMALS
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


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


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

            CBUFFER_END


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
                    TRANSFORM_TEX(
                        input.uv,
                        _MainTex
                    );


                return output;
            }


            half4 DepthNormalsFrag(
                Varyings input
            ) : SV_Target
            {
                half alpha =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    ).a;


                clip(
                    alpha - _ShadowAlphaClip
                );


                float3 normalWS =
                    TransformObjectToWorldNormal(
                        float3(0, 0, -1)
                    );


                normalWS =
                    normalize(
                        normalWS
                    );


                return half4(
                    normalWS * 0.5h + 0.5h,
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


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


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

            CBUFFER_END


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
                    TRANSFORM_TEX(
                        input.uv,
                        _MainTex
                    );


                return output;
            }


            half4 ShadowFrag(
                Varyings input
            ) : SV_Target
            {
                if (_CastShadows < 0.5h)
                    discard;


                half alpha =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    ).a;


                clip(
                    alpha - _ShadowAlphaClip
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


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


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

            CBUFFER_END


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
                    TRANSFORM_TEX(
                        input.uv,
                        _MainTex
                    );


                return output;
            }


            half4 DepthFrag(
                Varyings input
            ) : SV_Target
            {
                half alpha =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    ).a;


                clip(
                    alpha - _ShadowAlphaClip
                );


                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}


