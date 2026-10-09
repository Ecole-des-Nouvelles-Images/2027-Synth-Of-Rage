Shader "Custom/Toon/DeferredToon"
{
    Properties
    {
        // BASE
        [Header(Base)]
        _BaseMap
        (
            "Base Color Texture",
            2D
        ) = "white" {}

        _BaseColor
        (
            "Base Color Tint",
            Color
        ) = (1,1,1,1)


        // NORMAL MAP
        [Header(Normal Map)]

        [Normal]
        _NormalMap
        (
            "Normal Map",
            2D
        ) = "bump" {}

        _NormalStrength
        (
            "Normal Strength",
            Range(0,2)
        ) = 1


        // TOON LIGHTING
        [Header(Toon Lighting)]

        _LightBands
        (
            "Light Bands",
            Range(2,8)
        ) = 4

        _LightBias
        (
            "Light Bias",
            Range(-1,1)
        ) = 0

        _ShadowColor
        (
            "Shadow Tint",
            Color
        ) = (0.18,0.18,0.24,1)

        _DirectLightStrength
        (
            "Direct Light Strength",
            Range(0,3)
        ) = 1

        _AmbientStrength
        (
            "Ambient / Probe Strength",
            Range(0,2)
        ) = 0.25

        _ReceiveShadowStrength
        (
            "Receive Shadow Strength",
            Range(0,1)
        ) = 1


        // TOON STEP PATTERN
        [Header(Toon Step Pattern)]

        [Toggle(_TOON_STEP_PATTERN)]
        _UseStepPattern
        (
            "Enable Step Pattern",
            Float
        ) = 1

        _StepPattern
        (
            "Step Pattern",
            2D
        ) = "gray" {}

        _StepPatternWorldScale
        (
            "Pattern World Scale",
            Range(0.01,20)
        ) = 1

        _StepPatternWorldOffset
        (
            "Pattern World Offset",
            Vector
        ) = (0,0,0,0)

        _StepPatternStrength
        (
            "Light Steps Distortion",
            Range(0,0.5)
        ) = 0.08

        _RimPatternStrength
        (
            "Rim Distortion",
            Range(0,0.5)
        ) = 0.08

        _StepPatternContrast
        (
            "Pattern Contrast",
            Range(0.1,8)
        ) = 1

        _StepPatternProjectionBlend
        (
            "Triplanar Sharpness",
            Range(1,16)
        ) = 4

        [Toggle]
        _StepPatternInvert
        (
            "Invert Pattern",
            Float
        ) = 0


        // HALFTONE
        [Header(Halftone)]

        _HalftoneStrength
        (
            "Halftone Strength",
            Range(0,1)
        ) = 0.8

        _HalftoneColor
        (
            "Halftone Color",
            Color
        ) = (0.03,0.03,0.04,1)

        _HalftoneSize
        (
            "Dot Cell Size (pixels)",
            Range(2,64)
        ) = 10

        _HalftoneLevels
        (
            "Halftone Levels",
            Range(2,8)
        ) = 4

        _HalftoneMinRadius
        (
            "Min Dot Radius",
            Range(0,0.7)
        ) = 0.04

        _HalftoneMaxRadius
        (
            "Max Dot Radius",
            Range(0,0.7)
        ) = 0.46

        _HalftoneAngle
        (
            "Halftone Angle",
            Range(0,180)
        ) = 45


        // HALFTONE ANCHORING
        [Header(Halftone Anchoring)]

        [Toggle(_HALFTONE_WORLD_SPACE)]
        _HalftoneWorldSpace
        (
            "Lock Dots To World",
            Float
        ) = 0

        _HalftoneWorldScale
        (
            "World Dot Density",
            Range(0.05,20)
        ) = 2

        _HalftoneWorldOffset
        (
            "World Dot Offset",
            Vector
        ) = (0,0,0,0)

        _HalftoneWorldProjectionBlend
        (
            "World Projection Sharpness",
            Range(1,16)
        ) = 6


        // POSTERIZATION
        [Header(Posterization)]

        _PosterizeSteps
        (
            "Final Color Steps",
            Range(2,32)
        ) = 8

        _PosterizeStrength
        (
            "Posterize Strength",
            Range(0,1)
        ) = 1


        // SPECULAR
        [Header(Toon Specular)]

        _SpecularColor
        (
            "Specular Color",
            Color
        ) = (1,1,1,1)

        _SpecularStrength
        (
            "Specular Strength",
            Range(0,2)
        ) = 0.2

        _SpecularPower
        (
            "Specular Power",
            Range(1,256)
        ) = 64

        _SpecularThreshold
        (
            "Specular Threshold",
            Range(0,1)
        ) = 0.6


        // RIM
        [Header(Rim)]

        _RimColor
        (
            "Rim Color",
            Color
        ) = (1,1,1,1)

        _RimStrength
        (
            "Rim Strength",
            Range(0,2)
        ) = 0.1

        _RimPower
        (
            "Rim Power",
            Range(0.1,16)
        ) = 4

        _RimThreshold
        (
            "Rim Threshold",
            Range(0,1)
        ) = 0.5


        // ADDITIONAL LIGHTS
        [Header(Additional Lights)]

        _AdditionalLightStrength
        (
            "Additional Light Strength",
            Range(0,3)
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


        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Clustering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"


        Texture2D<float4> _BaseMap;
        SamplerState sampler_BaseMap;

        Texture2D<float4> _NormalMap;
        SamplerState sampler_NormalMap;

        Texture2D<float4> _StepPattern;
        SamplerState sampler_StepPattern;


        CBUFFER_START(UnityPerMaterial)

            float4 _BaseMap_ST;
            float4 _NormalMap_ST;

            float4 _BaseColor;
            float _NormalStrength;


            // TOON
            float _LightBands;
            float _LightBias;
            float4 _ShadowColor;
            float _DirectLightStrength;
            float _AmbientStrength;
            float _ReceiveShadowStrength;


            // STEP PATTERN
            float _UseStepPattern;
            float _StepPatternWorldScale;
            float4 _StepPatternWorldOffset;
            float _StepPatternStrength;
            float _RimPatternStrength;
            float _StepPatternContrast;
            float _StepPatternProjectionBlend;
            float _StepPatternInvert;


            // HALFTONE
            float _HalftoneStrength;
            float4 _HalftoneColor;
            float _HalftoneSize;
            float _HalftoneLevels;
            float _HalftoneMinRadius;
            float _HalftoneMaxRadius;
            float _HalftoneAngle;

            float _HalftoneWorldSpace;
            float _HalftoneWorldScale;
            float4 _HalftoneWorldOffset;
            float _HalftoneWorldProjectionBlend;


            // POSTERIZATION
            float _PosterizeSteps;
            float _PosterizeStrength;


            // SPECULAR
            float4 _SpecularColor;
            float _SpecularStrength;
            float _SpecularPower;
            float _SpecularThreshold;


            // RIM
            float4 _RimColor;
            float _RimStrength;
            float _RimPower;
            float _RimThreshold;


            // ADDITIONAL LIGHTS
            float _AdditionalLightStrength;

        CBUFFER_END


        float Quantize01
        (
            float value,
            float levels
        )
        {
            levels =
                max
                (
                    2.0,
                    round
                    (
                        levels
                    )
                );

            value =
                saturate
                (
                    value
                );

            float denominator =
                levels -
                1.0;

            return
                floor
                (
                    value *
                    denominator
                    +
                    0.5
                )
                /
                denominator;
        }


        float3 PosterizeColor
        (
            float3 color,
            float steps
        )
        {
            steps =
                max
                (
                    2.0,
                    round
                    (
                        steps
                    )
                );

            float denominator =
                steps -
                1.0;

            return
                round
                (
                    saturate
                    (
                        color
                    )
                    *
                    denominator
                )
                /
                denominator;
        }


        float2 Rotate2D
        (
            float2 p,
            float angleRad
        )
        {
            float s =
                sin
                (
                    angleRad
                );

            float c =
                cos
                (
                    angleRad
                );

            return float2
            (
                c * p.x -
                s * p.y,

                s * p.x +
                c * p.y
            );
        }


        // ============================================================
        // HALFTONE CORE
        // ============================================================

        float HalftoneDotMask
        (
            float2 patternPosition,
            float darkness
        )
        {
            darkness =
                Quantize01
                (
                    darkness,
                    _HalftoneLevels
                );

            if
            (
                _HalftoneStrength <= 0.0001
                ||
                darkness <= 0.0001
            )
            {
                return 0.0;
            }

            float2 cell =
                frac
                (
                    patternPosition
                )
                -
                0.5;

            float distToCenter =
                length
                (
                    cell
                );

            float radius =
                lerp
                (
                    _HalftoneMinRadius,
                    _HalftoneMaxRadius,
                    darkness
                );

            float aa =
                max
                (
                    fwidth
                    (
                        distToCenter
                    ),
                    0.0001
                );

            return
                1.0 -
                smoothstep
                (
                    radius -
                    aa,

                    radius +
                    aa,

                    distToCenter
                );
        }


        // ============================================================
        // SCREEN-SPACE HALFTONE
        // ============================================================
        //
        // Comportement original.
        //
        // Les dots sont verrouillés à l'écran.
        // La caméra se déplace "sous" le motif.
        // ============================================================

        float HalftoneMaskScreen
        (
            float2 pixelPosition,
            float darkness
        )
        {
            float2 p =
                Rotate2D
                (
                    pixelPosition,
                    radians
                    (
                        _HalftoneAngle
                    )
                );

            p /=
                max
                (
                    _HalftoneSize,
                    1.0
                );

            return
                HalftoneDotMask
                (
                    p,
                    darkness
                )
                *
                _HalftoneStrength;
        }


        // ============================================================
        // WORLD-SPACE HALFTONE
        // ============================================================
        //
        // Utilise positionWS + normale GEOMETRIQUE.
        //
        // La normal map artistique n'intervient donc pas dans
        // l'orientation du motif.
        //
        // Le motif devient attaché au décor.
        // ============================================================

        float HalftoneMaskWorld
        (
            float3 positionWS,
            float3 geometricNormalWS,
            float darkness
        )
        {
            if
            (
                _HalftoneStrength <= 0.0001
            )
            {
                return 0.0;
            }

            float3 p =
                (
                    positionWS
                    +
                    _HalftoneWorldOffset.xyz
                )
                *
                max
                (
                    _HalftoneWorldScale,
                    0.0001
                );

            float angle =
                radians
                (
                    _HalftoneAngle
                );


            // --------------------------------------------------------
            // GEOMETRIC TRIPLANAR WEIGHTS
            // --------------------------------------------------------

            float3 weights =
                abs
                (
                    normalize
                    (
                        geometricNormalWS
                    )
                );

            weights =
                pow
                (
                    max
                    (
                        weights,
                        float3
                        (
                            0.0001,
                            0.0001,
                            0.0001
                        )
                    ),

                    max
                    (
                        _HalftoneWorldProjectionBlend,
                        1.0
                    )
                );

            weights /=
                max
                (
                    weights.x +
                    weights.y +
                    weights.z,

                    0.0001
                );


            // --------------------------------------------------------
            // THREE WORLD PROJECTIONS
            // --------------------------------------------------------

            float maskX =
                HalftoneDotMask
                (
                    Rotate2D
                    (
                        p.zy,
                        angle
                    ),
                    darkness
                );

            float maskY =
                HalftoneDotMask
                (
                    Rotate2D
                    (
                        p.xz,
                        angle
                    ),
                    darkness
                );

            float maskZ =
                HalftoneDotMask
                (
                    Rotate2D
                    (
                        p.xy,
                        angle
                    ),
                    darkness
                );


            return
                (
                    maskX *
                    weights.x
                    +
                    maskY *
                    weights.y
                    +
                    maskZ *
                    weights.z
                )
                *
                _HalftoneStrength;
        }


        // ============================================================
        // HALFTONE MODE
        // ============================================================

        float HalftoneMask
        (
            float2 pixelPosition,
            float3 positionWS,
            float3 geometricNormalWS,
            float darkness
        )
        {
            #if defined(_HALFTONE_WORLD_SPACE)

                return
                    HalftoneMaskWorld
                    (
                        positionWS,
                        geometricNormalWS,
                        darkness
                    );

            #else

                return
                    HalftoneMaskScreen
                    (
                        pixelPosition,
                        darkness
                    );

            #endif
        }


        // ============================================================
        // STEP PATTERN
        // ============================================================

        float GetStepPatternSigned
        (
            float3 positionWS,
            float3 geometricNormalWS
        )
        {
            #if defined(_TOON_STEP_PATTERN)

                float3 patternPosition =
                    (
                        positionWS
                        +
                        _StepPatternWorldOffset.xyz
                    )
                    *
                    _StepPatternWorldScale;


                float3 weights =
                    abs
                    (
                        normalize
                        (
                            geometricNormalWS
                        )
                    );


                weights =
                    pow
                    (
                        max
                        (
                            weights,
                            float3
                            (
                                0.0001,
                                0.0001,
                                0.0001
                            )
                        ),

                        max
                        (
                            _StepPatternProjectionBlend,
                            1.0
                        )
                    );


                float weightSum =
                    weights.x
                    +
                    weights.y
                    +
                    weights.z;


                weights /=
                    max
                    (
                        weightSum,
                        0.0001
                    );


                float2 uvX =
                    frac
                    (
                        patternPosition.zy
                    );

                float2 uvY =
                    frac
                    (
                        patternPosition.xz
                    );

                float2 uvZ =
                    frac
                    (
                        patternPosition.xy
                    );


                float maskX =
                    _StepPattern.Sample
                    (
                        sampler_StepPattern,
                        uvX
                    ).r;

                float maskY =
                    _StepPattern.Sample
                    (
                        sampler_StepPattern,
                        uvY
                    ).r;

                float maskZ =
                    _StepPattern.Sample
                    (
                        sampler_StepPattern,
                        uvZ
                    ).r;


                float pattern =
                    maskX *
                    weights.x
                    +
                    maskY *
                    weights.y
                    +
                    maskZ *
                    weights.z;


                pattern =
                    saturate
                    (
                        (
                            pattern -
                            0.5
                        )
                        *
                        _StepPatternContrast
                        +
                        0.5
                    );


                pattern =
                    lerp
                    (
                        pattern,

                        1.0 -
                        pattern,

                        saturate
                        (
                            _StepPatternInvert
                        )
                    );


                return
                    pattern *
                    2.0
                    -
                    1.0;

            #else

                return 0.0;

            #endif
        }


        float3 BuildNormalWS
        (
            float3 baseNormalWS,
            float3 tangentWS,
            float tangentSign,
            float2 uvNormal
        )
        {
            float3 n =
                normalize
                (
                    baseNormalWS
                );

            float3 t =
                normalize
                (
                    tangentWS
                );

            float3 b =
                tangentSign *
                cross
                (
                    n,
                    t
                );


            float4 normalSample =
                _NormalMap.Sample
                (
                    sampler_NormalMap,
                    uvNormal
                );


            float3 normalTS =
                UnpackNormalScale
                (
                    normalSample,
                    _NormalStrength
                );


            float3x3 tangentToWorld =
                float3x3
                (
                    t,
                    b,
                    n
                );


            return
                NormalizeNormalPerPixel
                (
                    TransformTangentToWorld
                    (
                        normalTS,
                        tangentToWorld
                    )
                );
        }


        float3 EvaluateAdditionalToonLight
        (
            float3 normalWS,
            Light light,
            float signedPattern
        )
        {
            float ndotl =
                saturate
                (
                    dot
                    (
                        normalWS,
                        light.direction
                    )
                );


            float attenuation =
                light.distanceAttenuation
                *
                light.shadowAttenuation;


            float raw =
                saturate
                (
                    ndotl *
                    attenuation
                    +
                    _LightBias
                );


            #if defined(_TOON_STEP_PATTERN)

                raw =
                    saturate
                    (
                        raw
                        +
                        signedPattern *
                        _StepPatternStrength
                    );

            #endif


            float toon =
                Quantize01
                (
                    raw,
                    _LightBands
                );


            return
                light.color
                *
                toon
                *
                _AdditionalLightStrength;
        }

        ENDHLSL


        // ============================================================
        // TOON FORWARD ONLY
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

            #pragma multi_compile_fog

            #pragma shader_feature_local_fragment _TOON_STEP_PATTERN
            #pragma shader_feature_local_fragment _HALFTONE_WORLD_SPACE

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _LIGHT_COOKIES


            struct ToonAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };


            struct ToonVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uvBase : TEXCOORD3;
                float2 uvNormal : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };


            ToonVaryings ToonVert
            (
                ToonAttributes IN
            )
            {
                ToonVaryings OUT =
                    (ToonVaryings)0;


                VertexPositionInputs posInputs =
                    GetVertexPositionInputs
                    (
                        IN.positionOS.xyz
                    );


                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs
                    (
                        IN.normalOS,
                        IN.tangentOS
                    );


                OUT.positionCS =
                    posInputs.positionCS;

                OUT.positionWS =
                    posInputs.positionWS;

                OUT.normalWS =
                    normalInputs.normalWS;


                float tangentSign =
                    IN.tangentOS.w *
                    GetOddNegativeScale();


                OUT.tangentWS =
                    float4
                    (
                        normalInputs.tangentWS,
                        tangentSign
                    );


                OUT.uvBase =
                    IN.uv *
                    _BaseMap_ST.xy
                    +
                    _BaseMap_ST.zw;


                OUT.uvNormal =
                    IN.uv *
                    _NormalMap_ST.xy
                    +
                    _NormalMap_ST.zw;


                OUT.fogFactor =
                    ComputeFogFactor
                    (
                        posInputs.positionCS.z
                    );


                return OUT;
            }


            half4 ToonFrag
            (
                ToonVaryings IN
            ) : SV_Target
            {
                // BASE
                float4 baseSample =
                    _BaseMap.Sample
                    (
                        sampler_BaseMap,
                        IN.uvBase
                    );


                float3 albedo =
                    baseSample.rgb *
                    _BaseColor.rgb;


                // GEOMETRIC NORMAL
                float3 geometricNormalWS =
                    normalize
                    (
                        IN.normalWS
                    );


                // ARTISTIC NORMAL MAP
                float3 normalWS =
                    BuildNormalWS
                    (
                        IN.normalWS,
                        IN.tangentWS.xyz,
                        IN.tangentWS.w,
                        IN.uvNormal
                    );


                // VIEW
                float3 viewDirWS =
                    GetWorldSpaceNormalizeViewDir
                    (
                        IN.positionWS
                    );


                // WORLD STEP PATTERN
                float signedPattern =
                    GetStepPatternSigned
                    (
                        IN.positionWS,
                        geometricNormalWS
                    );


                // MAIN LIGHT
                float4 shadowCoord =
                    TransformWorldToShadowCoord
                    (
                        IN.positionWS
                    );


                half4 shadowMask =
                    half4
                    (
                        1,
                        1,
                        1,
                        1
                    );


                Light mainLight =
                    GetMainLight
                    (
                        shadowCoord,
                        IN.positionWS,
                        shadowMask
                    );


                float shadowAtten =
                    lerp
                    (
                        1.0,
                        mainLight.shadowAttenuation,
                        _ReceiveShadowStrength
                    );


                float ndotl =
                    saturate
                    (
                        dot
                        (
                            normalWS,
                            mainLight.direction
                        )
                    );


                float rawLight =
                    saturate
                    (
                        ndotl *
                        mainLight.distanceAttenuation *
                        shadowAtten
                        +
                        _LightBias
                    );


                #if defined(_TOON_STEP_PATTERN)

                    rawLight =
                        saturate
                        (
                            rawLight
                            +
                            signedPattern *
                            _StepPatternStrength
                        );

                #endif


                float toonLight =
                    Quantize01
                    (
                        rawLight,
                        _LightBands
                    );


                float3 toonLightColor =
                    lerp
                    (
                        _ShadowColor.rgb,
                        mainLight.color,
                        toonLight
                    )
                    *
                    _DirectLightStrength;


                float3 ambient =
                    max
                    (
                        SampleSH
                        (
                            normalWS
                        ),
                        0.0
                    )
                    *
                    _AmbientStrength;


                float3 color =
                    albedo *
                    (
                        toonLightColor
                        +
                        ambient
                    );


                // ====================================================
                // HALFTONE
                // ====================================================

                float darkness =
                    1.0 -
                    toonLight;


                float halftone =
                    HalftoneMask
                    (
                        IN.positionCS.xy,
                        IN.positionWS,
                        geometricNormalWS,
                        darkness
                    );


                color =
                    lerp
                    (
                        color,

                        color *
                        _HalftoneColor.rgb,

                        halftone
                    );


                // ====================================================
                // ADDITIONAL LIGHTS
                // ====================================================

                InputData inputData =
                    (InputData)0;


                inputData.positionWS =
                    IN.positionWS;

                inputData.normalWS =
                    normalWS;

                inputData.viewDirectionWS =
                    viewDirWS;

                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV
                    (
                        IN.positionCS
                    );


                float3 additionalLighting =
                    0.0;


                #if defined(_ADDITIONAL_LIGHTS)

                    #if USE_CLUSTER_LIGHT_LOOP

                        [loop]
                        for
                        (
                            uint lightIndex = 0;

                            lightIndex <
                            min
                            (
                                uint
                                (
                                    URP_FP_DIRECTIONAL_LIGHTS_COUNT
                                ),

                                uint
                                (
                                    MAX_VISIBLE_LIGHTS
                                )
                            );

                            lightIndex++
                        )
                        {
                            Light light =
                                GetAdditionalLight
                                (
                                    lightIndex,
                                    inputData.positionWS,
                                    shadowMask
                                );


                            additionalLighting +=
                                EvaluateAdditionalToonLight
                                (
                                    normalWS,
                                    light,
                                    signedPattern
                                );
                        }


                        ClusterIterator clusterIterator =
                            ClusterInit
                            (
                                inputData.normalizedScreenSpaceUV,
                                inputData.positionWS,
                                0
                            );


                        uint clusterLightIndex;


                        [loop]
                        while
                        (
                            ClusterNext
                            (
                                clusterIterator,
                                clusterLightIndex
                            )
                        )
                        {
                            clusterLightIndex +=
                                URP_FP_DIRECTIONAL_LIGHTS_COUNT;


                            Light light =
                                GetAdditionalLight
                                (
                                    clusterLightIndex,
                                    inputData.positionWS,
                                    shadowMask
                                );


                            additionalLighting +=
                                EvaluateAdditionalToonLight
                                (
                                    normalWS,
                                    light,
                                    signedPattern
                                );
                        }

                    #else

                        uint pixelLightCount =
                            GetAdditionalLightsCount();


                        [loop]
                        for
                        (
                            uint lightIndex = 0;

                            lightIndex <
                            pixelLightCount;

                            lightIndex++
                        )
                        {
                            Light light =
                                GetAdditionalLight
                                (
                                    lightIndex,
                                    inputData.positionWS,
                                    shadowMask
                                );


                            additionalLighting +=
                                EvaluateAdditionalToonLight
                                (
                                    normalWS,
                                    light,
                                    signedPattern
                                );
                        }

                    #endif

                #endif


                color +=
                    albedo *
                    additionalLighting;


                // SPECULAR
                float3 halfDir =
                    SafeNormalize
                    (
                        mainLight.direction +
                        viewDirWS
                    );


                float ndoth =
                    saturate
                    (
                        dot
                        (
                            normalWS,
                            halfDir
                        )
                    );


                float spec =
                    pow
                    (
                        ndoth,

                        max
                        (
                            _SpecularPower,
                            1.0
                        )
                    );


                spec =
                    step
                    (
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


                // RIM
                float rimRaw =
                    1.0 -
                    saturate
                    (
                        dot
                        (
                            normalWS,
                            viewDirWS
                        )
                    );


                rimRaw =
                    pow
                    (
                        rimRaw,

                        max
                        (
                            _RimPower,
                            0.0001
                        )
                    );


                float rimThreshold =
                    _RimThreshold;


                #if defined(_TOON_STEP_PATTERN)

                    rimThreshold =
                        saturate
                        (
                            _RimThreshold
                            -
                            signedPattern *
                            _RimPatternStrength
                        );

                #endif


                float rim =
                    step
                    (
                        rimThreshold,
                        rimRaw
                    )
                    *
                    _RimStrength;


                color +=
                    _RimColor.rgb *
                    rim;


                // POSTERIZATION
                float3 posterized =
                    PosterizeColor
                    (
                        color,
                        _PosterizeSteps
                    );


                color =
                    lerp
                    (
                        color,
                        posterized,
                        _PosterizeStrength
                    );


                // FOG
                color =
                    MixFog
                    (
                        color,
                        IN.fogFactor
                    );


                return half4
                (
                    color,
                    baseSample.a *
                    _BaseColor.a
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

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;


            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };


            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };


            float4 GetCustomShadowPositionHClip
            (
                ShadowAttributes IN
            )
            {
                float3 positionWS =
                    TransformObjectToWorld
                    (
                        IN.positionOS.xyz
                    );


                float3 normalWS =
                    TransformObjectToWorldNormal
                    (
                        IN.normalOS
                    );


                #if _CASTING_PUNCTUAL_LIGHT_SHADOW

                    float3 lightDirectionWS =
                        normalize
                        (
                            _LightPosition -
                            positionWS
                        );

                #else

                    float3 lightDirectionWS =
                        _LightDirection;

                #endif


                float4 positionCS =
                    TransformWorldToHClip
                    (
                        ApplyShadowBias
                        (
                            positionWS,
                            normalWS,
                            lightDirectionWS
                        )
                    );


                return
                    ApplyShadowClamping
                    (
                        positionCS
                    );
            }


            ShadowVaryings ShadowVert
            (
                ShadowAttributes IN
            )
            {
                ShadowVaryings OUT =
                    (ShadowVaryings)0;


                OUT.positionCS =
                    GetCustomShadowPositionHClip
                    (
                        IN
                    );


                return OUT;
            }


            half4 ShadowFrag
            (
                ShadowVaryings IN
            ) : SV_Target
            {
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


            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };


            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };


            DepthVaryings DepthVert
            (
                DepthAttributes IN
            )
            {
                DepthVaryings OUT =
                    (DepthVaryings)0;


                OUT.positionCS =
                    TransformObjectToHClip
                    (
                        IN.positionOS.xyz
                    );


                return OUT;
            }


            half DepthFrag
            (
                DepthVaryings IN
            ) : SV_Target
            {
                return IN.positionCS.z;
            }

            ENDHLSL
        }


        // ============================================================
        // DEPTH NORMALS ONLY
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

            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT


            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };


            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float4 tangentWS : TEXCOORD1;
                float2 uvNormal : TEXCOORD2;
            };


            DepthNormalsVaryings DepthNormalsVert
            (
                DepthNormalsAttributes IN
            )
            {
                DepthNormalsVaryings OUT =
                    (DepthNormalsVaryings)0;


                OUT.positionCS =
                    TransformObjectToHClip
                    (
                        IN.positionOS.xyz
                    );


                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs
                    (
                        IN.normalOS,
                        IN.tangentOS
                    );


                OUT.normalWS =
                    normalInputs.normalWS;


                float tangentSign =
                    IN.tangentOS.w *
                    GetOddNegativeScale();


                OUT.tangentWS =
                    float4
                    (
                        normalInputs.tangentWS,
                        tangentSign
                    );


                OUT.uvNormal =
                    IN.uv *
                    _NormalMap_ST.xy
                    +
                    _NormalMap_ST.zw;


                return OUT;
            }


            half4 DepthNormalsFrag
            (
                DepthNormalsVaryings IN
            ) : SV_Target
            {
                float3 normalWS =
                    BuildNormalWS
                    (
                        IN.normalWS,
                        IN.tangentWS.xyz,
                        IN.tangentWS.w,
                        IN.uvNormal
                    );


                #if defined(_GBUFFER_NORMALS_OCT)

                    float2 octNormal =
                        PackNormalOctQuadEncode
                        (
                            normalWS
                        );


                    float2 remapped =
                        saturate
                        (
                            octNormal *
                            0.5
                            +
                            0.5
                        );


                    half3 packed =
                        PackFloat2To888
                        (
                            remapped
                        );


                    return half4
                    (
                        packed,
                        0
                    );

                #else

                    return half4
                    (
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