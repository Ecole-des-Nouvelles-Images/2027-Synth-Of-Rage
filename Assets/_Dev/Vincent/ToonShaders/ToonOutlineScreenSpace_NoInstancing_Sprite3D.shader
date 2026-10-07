Shader "Custom/Toon/DepthOutlineWorldMask-S3DC"
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
        // ============================================================
        // INCLUDES
        // ============================================================

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
            Name "Combined Mesh Sprite Depth Outline"


            // ========================================================
            // SPRITE STENCIL
            // ========================================================
            //
            // Ref 8 est écrit par Sprite3D/Lit.
            //
            // Le fullscreen ne dessine donc jamais SUR
            // les pixels visibles du sprite.
            //
            // Par contre il peut dessiner AUTOUR du sprite
            // grâce à _Sprite3DDepthTexture.
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


            // ========================================================
            // PROGRAM
            // ========================================================

            #pragma target 4.5

            #pragma vertex Vert

            #pragma fragment Frag


            // ========================================================
            // DEFERRED NORMALS
            // ========================================================

            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT


            // ========================================================
            // STATIC MATERIAL BOOL
            // ========================================================
            //
            // OFF :
            //
            // Mesh uniquement.
            //
            //
            // ON :
            //
            // Mesh + Sprite3D fusionnés AVANT
            // le calcul du contour.
            // ========================================================

            #pragma shader_feature_local_fragment _OUTLINE_SPRITES3D


            // ========================================================
            // QUALITY
            // ========================================================

            #define OUTLINE_RINGS 8


            // ========================================================
            // WORLD MASK
            // ========================================================

            Texture2D<float4> _OutlineMask;

            SamplerState sampler_OutlineMask;


            // ========================================================
            // SPRITE DEPTH
            // ========================================================
            //
            // Texture créée par Sprite3DDepthFeature.
            //
            // Rouge :
            //
            // 0 = proche caméra
            // 1 = far plane / pas de sprite
            // ========================================================

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


                float _MaskStrength;

                float _MaskWorldScale;

                float4 _MaskWorldOffset;

                float _MaskThreshold;

                float _MaskSoftness;

                float _MaskInvert;

                float _MaskProjectionBlend;

            CBUFFER_END


            // ========================================================
            // SURFACE DATA
            // ========================================================
            //
            // valid :
            //
            // 0 = aucune surface
            // 1 = surface
            //
            //
            // source :
            //
            // 0 = sky / rien
            // 1 = mesh
            // 2 = Sprite3D
            // ========================================================

            struct SurfaceData
            {
                float valid;

                float eyeDepth;

                float source;

                float rawDepth;
            };


            // ========================================================
            // SCREEN UV
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

                        float2
                        (
                            1.0,
                            1.0
                        )
                    );


                float2 texelSize =
                    1.0 /
                    screenSize;


                return clamp
                (
                    uv,

                    texelSize *
                    0.5,

                    1.0 -
                    texelSize *
                    0.5
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
            // BACKGROUND / SKY
            // ========================================================

            float IsBackground
            (
                float rawDepth
            )
            {
                #if UNITY_REVERSED_Z

                    // DX12 / reversed Z.
                    //
                    // Sky / far plane = proche de 0.

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
            // LINEAR EYE DEPTH
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
            // EYE DEPTH -> RAW DEPTH
            // ========================================================

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
            // WORLD POSITION FROM RAW DEPTH
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
            // WORLD POSITION FROM EYE DEPTH
            // ========================================================

            float3 GetWorldPositionFromEyeDepth
            (
                float2 uv,

                float eyeDepth
            )
            {
                float rawDepth =
                    EyeDepthToRawDepth
                    (
                        eyeDepth
                    );


                return GetWorldPosition
                (
                    uv,

                    rawDepth
                );
            }


            // ========================================================
            // SCENE NORMAL
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


                float lengthSq =
                    dot
                    (
                        normalWS,

                        normalWS
                    );


                if
                (
                    lengthSq <
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
            // SPRITE DEPTH
            // ========================================================

            float GetSpriteDepth01
            (
                float2 uv
            )
            {
                uv =
                    ClampScreenUV
                    (
                        uv
                    );


                return _Sprite3DDepthTexture.Sample
                (
                    sampler_Sprite3DDepthTexture,

                    uv
                ).r;
            }


            // ========================================================
            // COMBINED VISIBLE SURFACE
            // ========================================================
            //
            // C'est LE coeur du système.
            //
            // On fusionne :
            //
            // Camera Depth
            // +
            // Sprite3D Depth
            //
            // AVANT de calculer l'outline.
            //
            // La surface la plus proche gagne.
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
                // SPRITE 3D
                // ====================================================

                #if defined(_OUTLINE_SPRITES3D)


                    float spriteDepth01 =
                        GetSpriteDepth01
                        (
                            uv
                        );


                    // 1 = texture clear.
                    //
                    // Donc aucun sprite.

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


                        // Le sprite remplace le mesh uniquement
                        // s'il est devant.

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
            // DISTANCE -> THICKNESS
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
                // FALLOFF
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
            // WORLD MASK
            // ========================================================

            float GetWorldMask
            (
                float3 worldPosition,

                float3 normalWS
            )
            {
                // ====================================================
                // MASK DISABLED
                // ====================================================

                if
                (
                    _MaskStrength <=
                    0.0001
                )
                {
                    return 1.0;
                }


                // ====================================================
                // WORLD POSITION
                // ====================================================

                float3 p =
                    (
                        worldPosition +
                        _MaskWorldOffset.xyz
                    )
                    *
                    _MaskWorldScale;


                // ====================================================
                // TRIPLANAR WEIGHTS
                // ====================================================

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


                float weightSum =
                    weights.x +
                    weights.y +
                    weights.z;


                weights /=
                    max
                    (
                        weightSum,

                        0.0001
                    );


                // ====================================================
                // TRIPLANAR UV
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
                    maskX *
                    weights.x
                    +
                    maskY *
                    weights.y
                    +
                    maskZ *
                    weights.z;


                // ====================================================
                // INVERT
                // ====================================================

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
                // STRENGTH
                // ====================================================

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
            // SURFACE WORLD MASK
            // ========================================================

            float GetSurfaceWorldMask
            (
                float2 uv,

                SurfaceData surface
            )
            {
                if
                (
                    surface.valid <
                    0.5
                )
                {
                    return 1.0;
                }


                // ====================================================
                // MESH
                // ====================================================

                if
                (
                    surface.source <
                    1.5
                )
                {
                    float3 worldPosition =
                        GetWorldPosition
                        (
                            uv,

                            surface.rawDepth
                        );


                    float3 normalWS =
                        GetSceneNormal
                        (
                            uv
                        );


                    return GetWorldMask
                    (
                        worldPosition,

                        normalWS
                    );
                }


                // ====================================================
                // SPRITE 3D
                // ====================================================

                float3 spriteWorldPosition =
                    GetWorldPositionFromEyeDepth
                    (
                        uv,

                        surface.eyeDepth
                    );


                // Pour le masque triplanar du Sprite3D,
                // on utilise une normale orientée caméra.

                float3 spriteNormalWS =
                    normalize
                    (
                        _WorldSpaceCameraPos -
                        spriteWorldPosition
                    );


                return GetWorldMask
                (
                    spriteWorldPosition,

                    spriteNormalWS
                );
            }


            // ========================================================
            // COMBINED EDGE TEST
            // ========================================================

            float EvaluateCombinedOutsideSample
            (
                float2 centerUV,

                float2 neighborUV,

                float sampleRadiusPixels
            )
            {
                SurfaceData center =
                    GetVisibleSurface
                    (
                        centerUV
                    );


                SurfaceData neighbor =
                    GetVisibleSurface
                    (
                        neighborUV
                    );


                // ====================================================
                // EMPTY -> EMPTY
                // ====================================================
                //
                // Skybox contre skybox.
                //
                // Aucun outline.
                // ====================================================

                if
                (
                    center.valid <
                    0.5
                    &&
                    neighbor.valid <
                    0.5
                )
                {
                    return 0.0;
                }


                // ====================================================
                // SURFACE -> EMPTY
                // ====================================================
                //
                // Le pixel courant appartient à l'objet.
                //
                // On ne dessine pas ici.
                //
                // Ça garde le contour à l'extérieur.
                // ====================================================

                if
                (
                    center.valid >
                    0.5
                    &&
                    neighbor.valid <
                    0.5
                )
                {
                    return 0.0;
                }


                // ====================================================
                // EMPTY -> SURFACE
                // ====================================================
                //
                // Pixel courant = vide / ciel
                //
                // voisin = objet
                //
                // => silhouette extérieure.
                // ====================================================

                if
                (
                    center.valid <
                    0.5
                    &&
                    neighbor.valid >
                    0.5
                )
                {
                    float desiredThickness =
                        GetThicknessForDepth
                        (
                            neighbor.eyeDepth
                        );


                    if
                    (
                        sampleRadiusPixels >
                        desiredThickness
                    )
                    {
                        return 0.0;
                    }


                    float worldMask =
                        GetSurfaceWorldMask
                        (
                            neighborUV,

                            neighbor
                        );


                    return worldMask;
                }


                // ====================================================
                // SURFACE -> SURFACE
                // ====================================================
                //
                // Pour créer un contour extérieur :
                //
                // le voisin doit être plus proche.
                //
                //
                // Exemple :
                //
                // current  = bâtiment derrière
                //
                // neighbor = Sprite3D devant
                //
                // => contour du Sprite sur le bâtiment.
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
                    return 0.0;
                }


                // ====================================================
                // RELATIVE DEPTH DIFFERENCE
                // ====================================================

                float referenceDepth =
                    max
                    (
                        neighbor.eyeDepth,

                        0.001
                    );


                float relativeDifference =
                    difference /
                    referenceDepth;


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


                // ====================================================
                // DISTANCE THICKNESS
                // ====================================================

                float desiredThickness =
                    GetThicknessForDepth
                    (
                        neighbor.eyeDepth
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
                // WORLD MASK
                // ====================================================

                float worldMask =
                    GetSurfaceWorldMask
                    (
                        neighborUV,

                        neighbor
                    );


                return
                    depthEdge *
                    worldMask;
            }


            // ========================================================
            // SAMPLE ONE OUTLINE RING
            // ========================================================

            float SampleCombinedOutlineRing
            (
                float2 uv,

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

                        EvaluateCombinedOutsideSample
                        (
                            uv,

                            uvL,

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

                        EvaluateCombinedOutsideSample
                        (
                            uv,

                            uvR,

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

                        EvaluateCombinedOutsideSample
                        (
                            uv,

                            uvU,

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

                        EvaluateCombinedOutsideSample
                        (
                            uv,

                            uvD,

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

                        EvaluateCombinedOutsideSample
                        (
                            uv,

                            uvUL,

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

                        EvaluateCombinedOutsideSample
                        (
                            uv,

                            uvUR,

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

                        EvaluateCombinedOutsideSample
                        (
                            uv,

                            uvDL,

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

                        EvaluateCombinedOutsideSample
                        (
                            uv,

                            uvDR,

                            radiusPixels
                        )
                    );


                return edge;
            }


            // ========================================================
            // COMBINED DEPTH OUTLINE
            // ========================================================

            float GetCombinedDepthEdge
            (
                float2 uv
            )
            {
                float2 screenSize =
                    max
                    (
                        GetScaledScreenParams().xy,

                        float2
                        (
                            1.0,
                            1.0
                        )
                    );


                float2 texelSize =
                    1.0 /
                    screenSize;


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
                //
                // IMPORTANT DX12 :
                //
                // On utilise [loop] et PAS [unroll].
                //
                // SampleCombinedOutlineRing est maintenant lourd :
                //
                // - Camera Depth
                // - Sprite Depth
                // - Depth comparison
                // - World reconstruction
                // - Normals
                // - Triplanar mask
                //
                // Unroll * 8 faisait exploser le shader à la compile.
                // ====================================================

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

                                texelSize,

                                radiusPixels
                            )
                        );
                }


                // ====================================================
                // STRENGTH
                // ====================================================

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
                // SINGLE COMBINED OUTLINE
                // ====================================================
                //
                // IMPORTANT :
                //
                // Il n'y a PLUS :
                //
                // Mesh Outline
                // +
                // Sprite Outline
                //
                //
                // Tout passe par :
                //
                // GetVisibleSurface()
                //
                // puis par UNE SEULE détection de contour.
                // ====================================================

                float edge =
                    GetCombinedDepthEdge
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