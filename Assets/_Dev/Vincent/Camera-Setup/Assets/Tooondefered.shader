Shader "Custom/Toon/DeferredToon"
{
    Properties
    {
        // ============================================================
        // BASE
        // ============================================================

        [Header(Base)]
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)


        // ============================================================
        // TOON LIGHTING
        // ============================================================

        [Header(Toon Lighting)]
        _LightBands ("Light Bands", Range(2, 8)) = 4

        _ShadowColor
        (
            "Shadow Color",
            Color
        ) = (0.15, 0.17, 0.22, 1)

        _LightBias
        (
            "Light Bias",
            Range(-1, 1)
        ) = 0

        _DirectLightStrength
        (
            "Direct Light Strength",
            Range(0, 3)
        ) = 1

        _AmbientStrength
        (
            "Ambient Strength",
            Range(0, 2)
        ) = 0.25


        // ============================================================
        // HALFTONE
        // ============================================================

        [Header(Halftone Shadows)]

        _HalftoneStrength
        (
            "Halftone Strength",
            Range(0, 1)
        ) = 0.8

        _HalftoneColor
        (
            "Halftone Color",
            Color
        ) = (0.02, 0.02, 0.03, 1)

        _HalftoneSize
        (
            "Dot Cell Size Pixels",
            Range(2, 64)
        ) = 12

        _HalftoneLevels
        (
            "Halftone Intensity Levels",
            Range(2, 8)
        ) = 4

        _HalftoneMinRadius
        (
            "Min Dot Radius",
            Range(0, 0.7)
        ) = 0.05

        _HalftoneMaxRadius
        (
            "Max Dot Radius",
            Range(0, 0.7)
        ) = 0.46

        _HalftoneAngle
        (
            "Halftone Angle",
            Range(0, 180)
        ) = 45


        // ============================================================
        // POSTERIZATION
        // ============================================================

        [Header(Posterization)]

        _PosterizeSteps
        (
            "Color Steps",
            Range(2, 32)
        ) = 8

        _PosterizeStrength
        (
            "Posterize Strength",
            Range(0, 1)
        ) = 1


        // ============================================================
        // SPECULAR
        // ============================================================

        [Header(Toon Specular)]

        _SpecularStrength
        (
            "Specular Strength",
            Range(0, 2)
        ) = 0.25

        _SpecularColor
        (
            "Specular Color",
            Color
        ) = (1,1,1,1)

        _SpecularPower
        (
            "Specular Power",
            Range(1, 256)
        ) = 64

        _SpecularThreshold
        (
            "Specular Threshold",
            Range(0,1)
        ) = 0.6


        // ============================================================
        // RIM
        // ============================================================

        [Header(Rim Light)]

        _RimStrength
        (
            "Rim Strength",
            Range(0, 2)
        ) = 0.15

        _RimColor
        (
            "Rim Color",
            Color
        ) = (1,1,1,1)

        _RimPower
        (
            "Rim Power",
            Range(0.1, 16)
        ) = 4

        _RimThreshold
        (
            "Rim Threshold",
            Range(0,1)
        ) = 0.5


        // ============================================================
        // ADDITIONAL LIGHTS
        // ============================================================

        [Header(Additional Lights)]

        _AdditionalLightStrength
        (
            "Additional Light Strength",
            Range(0, 3)
        ) = 1
    }


    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }


        // ============================================================
        // MAIN TOON PASS
        // ============================================================

        Pass
        {
            Name "ToonForward"

            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            Cull Back
            ZWrite On
            ZTest LEqual


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            // Main Light
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            // Additional lights
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            // Forward+ / clustered
            #pragma multi_compile _ _FORWARD_PLUS

            // Cookies
            #pragma multi_compile _ _LIGHT_COOKIES


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float4 _ShadowColor;

                float _LightBands;
                float _LightBias;
                float _DirectLightStrength;
                float _AmbientStrength;

                float4 _HalftoneColor;
                float _HalftoneStrength;
                float _HalftoneSize;
                float _HalftoneLevels;
                float _HalftoneMinRadius;
                float _HalftoneMaxRadius;
                float _HalftoneAngle;

                float _PosterizeSteps;
                float _PosterizeStrength;

                float4 _SpecularColor;
                float _SpecularStrength;
                float _SpecularPower;
                float _SpecularThreshold;

                float4 _RimColor;
                float _RimStrength;
                float _RimPower;
                float _RimThreshold;

                float _AdditionalLightStrength;

            CBUFFER_END


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;

                float fogFactor   : TEXCOORD3;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };


            // ========================================================
            // UTILITIES
            // ========================================================

            float Quantize01(float value, float levels)
            {
                levels = max(2.0, round(levels));

                value = saturate(value);

                float index =
                    min(
                        levels - 1.0,
                        floor(value * levels)
                    );

                return index / (levels - 1.0);
            }


            float3 PosterizeColor(float3 color, float steps)
            {
                steps = max(2.0, round(steps));

                float divisor = steps - 1.0;

                return
                    round(saturate(color) * divisor)
                    / divisor;
            }


            float2 Rotate2D(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);

                return float2(
                    c * p.x - s * p.y,
                    s * p.x + c * p.y
                );
            }


            // shadowAmount :
            //
            // 0 = lumière
            // 1 = ombre maximale
            //
            float Halftone(
                float2 pixelPosition,
                float shadowAmount
            )
            {
                if (_HalftoneStrength <= 0.0001)
                    return 0.0;


                // -----------------------------------------------
                // Quantification de l'intensité du halftone
                // -----------------------------------------------

                float halftoneLevel =
                    Quantize01(
                        shadowAmount,
                        _HalftoneLevels
                    );


                if (halftoneLevel <= 0.0001)
                    return 0.0;


                // -----------------------------------------------
                // Rotation
                // -----------------------------------------------

                float angle =
                    radians(_HalftoneAngle);

                float2 uv =
                    Rotate2D(
                        pixelPosition,
                        angle
                    );


                // Taille exprimée directement en pixels.
                uv /= max(_HalftoneSize, 1.0);


                // Cellule [-0.5 ; 0.5]
                float2 cell =
                    frac(uv) - 0.5;


                float distanceToCenter =
                    length(cell);


                // Plus on est sombre,
                // plus le point grossit.
                float radius =
                    lerp(
                        _HalftoneMinRadius,
                        _HalftoneMaxRadius,
                        halftoneLevel
                    );


                // Anti aliasing analytique.
                float aa =
                    max(
                        fwidth(distanceToCenter),
                        0.0001
                    );


                float dotMask =
                    1.0
                    - smoothstep(
                        radius - aa,
                        radius + aa,
                        distanceToCenter
                    );


                return
                    dotMask
                    * _HalftoneStrength;
            }


            float3 ToonAdditionalLight(
                float3 normalWS,
                Light light
            )
            {
                float NdotL =
                    saturate(
                        dot(
                            normalWS,
                            light.direction
                        )
                    );


                float attenuation =
                    light.distanceAttenuation
                    * light.shadowAttenuation;


                float lightValue =
                    saturate(
                        NdotL
                        * attenuation
                        + _LightBias
                    );


                float toonValue =
                    Quantize01(
                        lightValue,
                        _LightBands
                    );


                return
                    light.color
                    * toonValue
                    * _AdditionalLightStrength;
            }


            // ========================================================
            // VERTEX
            // ========================================================

            Varyings Vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        IN.positionOS.xyz
                    );


                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        IN.normalOS
                    );


                OUT.positionCS =
                    positionInputs.positionCS;

                OUT.positionWS =
                    positionInputs.positionWS;

                OUT.normalWS =
                    NormalizeNormalPerVertex(
                        normalInputs.normalWS
                    );


                OUT.uv =
                    IN.uv
                    * _BaseMap_ST.xy
                    + _BaseMap_ST.zw;


                OUT.fogFactor =
                    ComputeFogFactor(
                        positionInputs.positionCS.z
                    );


                return OUT;
            }


            // ========================================================
            // FRAGMENT
            // ========================================================

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);


                // ----------------------------------------------------
                // Base texture
                // ----------------------------------------------------

                float4 baseTexture =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        IN.uv
                    );


                float3 albedo =
                    baseTexture.rgb
                    * _BaseColor.rgb;


                float3 normalWS =
                    normalize(IN.normalWS);


                float3 viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(
                        IN.positionWS
                    );


                // ----------------------------------------------------
                // Main light + realtime shadow
                // ----------------------------------------------------

                float4 shadowCoord =
                    TransformWorldToShadowCoord(
                        IN.positionWS
                    );


                half4 shadowMask =
                    half4(1,1,1,1);


                Light mainLight =
                    GetMainLight(
                        shadowCoord,
                        IN.positionWS,
                        shadowMask
                    );


                float NdotL =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction
                        )
                    );


                float mainAttenuation =
                    mainLight.distanceAttenuation
                    * mainLight.shadowAttenuation;


                float rawLighting =
                    saturate(
                        NdotL
                        * mainAttenuation
                        + _LightBias
                    );


                // ----------------------------------------------------
                // Toon quantization
                // ----------------------------------------------------

                float toonLighting =
                    Quantize01(
                        rawLighting,
                        _LightBands
                    );


                float shadowAmount =
                    1.0 - toonLighting;


                // ----------------------------------------------------
                // Couleur des différents niveaux
                //
                // toonLighting = 0 :
                //      ShadowColor
                //
                // toonLighting = 1 :
                //      couleur de la lumière
                // ----------------------------------------------------

                float3 directLighting =
                    lerp(
                        _ShadowColor.rgb,
                        mainLight.color,
                        toonLighting
                    );


                directLighting *=
                    _DirectLightStrength;


                // ----------------------------------------------------
                // Ambient / Light probes
                // ----------------------------------------------------

                float3 ambient =
                    max(
                        SampleSH(normalWS),
                        0.0
                    );


                ambient *=
                    _AmbientStrength;


                float3 color =
                    albedo
                    * (
                        directLighting
                        + ambient
                    );


                // ----------------------------------------------------
                // HALFTONE
                // ----------------------------------------------------

                float halftone =
                    Halftone(
                        IN.positionCS.xy,
                        shadowAmount
                    );


                color =
                    lerp(
                        color,
                        color * _HalftoneColor.rgb,
                        halftone
                    );


                // ----------------------------------------------------
                // ADDITIONAL LIGHTS
                // ----------------------------------------------------

                InputData inputData =
                    (InputData)0;


                inputData.positionWS =
                    IN.positionWS;

                inputData.normalWS =
                    normalWS;

                inputData.viewDirectionWS =
                    viewDirectionWS;

                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(
                        IN.positionCS
                    );


                float3 additionalLighting = 0;


                #if defined(_ADDITIONAL_LIGHTS) || USE_FORWARD_PLUS


                    // -----------------------------------------------
                    // Forward+ directional lights
                    // -----------------------------------------------

                    #if USE_FORWARD_PLUS

                    UNITY_LOOP
                    for (
                        uint lightIndex = 0;
                        lightIndex <
                        min(
                            URP_FP_DIRECTIONAL_LIGHTS_COUNT,
                            MAX_VISIBLE_LIGHTS
                        );
                        ++lightIndex
                    )
                    {
                        Light light =
                            GetAdditionalLight(
                                lightIndex,
                                IN.positionWS,
                                shadowMask
                            );


                        additionalLighting +=
                            ToonAdditionalLight(
                                normalWS,
                                light
                            );
                    }

                    #endif


                    // -----------------------------------------------
                    // Point / spot / additional lights
                    // -----------------------------------------------

                    uint pixelLightCount =
                        GetAdditionalLightsCount();


                    LIGHT_LOOP_BEGIN(pixelLightCount)

                        Light light =
                            GetAdditionalLight(
                                lightIndex,
                                IN.positionWS,
                                shadowMask
                            );


                        additionalLighting +=
                            ToonAdditionalLight(
                                normalWS,
                                light
                            );

                    LIGHT_LOOP_END


                #endif


                color +=
                    albedo
                    * additionalLighting;


                // ----------------------------------------------------
                // TOON SPECULAR
                // ----------------------------------------------------

                float3 halfVector =
                    normalize(
                        mainLight.direction
                        + viewDirectionWS
                    );


                float NdotH =
                    saturate(
                        dot(
                            normalWS,
                            halfVector
                        )
                    );


                float specularValue =
                    pow(
                        NdotH,
                        max(
                            _SpecularPower,
                            1.0
                        )
                    );


                // Spec entièrement dur/toon.
                specularValue =
                    step(
                        _SpecularThreshold,
                        specularValue
                    );


                specularValue *=
                    toonLighting
                    * _SpecularStrength;


                color +=
                    _SpecularColor.rgb
                    * mainLight.color
                    * specularValue;


                // ----------------------------------------------------
                // RIM
                // ----------------------------------------------------

                float rim =
                    1.0
                    - saturate(
                        dot(
                            normalWS,
                            viewDirectionWS
                        )
                    );


                rim =
                    pow(
                        rim,
                        _RimPower
                    );


                rim =
                    step(
                        _RimThreshold,
                        rim
                    );


                rim *=
                    _RimStrength;


                color +=
                    _RimColor.rgb
                    * rim;


                // ----------------------------------------------------
                // POSTERIZATION
                // ----------------------------------------------------

                float3 posterized =
                    PosterizeColor(
                        color,
                        _PosterizeSteps
                    );


                color =
                    lerp(
                        color,
                        posterized,
                        _PosterizeStrength
                    );


                // ----------------------------------------------------
                // Fog
                // ----------------------------------------------------

                color =
                    MixFog(
                        color,
                        IN.fogFactor
                    );


                return half4(
                    color,
                    1.0
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

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back


            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"


            float3 _LightDirection;
            float3 _LightPosition;


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            float4 GetShadowPositionHClipCustom(
                Attributes IN
            )
            {
                float3 positionWS =
                    TransformObjectToWorld(
                        IN.positionOS.xyz
                    );


                float3 normalWS =
                    TransformObjectToWorldNormal(
                        IN.normalOS
                    );


                #if _CASTING_PUNCTUAL_LIGHT_SHADOW

                    float3 lightDirectionWS =
                        normalize(
                            _LightPosition
                            - positionWS
                        );

                #else

                    float3 lightDirectionWS =
                        _LightDirection;

                #endif


                float4 positionCS =
                    TransformWorldToHClip(
                        ApplyShadowBias(
                            positionWS,
                            normalWS,
                            lightDirectionWS
                        )
                    );


                positionCS =
                    ApplyShadowClamping(
                        positionCS
                    );


                return positionCS;
            }


            Varyings ShadowVert(
                Attributes IN
            )
            {
                Varyings OUT =
                    (Varyings)0;


                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);


                OUT.positionCS =
                    GetShadowPositionHClipCustom(
                        IN
                    );


                return OUT;
            }


            half4 ShadowFrag(
                Varyings IN
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

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

            ZWrite On
            ColorMask 0
            Cull Back


            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            Varyings DepthVert(
                Attributes IN
            )
            {
                Varyings OUT =
                    (Varyings)0;


                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);


                OUT.positionCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );


                return OUT;
            }


            half DepthFrag(
                Varyings IN
            ) : SV_Target
            {
                return IN.positionCS.z;
            }

            ENDHLSL
        }



        // ============================================================
        // DEPTH NORMALS
        //
        // Important pour Deferred / SSAO.
        // ============================================================

        Pass
        {
            Name "DepthNormalsOnly"

            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }

            ZWrite On
            Cull Back


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };


            Varyings DepthNormalsVert(
                Attributes IN
            )
            {
                Varyings OUT =
                    (Varyings)0;


                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);


                OUT.positionCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );


                OUT.normalWS =
                    NormalizeNormalPerVertex(
                        TransformObjectToWorldNormal(
                            IN.normalOS
                        )
                    );


                return OUT;
            }


            half4 DepthNormalsFrag(
                Varyings IN
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);


                float3 normalWS =
                    NormalizeNormalPerPixel(
                        IN.normalWS
                    );


                #if defined(_GBUFFER_NORMALS_OCT)

                    float2 octNormalWS =
                        PackNormalOctQuadEncode(
                            normalWS
                        );


                    float2 remappedOctNormalWS =
                        saturate(
                            octNormalWS
                            * 0.5
                            + 0.5
                        );


                    half3 packedNormalWS =
                        PackFloat2To888(
                            remappedOctNormalWS
                        );


                    return half4(
                        packedNormalWS,
                        0
                    );

                #else

                    return half4(
                        normalWS,
                        0
                    );

                #endif
            }

            ENDHLSL
        }
    }

    FallBack Off
}