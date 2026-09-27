Shader "Custom/Sprite3D/Lit"
{
    Properties
    {
        [MainTexture]
        _MainTex("Sprite Texture", 2D) = "white" {}

        [MainColor]
        _Color("Color", Color) = (1,1,1,1)


        // ============================================================
        // SHADOWS
        // ============================================================

        [Toggle]
        _ReceiveShadows("Receive Shadows", Float) = 1

        [Toggle]
        _CastShadows("Cast Shadows", Float) = 1

        _ShadowAlphaClip("Shadow Alpha Clip", Range(0,1)) = 0.01


        // ============================================================
        // AMBIENT
        // ============================================================

        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.1


        // ============================================================
        // EDGE LIGHTING
        // ============================================================

        _EdgeWidth(
            "Edge Width",
            Range(0.001, 0.1)
        ) = 0.01

        _EdgeSensitivity(
            "Edge Sensitivity",
            Range(0, 10)
        ) = 2

        _EdgeLightBoost(
            "Edge Light Boost",
            Range(0, 10)
        ) = 2
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

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing


            // --------------------------------------------------------
            // Main light
            // --------------------------------------------------------

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN


            // --------------------------------------------------------
            // Additional lights
            // --------------------------------------------------------

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #pragma multi_compile _ _FORWARD_PLUS

            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile_fragment _ _SHADOWS_SOFT


            // --------------------------------------------------------
            // URP
            // --------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;
                float2 uv         : TEXCOORD1;

                half4 color       : COLOR;

                float4 shadowCoord : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
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

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);


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


                output.shadowCoord =
                    GetShadowCoord(positionInputs);


                return output;
            }


            // ========================================================
            // EDGE DETECTION
            // ========================================================

            half GetEdgeMask(float2 uv)
            {
                // ----------------------------------------------------
                // Texture texel size
                // ----------------------------------------------------

                float2 texelSize =
                    _EdgeWidth;


                // ----------------------------------------------------
                // Sample alpha around current pixel
                // ----------------------------------------------------

                half alphaCenter =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv
                    ).a;


                half alphaLeft =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(-texelSize.x, 0)
                    ).a;


                half alphaRight =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(texelSize.x, 0)
                    ).a;


                half alphaUp =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(0, texelSize.y)
                    ).a;


                half alphaDown =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(0, -texelSize.y)
                    ).a;


                // ----------------------------------------------------
                // Alpha gradient
                // ----------------------------------------------------

                float2 gradient;

                gradient.x =
                    alphaRight - alphaLeft;

                gradient.y =
                    alphaUp - alphaDown;


                float edge =
                    length(gradient);


                // ----------------------------------------------------
                // Sensitivity
                // ----------------------------------------------------

                edge *= _EdgeSensitivity;


                edge =
                    saturate(edge);


                return edge;
            }


            // ============================================================
            // FRAGMENT
            // ============================================================

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


                // ====================================================
                // TEXTURE
                // ====================================================

                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );


                half4 color =
                    tex * input.color;


                clip(color.a - 0.001);


                // ====================================================
                // SPRITE NORMAL
                // ====================================================

                float3 normalWS =
                    TransformObjectToWorldNormal(
                        float3(0, 0, -1)
                    );


                normalWS =
                    normalize(normalWS);


                // ====================================================
                // INPUT DATA
                // ====================================================

                InputData inputData = (InputData)0;


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

                half3 lighting = 0;


                // ====================================================
                // MAIN LIGHT
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


                half mainShadow =
                    lerp(
                        1.0h,
                        mainLight.shadowAttenuation,
                        _ReceiveShadows
                    );


                lighting +=
                    mainLight.color
                    * mainNdotL
                    * mainLight.distanceAttenuation
                    * mainShadow;


                // ====================================================
                // ADDITIONAL LIGHTS
                // ====================================================

                #if defined(_ADDITIONAL_LIGHTS)

                    // ------------------------------------------------
                    // Forward+
                    // ------------------------------------------------

                    #if USE_FORWARD_PLUS

                        UNITY_LOOP

                        for (
                            uint lightIndex = 0;
                            lightIndex <
                                min(
                                    URP_FP_DIRECTIONAL_LIGHTS_COUNT,
                                    MAX_VISIBLE_LIGHTS
                                );
                            lightIndex++
                        )
                        {
                            Light light =
                                GetAdditionalLight(
                                    lightIndex,
                                    inputData.positionWS,
                                    half4(1,1,1,1)
                                );


                            float NdotL =
                                abs(
                                    dot(
                                        normalWS,
                                        light.direction
                                    )
                                );


                            NdotL =
                                saturate(
                                    NdotL
                                );


                            lighting +=
                                light.color
                                * NdotL
                                * light.distanceAttenuation
                                * light.shadowAttenuation;
                        }

                    #endif


                    // ------------------------------------------------
                    // Point / Spot
                    // ------------------------------------------------

                    uint pixelLightCount =
                        GetAdditionalLightsCount();


                    LIGHT_LOOP_BEGIN(pixelLightCount)

                        Light light =
                            GetAdditionalLight(
                                lightIndex,
                                inputData.positionWS,
                                half4(1,1,1,1)
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
                            * additionalNdotL
                            * light.distanceAttenuation
                            * light.shadowAttenuation;

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
                    * _AmbientStrength;


                // ====================================================
                // EDGE DETECTION
                // ====================================================

                half edgeMask =
                    GetEdgeMask(
                        input.uv
                    );


                // ====================================================
                // EDGE LIGHT BOOST
                // ====================================================

                // Important:
                //
                // We multiply the existing lighting instead of
                // adding white.
                //
                // This means:
                //
                // red Point Light -> red edge
                // blue Spot Light -> blue edge
                // etc.

                half edgeBoost =
                    1.0h
                    + edgeMask
                    * _EdgeLightBoost;


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

            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
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


            Varyings ShadowVert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);


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


            half4 ShadowFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


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

            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
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


            Varyings DepthVert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);


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


            half4 DepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


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