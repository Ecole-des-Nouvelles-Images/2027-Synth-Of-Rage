Shader "Custom/Toon/DeferredToon"
{
    Properties
    {
        // ============================================================
        // BASE
        // ============================================================
        [Header(Base)]
        _BaseMap ("Base Color Texture", 2D) = "white" {}
        _BaseColor ("Base Color Tint", Color) = (1,1,1,1)

        [Header(Normal Map)]
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,2)) = 1

        // ============================================================
        // TOON LIGHTING
        // ============================================================
        [Header(Toon Lighting)]
        _LightBands ("Light Bands", Range(2,8)) = 4
        _LightBias ("Light Bias", Range(-1,1)) = 0
        _ShadowColor ("Shadow Tint", Color) = (0.18,0.18,0.24,1)
        _DirectLightStrength ("Direct Light Strength", Range(0,3)) = 1
        _AmbientStrength ("Ambient / Probe Strength", Range(0,2)) = 0.25
        _ReceiveShadowStrength ("Receive Shadow Strength", Range(0,1)) = 1

        // ============================================================
        // HALFTONE
        // ============================================================
        [Header(Halftone)]
        _HalftoneStrength ("Halftone Strength", Range(0,1)) = 0.8
        _HalftoneColor ("Halftone Color", Color) = (0.03,0.03,0.04,1)
        _HalftoneSize ("Dot Cell Size (pixels)", Range(2,64)) = 10
        _HalftoneLevels ("Halftone Levels", Range(2,8)) = 4
        _HalftoneMinRadius ("Min Dot Radius", Range(0,0.7)) = 0.04
        _HalftoneMaxRadius ("Max Dot Radius", Range(0,0.7)) = 0.46
        _HalftoneAngle ("Halftone Angle", Range(0,180)) = 45

        // ============================================================
        // POSTERIZATION
        // ============================================================
        [Header(Posterization)]
        _PosterizeSteps ("Final Color Steps", Range(2,32)) = 8
        _PosterizeStrength ("Posterize Strength", Range(0,1)) = 1

        // ============================================================
        // SPECULAR
        // ============================================================
        [Header(Toon Specular)]
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularStrength ("Specular Strength", Range(0,2)) = 0.2
        _SpecularPower ("Specular Power", Range(1,256)) = 64
        _SpecularThreshold ("Specular Threshold", Range(0,1)) = 0.6

        // ============================================================
        // RIM
        // ============================================================
        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimStrength ("Rim Strength", Range(0,2)) = 0.1
        _RimPower ("Rim Power", Range(0.1,16)) = 4
        _RimThreshold ("Rim Threshold", Range(0,1)) = 0.5

        // ============================================================
        // ADDITIONAL LIGHTS
        // ============================================================
        [Header(Additional Lights)]
        _AdditionalLightStrength ("Additional Light Strength", Range(0,3)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        TEXTURE2D(_NormalMap);
        SAMPLER(sampler_NormalMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _NormalMap_ST;

            float4 _BaseColor;

            float _NormalStrength;

            float _LightBands;
            float _LightBias;
            float4 _ShadowColor;
            float _DirectLightStrength;
            float _AmbientStrength;
            float _ReceiveShadowStrength;

            float _HalftoneStrength;
            float4 _HalftoneColor;
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

        float Quantize01(float value, float levels)
        {
            levels = max(2.0, round(levels));
            value = saturate(value);

            // levels=4 -> 0, 1/3, 2/3, 1
            return floor(value * (levels - 1.0) + 0.5) / (levels - 1.0);
        }

        float3 PosterizeColor(float3 color, float steps)
        {
            steps = max(2.0, round(steps));
            float d = steps - 1.0;
            return round(saturate(color) * d) / d;
        }

        float2 Rotate2D(float2 p, float angleRad)
        {
            float s = sin(angleRad);
            float c = cos(angleRad);

            return float2(
                c * p.x - s * p.y,
                s * p.x + c * p.y
            );
        }

        // 0 = aucun point
        // 1 = point présent
        float HalftoneMask(float2 pixelPosition, float darkness)
        {
            if (_HalftoneStrength <= 0.0001)
                return 0.0;

            darkness = Quantize01(darkness, _HalftoneLevels);

            if (darkness <= 0.0001)
                return 0.0;

            float2 p = Rotate2D(pixelPosition, radians(_HalftoneAngle));
            p /= max(_HalftoneSize, 1.0);

            float2 cell = frac(p) - 0.5;
            float distToCenter = length(cell);

            float radius = lerp(
                _HalftoneMinRadius,
                _HalftoneMaxRadius,
                darkness
            );

            float aa = max(fwidth(distToCenter), 0.0001);

            float dotMask = 1.0 - smoothstep(
                radius - aa,
                radius + aa,
                distToCenter
            );

            return dotMask * _HalftoneStrength;
        }

        float3 BuildNormalWS(
            float3 baseNormalWS,
            float3 tangentWS,
            float tangentSign,
            float2 uvNormal)
        {
            float3 n = normalize(baseNormalWS);
            float3 t = normalize(tangentWS);

            float3 b = tangentSign * cross(n, t);

            float3 normalTS = UnpackNormalScale(
                SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvNormal),
                _NormalStrength
            );

            float3x3 tangentToWorld = float3x3(t, b, n);

            return NormalizeNormalPerPixel(
                TransformTangentToWorld(normalTS, tangentToWorld)
            );
        }

        float3 EvaluateAdditionalToonLight(float3 normalWS, Light light)
        {
            float ndotl = saturate(dot(normalWS, light.direction));

            float atten =
                light.distanceAttenuation *
                light.shadowAttenuation;

            float raw = saturate(ndotl * atten + _LightBias);
            float toon = Quantize01(raw, _LightBands);

            return
                light.color *
                toon *
                _AdditionalLightStrength;
        }

        ENDHLSL

        // ============================================================
        // CUSTOM TOON LIGHTING
        // Runs as ForwardOnly inside a Deferred Renderer.
        // ============================================================
        Pass
        {
            Name "ToonForwardOnly"

            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex ToonVert
            #pragma fragment ToonFrag

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _LIGHT_COOKIES

            struct ToonAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ToonVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;

                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;

                float2 uvBase     : TEXCOORD3;
                float2 uvNormal   : TEXCOORD4;

                float fogFactor   : TEXCOORD5;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ToonVaryings ToonVert(ToonAttributes IN)
            {
                ToonVaryings OUT = (ToonVaryings)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs =
                    GetVertexPositionInputs(IN.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;

                OUT.normalWS = normalInputs.normalWS;

                float tangentSign =
                    IN.tangentOS.w * GetOddNegativeScale();

                OUT.tangentWS =
                    float4(normalInputs.tangentWS, tangentSign);

                OUT.uvBase =
                    IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;

                OUT.uvNormal =
                    IN.uv * _NormalMap_ST.xy + _NormalMap_ST.zw;

                OUT.fogFactor =
                    ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            half4 ToonFrag(ToonVaryings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // ----------------------------------------------------
                // SURFACE
                // ----------------------------------------------------
                float4 baseSample =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        IN.uvBase
                    );

                float3 albedo =
                    baseSample.rgb * _BaseColor.rgb;

                float3 normalWS = BuildNormalWS(
                    IN.normalWS,
                    IN.tangentWS.xyz,
                    IN.tangentWS.w,
                    IN.uvNormal
                );

                float3 viewDirWS =
                    GetWorldSpaceNormalizeViewDir(IN.positionWS);

                // ----------------------------------------------------
                // MAIN LIGHT + SHADOWS
                // ----------------------------------------------------
                float4 shadowCoord =
                    TransformWorldToShadowCoord(IN.positionWS);

                half4 shadowMask =
                    half4(1, 1, 1, 1);

                Light mainLight =
                    GetMainLight(
                        shadowCoord,
                        IN.positionWS,
                        shadowMask
                    );

                float shadowAtten =
                    lerp(
                        1.0,
                        mainLight.shadowAttenuation,
                        _ReceiveShadowStrength
                    );

                float ndotl =
                    saturate(
                        dot(normalWS, mainLight.direction)
                    );

                float rawLight =
                    saturate(
                        ndotl *
                        mainLight.distanceAttenuation *
                        shadowAtten +
                        _LightBias
                    );

                float toonLight =
                    Quantize01(rawLight, _LightBands);

                float3 toonLightColor =
                    lerp(
                        _ShadowColor.rgb,
                        mainLight.color,
                        toonLight
                    ) * _DirectLightStrength;

                // ----------------------------------------------------
                // AMBIENT / LIGHT PROBES
                // ----------------------------------------------------
                float3 ambient =
                    max(SampleSH(normalWS), 0.0) *
                    _AmbientStrength;

                float3 color =
                    albedo *
                    (toonLightColor + ambient);

                // ----------------------------------------------------
                // HALFTONE
                // Quantized darkness -> several dot sizes.
                // ----------------------------------------------------
                float darkness =
                    1.0 - toonLight;

                float halftone =
                    HalftoneMask(
                        IN.positionCS.xy,
                        darkness
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
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(IN.positionCS);

                float3 additionalLighting = 0.0;

                #if defined(_ADDITIONAL_LIGHTS)

                    // Forward+ extra directional lights
                    #if USE_FORWARD_PLUS
                        UNITY_LOOP
                        for (
                            uint lightIndex = 0;
                            lightIndex < min(
                                URP_FP_DIRECTIONAL_LIGHTS_COUNT,
                                MAX_VISIBLE_LIGHTS
                            );
                            ++lightIndex
                        )
                        {
                            Light light =
                                GetAdditionalLight(
                                    lightIndex,
                                    inputData.positionWS,
                                    shadowMask
                                );

                            additionalLighting +=
                                EvaluateAdditionalToonLight(
                                    normalWS,
                                    light
                                );
                        }
                    #endif

                    uint pixelLightCount =
                        GetAdditionalLightsCount();

                    LIGHT_LOOP_BEGIN(pixelLightCount)

                        Light light =
                            GetAdditionalLight(
                                lightIndex,
                                inputData.positionWS,
                                shadowMask
                            );

                        additionalLighting +=
                            EvaluateAdditionalToonLight(
                                normalWS,
                                light
                            );

                    LIGHT_LOOP_END

                #endif

                color +=
                    albedo * additionalLighting;

                // ----------------------------------------------------
                // TOON SPECULAR
                // ----------------------------------------------------
                float3 halfDir =
                    SafeNormalize(
                        mainLight.direction +
                        viewDirWS
                    );

                float ndoth =
                    saturate(
                        dot(normalWS, halfDir)
                    );

                float spec =
                    pow(
                        ndoth,
                        max(_SpecularPower, 1.0)
                    );

                spec =
                    step(
                        _SpecularThreshold,
                        spec
                    );

                spec *=
                    toonLight *
                    _SpecularStrength;

                color +=
                    _SpecularColor.rgb *
                    mainLight.color *
                    spec;

                // ----------------------------------------------------
                // RIM
                // ----------------------------------------------------
                float rim =
                    1.0 -
                    saturate(
                        dot(normalWS, viewDirWS)
                    );

                rim =
                    pow(
                        rim,
                        max(_RimPower, 0.0001)
                    );

                rim =
                    step(
                        _RimThreshold,
                        rim
                    ) *
                    _RimStrength;

                color +=
                    _RimColor.rgb *
                    rim;

                // ----------------------------------------------------
                // FINAL POSTERIZATION
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
                // FOG
                // ----------------------------------------------------
                color =
                    MixFog(
                        color,
                        IN.fogFactor
                    );

                return half4(
                    color,
                    baseSample.a * _BaseColor.a
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

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 GetCustomShadowPositionHClip(ShadowAttributes IN)
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
                            _LightPosition - positionWS
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

                return ApplyShadowClamping(positionCS);
            }

            ShadowVaryings ShadowVert(ShadowAttributes IN)
            {
                ShadowVaryings OUT = (ShadowVaryings)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS =
                    GetCustomShadowPositionHClip(IN);

                return OUT;
            }

            half4 ShadowFrag(ShadowVaryings IN) : SV_Target
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
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttributes IN)
            {
                DepthVaryings OUT = (DepthVaryings)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );

                return OUT;
            }

            half DepthFrag(DepthVaryings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return IN.positionCS.z;
            }

            ENDHLSL
        }

        // ============================================================
        // DEPTH NORMALS ONLY
        // Required for ForwardOnly materials in Deferred, and useful
        // for SSAO/depth-normal dependent renderer features.
        // Uses the same normal map as the toon pass.
        // ============================================================
        Pass
        {
            Name "DepthNormalsOnly"

            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float4 tangentWS  : TEXCOORD1;
                float2 uvNormal   : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthNormalsVaryings DepthNormalsVert(
                DepthNormalsAttributes IN)
            {
                DepthNormalsVaryings OUT =
                    (DepthNormalsVaryings)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        IN.normalOS,
                        IN.tangentOS
                    );

                OUT.normalWS =
                    normalInputs.normalWS;

                float tangentSign =
                    IN.tangentOS.w *
                    GetOddNegativeScale();

                OUT.tangentWS =
                    float4(
                        normalInputs.tangentWS,
                        tangentSign
                    );

                OUT.uvNormal =
                    IN.uv *
                    _NormalMap_ST.xy +
                    _NormalMap_ST.zw;

                return OUT;
            }

            half4 DepthNormalsFrag(
                DepthNormalsVaryings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS =
                    BuildNormalWS(
                        IN.normalWS,
                        IN.tangentWS.xyz,
                        IN.tangentWS.w,
                        IN.uvNormal
                    );

                #if defined(_GBUFFER_NORMALS_OCT)

                    float2 octNormal =
                        PackNormalOctQuadEncode(
                            normalWS
                        );

                    float2 remapped =
                        saturate(
                            octNormal * 0.5 + 0.5
                        );

                    half3 packed =
                        PackFloat2To888(
                            remapped
                        );

                    return half4(packed, 0);

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
