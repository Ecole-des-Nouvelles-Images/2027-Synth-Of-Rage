Shader "Custom/Toon/DepthOutlineWorldMask"
{
    Properties
    {
        // ============================================================
        // OUTLINE
        // ============================================================

        [Header(Outline)]

        _OutlineColor
        (
            "Outline Color",
            Color
        ) = (0, 0, 0, 1)

        _OutlineThickness
        (
            "Base Outline Thickness Pixels",
            Range(0.5, 8)
        ) = 2

        _OutlineOpacity
        (
            "Outline Opacity",
            Range(0, 1)
        ) = 1


        // ============================================================
        // DISTANCE THICKNESS
        // ============================================================

        [Header(Distance Thickness)]

        _NearThicknessMultiplier
        (
            "Near Thickness Multiplier",
            Range(1, 6)
        ) = 2

        _DistanceNear
        (
            "Near Distance",
            Float
        ) = 2

        _DistanceFar
        (
            "Far Distance",
            Float
        ) = 40

        _DistanceFalloff
        (
            "Distance Falloff",
            Range(0.1, 4)
        ) = 1


        // ============================================================
        // DEPTH
        // ============================================================

        [Header(Depth Detection)]

        _DepthThreshold
        (
            "Depth Threshold",
            Range(0.0001, 0.5)
        ) = 0.005

        _DepthSoftness
        (
            "Depth Softness",
            Range(0.0001, 0.2)
        ) = 0.003

        _DepthStrength
        (
            "Depth Strength",
            Range(0, 3)
        ) = 1


        // ============================================================
        // WORLD SPACE MASK
        // ============================================================

        [Header(World Space Mask)]

        _OutlineMask
        (
            "Outline Mask",
            2D
        ) = "white" {}

        _MaskStrength
        (
            "Mask Strength",
            Range(0, 1)
        ) = 0

        _MaskWorldScale
        (
            "World Mask Scale",
            Range(0.01, 20)
        ) = 1

        _MaskWorldOffset
        (
            "World Mask Offset",
            Vector
        ) = (0, 0, 0, 0)

        _MaskThreshold
        (
            "Mask Threshold",
            Range(0, 1)
        ) = 0.5

        _MaskSoftness
        (
            "Mask Softness",
            Range(0.001, 0.5)
        ) = 0.05

        _MaskInvert
        (
            "Invert Mask",
            Range(0, 1)
        ) = 0

        _MaskProjectionBlend
        (
            "Triplanar Sharpness",
            Range(1, 16)
        ) = 4
    }


    SubShader
    {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

        ENDHLSL


        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }


        LOD 100

        Cull Off
        ZWrite Off
        ZTest Always


        Pass
        {
            Name "World Space Depth Outline"


            // ========================================================
            // SPRITE STENCIL
            // ========================================================

            Stencil
            {
                Ref 8
                ReadMask 8

                Comp NotEqual

                Pass Keep
                Fail Keep
                ZFail Keep
            }


            HLSLPROGRAM


            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT


            // ========================================================
            // OUTLINE QUALITY
            // ========================================================

            #define OUTLINE_RINGS 8


            // ========================================================
            // MASK TEXTURE
            // ========================================================

            Texture2D<float4> _OutlineMask;

            SamplerState sampler_OutlineMask;


            // ========================================================
            // MATERIAL
            // ========================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _OutlineColor;

                float _OutlineThickness;
                float _OutlineOpacity;


                float _NearThicknessMultiplier;

                float _DistanceNear;
                float _DistanceFar;
                float _DistanceFalloff;


                float _DepthThreshold;
                float _DepthSoftness;
                float _DepthStrength;


                float _MaskStrength;

                float _MaskWorldScale;

                float4 _MaskWorldOffset;

                float _MaskThreshold;
                float _MaskSoftness;
                float _MaskInvert;

                float _MaskProjectionBlend;

            CBUFFER_END


            // ========================================================
            // CLAMP UV
            // ========================================================

            float2 ClampScreenUV
            (
                float2 uv
            )
            {
                float2 screenSize =
                    max
                    (
                        GetScaledScreenParams().xy,
                        float2(1.0, 1.0)
                    );


                float2 texelSize =
                    1.0 /
                    screenSize;


                return clamp
                (
                    uv,
                    texelSize * 0.5,
                    1.0 - texelSize * 0.5
                );
            }


            // ========================================================
            // SCENE COLOR
            // ========================================================

            float4 GetSceneColor
            (
                float2 uv
            )
            {
                uv =
                    ClampScreenUV
                    (
                        uv
                    );


                return _BlitTexture.Sample
                (
                    sampler_LinearClamp,
                    uv
                );
            }


            // ========================================================
            // RAW DEPTH
            // ========================================================

            float GetRawDepth
            (
                float2 uv
            )
            {
                uv =
                    ClampScreenUV
                    (
                        uv
                    );


                return SampleSceneDepth
                (
                    uv
                );
            }


            // ========================================================
            // BACKGROUND
            // ========================================================

            float IsBackground
            (
                float rawDepth
            )
            {
                #if UNITY_REVERSED_Z

                    return
                        1.0 -
                        step
                        (
                            0.000001,
                            rawDepth
                        );

                #else

                    return
                        step
                        (
                            0.999999,
                            rawDepth
                        );

                #endif
            }


            // ========================================================
            // EYE DEPTH
            // ========================================================

            float GetEyeDepth
            (
                float rawDepth
            )
            {
                return LinearEyeDepth
                (
                    rawDepth,
                    _ZBufferParams
                );
            }


            // ========================================================
            // RECONSTRUCT WORLD POSITION
            // ========================================================

            float3 GetWorldPosition
            (
                float2 uv,
                float rawDepth
            )
            {
                uv =
                    ClampScreenUV
                    (
                        uv
                    );


                float deviceDepth =
                    rawDepth;


                #if !UNITY_REVERSED_Z

                    deviceDepth =
                        lerp
                        (
                            UNITY_NEAR_CLIP_VALUE,
                            1.0,
                            rawDepth
                        );

                #endif


                return ComputeWorldSpacePosition
                (
                    uv,
                    deviceDepth,
                    UNITY_MATRIX_I_VP
                );
            }


            // ========================================================
            // SCENE NORMAL
            // ========================================================
            //
            // La normale ne sert PAS à créer l'outline.
            //
            // Elle sert uniquement à choisir la bonne projection
            // du masque World Space Triplanar.
            // ========================================================

            float3 GetSceneNormal
            (
                float2 uv
            )
            {
                uv =
                    ClampScreenUV
                    (
                        uv
                    );


                float3 normalWS =
                    SampleSceneNormals
                    (
                        uv
                    );


                float normalLength =
                    dot
                    (
                        normalWS,
                        normalWS
                    );


                if
                (
                    normalLength <
                    0.000001
                )
                {
                    return float3
                    (
                        0,
                        1,
                        0
                    );
                }


                return normalize
                (
                    normalWS
                );
            }


            // ========================================================
            // DISTANCE THICKNESS
            // ========================================================

            float GetThicknessForDepth
            (
                float eyeDepth
            )
            {
                float nearDistance =
                    min
                    (
                        _DistanceNear,
                        _DistanceFar
                    );


                float farDistance =
                    max
                    (
                        _DistanceNear,
                        _DistanceFar
                    );


                float distanceRange =
                    max
                    (
                        farDistance -
                        nearDistance,
                        0.001
                    );


                // ----------------------------------------------------
                // 1 = PROCHE
                // 0 = LOIN
                // ----------------------------------------------------

                float proximity =
                    1.0 -
                    saturate
                    (
                        (
                            eyeDepth -
                            nearDistance
                        )
                        /
                        distanceRange
                    );


                // ----------------------------------------------------
                // CURVE
                // ----------------------------------------------------

                proximity =
                    pow
                    (
                        proximity,

                        max
                        (
                            _DistanceFalloff,
                            0.01
                        )
                    );


                // ----------------------------------------------------
                // MULTIPLIER
                // ----------------------------------------------------

                float multiplier =
                    lerp
                    (
                        1.0,

                        max
                        (
                            _NearThicknessMultiplier,
                            1.0
                        ),

                        proximity
                    );


                return
                    _OutlineThickness *
                    multiplier;
            }


            // ========================================================
            // TRIPLANAR WORLD MASK
            // ========================================================

            float GetWorldMask
            (
                float3 worldPosition,
                float3 normalWS
            )
            {
                // ----------------------------------------------------
                // MASK DISABLED
                // ----------------------------------------------------

                if
                (
                    _MaskStrength <=
                    0.0001
                )
                {
                    return 1.0;
                }


                // ----------------------------------------------------
                // WORLD POSITION
                // ----------------------------------------------------

                float3 p =
                    (
                        worldPosition +
                        _MaskWorldOffset.xyz
                    )
                    *
                    _MaskWorldScale;


                // ----------------------------------------------------
                // TRIPLANAR WEIGHTS
                // ----------------------------------------------------

                float3 weights =
                    abs
                    (
                        normalWS
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
                            _MaskProjectionBlend,
                            1.0
                        )
                    );


                float weightTotal =
                    weights.x +
                    weights.y +
                    weights.z;


                weights /=
                    max
                    (
                        weightTotal,
                        0.0001
                    );


                // ====================================================
                // WORLD UVs
                // ====================================================

                float2 uvX =
                    frac
                    (
                        p.zy
                    );


                float2 uvY =
                    frac
                    (
                        p.xz
                    );


                float2 uvZ =
                    frac
                    (
                        p.xy
                    );


                // ====================================================
                // SAMPLES
                // ====================================================

                float maskX =
                    _OutlineMask.Sample
                    (
                        sampler_OutlineMask,
                        uvX
                    ).r;


                float maskY =
                    _OutlineMask.Sample
                    (
                        sampler_OutlineMask,
                        uvY
                    ).r;


                float maskZ =
                    _OutlineMask.Sample
                    (
                        sampler_OutlineMask,
                        uvZ
                    ).r;


                // ====================================================
                // BLEND
                // ====================================================

                float mask =
                    maskX * weights.x
                    +
                    maskY * weights.y
                    +
                    maskZ * weights.z;


                // ====================================================
                // INVERT
                // ====================================================

                mask =
                    lerp
                    (
                        mask,
                        1.0 - mask,

                        saturate
                        (
                            _MaskInvert
                        )
                    );


                // ====================================================
                // THRESHOLD
                // ====================================================

                float softness =
                    max
                    (
                        _MaskSoftness,
                        0.0001
                    );


                mask =
                    smoothstep
                    (
                        _MaskThreshold -
                        softness,

                        _MaskThreshold +
                        softness,

                        mask
                    );


                // ====================================================
                // MASK STRENGTH
                // ====================================================

                mask =
                    lerp
                    (
                        1.0,
                        mask,

                        saturate
                        (
                            _MaskStrength
                        )
                    );


                return mask;
            }


            // ========================================================
            // EVALUATE OUTSIDE SAMPLE
            // ========================================================

            float EvaluateOutsideSample
            (
                float2 neighborUV,

                float centerRaw,
                float neighborRaw,

                float sampleRadiusPixels
            )
            {
                float centerBackground =
                    IsBackground
                    (
                        centerRaw
                    );


                float neighborBackground =
                    IsBackground
                    (
                        neighborRaw
                    );


                // ====================================================
                // SKY -> SKY
                // ====================================================

                if
                (
                    centerBackground > 0.5 &&
                    neighborBackground > 0.5
                )
                {
                    return 0.0;
                }


                // ====================================================
                // OBJECT -> SKY
                // ====================================================
                //
                // Ne jamais dessiner vers l'intérieur.
                // ====================================================

                if
                (
                    centerBackground < 0.5 &&
                    neighborBackground > 0.5
                )
                {
                    return 0.0;
                }


                // ====================================================
                // NEIGHBOR MUST BE OBJECT
                // ====================================================

                if
                (
                    neighborBackground >
                    0.5
                )
                {
                    return 0.0;
                }


                // ====================================================
                // NEIGHBOR DEPTH
                // ====================================================

                float neighborEyeDepth =
                    GetEyeDepth
                    (
                        neighborRaw
                    );


                // ====================================================
                // DEPTH EDGE
                // ====================================================

                float depthEdge =
                    1.0;


                // ====================================================
                // OBJECT -> OBJECT
                // ====================================================

                if
                (
                    centerBackground <
                    0.5
                )
                {
                    float centerEyeDepth =
                        GetEyeDepth
                        (
                            centerRaw
                        );


                    // ------------------------------------------------
                    // Le voisin doit être PLUS PROCHE.
                    //
                    // Le pixel courant reçoit donc l'outline
                    // sur le côté extérieur.
                    // ------------------------------------------------

                    float difference =
                        centerEyeDepth -
                        neighborEyeDepth;


                    if
                    (
                        difference <=
                        0.0
                    )
                    {
                        return 0.0;
                    }


                    float referenceDepth =
                        max
                        (
                            neighborEyeDepth,
                            0.001
                        );


                    float relativeDifference =
                        difference /
                        referenceDepth;


                    depthEdge =
                        smoothstep
                        (
                            _DepthThreshold,

                            _DepthThreshold +
                            max
                            (
                                _DepthSoftness,
                                0.00001
                            ),

                            relativeDifference
                        );


                    if
                    (
                        depthEdge <=
                        0.0001
                    )
                    {
                        return 0.0;
                    }
                }


                // ====================================================
                // DISTANCE BASED THICKNESS
                // ====================================================

                float desiredThickness =
                    GetThicknessForDepth
                    (
                        neighborEyeDepth
                    );


                if
                (
                    sampleRadiusPixels >
                    desiredThickness
                )
                {
                    return 0.0;
                }


                // ====================================================
                // WORLD POSITION OF THE OBJECT
                // ====================================================

                neighborUV =
                    ClampScreenUV
                    (
                        neighborUV
                    );


                float3 worldPosition =
                    GetWorldPosition
                    (
                        neighborUV,
                        neighborRaw
                    );


                // ====================================================
                // WORLD NORMAL
                // ====================================================

                float3 normalWS =
                    GetSceneNormal
                    (
                        neighborUV
                    );


                // ====================================================
                // WORLD MASK
                // ====================================================

                float worldMask =
                    GetWorldMask
                    (
                        worldPosition,
                        normalWS
                    );


                return
                    depthEdge *
                    worldMask;
            }


            // ========================================================
            // SAMPLE RING
            // ========================================================

            float SampleOutlineRing
            (
                float2 uv,

                float centerRaw,

                float2 texelSize,

                float radiusPixels
            )
            {
                float2 cardinal =
                    texelSize *
                    radiusPixels;


                float2 diagonal =
                    cardinal *
                    0.70710678;


                float edge =
                    0.0;


                // ====================================================
                // LEFT
                // ====================================================

                float2 uvL =
                    uv +
                    float2
                    (
                        -cardinal.x,
                        0
                    );


                edge =
                    max
                    (
                        edge,

                        EvaluateOutsideSample
                        (
                            uvL,

                            centerRaw,

                            GetRawDepth
                            (
                                uvL
                            ),

                            radiusPixels
                        )
                    );


                // ====================================================
                // RIGHT
                // ====================================================

                float2 uvR =
                    uv +
                    float2
                    (
                        cardinal.x,
                        0
                    );


                edge =
                    max
                    (
                        edge,

                        EvaluateOutsideSample
                        (
                            uvR,

                            centerRaw,

                            GetRawDepth
                            (
                                uvR
                            ),

                            radiusPixels
                        )
                    );


                // ====================================================
                // UP
                // ====================================================

                float2 uvU =
                    uv +
                    float2
                    (
                        0,
                        cardinal.y
                    );


                edge =
                    max
                    (
                        edge,

                        EvaluateOutsideSample
                        (
                            uvU,

                            centerRaw,

                            GetRawDepth
                            (
                                uvU
                            ),

                            radiusPixels
                        )
                    );


                // ====================================================
                // DOWN
                // ====================================================

                float2 uvD =
                    uv +
                    float2
                    (
                        0,
                        -cardinal.y
                    );


                edge =
                    max
                    (
                        edge,

                        EvaluateOutsideSample
                        (
                            uvD,

                            centerRaw,

                            GetRawDepth
                            (
                                uvD
                            ),

                            radiusPixels
                        )
                    );


                // ====================================================
                // UP LEFT
                // ====================================================

                float2 uvUL =
                    uv +
                    float2
                    (
                        -diagonal.x,
                        diagonal.y
                    );


                edge =
                    max
                    (
                        edge,

                        EvaluateOutsideSample
                        (
                            uvUL,

                            centerRaw,

                            GetRawDepth
                            (
                                uvUL
                            ),

                            radiusPixels
                        )
                    );


                // ====================================================
                // UP RIGHT
                // ====================================================

                float2 uvUR =
                    uv +
                    float2
                    (
                        diagonal.x,
                        diagonal.y
                    );


                edge =
                    max
                    (
                        edge,

                        EvaluateOutsideSample
                        (
                            uvUR,

                            centerRaw,

                            GetRawDepth
                            (
                                uvUR
                            ),

                            radiusPixels
                        )
                    );


                // ====================================================
                // DOWN LEFT
                // ====================================================

                float2 uvDL =
                    uv +
                    float2
                    (
                        -diagonal.x,
                        -diagonal.y
                    );


                edge =
                    max
                    (
                        edge,

                        EvaluateOutsideSample
                        (
                            uvDL,

                            centerRaw,

                            GetRawDepth
                            (
                                uvDL
                            ),

                            radiusPixels
                        )
                    );


                // ====================================================
                // DOWN RIGHT
                // ====================================================

                float2 uvDR =
                    uv +
                    float2
                    (
                        diagonal.x,
                        -diagonal.y
                    );


                edge =
                    max
                    (
                        edge,

                        EvaluateOutsideSample
                        (
                            uvDR,

                            centerRaw,

                            GetRawDepth
                            (
                                uvDR
                            ),

                            radiusPixels
                        )
                    );


                return edge;
            }


            // ========================================================
            // GET OUTLINE
            // ========================================================

            float GetDepthEdge
            (
                float2 uv
            )
            {
                float2 screenSize =
                    max
                    (
                        GetScaledScreenParams().xy,
                        float2(1.0, 1.0)
                    );


                float2 texelSize =
                    1.0 /
                    screenSize;


                float centerRaw =
                    GetRawDepth
                    (
                        uv
                    );


                // ====================================================
                // MAXIMUM POSSIBLE WIDTH
                // ====================================================

                float maxThickness =
                    max
                    (
                        _OutlineThickness *
                        max
                        (
                            _NearThicknessMultiplier,
                            1.0
                        ),

                        0.5
                    );


                float edge =
                    0.0;


                // ====================================================
                // DILATION RINGS
                // ====================================================

                [unroll]

                for
                (
                    int ring = 1;

                    ring <= OUTLINE_RINGS;

                    ring++
                )
                {
                    float radiusPixels =
                        maxThickness *
                        (
                            (float)ring /
                            (float)OUTLINE_RINGS
                        );


                    edge =
                        max
                        (
                            edge,

                            SampleOutlineRing
                            (
                                uv,

                                centerRaw,

                                texelSize,

                                radiusPixels
                            )
                        );
                }


                edge *=
                    _DepthStrength;


                return saturate
                (
                    edge
                );
            }


            // ========================================================
            // FRAGMENT
            // ========================================================

            float4 Frag
            (
                Varyings input
            ) : SV_Target
            {
                float2 uv =
                    input.texcoord;


                // ====================================================
                // ORIGINAL SCENE
                // ====================================================

                float4 sceneColor =
                    GetSceneColor
                    (
                        uv
                    );


                // ====================================================
                // OUTLINE
                // ====================================================

                float edge =
                    GetDepthEdge
                    (
                        uv
                    );


                // ====================================================
                // OPACITY
                // ====================================================

                edge *=
                    saturate
                    (
                        _OutlineOpacity
                    );


                edge =
                    saturate
                    (
                        edge
                    );


                // ====================================================
                // FINAL
                // ====================================================

                float3 finalColor =
                    lerp
                    (
                        sceneColor.rgb,

                        _OutlineColor.rgb,

                        edge *
                        _OutlineColor.a
                    );


                return float4
                (
                    finalColor,
                    sceneColor.a
                );
            }


            ENDHLSL
        }
    }


    FallBack Off
}