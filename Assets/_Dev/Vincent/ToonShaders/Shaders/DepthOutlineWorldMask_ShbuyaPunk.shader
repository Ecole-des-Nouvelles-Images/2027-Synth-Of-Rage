Shader "Custom/Toon/DepthOutlineWorldMask-ShibuyaPunk-S3DC"
{
    Properties
    {
        // ============================================================
        // OUTLINE
        // ============================================================

        [Header(Outline)]

        [Toggle(_OUTLINE_SPRITES3D)]
        _OutlineSprites3D
        (
            "Outline Sprite 3D",
            Float
        ) = 0

        _OutlineColor
        (
            "Outline Color",
            Color
        ) = (0, 0, 0, 1)

        _OutlineThickness
        (
            "Base Outline Thickness Pixels",
            Range(0.5, 50)
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
            Range(0.0001, 2.5)
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
        // SLOPE REJECTION
        // ============================================================
        //
        // Supprime les faux contours provoqués par :
        //
        // - sols inclinés
        // - murs inclinés
        // - perspective
        // - surfaces continues vues à angle rasant
        //
        // 0 = désactivé.
        // ============================================================

        [Header(Depth Slope Compensation)]

        _SlopeCompensation
        (
            "Slope Compensation",
            Range(0, 3)
        ) = 1.25

        _SlopeSampleReject
        (
            "Slope Sample Reject",
            Range(0.001, 0.25)
        ) = 0.05


        // ============================================================
        // SHIBUYA PUNK / BRUSH IMPERFECTIONS
        // ============================================================

        [Header(Shibuya Punk Outline)]

        [Toggle(_OUTLINE_IMPERFECTIONS)]
        _UseOutlineImperfections
        (
            "Enable Shibuya Punk Style",
            Float
        ) = 0

        _ImperfectionPattern
        (
            "Brush / Graffiti Pattern",
            2D
        ) = "gray" {}

        [Toggle(_IMPERFECTION_FULL_TRIPLANAR)]
        _ImperfectionFullTriplanar
        (
            "Full Triplanar Pattern Higher Cost",
            Float
        ) = 0

        _ImperfectionWorldScale
        (
            "Brush World Scale",
            Range(0.01, 20)
        ) = 1.5

        _ImperfectionWorldOffset
        (
            "Brush World Offset",
            Vector
        ) = (0, 0, 0, 0)

        _ImperfectionSeed
        (
            "Random Seed",
            Float
        ) = 0

        _ImperfectionContrast
        (
            "Brush Contrast",
            Range(0.1, 12)
        ) = 3

        _ImperfectionProjectionBlend
        (
            "Triplanar Sharpness",
            Range(1, 16)
        ) = 6


        // ============================================================
        // MACRO SHAPE
        // ============================================================
        // Creates large chunks of thick / thin paint instead of only
        // small noisy fluctuations.
        // ============================================================

        _MacroScale
        (
            "Macro Chunk Scale",
            Range(0.02, 2)
        ) = 0.22

        _MacroStrength
        (
            "Macro Chunk Strength",
            Range(0, 1)
        ) = 0.75

        _ChunkBoost
        (
            "Chunk Protrusion Pixels",
            Range(0, 6)
        ) = 2.0

        _ChunkThreshold
        (
            "Chunk Threshold",
            Range(0, 1)
        ) = 0.62


        // ============================================================
        // THICKNESS / DRY BRUSH
        // ============================================================

        _ThicknessJitter
        (
            "Thickness Chaos",
            Range(0, 1.5)
        ) = 0.55

        _BreakupStrength
        (
            "Dry Brush Breakup",
            Range(0, 1)
        ) = 0.55

        _BreakupThreshold
        (
            "Breakup Threshold",
            Range(0, 1)
        ) = 0.48

        _BreakupSoftness
        (
            "Breakup Softness",
            Range(0.001, 0.5)
        ) = 0.045

        _CoreIntegrity
        (
            "Core Integrity",
            Range(0, 1)
        ) = 0.35

        _OpacityJitter
        (
            "Paint Opacity Chaos",
            Range(0, 1)
        ) = 0.35


        // ============================================================
        // SPIKES / BRUSH FLICKS
        // ============================================================

        _SpikeLength
        (
            "Brush Flick Length Pixels",
            Range(0, 8)
        ) = 2.5

        _SpikeThreshold
        (
            "Brush Flick Rarity",
            Range(0, 1)
        ) = 0.76

        _SpikeDirectionality
        (
            "Brush Flick Directionality",
            Range(1, 16)
        ) = 7


        // ============================================================
        // PAINT DRIPS
        // ============================================================
        // Screen-space downwards on purpose: a graphic / graffiti
        // effect rather than a physical world-space simulation.
        // ============================================================

        _DripLength
        (
            "Paint Drip Length Pixels",
            Range(0, 10)
        ) = 3

        _DripThreshold
        (
            "Paint Drip Rarity",
            Range(0, 1)
        ) = 0.80

        _DripDirectionality
        (
            "Paint Drip Directionality",
            Range(1, 16)
        ) = 8


        // ============================================================
        // OVERSPRAY
        // ============================================================

        _OversprayStrength
        (
            "Spray / Dust Strength",
            Range(0, 1)
        ) = 0.35

        _OversprayWidth
        (
            "Spray Width Pixels",
            Range(0, 8)
        ) = 2.5

        _OversprayThreshold
        (
            "Spray Density Threshold",
            Range(0, 1)
        ) = 0.70

        _OverspraySoftness
        (
            "Spray Softness",
            Range(0.001, 0.5)
        ) = 0.035


        // ============================================================
        // NEON ACCENT STREAKS
        // ============================================================
        // A second colour channel generated during the SAME outline
        // search. No second fullscreen outline pass.
        // ============================================================

        _AccentColor
        (
            "Graffiti Accent Color",
            Color
        ) = (1, 0.05, 0.65, 1)

        _AccentStrength
        (
            "Graffiti Accent Strength",
            Range(0, 1)
        ) = 0.65

        _AccentThreshold
        (
            "Graffiti Accent Rarity",
            Range(0, 1)
        ) = 0.68

        _AccentSoftness
        (
            "Graffiti Accent Softness",
            Range(0.001, 0.3)
        ) = 0.04

        _AccentOuterBias
        (
            "Graffiti Accent Outer Bias",
            Range(0, 1)
        ) = 0.65


        // ============================================================
        // WORLD MASK
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
            Name "Combined Mesh Sprite Depth Outline"


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


            #pragma shader_feature_local_fragment _OUTLINE_SPRITES3D

            #pragma shader_feature_local_fragment _OUTLINE_IMPERFECTIONS

            #pragma shader_feature_local_fragment _IMPERFECTION_FULL_TRIPLANAR


            // ========================================================
            // QUALITY
            // ========================================================

            #define OUTLINE_RINGS 8


            // ========================================================
            // TEXTURES
            // ========================================================

            Texture2D<float4> _OutlineMask;

            SamplerState sampler_OutlineMask;


            Texture2D<float4> _ImperfectionPattern;

            SamplerState sampler_ImperfectionPattern;


            Texture2D<float4> _Sprite3DDepthTexture;

            SamplerState sampler_Sprite3DDepthTexture;


            // ========================================================
            // MATERIAL
            // ========================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _OutlineColor;

                float _OutlineSprites3D;

                float _OutlineThickness;

                float _OutlineOpacity;


                float _NearThicknessMultiplier;

                float _DistanceNear;

                float _DistanceFar;

                float _DistanceFalloff;


                float _DepthThreshold;

                float _DepthSoftness;

                float _DepthStrength;


                float _SlopeCompensation;

                float _SlopeSampleReject;


                float _UseOutlineImperfections;

                float _ImperfectionFullTriplanar;

                float _ImperfectionWorldScale;

                float4 _ImperfectionWorldOffset;

                float _ImperfectionSeed;

                float _ImperfectionContrast;

                float _ImperfectionProjectionBlend;

                float _MacroScale;

                float _MacroStrength;

                float _ChunkBoost;

                float _ChunkThreshold;

                float _ThicknessJitter;

                float _BreakupStrength;

                float _BreakupThreshold;

                float _BreakupSoftness;

                float _CoreIntegrity;

                float _OpacityJitter;

                float _SpikeLength;

                float _SpikeThreshold;

                float _SpikeDirectionality;

                float _DripLength;

                float _DripThreshold;

                float _DripDirectionality;

                float _OversprayStrength;

                float _OversprayWidth;

                float _OversprayThreshold;

                float _OverspraySoftness;

                float4 _AccentColor;

                float _AccentStrength;

                float _AccentThreshold;

                float _AccentSoftness;

                float _AccentOuterBias;

                float _MaskStrength;

                float _MaskWorldScale;

                float4 _MaskWorldOffset;

                float _MaskThreshold;

                float _MaskSoftness;

                float _MaskInvert;

                float _MaskProjectionBlend;

            CBUFFER_END


            // ========================================================
            // SURFACE
            // ========================================================

            struct SurfaceData
            {
                float valid;

                float eyeDepth;

                float source;

                float rawDepth;
            };


            // ========================================================
            // SCREEN
            // ========================================================

            float2 GetScreenSize()
            {
                return max
                (
                    GetScaledScreenParams().xy,

                    float2
                    (
                        1.0,
                        1.0
                    )
                );
            }


            float2 GetScreenTexelSize()
            {
                return
                    1.0 /
                    GetScreenSize();
            }


            float2 ClampScreenUV
            (
                float2 uv
            )
            {
                float2 texelSize =
                    GetScreenTexelSize();


                return clamp
                (
                    uv,

                    texelSize * 0.5,

                    1.0 -
                    texelSize * 0.5
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
                return _BlitTexture.Sample
                (
                    sampler_LinearClamp,

                    ClampScreenUV
                    (
                        uv
                    )
                );
            }


            // ========================================================
            // DEPTH
            // ========================================================

            float GetRawDepth
            (
                float2 uv
            )
            {
                return SampleSceneDepth
                (
                    ClampScreenUV
                    (
                        uv
                    )
                );
            }


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


            float EyeDepthToRawDepth
            (
                float eyeDepth
            )
            {
                float safeDepth =
                    max
                    (
                        eyeDepth,

                        0.00001
                    );


                float rawDepth =
                    (
                        rcp
                        (
                            safeDepth
                        )
                        -
                        _ZBufferParams.w
                    )
                    /
                    _ZBufferParams.z;


                return saturate
                (
                    rawDepth
                );
            }


            // ========================================================
            // WORLD POSITION
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


            float3 GetWorldPositionFromEyeDepth
            (
                float2 uv,

                float eyeDepth
            )
            {
                return GetWorldPosition
                (
                    uv,

                    EyeDepthToRawDepth
                    (
                        eyeDepth
                    )
                );
            }


            // ========================================================
            // SPRITE DEPTH
            // ========================================================

            float GetSpriteDepth01
            (
                float2 uv
            )
            {
                return _Sprite3DDepthTexture.Sample
                (
                    sampler_Sprite3DDepthTexture,

                    ClampScreenUV
                    (
                        uv
                    )
                ).r;
            }


            // ========================================================
            // COMBINED SURFACE
            // ========================================================

            SurfaceData GetVisibleSurface
            (
                float2 uv
            )
            {
                uv =
                    ClampScreenUV
                    (
                        uv
                    );


                SurfaceData result;


                result.valid =
                    0.0;


                result.eyeDepth =
                    0.0;


                result.source =
                    0.0;


                result.rawDepth =
                    0.0;


                // ====================================================
                // MESH
                // ====================================================

                float sceneRawDepth =
                    GetRawDepth
                    (
                        uv
                    );


                if
                (
                    IsBackground
                    (
                        sceneRawDepth
                    )
                    <
                    0.5
                )
                {
                    result.valid =
                        1.0;


                    result.eyeDepth =
                        GetEyeDepth
                        (
                            sceneRawDepth
                        );


                    result.source =
                        1.0;


                    result.rawDepth =
                        sceneRawDepth;
                }


                // ====================================================
                // SPRITE
                // ====================================================

                #if defined(_OUTLINE_SPRITES3D)


                    float spriteDepth01 =
                        GetSpriteDepth01
                        (
                            uv
                        );


                    if
                    (
                        spriteDepth01 <
                        0.99999
                    )
                    {
                        float spriteEyeDepth =
                            max
                            (
                                spriteDepth01 *
                                _ProjectionParams.z,

                                0.0001
                            );


                        if
                        (
                            result.valid <
                            0.5
                            ||
                            spriteEyeDepth <
                            result.eyeDepth
                        )
                        {
                            result.valid =
                                1.0;


                            result.eyeDepth =
                                spriteEyeDepth;


                            result.source =
                                2.0;


                            result.rawDepth =
                                EyeDepthToRawDepth
                                (
                                    spriteEyeDepth
                                );
                        }
                    }


                #endif


                return result;
            }


            // ========================================================
            // MESH WORLD SAMPLE
            // ========================================================
            //
            // IMPORTANT :
            //
            // utilise UNIQUEMENT la depth caméra.
            //
            // Donc aucune Normal Map artistique ici.
            // ========================================================

            void GetMeshWorldSample
            (
                float2 uv,

                out float valid,

                out float3 worldPosition
            )
            {
                float rawDepth =
                    GetRawDepth
                    (
                        uv
                    );


                if
                (
                    IsBackground
                    (
                        rawDepth
                    )
                    >
                    0.5
                )
                {
                    valid =
                        0.0;


                    worldPosition =
                        float3
                        (
                            0,
                            0,
                            0
                        );


                    return;
                }


                valid =
                    1.0;


                worldPosition =
                    GetWorldPosition
                    (
                        uv,

                        rawDepth
                    );
            }


            // ========================================================
            // DEPTH-DERIVED GEOMETRIC NORMAL
            // ========================================================
            //
            // Reconstruit une vraie normale géométrique depuis
            // les positions monde voisines.
            //
            // La Normal Map du matériau n'entre jamais ici.
            // ========================================================

            float3 GetMeshGeometricNormalWS
            (
                float2 uv,

                float3 centerPosition
            )
            {
                float2 texelSize =
                    GetScreenTexelSize();


                float validL;
                float validR;
                float validU;
                float validD;


                float3 posL;
                float3 posR;
                float3 posU;
                float3 posD;


                GetMeshWorldSample
                (
                    uv -
                    float2
                    (
                        texelSize.x,
                        0
                    ),

                    validL,

                    posL
                );


                GetMeshWorldSample
                (
                    uv +
                    float2
                    (
                        texelSize.x,
                        0
                    ),

                    validR,

                    posR
                );


                GetMeshWorldSample
                (
                    uv +
                    float2
                    (
                        0,
                        texelSize.y
                    ),

                    validU,

                    posU
                );


                GetMeshWorldSample
                (
                    uv -
                    float2
                    (
                        0,
                        texelSize.y
                    ),

                    validD,

                    posD
                );


                // ====================================================
                // X DERIVATIVE
                // ====================================================

                float3 deltaR =
                    posR -
                    centerPosition;


                float3 deltaL =
                    centerPosition -
                    posL;


                float distR =
                    (
                        validR >
                        0.5
                    )
                    ?
                    dot
                    (
                        deltaR,
                        deltaR
                    )
                    :
                    1e20;


                float distL =
                    (
                        validL >
                        0.5
                    )
                    ?
                    dot
                    (
                        deltaL,
                        deltaL
                    )
                    :
                    1e20;


                float xValid =
                    step
                    (
                        min
                        (
                            distR,
                            distL
                        ),

                        1e19
                    );


                float3 dx =
                    (
                        distR <
                        distL
                    )
                    ?
                    deltaR
                    :
                    deltaL;


                // ====================================================
                // Y DERIVATIVE
                // ====================================================

                float3 deltaU =
                    posU -
                    centerPosition;


                float3 deltaD =
                    centerPosition -
                    posD;


                float distU =
                    (
                        validU >
                        0.5
                    )
                    ?
                    dot
                    (
                        deltaU,
                        deltaU
                    )
                    :
                    1e20;


                float distD =
                    (
                        validD >
                        0.5
                    )
                    ?
                    dot
                    (
                        deltaD,
                        deltaD
                    )
                    :
                    1e20;


                float yValid =
                    step
                    (
                        min
                        (
                            distU,
                            distD
                        ),

                        1e19
                    );


                float3 dy =
                    (
                        distU <
                        distD
                    )
                    ?
                    deltaU
                    :
                    deltaD;


                // ====================================================
                // FALLBACK
                // ====================================================

                if
                (
                    xValid <
                    0.5
                    ||
                    yValid <
                    0.5
                )
                {
                    return normalize
                    (
                        _WorldSpaceCameraPos -
                        centerPosition
                    );
                }


                float3 normalWS =
                    cross
                    (
                        dx,

                        dy
                    );


                float lengthSq =
                    dot
                    (
                        normalWS,

                        normalWS
                    );


                if
                (
                    lengthSq <
                    0.00000001
                )
                {
                    return normalize
                    (
                        _WorldSpaceCameraPos -
                        centerPosition
                    );
                }


                normalWS =
                    normalize
                    (
                        normalWS
                    );


                // Orientation cohérente vers la caméra.
                if
                (
                    dot
                    (
                        normalWS,

                        _WorldSpaceCameraPos -
                        centerPosition
                    )
                    <
                    0.0
                )
                {
                    normalWS =
                        -normalWS;
                }


                return normalWS;
            }


            // ========================================================
            // SURFACE WORLD DATA
            // ========================================================

            void GetSurfaceWorldData
            (
                float2 uv,

                SurfaceData surface,

                out float3 worldPosition,

                out float3 geometricNormalWS
            )
            {
                // ====================================================
                // MESH
                // ====================================================

                if
                (
                    surface.source <
                    1.5
                )
                {
                    worldPosition =
                        GetWorldPosition
                        (
                            uv,

                            surface.rawDepth
                        );


                    geometricNormalWS =
                        GetMeshGeometricNormalWS
                        (
                            uv,

                            worldPosition
                        );


                    return;
                }


                // ====================================================
                // SPRITE 3D
                // ====================================================

                worldPosition =
                    GetWorldPositionFromEyeDepth
                    (
                        uv,

                        surface.eyeDepth
                    );


                geometricNormalWS =
                    normalize
                    (
                        _WorldSpaceCameraPos -
                        worldPosition
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
            // WORLD MASK
            // ========================================================

            float GetWorldMask
            (
                float3 worldPosition,

                float3 geometricNormalWS
            )
            {
                if
                (
                    _MaskStrength <=
                    0.0001
                )
                {
                    return 1.0;
                }


                float3 p =
                    (
                        worldPosition +
                        _MaskWorldOffset.xyz
                    )
                    *
                    _MaskWorldScale;


                float3 weights =
                    abs
                    (
                        geometricNormalWS
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


                weights /=
                    max
                    (
                        weights.x +
                        weights.y +
                        weights.z,

                        0.0001
                    );


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


                float mask =
                    maskX * weights.x
                    +
                    maskY * weights.y
                    +
                    maskZ * weights.z;


                mask =
                    lerp
                    (
                        mask,

                        1.0 -
                        mask,

                        saturate
                        (
                            _MaskInvert
                        )
                    );


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


                return lerp
                (
                    1.0,

                    mask,

                    saturate
                    (
                        _MaskStrength
                    )
                );
            }


            // ========================================================
            // PROCEDURAL HASH
            // ========================================================

            float Hash31
            (
                float3 p
            )
            {
                p =
                    frac
                    (
                        p *
                        0.1031
                    );


                p +=
                    dot
                    (
                        p,

                        p.yzx +
                        33.33
                    );


                return frac
                (
                    (
                        p.x +
                        p.y
                    )
                    *
                    p.z
                );
            }


            // ========================================================
            // IMPERFECTION PATTERN
            // ========================================================

            float SampleImperfectionPatternDominantAxis
            (
                float3 p,

                float3 geometricNormalWS
            )
            {
                float3 axis =
                    abs
                    (
                        geometricNormalWS
                    );


                if
                (
                    axis.x >= axis.y
                    &&
                    axis.x >= axis.z
                )
                {
                    return _ImperfectionPattern.Sample
                    (
                        sampler_ImperfectionPattern,

                        frac
                        (
                            p.zy
                        )
                    ).r;
                }


                if
                (
                    axis.y >= axis.z
                )
                {
                    return _ImperfectionPattern.Sample
                    (
                        sampler_ImperfectionPattern,

                        frac
                        (
                            p.xz
                        )
                    ).r;
                }


                return _ImperfectionPattern.Sample
                (
                    sampler_ImperfectionPattern,

                    frac
                    (
                        p.xy
                    )
                ).r;
            }


            float GetImperfectionPattern
            (
                float3 worldPosition,

                float3 geometricNormalWS
            )
            {
                #if defined(_OUTLINE_IMPERFECTIONS)


                    float seed =
                        _ImperfectionSeed *
                        13.371;


                    float3 seedOffset =
                        float3
                        (
                            seed,

                            seed * 0.37,

                            seed * 0.73
                        );


                    float3 p =
                        (
                            worldPosition +
                            _ImperfectionWorldOffset.xyz +
                            seedOffset
                        )
                        *
                        _ImperfectionWorldScale;


                    float pattern;


                    // ================================================
                    // FAST MODE
                    // ================================================
                    // One texture sample instead of three.
                    // For dirty brush / graffiti this also gives a
                    // slightly harsher projection which fits the DA.
                    // ================================================

                    #if !defined(_IMPERFECTION_FULL_TRIPLANAR)


                        pattern =
                            SampleImperfectionPatternDominantAxis
                            (
                                p,

                                geometricNormalWS
                            );


                    #else


                        float3 weights =
                            abs
                            (
                                geometricNormalWS
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
                                    _ImperfectionProjectionBlend,

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


                        float patternX =
                            _ImperfectionPattern.Sample
                            (
                                sampler_ImperfectionPattern,

                                frac
                                (
                                    p.zy
                                )
                            ).r;


                        float patternY =
                            _ImperfectionPattern.Sample
                            (
                                sampler_ImperfectionPattern,

                                frac
                                (
                                    p.xz
                                )
                            ).r;


                        float patternZ =
                            _ImperfectionPattern.Sample
                            (
                                sampler_ImperfectionPattern,

                                frac
                                (
                                    p.xy
                                )
                            ).r;


                        pattern =
                            patternX * weights.x +
                            patternY * weights.y +
                            patternZ * weights.z;


                    #endif


                    pattern =
                        saturate
                        (
                            (
                                pattern -
                                0.5
                            )
                            *
                            _ImperfectionContrast
                            +
                            0.5
                        );


                    return pattern;


                #else


                    return 0.5;


                #endif
            }


            // ========================================================
            // MACRO NOISE
            // ========================================================
            // Large stable chunks. Procedural: no extra texture sample.
            // ========================================================

            float GetMacroNoise
            (
                float3 worldPosition
            )
            {
                float macroScale =
                    max
                    (
                        _ImperfectionWorldScale *
                        _MacroScale,

                        0.001
                    );


                float3 cell =
                    floor
                    (
                        worldPosition *
                        macroScale
                        +
                        float3
                        (
                            _ImperfectionSeed * 7.13,
                            _ImperfectionSeed * 3.71,
                            _ImperfectionSeed * 5.29
                        )
                    );


                return Hash31
                (
                    cell
                );
            }


            // ========================================================
            // LOCAL DEPTH GRADIENT
            // ========================================================
            // LOCAL DEPTH GRADIENT
            // ========================================================
            //
            // On mesure la pente locale à +/- 1 pixel.
            //
            // On choisit pour chaque axe le voisin dont la différence
            // est la plus faible.
            //
            // Ça évite qu'un vrai bord serve à calculer la pente.
            // ========================================================

            float GetSlopeCandidate
            (
                SurfaceData center,

                float2 sampleUV,

                float directionSign
            )
            {
                SurfaceData sample =
                    GetVisibleSurface
                    (
                        sampleUV
                    );


                if
                (
                    center.valid <
                    0.5
                    ||
                    sample.valid <
                    0.5
                )
                {
                    return 1e20;
                }


                // Sprite et mesh ne doivent pas participer au même
                // calcul local de pente.
                if
                (
                    abs
                    (
                        sample.source -
                        center.source
                    )
                    >
                    0.25
                )
                {
                    return 1e20;
                }


                float referenceDepth =
                    max
                    (
                        center.eyeDepth,

                        0.001
                    );


                float observedDifference =
                    (
                        center.eyeDepth -
                        sample.eyeDepth
                    )
                    /
                    referenceDepth;


                // Pour le voisin négatif (-X/-Y),
                // on remet la dérivée dans le sens positif.
                return
                    observedDifference /
                    directionSign;
            }


            float SelectSlopeCandidate
            (
                float negativeCandidate,

                float positiveCandidate
            )
            {
                float negativeAbs =
                    abs
                    (
                        negativeCandidate
                    );


                float positiveAbs =
                    abs
                    (
                        positiveCandidate
                    );


                float chosen =
                    (
                        positiveAbs <
                        negativeAbs
                    )
                    ?
                    positiveCandidate
                    :
                    negativeCandidate;


                float chosenAbs =
                    min
                    (
                        negativeAbs,

                        positiveAbs
                    );


                // Aucun voisin exploitable.
                if
                (
                    chosenAbs >
                    1e10
                )
                {
                    return 0.0;
                }


                // Si même le meilleur voisin change énormément de
                // profondeur, on considère qu'il s'agit probablement
                // d'une vraie rupture et non d'une pente.
                if
                (
                    chosenAbs >
                    _SlopeSampleReject
                )
                {
                    return 0.0;
                }


                return chosen;
            }


            float2 GetLocalRelativeDepthGradient
            (
                float2 uv,

                SurfaceData center,

                float2 texelSize
            )
            {
                if
                (
                    center.valid <
                    0.5
                )
                {
                    return float2
                    (
                        0,
                        0
                    );
                }


                float slopeLeft =
                    GetSlopeCandidate
                    (
                        center,

                        uv -
                        float2
                        (
                            texelSize.x,
                            0
                        ),

                        -1.0
                    );


                float slopeRight =
                    GetSlopeCandidate
                    (
                        center,

                        uv +
                        float2
                        (
                            texelSize.x,
                            0
                        ),

                        1.0
                    );


                float slopeDown =
                    GetSlopeCandidate
                    (
                        center,

                        uv -
                        float2
                        (
                            0,
                            texelSize.y
                        ),

                        -1.0
                    );


                float slopeUp =
                    GetSlopeCandidate
                    (
                        center,

                        uv +
                        float2
                        (
                            0,
                            texelSize.y
                        ),

                        1.0
                    );


                float slopeX =
                    SelectSlopeCandidate
                    (
                        slopeLeft,

                        slopeRight
                    );


                float slopeY =
                    SelectSlopeCandidate
                    (
                        slopeDown,

                        slopeUp
                    );


                return float2
                (
                    slopeX,

                    slopeY
                );
            }


            // ========================================================
            // APPLY BRUSH STYLE
            // ========================================================

            float2 ApplyOutlineSurfaceStyle
            (
                float baseEdge,

                float2 surfaceUV,

                SurfaceData surface,

                float sampleRadiusPixels,

                float baseThickness,

                float2 sampleDirectionPixels
            )
            {
                float3 worldPosition;

                float3 geometricNormalWS;


                GetSurfaceWorldData
                (
                    surfaceUV,

                    surface,

                    worldPosition,

                    geometricNormalWS
                );


                // ====================================================
                // WORLD MASK
                // ====================================================

                float worldMask =
                    GetWorldMask
                    (
                        worldPosition,

                        geometricNormalWS
                    );


                baseEdge *=
                    worldMask;


                // X = main/core stroke
                // Y = neon graffiti accent
                float2 result =
                    float2
                    (
                        0.0,
                        0.0
                    );


                // ====================================================
                // STYLE OFF
                // ====================================================

                #if !defined(_OUTLINE_IMPERFECTIONS)


                    if
                    (
                        sampleRadiusPixels >
                        baseThickness
                    )
                    {
                        return result;
                    }


                    result.x =
                        baseEdge;


                    return result;


                #else


                    float brush =
                        GetImperfectionPattern
                        (
                            worldPosition,

                            geometricNormalWS
                        );


                    float macro =
                        GetMacroNoise
                        (
                            worldPosition
                        );


                    float proceduralScale =
                        max
                        (
                            _ImperfectionWorldScale *
                            3.0,

                            0.01
                        );


                    float3 hashPosition =
                        floor
                        (
                            worldPosition *
                            proceduralScale
                            +
                            float3
                            (
                                _ImperfectionSeed,
                                _ImperfectionSeed * 1.37,
                                _ImperfectionSeed * 2.11
                            )
                        );


                    float detail =
                        Hash31
                        (
                            hashPosition
                        );


                    float detailB =
                        Hash31
                        (
                            hashPosition +
                            float3
                            (
                                19.19,
                                7.73,
                                31.41
                            )
                        );


                    // =================================================
                    // MACRO CHUNKS
                    // =================================================

                    float macroMixed =
                        lerp
                        (
                            0.5,

                            macro,

                            saturate
                            (
                                _MacroStrength
                            )
                        );


                    float chunkPulse =
                        smoothstep
                        (
                            _ChunkThreshold - 0.06,

                            _ChunkThreshold + 0.06,

                            saturate
                            (
                                macroMixed * 0.62 +
                                brush * 0.38
                            )
                        );


                    // =================================================
                    // THICKNESS CHAOS
                    // =================================================

                    float thicknessNoise =
                        saturate
                        (
                            brush * 0.52 +
                            macroMixed * 0.33 +
                            detail * 0.15
                        );


                    float signedThickness =
                        thicknessNoise *
                        2.0 -
                        1.0;


                    float jitterMultiplier =
                        max
                        (
                            0.08,

                            1.0 +
                            signedThickness *
                            _ThicknessJitter
                        );


                    float jitteredThickness =
                        baseThickness *
                        jitterMultiplier;


                    jitteredThickness +=
                        chunkPulse *
                        _ChunkBoost;


                    // =================================================
                    // DIRECTION DATA
                    // =================================================

                    float directionLength =
                        max
                        (
                            length
                            (
                                sampleDirectionPixels
                            ),

                            0.0001
                        );


                    float2 sampleDirection =
                        sampleDirectionPixels /
                        directionLength;


                    // =================================================
                    // BRUSH FLICKS / SPIKES
                    // =================================================
                    // Random direction per stable world-space cell.
                    // No sin/cos: two hashes create the 2D direction.
                    // =================================================

                    float2 randomDirection =
                        float2
                        (
                            detail * 2.0 - 1.0,
                            detailB * 2.0 - 1.0
                        );


                    randomDirection /=
                        max
                        (
                            length
                            (
                                randomDirection
                            ),

                            0.001
                        );


                    float flickAlignment =
                        pow
                        (
                            saturate
                            (
                                dot
                                (
                                    sampleDirection,

                                    randomDirection
                                )
                            ),

                            max
                            (
                                _SpikeDirectionality,

                                1.0
                            )
                        );


                    float flickPresence =
                        smoothstep
                        (
                            _SpikeThreshold - 0.04,

                            _SpikeThreshold + 0.04,

                            saturate
                            (
                                detailB * 0.55 +
                                macroMixed * 0.45
                            )
                        );


                    float spikeExtension =
                        _SpikeLength *
                        flickPresence *
                        flickAlignment;


                    // =================================================
                    // PAINT DRIPS
                    // =================================================
                    // Current pixel is below the silhouette when the
                    // neighbour sample points UP toward the object.
                    // =================================================

                    float dripDirection =
                        pow
                        (
                            saturate
                            (
                                sampleDirection.y
                            ),

                            max
                            (
                                _DripDirectionality,

                                1.0
                            )
                        );


                    float dripPresence =
                        smoothstep
                        (
                            _DripThreshold - 0.035,

                            _DripThreshold + 0.035,

                            saturate
                            (
                                macroMixed * 0.65 +
                                detail * 0.35
                            )
                        );


                    float dripExtension =
                        _DripLength *
                        dripDirection *
                        dripPresence;


                    float effectiveThickness =
                        jitteredThickness +
                        spikeExtension +
                        dripExtension;


                    // =================================================
                    // CORE STROKE
                    // =================================================

                    if
                    (
                        sampleRadiusPixels <=
                        effectiveThickness
                    )
                    {
                        float normalizedRadius =
                            saturate
                            (
                                sampleRadiusPixels /
                                max
                                (
                                    effectiveThickness,

                                    0.001
                                )
                            );


                        // Protect the innermost part of the outline so
                        // the silhouette remains readable even with
                        // very aggressive breakup values.
                        float breakupInfluence =
                            smoothstep
                            (
                                saturate
                                (
                                    _CoreIntegrity
                                ),

                                1.0,

                                normalizedRadius
                            );


                        float breakupNoise =
                            saturate
                            (
                                brush * 0.58 +
                                macroMixed * 0.22 +
                                (1.0 - detail) * 0.20
                            );


                        float breakupSoftness =
                            max
                            (
                                _BreakupSoftness,

                                0.0001
                            );


                        float breakupKeep =
                            smoothstep
                            (
                                _BreakupThreshold -
                                breakupSoftness,

                                _BreakupThreshold +
                                breakupSoftness,

                                breakupNoise
                            );


                        float finalBreakup =
                            lerp
                            (
                                1.0,

                                breakupKeep,

                                saturate
                                (
                                    _BreakupStrength *
                                    breakupInfluence
                                )
                            );


                        float opacityNoise =
                            saturate
                            (
                                brush * 0.42 +
                                macroMixed * 0.23 +
                                detail * 0.35
                            );


                        float opacityFactor =
                            lerp
                            (
                                1.0,

                                lerp
                                (
                                    0.12,

                                    1.0,

                                    opacityNoise
                                ),

                                saturate
                                (
                                    _OpacityJitter
                                )
                            );


                        result.x =
                            baseEdge *
                            finalBreakup *
                            opacityFactor;


                        // =============================================
                        // NEON ACCENT STREAK
                        // =============================================

                        float accentNoise =
                            saturate
                            (
                                brush * 0.48 +
                                detailB * 0.32 +
                                chunkPulse * 0.20
                            );


                        float accentSoftness =
                            max
                            (
                                _AccentSoftness,

                                0.0001
                            );


                        float accentMask =
                            smoothstep
                            (
                                _AccentThreshold -
                                accentSoftness,

                                _AccentThreshold +
                                accentSoftness,

                                accentNoise
                            );


                        float outerBias =
                            lerp
                            (
                                1.0,

                                normalizedRadius,

                                saturate
                                (
                                    _AccentOuterBias
                                )
                            );


                        result.y =
                            baseEdge *
                            accentMask *
                            outerBias *
                            saturate
                            (
                                _AccentStrength
                            );


                        return result;
                    }


                    // =================================================
                    // OVERSPRAY / AEROSOL DUST
                    // =================================================

                    if
                    (
                        _OversprayStrength <=
                        0.0001
                    )
                    {
                        return result;
                    }


                    float oversprayWidth =
                        max
                        (
                            _OversprayWidth,

                            0.0001
                        );


                    if
                    (
                        sampleRadiusPixels >
                        effectiveThickness +
                        oversprayWidth
                    )
                    {
                        return result;
                    }


                    float oversprayNoise =
                        saturate
                        (
                            detail * 0.52 +
                            detailB * 0.28 +
                            brush * 0.20
                        );


                    float overspraySoftness =
                        max
                        (
                            _OverspraySoftness,

                            0.0001
                        );


                    float sprayMask =
                        smoothstep
                        (
                            _OversprayThreshold -
                            overspraySoftness,

                            _OversprayThreshold +
                            overspraySoftness,

                            oversprayNoise
                        );


                    float sprayDistance =
                        sampleRadiusPixels -
                        effectiveThickness;


                    float radialFade =
                        1.0 -
                        saturate
                        (
                            sprayDistance /
                            oversprayWidth
                        );


                    float spray =
                        baseEdge *
                        sprayMask *
                        radialFade *
                        saturate
                        (
                            _OversprayStrength
                        );


                    // Spray mostly uses the accent channel. A smaller
                    // amount stays in the main outline so the effect
                    // still works when Accent Strength = 0.
                    result.x =
                        spray *
                        0.35;


                    result.y =
                        spray *
                        saturate
                        (
                            _AccentStrength
                        );


                    return result;


                #endif
            }


            // ========================================================
            // EDGE TEST
            // ========================================================
            // EDGE TEST
            // ========================================================

            float2 EvaluateCombinedOutsideSample
            (
                float2 centerUV,

                SurfaceData center,

                float2 neighborUV,

                float sampleRadiusPixels,

                float2 texelSize,

                float2 localDepthGradient
            )
            {
                SurfaceData neighbor =
                    GetVisibleSurface
                    (
                        neighborUV
                    );


                float2 zeroEdge =
                    float2
                    (
                        0.0,
                        0.0
                    );


                if
                (
                    center.valid < 0.5
                    &&
                    neighbor.valid < 0.5
                )
                {
                    return zeroEdge;
                }


                if
                (
                    center.valid > 0.5
                    &&
                    neighbor.valid < 0.5
                )
                {
                    return zeroEdge;
                }


                float2 sampleDirectionPixels =
                    (
                        neighborUV -
                        centerUV
                    )
                    /
                    max
                    (
                        texelSize,

                        float2
                        (
                            0.0000001,
                            0.0000001
                        )
                    );


                // ====================================================
                // EMPTY -> SURFACE
                // ====================================================

                if
                (
                    center.valid < 0.5
                    &&
                    neighbor.valid > 0.5
                )
                {
                    float desiredThickness =
                        GetThicknessForDepth
                        (
                            neighbor.eyeDepth
                        );


                    return ApplyOutlineSurfaceStyle
                    (
                        1.0,

                        neighborUV,

                        neighbor,

                        sampleRadiusPixels,

                        desiredThickness,

                        sampleDirectionPixels
                    );
                }


                // ====================================================
                // SURFACE -> SURFACE
                // ====================================================

                float difference =
                    center.eyeDepth -
                    neighbor.eyeDepth;


                if
                (
                    difference <=
                    0.0
                )
                {
                    return zeroEdge;
                }


                float referenceDepth =
                    max
                    (
                        neighbor.eyeDepth,

                        0.001
                    );


                float relativeDifference =
                    difference /
                    referenceDepth;


                float2 deltaPixels =
                    sampleDirectionPixels;


                float predictedSlopeDifference =
                    dot
                    (
                        localDepthGradient,

                        deltaPixels
                    );


                predictedSlopeDifference =
                    max
                    (
                        predictedSlopeDifference,

                        0.0
                    );


                float compensatedDifference =
                    relativeDifference
                    -
                    predictedSlopeDifference *
                    _SlopeCompensation;


                compensatedDifference =
                    max
                    (
                        compensatedDifference,

                        0.0
                    );


                float depthEdge =
                    smoothstep
                    (
                        _DepthThreshold,

                        _DepthThreshold +
                        max
                        (
                            _DepthSoftness,

                            0.00001
                        ),

                        compensatedDifference
                    );


                if
                (
                    depthEdge <=
                    0.0001
                )
                {
                    return zeroEdge;
                }


                float desiredThickness =
                    GetThicknessForDepth
                    (
                        neighbor.eyeDepth
                    );


                return ApplyOutlineSurfaceStyle
                (
                    depthEdge,

                    neighborUV,

                    neighbor,

                    sampleRadiusPixels,

                    desiredThickness,

                    sampleDirectionPixels
                );
            }


            // ========================================================
            // ONE RING
            // ========================================================
            // ONE RING
            // ========================================================

            float2 SampleCombinedOutlineRing
            (
                float2 uv,

                SurfaceData center,

                float2 texelSize,

                float2 localDepthGradient,

                float radiusPixels
            )
            {
                float2 cardinal =
                    texelSize *
                    radiusPixels;


                float2 diagonal =
                    cardinal *
                    0.70710678;


                float2 edge =
                    float2
                    (
                        0.0,
                        0.0
                    );


                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + float2(-cardinal.x, 0), radiusPixels, texelSize, localDepthGradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + float2( cardinal.x, 0), radiusPixels, texelSize, localDepthGradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + float2(0,  cardinal.y), radiusPixels, texelSize, localDepthGradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + float2(0, -cardinal.y), radiusPixels, texelSize, localDepthGradient));

                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + float2(-diagonal.x,  diagonal.y), radiusPixels, texelSize, localDepthGradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + float2( diagonal.x,  diagonal.y), radiusPixels, texelSize, localDepthGradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + float2(-diagonal.x, -diagonal.y), radiusPixels, texelSize, localDepthGradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + float2( diagonal.x, -diagonal.y), radiusPixels, texelSize, localDepthGradient));


                return edge;
            }


            // ========================================================
            // COMPLETE OUTLINE
            // ========================================================
            // COMPLETE OUTLINE
            // ========================================================

            float2 GetCombinedDepthEdge
            (
                float2 uv
            )
            {
                float2 texelSize =
                    GetScreenTexelSize();


                SurfaceData center =
                    GetVisibleSurface
                    (
                        uv
                    );


                float2 localDepthGradient =
                    GetLocalRelativeDepthGradient
                    (
                        uv,

                        center,

                        texelSize
                    );


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


                #if defined(_OUTLINE_IMPERFECTIONS)


                    maxThickness *=
                        1.0 +
                        max
                        (
                            _ThicknessJitter,

                            0.0
                        );


                    maxThickness +=
                        max
                        (
                            _ChunkBoost,

                            0.0
                        );


                    maxThickness +=
                        max
                        (
                            _SpikeLength,

                            0.0
                        );


                    maxThickness +=
                        max
                        (
                            _DripLength,

                            0.0
                        );


                    maxThickness +=
                        max
                        (
                            _OversprayWidth,

                            0.0
                        );


                #endif


                float2 edge =
                    float2
                    (
                        0.0,
                        0.0
                    );


                [loop]
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

                            SampleCombinedOutlineRing
                            (
                                uv,

                                center,

                                texelSize,

                                localDepthGradient,

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
            // FRAGMENT
            // ========================================================

            float4 Frag
            (
                Varyings input
            ) : SV_Target
            {
                float2 uv =
                    input.texcoord;


                float4 sceneColor =
                    GetSceneColor
                    (
                        uv
                    );


                float2 outlineChannels =
                    GetCombinedDepthEdge
                    (
                        uv
                    );


                float coreEdge =
                    saturate
                    (
                        outlineChannels.x *
                        _OutlineOpacity
                    );


                float accentEdge =
                    saturate
                    (
                        outlineChannels.y *
                        _OutlineOpacity *
                        _AccentColor.a
                    );


                // Main paint stroke first.
                float3 finalColor =
                    lerp
                    (
                        sceneColor.rgb,

                        _OutlineColor.rgb,

                        coreEdge *
                        _OutlineColor.a
                    );


                // Neon graffiti is generated as a second channel during
                // the SAME edge search, so there is no second outline pass.
                finalColor =
                    lerp
                    (
                        finalColor,

                        _AccentColor.rgb,

                        accentEdge
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
