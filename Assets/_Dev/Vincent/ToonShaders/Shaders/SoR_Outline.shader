Shader "Custom/SOR/Outline"
{
    Properties
    {
        [Header(Outline)]
        [Toggle(_OUTLINE_SPRITES3D)] _OutlineSprites3D("Outline Sprite 3D", Float) = 0
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness("Base Outline Thickness Pixels", Range(0.5, 50)) = 7
        _OutlineOpacity("Outline Opacity", Range(0, 1)) = 1

        [Header(Distance Thickness)]
        _NearThicknessMultiplier("Near Thickness Multiplier", Range(1, 6)) = 2.15
        _DistanceNear("Near Distance", Float) = 2
        _DistanceFar("Far Distance", Float) = 120
        _FarThicknessMultiplier("Far Thickness Multiplier", Range(0.1, 1)) = 0.52
        _DistanceFalloff("Distance Falloff", Range(0.1, 4)) = 1.15

        [Header(Depth Detection)]
        _DepthThreshold("Depth Threshold", Range(0.0001, 2.5)) = 0.01
        _DepthSoftness("Depth Softness", Range(0.0001, 0.2)) = 0.004
        _DepthStrength("Depth Strength", Range(0, 3)) = 1

        [Header(Depth Slope Compensation)]
        _SlopeCompensation("Slope Compensation", Range(0, 3)) = 1.25
        _SlopeSampleReject("Slope Sample Reject", Range(0.001, 0.25)) = 0.05

        [Header(Distance Style Control)]
        [Toggle] _UseDistanceDirtyFade("Enable Distance Style Control", Float) = 1
        _DirtyFadeStart("Micro Fade Start", Float) = 16
        _DirtyFadeEnd("Micro Fade End", Float) = 160
        _FarDirtyMin("Far Micro Minimum", Range(0, 1)) = 0.82
        _MacroFadeStart("Macro Fade Start", Float) = 10
        _MacroFadeEnd("Macro Fade End", Float) = 80
        _FarMacroMinimum("Far Macro Minimum", Range(0, 1)) = 0.16
        _NearGrandBoost("Near Grand Boost", Range(1, 3)) = 1.6
        _GrandBoostDistance("Grand Boost Distance", Float) = 18
        _FarGraffitiShellWidth("Far Graffiti Shell Width Pixels", Range(0, 6)) = 1.5
        _FarGraffitiShellStrength("Far Graffiti Shell Strength", Range(0, 1)) = 0.78

        [Header(Shibuya Punk Outline)]
        [Toggle(_OUTLINE_IMPERFECTIONS)] _UseOutlineImperfections("Enable Shibuya Punk Style", Float) = 0
        _ImperfectionPattern("Brush / Graffiti Pattern", 2D) = "gray" {}
        [Toggle(_IMPERFECTION_FULL_TRIPLANAR)] _ImperfectionFullTriplanar("Full Triplanar Pattern Higher Cost", Float) = 0
        _ImperfectionWorldScale("Brush World Scale", Range(0.01, 20)) = 0.95
        _ImperfectionWorldOffset("Brush World Offset", Vector) = (0, 0, 0, 0)
        _ImperfectionSeed("Random Seed", Float) = 13
        _ImperfectionContrast("Brush Contrast", Range(0.1, 12)) = 2.6
        _ImperfectionProjectionBlend("Triplanar Sharpness", Range(1, 16)) = 2

        [Header(Macro Shape)]
        _MacroScale("Macro Chunk Scale", Range(0.02, 2)) = 0.18
        _MacroStrength("Macro Chunk Strength", Range(0, 1)) = 0.72
        _ChunkBoost("Chunk Protrusion Pixels", Range(0, 50)) = 7
        _ChunkThreshold("Chunk Threshold", Range(0, 1)) = 0.58

        [Header(Thickness And Dry Brush)]
        _ThicknessJitter("Thickness Chaos", Range(0, 1.5)) = 0.58
        _BreakupStrength("Dry Brush Breakup", Range(0, 1)) = 0.42
        _BreakupThreshold("Breakup Threshold", Range(0, 1)) = 0.50
        _BreakupSoftness("Breakup Softness", Range(0.001, 0.5)) = 0.085
        _CoreIntegrity("Core Integrity", Range(0, 1)) = 0.74
        _OpacityJitter("Paint Opacity Chaos", Range(0, 1)) = 0.22

        [Header(Brush Flicks)]
        _SpikeLength("Brush Flick Length Pixels", Range(0, 20)) = 10
        _SpikeThreshold("Brush Flick Rarity", Range(0, 1)) = 0.58
        _SpikeDirectionality("Brush Flick Directionality", Range(1, 16)) = 6

        [Header(World Anchored Textured Paint Drips)]
        _DripTexture("Drip Shape / Atlas", 2D) = "black" {}
        _DripTextureColumns("Drip Atlas Columns", Range(1, 4)) = 1
        _DripTextureContrast("Drip Texture Contrast", Range(0.1, 8)) = 1
        _DripTextureThreshold("Drip Texture Threshold", Range(0, 1)) = 0.5
        _DripTextureSoftness("Drip Texture Softness", Range(0.001, 0.25)) = 0.035

        _DripTextureDilation("Drip Texture Dilation Texels", Range(0, 8)) = 2.5

        [Toggle] _DripTextureInvert("Invert Drip Texture", Float) = 0

        _DripLength("Drip Base Length Pixels", Range(2, 32)) = 12
        _DripMinLength("Drip Minimum Length Pixels", Range(2, 20)) = 9
        _DripSearchHeight("Drip Search Height Pixels", Range(8, 48)) = 28

        _DripWorldSpacing("Drip World Spacing", Range(0.1, 4)) = 0.75
        _DripWorldWidth("Drip World Anchor Width", Range(0.02, 1.5)) = 0.5
        _DripWidth("Drip Texture Width Multiplier", Range(0.5, 10)) = 4
        _DripFarWidthBoost("Drip Far Width Boost", Range(1, 8)) = 4

        _DripLengthVariation("Drip Length Variation", Range(0, 0.8)) = 0.50
        _DripLongChance("Long Drip Chance", Range(0, 1)) = 0.22
        _DripLongMultiplier("Long Drip Multiplier", Range(1, 20)) = 1.75

        _DripThreshold("Drip Rarity", Range(0, 1)) = 0.58
        _DripOpacity("Drip Opacity", Range(0, 1)) = 1

        [Header(Spray And Dust)]
        _OversprayStrength("Spray / Dust Strength", Range(0, 1)) = 0.14
        _OversprayWidth("Spray Width Pixels", Range(0, 12)) = 1.8
        _OversprayThreshold("Spray Density Threshold", Range(0, 1)) = 0.82
        _OverspraySoftness("Spray Softness", Range(0.001, 0.5)) = 0.07

        [Header(Graffiti Accent)]
        _AccentColor("Graffiti Accent Color", Color) = (1, 0.02, 0.38, 1)
        _AccentStrength("Graffiti Accent Strength", Range(0, 1)) = 0.42
        _AccentThreshold("Graffiti Accent Rarity", Range(0, 1)) = 0.73
        _AccentSoftness("Graffiti Accent Softness", Range(0.001, 0.3)) = 0.08
        _AccentOuterBias("Graffiti Accent Outer Bias", Range(0, 1)) = 0.82

        [Header(World Space Mask)]
        _OutlineMask("Outline Mask", 2D) = "white" {}
        _MaskStrength("Mask Strength", Range(0, 1)) = 0.10
        _MaskWorldScale("World Mask Scale", Range(0.01, 20)) = 0.22
        _MaskWorldOffset("World Mask Offset", Vector) = (0, 0, 0, 0)
        _MaskThreshold("Mask Threshold", Range(0, 1)) = 0.48
        _MaskSoftness("Mask Softness", Range(0.001, 0.5)) = 0.13
        _MaskInvert("Invert Mask", Range(0, 1)) = 0
        _MaskProjectionBlend("Triplanar Sharpness", Range(1, 16)) = 2
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

            #define OUTLINE_RINGS 8
            #define DRIP_SEARCH_STEPS 16

            Texture2D<float4> _OutlineMask;
            SamplerState sampler_OutlineMask;

            Texture2D<float4> _ImperfectionPattern;
            SamplerState sampler_ImperfectionPattern;

            Texture2D<float4> _DripTexture;
            SamplerState sampler_DripTexture;

            Texture2D<float4> _Sprite3DDepthTexture;
            SamplerState sampler_Sprite3DDepthTexture;

            CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float _OutlineSprites3D, _OutlineThickness, _OutlineOpacity;

            float _NearThicknessMultiplier, _DistanceNear, _DistanceFar;
            float _FarThicknessMultiplier, _DistanceFalloff;

            float _DepthThreshold, _DepthSoftness, _DepthStrength;
            float _SlopeCompensation, _SlopeSampleReject;

            float _UseDistanceDirtyFade;
            float _DirtyFadeStart, _DirtyFadeEnd, _FarDirtyMin;
            float _MacroFadeStart, _MacroFadeEnd, _FarMacroMinimum;
            float _NearGrandBoost, _GrandBoostDistance;
            float _FarGraffitiShellWidth, _FarGraffitiShellStrength;

            float _UseOutlineImperfections, _ImperfectionFullTriplanar;
            float _ImperfectionWorldScale;
            float4 _ImperfectionWorldOffset;
            float _ImperfectionSeed, _ImperfectionContrast, _ImperfectionProjectionBlend;

            float _MacroScale, _MacroStrength, _ChunkBoost, _ChunkThreshold;
            float _ThicknessJitter, _BreakupStrength, _BreakupThreshold;
            float _BreakupSoftness, _CoreIntegrity, _OpacityJitter;

            float _SpikeLength, _SpikeThreshold, _SpikeDirectionality;

            float _DripTextureColumns, _DripTextureContrast;
            float _DripTextureThreshold, _DripTextureSoftness, _DripTextureDilation, _DripTextureInvert;

            float _DripLength, _DripMinLength, _DripSearchHeight;
            float _DripWorldSpacing, _DripWorldWidth, _DripWidth, _DripFarWidthBoost;
            float _DripLengthVariation, _DripLongChance, _DripLongMultiplier;
            float _DripThreshold, _DripOpacity;

            float _OversprayStrength, _OversprayWidth;
            float _OversprayThreshold, _OverspraySoftness;

            float4 _AccentColor;
            float _AccentStrength, _AccentThreshold;
            float _AccentSoftness, _AccentOuterBias;

            float _MaskStrength, _MaskWorldScale;
            float4 _MaskWorldOffset;
            float _MaskThreshold, _MaskSoftness;
            float _MaskInvert, _MaskProjectionBlend;
            CBUFFER_END

            struct SurfaceData
            {
                float valid;
                float eyeDepth;
                float source;
                float rawDepth;
            };

            float2 GetScreenSize()
            {
                return max(GetScaledScreenParams().xy, float2(1, 1));
            }

            float2 GetScreenTexelSize()
            {
                return 1.0 / GetScreenSize();
            }

            float2 ClampScreenUV(float2 uv)
            {
                float2 t = GetScreenTexelSize();
                return clamp(uv, t * 0.5, 1.0 - t * 0.5);
            }

            float4 GetSceneColor(float2 uv)
            {
                return _BlitTexture.Sample(sampler_LinearClamp, ClampScreenUV(uv));
            }

            float GetRawDepth(float2 uv)
            {
                return SampleSceneDepth(ClampScreenUV(uv));
            }

            float IsBackground(float rawDepth)
            {
                #if UNITY_REVERSED_Z
                return 1.0 - step(0.000001, rawDepth);
                #else
                return step(0.999999, rawDepth);
                #endif
            }

            float GetEyeDepth(float rawDepth)
            {
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            float EyeDepthToRawDepth(float eyeDepth)
            {
                float d = max(eyeDepth, 0.00001);
                return saturate((rcp(d) - _ZBufferParams.w) / _ZBufferParams.z);
            }

            float3 GetWorldPosition(float2 uv, float rawDepth)
            {
                uv = ClampScreenUV(uv);
                float deviceDepth = rawDepth;

                #if !UNITY_REVERSED_Z
                deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif

                return ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
            }

            float3 GetWorldPositionFromEyeDepth(float2 uv, float eyeDepth)
            {
                return GetWorldPosition(uv, EyeDepthToRawDepth(eyeDepth));
            }

            float GetSpriteDepth01(float2 uv)
            {
                return _Sprite3DDepthTexture.Sample(sampler_Sprite3DDepthTexture, ClampScreenUV(uv)).r;
            }

            SurfaceData GetVisibleSurface(float2 uv)
            {
                uv = ClampScreenUV(uv);

                SurfaceData r;
                r.valid = 0;
                r.eyeDepth = 0;
                r.source = 0;
                r.rawDepth = 0;

                float raw = GetRawDepth(uv);

                if (IsBackground(raw) < 0.5)
                {
                    r.valid = 1;
                    r.eyeDepth = GetEyeDepth(raw);
                    r.source = 1;
                    r.rawDepth = raw;
                }

                #if defined(_OUTLINE_SPRITES3D)

                float spriteDepth01 = GetSpriteDepth01(uv);

                if (spriteDepth01 < 0.99999)
                {
                    float spriteEyeDepth = max(spriteDepth01 * _ProjectionParams.z, 0.0001);

                    if (r.valid < 0.5 || spriteEyeDepth < r.eyeDepth)
                    {
                        r.valid = 1;
                        r.eyeDepth = spriteEyeDepth;
                        r.source = 2;
                        r.rawDepth = EyeDepthToRawDepth(spriteEyeDepth);
                    }
                }

                #endif

                return r;
            }

            void GetMeshWorldSample(float2 uv, out float valid, out float3 positionWS)
            {
                float raw = GetRawDepth(uv);

                if (IsBackground(raw) > 0.5)
                {
                    valid = 0;
                    positionWS = float3(0, 0, 0);
                    return;
                }

                valid = 1;
                positionWS = GetWorldPosition(uv, raw);
            }

            float3 GetMeshGeometricNormalWS(float2 uv, float3 centerPosition)
            {
                float2 t = GetScreenTexelSize();

                float vl, vr, vu, vd;
                float3 pl, pr, pu, pd;

                GetMeshWorldSample(uv - float2(t.x, 0), vl, pl);
                GetMeshWorldSample(uv + float2(t.x, 0), vr, pr);
                GetMeshWorldSample(uv + float2(0, t.y), vu, pu);
                GetMeshWorldSample(uv - float2(0, t.y), vd, pd);

                float3 dr = pr - centerPosition;
                float3 dl = centerPosition - pl;
                float3 du = pu - centerPosition;
                float3 dd = centerPosition - pd;

                float rr = vr > 0.5 ? dot(dr, dr) : 1e20;
                float rl = vl > 0.5 ? dot(dl, dl) : 1e20;
                float ru = vu > 0.5 ? dot(du, du) : 1e20;
                float rd = vd > 0.5 ? dot(dd, dd) : 1e20;

                if (min(rr, rl) > 1e19 || min(ru, rd) > 1e19)
                    return normalize(_WorldSpaceCameraPos - centerPosition);

                float3 dx = rr < rl ? dr : dl;
                float3 dy = ru < rd ? du : dd;
                float3 n = cross(dx, dy);

                if (dot(n, n) < 0.00000001)
                    return normalize(_WorldSpaceCameraPos - centerPosition);

                n = normalize(n);

                if (dot(n, _WorldSpaceCameraPos - centerPosition) < 0)
                    n = -n;

                return n;
            }

            void GetSurfaceWorldData(float2 uv, SurfaceData surface, out float3 worldPosition, out float3 geometricNormalWS)
            {
                if (surface.source < 1.5)
                {
                    worldPosition = GetWorldPosition(uv, surface.rawDepth);
                    geometricNormalWS = GetMeshGeometricNormalWS(uv, worldPosition);
                    return;
                }

                worldPosition = GetWorldPositionFromEyeDepth(uv, surface.eyeDepth);
                geometricNormalWS = normalize(_WorldSpaceCameraPos - worldPosition);
            }

            float GetDistanceBlend01(float eyeDepth, float startDistance, float endDistance, float falloff)
            {
                float start = min(startDistance, endDistance);
                float end = max(startDistance, endDistance);
                float range = max(end - start, 0.001);
                float t = 1.0 - saturate((eyeDepth - start) / range);
                return pow(t, max(falloff, 0.01));
            }

            float GetNearGrandBoostFactor(float eyeDepth)
            {
                float t = 1.0 - saturate(eyeDepth / max(_GrandBoostDistance, 0.001));
                return lerp(1.0, max(_NearGrandBoost, 1.0), t);
            }

            void GetStyleDistanceFactors(
                float eyeDepth,
                out float microProximity,
                out float macroProximity,
                out float microFactor,
                out float macroFactor,
                out float nearGrandBoost)
            {
                if (_UseDistanceDirtyFade < 0.5)
                {
                    microProximity = 1;
                    macroProximity = 1;
                    microFactor = 1;
                    macroFactor = 1;
                    nearGrandBoost = 1;
                    return;
                }

                microProximity = GetDistanceBlend01(eyeDepth, _DirtyFadeStart, _DirtyFadeEnd, 1);
                macroProximity = GetDistanceBlend01(eyeDepth, _MacroFadeStart, _MacroFadeEnd, 1);
                microFactor = lerp(saturate(_FarDirtyMin), 1, microProximity);
                macroFactor = lerp(saturate(_FarMacroMinimum), 1, macroProximity);
                nearGrandBoost = GetNearGrandBoostFactor(eyeDepth);
            }

            float GetThicknessForDepth(float eyeDepth)
            {
                float proximity = GetDistanceBlend01(eyeDepth, _DistanceNear, _DistanceFar, _DistanceFalloff);
                float mult = lerp(
                    max(_FarThicknessMultiplier, 0.1),
                    max(_NearThicknessMultiplier, 1),
                    proximity
                );

                return _OutlineThickness * mult;
            }

            float GetWorldMask(float3 worldPosition, float3 normalWS, float strength)
            {
                if (strength <= 0.0001)
                    return 1;

                float3 p = (worldPosition + _MaskWorldOffset.xyz) * _MaskWorldScale;

                float3 w = pow(
                    max(abs(normalWS), float3(0.0001, 0.0001, 0.0001)),
                    max(_MaskProjectionBlend, 1)
                );

                w /= max(w.x + w.y + w.z, 0.0001);

                float x = _OutlineMask.Sample(sampler_OutlineMask, frac(p.zy)).r;
                float y = _OutlineMask.Sample(sampler_OutlineMask, frac(p.xz)).r;
                float z = _OutlineMask.Sample(sampler_OutlineMask, frac(p.xy)).r;

                float mask = x*w.x + y*w.y + z*w.z;
                mask = lerp(mask, 1-mask, saturate(_MaskInvert));

                float softness = max(_MaskSoftness, 0.0001);
                mask = smoothstep(_MaskThreshold - softness, _MaskThreshold + softness, mask);

                return lerp(1, mask, saturate(strength));
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x+p.y) * p.z);
            }

            float SampleImperfectionPatternDominantAxis(float3 p, float3 normalWS)
            {
                float3 axis = abs(normalWS);

                if (axis.x >= axis.y && axis.x >= axis.z)
                    return _ImperfectionPattern.Sample(sampler_ImperfectionPattern, frac(p.zy)).r;

                if (axis.y >= axis.z)
                    return _ImperfectionPattern.Sample(sampler_ImperfectionPattern, frac(p.xz)).r;

                return _ImperfectionPattern.Sample(sampler_ImperfectionPattern, frac(p.xy)).r;
            }

            float SampleImperfectionPatternTriplanar(float3 p, float3 normalWS)
            {
                float3 w = pow(
                    max(abs(normalWS), float3(0.0001, 0.0001, 0.0001)),
                    max(_ImperfectionProjectionBlend, 1)
                );

                w /= max(w.x+w.y+w.z, 0.0001);

                float x = _ImperfectionPattern.Sample(sampler_ImperfectionPattern, frac(p.zy)).r;
                float y = _ImperfectionPattern.Sample(sampler_ImperfectionPattern, frac(p.xz)).r;
                float z = _ImperfectionPattern.Sample(sampler_ImperfectionPattern, frac(p.xy)).r;

                return x*w.x + y*w.y + z*w.z;
            }

            float GetImperfectionPattern(float3 worldPosition, float3 normalWS)
            {
                #if !defined(_OUTLINE_IMPERFECTIONS)

                return 0.5;

                #else

                float seed = _ImperfectionSeed * 13.371;
                float3 offset = float3(seed, seed*0.37, seed*0.73);
                float3 p = (worldPosition + _ImperfectionWorldOffset.xyz + offset) * _ImperfectionWorldScale;

                float pattern;

                #if defined(_IMPERFECTION_FULL_TRIPLANAR)
                pattern = SampleImperfectionPatternTriplanar(p, normalWS);
                #else
                pattern = SampleImperfectionPatternDominantAxis(p, normalWS);
                #endif

                return saturate((pattern-0.5) * _ImperfectionContrast + 0.5);

                #endif
            }

            float GetMacroNoise(float3 worldPosition, float3 normalWS)
            {
                float seed = _ImperfectionSeed * 5.731;

                float3 p =
                    (worldPosition + _ImperfectionWorldOffset.xyz + float3(seed, seed*0.47, seed*0.81))
                    * max(_MacroScale, 0.001);

                #if defined(_IMPERFECTION_FULL_TRIPLANAR)
                return SampleImperfectionPatternTriplanar(p, normalWS);
                #else
                return SampleImperfectionPatternDominantAxis(p, normalWS);
                #endif
            }

            float GetSlopeCandidate(SurfaceData center, float2 sampleUV, float signValue)
            {
                SurfaceData sample = GetVisibleSurface(sampleUV);

                if (center.valid < 0.5 || sample.valid < 0.5)
                    return 1e20;

                if (abs(sample.source - center.source) > 0.25)
                    return 1e20;

                float refDepth = max(center.eyeDepth, 0.001);
                return ((center.eyeDepth - sample.eyeDepth) / refDepth) / signValue;
            }

            float SelectSlopeCandidate(float negativeValue, float positiveValue)
            {
                float na = abs(negativeValue);
                float pa = abs(positiveValue);
                float result = pa < na ? positiveValue : negativeValue;
                float best = min(na, pa);

                if (best > 1e10 || best > _SlopeSampleReject)
                    return 0;

                return result;
            }

            float2 GetLocalRelativeDepthGradient(float2 uv, SurfaceData center, float2 texel)
            {
                if (center.valid < 0.5)
                    return float2(0, 0);

                float l = GetSlopeCandidate(center, uv-float2(texel.x, 0), -1);
                float r = GetSlopeCandidate(center, uv+float2(texel.x, 0), 1);
                float d = GetSlopeCandidate(center, uv-float2(0, texel.y), -1);
                float u = GetSlopeCandidate(center, uv+float2(0, texel.y), 1);

                return float2(
                    SelectSlopeCandidate(l, r),
                    SelectSlopeCandidate(d, u)
                );
            }

            float2 ApplyOutlineSurfaceStyle(
                float baseEdge,
                float2 surfaceUV,
                SurfaceData surface,
                float radiusPixels,
                float baseThickness,
                float2 directionPixels)
            {
                float3 worldPosition;
                float3 normalWS;

                GetSurfaceWorldData(surfaceUV, surface, worldPosition, normalWS);

                float microProximity;
                float macroProximity;
                float microFactor;
                float macroFactor;
                float nearGrandBoost;

                GetStyleDistanceFactors(
                    surface.eyeDepth,
                    microProximity,
                    macroProximity,
                    microFactor,
                    macroFactor,
                    nearGrandBoost
                );

                float farAmount = 1 - microProximity;

                float effectiveMask = _MaskStrength * lerp(0.82, 1, microFactor);
                float effectiveMacro = _MacroStrength * macroFactor;
                float effectiveChunk = _ChunkBoost * macroFactor * nearGrandBoost;

                float effectiveJitter =
                    _ThicknessJitter *
                    lerp(0.72, 1, microFactor) *
                    lerp(1, nearGrandBoost, 0.2);

                float effectiveBreakup = _BreakupStrength * lerp(0.85, 1, microFactor);
                float effectiveOpacity = _OpacityJitter * lerp(0.82, 1, microFactor);
                float effectiveSpike = _SpikeLength * macroFactor * nearGrandBoost;

                float effectiveSprayWidth =
                    _OversprayWidth *
                    lerp(0.55, 1, macroProximity) *
                    lerp(1, nearGrandBoost, 0.2);

                float effectiveSprayStrength =
                    _OversprayStrength *
                    lerp(0.25, 1, macroFactor) *
                    lerp(1, nearGrandBoost, 0.35);

                float effectiveAccent =
                    _AccentStrength *
                    lerp(0.78, 1, microFactor) *
                    lerp(1, nearGrandBoost, 0.25);

                float coreIntegrity = lerp(
                    saturate(_CoreIntegrity + 0.10),
                    _CoreIntegrity,
                    microProximity
                );

                float breakupThreshold = lerp(
                    saturate(_BreakupThreshold + 0.025),
                    _BreakupThreshold,
                    microProximity
                );

                float breakupSoftness = lerp(
                    min(max(_BreakupSoftness * 1.25, 0.001), 0.5),
                    _BreakupSoftness,
                    microProximity
                );

                float chunkThreshold = lerp(
                    saturate(_ChunkThreshold + 0.15),
                    _ChunkThreshold,
                    macroProximity
                );

                float spikeThreshold = lerp(
                    saturate(_SpikeThreshold + 0.12),
                    _SpikeThreshold,
                    macroProximity
                );

                float sprayThreshold = lerp(
                    saturate(_OversprayThreshold + 0.10),
                    _OversprayThreshold,
                    macroProximity
                );

                baseEdge *= GetWorldMask(worldPosition, normalWS, effectiveMask);

                float2 result = float2(0, 0);

                #if !defined(_OUTLINE_IMPERFECTIONS)

                if (radiusPixels <= baseThickness)
                    result.x = baseEdge;

                return result;

                #else

                float brush = GetImperfectionPattern(worldPosition, normalWS);
                float macro = GetMacroNoise(worldPosition, normalWS);

                float scale = max(_ImperfectionWorldScale * 3, 0.01);

                float3 hp = floor(
                    worldPosition * scale +
                    float3(
                        _ImperfectionSeed,
                        _ImperfectionSeed * 1.37,
                        _ImperfectionSeed * 2.11
                    )
                );

                float a = Hash31(hp);
                float b = Hash31(hp + float3(19.19, 7.73, 31.41));
                float c = Hash31(hp + float3(5.71, 27.13, 11.89));

                float dirLength = max(length(directionPixels), 0.0001);
                float2 inward = directionPixels / dirLength;
                float2 outward = -inward;

                float2 randomDirection = float2(a*2-1, b*2-1);
                randomDirection /= max(length(randomDirection), 0.001);

                float macroMixed =
                    saturate(
                        lerp(0.5, macro, saturate(effectiveMacro)) * 0.72 +
                        brush * 0.28
                    );

                float chunkPulse =
                    smoothstep(
                        chunkThreshold - 0.10,
                        chunkThreshold + 0.08,
                        macroMixed
                    );

                chunkPulse *= lerp(0.40, 1, macroFactor);

                float thicknessNoise =
                    saturate(
                        brush * 0.48 +
                        macroMixed * 0.32 +
                        c * 0.20
                    );

                float signedThickness = thicknessNoise * 2 - 1;

                float coreThickness =
                    baseThickness *
                    max(
                        0.72,
                        1 + signedThickness * (effectiveJitter * 0.24)
                    );

                coreThickness += chunkPulse * effectiveChunk * 0.16;
                coreThickness = max(coreThickness, 0.5);

                float microNoise =
                    saturate(
                        brush * 0.58 +
                        a * 0.17 +
                        (1-b) * 0.14 +
                        c * 0.11
                    );

                float microPattern = smoothstep(0.28, 0.76, microNoise);

                float microWidth =
                    max(baseThickness * 0.16, 0.35) +
                    _FarGraffitiShellWidth * lerp(0.22, 1, farAmount);

                microWidth *= lerp(0.72, 1, microPattern);

                float microReach = coreThickness + microWidth;

                float microMask =
                    1 -
                    smoothstep(
                        microReach - 0.32,
                        microReach + 0.24,
                        radiusPixels
                    );

                float outsideCore =
                    smoothstep(
                        coreThickness - 0.15,
                        coreThickness + 0.40,
                        radiusPixels
                    );

                microMask *= outsideCore;

                float microStrength =
                    lerp(0.42, _FarGraffitiShellStrength, farAmount) *
                    microFactor;

                float microBreak = lerp(0.35, 1, microPattern);
                float microPresence = microMask * microBreak * microStrength;

                float chunkReach = effectiveChunk * 1.65 * chunkPulse;
                float organicReach = coreThickness + chunkReach;

                float spikeNoise =
                    saturate(
                        brush * 0.24 +
                        macroMixed * 0.31 +
                        b * 0.45
                    );

                float spikePresence =
                    smoothstep(
                        spikeThreshold - 0.055,
                        spikeThreshold + 0.025,
                        spikeNoise
                    );

                spikePresence *= spikePresence;

                float alignment =
                    saturate(
                        dot(outward, randomDirection) * 0.5 + 0.5
                    );

                float outsideDistance = max(radiusPixels - coreThickness, 0);

                float spikeReach =
                    max(
                        effectiveSpike *
                        2.5 *
                        spikePresence *
                        lerp(0.75, 1.35, chunkPulse),
                        0.0001
                    );

                float spike01 = saturate(outsideDistance / spikeReach);

                float angularPower =
                    lerp(1.5, 14, spike01) *
                    max(_SpikeDirectionality * 0.35, 0.5);

                float spikeShape =
                    spikePresence *
                    pow(alignment, angularPower) *
                    (1 - smoothstep(0.78, 1.02, spike01));

                float shellSoftness =
                    max(
                        0.55,
                        min(1.5, baseThickness * 0.12)
                    );

                float organicShell =
                    1 -
                    smoothstep(
                        organicReach - shellSoftness,
                        organicReach + shellSoftness * 0.35,
                        radiusPixels
                    );

                if (radiusPixels <= coreThickness)
                {
                    float core01 =
                        saturate(
                            radiusPixels /
                            max(coreThickness, 0.001)
                        );

                    float opacityNoise =
                        saturate(
                            brush * 0.55 +
                            macroMixed * 0.18 +
                            a * 0.27
                        );

                    float opacity =
                        lerp(
                            1,
                            lerp(0.76, 1, opacityNoise),
                            saturate(effectiveOpacity * 0.28)
                        );

                    result.x = baseEdge * opacity;

                    float accentNoise =
                        saturate(
                            brush * 0.35 +
                            b * 0.35 +
                            chunkPulse * 0.30
                        );

                    float soft = max(_AccentSoftness, 0.0001);

                    float accentMask =
                        smoothstep(
                            _AccentThreshold - soft,
                            _AccentThreshold + soft,
                            accentNoise
                        );

                    float accentBias =
                        lerp(
                            1,
                            core01,
                            saturate(_AccentOuterBias)
                        );

                    result.y =
                        baseEdge *
                        accentMask *
                        accentBias *
                        saturate(effectiveAccent) *
                        0.22;

                    return result;
                }

                float shellWidth =
                    max(
                        organicReach - coreThickness,
                        0.001
                    );

                float shell01 =
                    saturate(
                        outsideDistance /
                        shellWidth
                    );

                float breakupNoise =
                    saturate(
                        brush * 0.42 +
                        macroMixed * 0.27 +
                        (1-a) * 0.18 +
                        b * 0.13
                    );

                float torn =
                    smoothstep(
                        breakupThreshold - breakupSoftness,
                        breakupThreshold + breakupSoftness,
                        breakupNoise
                    );

                float tornMask =
                    lerp(
                        1,
                        torn,
                        saturate(effectiveBreakup)
                    );

                tornMask =
                    lerp(
                        tornMask,
                        1,
                        saturate(
                            (1-shell01) *
                            coreIntegrity
                        )
                    );

                float shellPresence =
                    organicShell *
                    tornMask;

                shellPresence = max(shellPresence, spikeShape);
                shellPresence = max(shellPresence, microPresence);

                if (shellPresence > 0.0001)
                {
                    float opacityNoise =
                        saturate(
                            brush * 0.34 +
                            macroMixed * 0.27 +
                            c * 0.39
                        );

                    float macroOpacity =
                        lerp(
                            0.20,
                            1,
                            opacityNoise
                        );

                    float microOpacity =
                        lerp(
                            0.48,
                            1,
                            microPattern
                        );

                    float opacity =
                        lerp(
                            microOpacity,
                            macroOpacity,
                            macroFactor
                        );

                    opacity =
                        lerp(
                            opacity,
                            1,
                            1 - saturate(effectiveOpacity)
                        );

                    result.x =
                        baseEdge *
                        shellPresence *
                        opacity;

                    float accentNoise =
                        saturate(
                            brush * 0.36 +
                            b * 0.28 +
                            chunkPulse * 0.16 +
                            spikeShape * 0.20
                        );

                    float soft = max(_AccentSoftness, 0.0001);

                    float accentMask =
                        smoothstep(
                            _AccentThreshold - soft,
                            _AccentThreshold + soft,
                            accentNoise
                        );

                    float accentBias =
                        lerp(
                            1,
                            max(shell01, spike01),
                            saturate(_AccentOuterBias)
                        );

                    result.y =
                        baseEdge *
                        shellPresence *
                        accentMask *
                        accentBias *
                        saturate(effectiveAccent) *
                        lerp(0.62, 1, microFactor);

                    return result;
                }

                if (effectiveSprayStrength <= 0.0001)
                    return result;

                float outerReach =
                    max(
                        microReach,
                        max(
                            organicReach,
                            coreThickness + spikeReach
                        )
                    );

                float sprayWidth = max(effectiveSprayWidth, 0.0001);

                if (radiusPixels > outerReach + sprayWidth)
                    return result;

                float sprayDistance =
                    max(
                        radiusPixels - outerReach,
                        0
                    );

                float radialFade =
                    1 -
                    saturate(
                        sprayDistance /
                        sprayWidth
                    );

                float sprayNoise =
                    saturate(
                        a * 0.43 +
                        b * 0.27 +
                        brush * 0.18 +
                        macroMixed * 0.12
                    );

                float soft = max(_OverspraySoftness, 0.0001);

                float sprayMask =
                    smoothstep(
                        sprayThreshold - soft,
                        sprayThreshold + soft,
                        sprayNoise
                    );

                float spray =
                    baseEdge *
                    radialFade *
                    sprayMask *
                    saturate(effectiveSprayStrength);

                result.x = spray * 0.18;

                result.y =
                    spray *
                    saturate(effectiveAccent) *
                    0.65;

                return result;

                #endif
            }

            float GetStableDripWorldCoordinate(float3 worldPosition)
            {
                return
                    worldPosition.x * 0.754877666 +
                    worldPosition.z * 0.655866146 +
                    worldPosition.y * 0.173205081;
            }

            float GetDownwardBoundaryStrength(SurfaceData lowerSurface, SurfaceData upperSurface)
            {
                if (upperSurface.valid < 0.5)
                    return 0;

                if (lowerSurface.valid < 0.5)
                    return 1;

                float difference =
                    lowerSurface.eyeDepth -
                    upperSurface.eyeDepth;

                if (difference <= 0)
                    return 0;

                float relativeDifference =
                    difference /
                    max(
                        upperSurface.eyeDepth,
                        0.001
                    );

                return smoothstep(
                    _DepthThreshold,
                    _DepthThreshold +
                    max(_DepthSoftness, 0.00001),
                    relativeDifference
                );
            }

            float SampleDripAtlasMask(float2 localUV, float variant, float columns)
            {
                localUV = clamp(localUV, float2(0.001, 0.001), float2(0.999, 0.999));

                float2 atlasUV =
                    float2(
                        (variant + localUV.x) / columns,
                        localUV.y
                    );

                float4 s =
                    _DripTexture.Sample(
                        sampler_DripTexture,
                        atlasUV
                    );

                return min(s.r, s.a);
            }

            float GetDripTextureMask(float2 localUV, float variantHash, float microProximity)
            {
                if (
                    localUV.x <= 0.0 ||
                    localUV.x >= 1.0 ||
                    localUV.y <= 0.0 ||
                    localUV.y >= 1.0
                )
                    return 0.0;

                float columns =
                    clamp(
                        floor(_DripTextureColumns + 0.5),
                        1.0,
                        4.0
                    );

                float variant =
                    min(
                        floor(saturate(variantHash) * columns),
                        columns - 1.0
                    );

                uint texWidth, texHeight;
                _DripTexture.GetDimensions(texWidth, texHeight);

                float localTexelX =
                    columns /
                    max((float)texWidth, 1.0);

                float dilation =
                    _DripTextureDilation *
                    lerp(
                        1.65,
                        1.0,
                        microProximity
                    );

                float dx = localTexelX * dilation;

                float mask =
                    SampleDripAtlasMask(
                        localUV,
                        variant,
                        columns
                    );

                mask =
                    max(
                        mask,
                        SampleDripAtlasMask(
                            localUV + float2(dx, 0),
                            variant,
                            columns
                        )
                    );

                mask =
                    max(
                        mask,
                        SampleDripAtlasMask(
                            localUV - float2(dx, 0),
                            variant,
                            columns
                        )
                    );

                mask =
                    lerp(
                        mask,
                        1.0 - mask,
                        saturate(_DripTextureInvert)
                    );

                mask =
                    saturate(
                        (mask - 0.5) *
                        max(_DripTextureContrast, 0.0001) +
                        0.5
                    );

                float softness =
                    max(
                        _DripTextureSoftness,
                        0.0001
                    );

                return smoothstep(
                    _DripTextureThreshold - softness,
                    _DripTextureThreshold + softness,
                    mask
                );
            }

            float2 GetVerticalDripEdge(float2 uv, SurfaceData currentSurface, float2 texelSize)
            {
                float2 result = float2(0, 0);

                #if !defined(_OUTLINE_IMPERFECTIONS)

                return result;

                #else

                float searchHeight = max(_DripSearchHeight, 1.0);
                float stepPixels = searchHeight / (float)DRIP_SEARCH_STEPS;

                SurfaceData lowerSurface = currentSurface;

                [loop]
                for (int stepIndex = 1; stepIndex <= DRIP_SEARCH_STEPS; stepIndex++)
                {
                    float verticalDistancePixels =
                        stepPixels *
                        (float)stepIndex;

                    float2 upperUV =
                        ClampScreenUV(
                            uv +
                            float2(
                                0,
                                texelSize.y *
                                verticalDistancePixels
                            )
                        );

                    SurfaceData upperSurface =
                        GetVisibleSurface(upperUV);

                    float boundaryStrength =
                        GetDownwardBoundaryStrength(
                            lowerSurface,
                            upperSurface
                        );

                    if (boundaryStrength > 0.0001)
                    {
                        if (currentSurface.valid > 0.5)
                        {
                            float occlusionMargin =
                                max(
                                    upperSurface.eyeDepth * 0.008,
                                    0.015
                                );

                            if (
                                currentSurface.eyeDepth <
                                upperSurface.eyeDepth -
                                occlusionMargin
                            )
                                return result;
                        }

                        float3 anchorPositionWS;
                        float3 anchorNormalWS;

                        GetSurfaceWorldData(
                            upperUV,
                            upperSurface,
                            anchorPositionWS,
                            anchorNormalWS
                        );

                        float stableCoordinate =
                            GetStableDripWorldCoordinate(anchorPositionWS);

                        float spacing =
                            max(_DripWorldSpacing, 0.01);

                        float normalizedCoordinate =
                            stableCoordinate /
                            spacing +
                            _ImperfectionSeed *
                            0.6180339887;

                        float cellID = floor(normalizedCoordinate);
                        float localCoordinate = frac(normalizedCoordinate);

                        float cellHashA =
                            Hash31(
                                float3(
                                    cellID,
                                    _ImperfectionSeed * 0.713,
                                    17.173
                                )
                            );

                        float cellHashB =
                            Hash31(
                                float3(
                                    cellID + 13.731,
                                    _ImperfectionSeed * 1.917,
                                    41.113
                                )
                            );

                        float cellHashC =
                            Hash31(
                                float3(
                                    cellID + 71.331,
                                    _ImperfectionSeed * 0.317,
                                    9.817
                                )
                            );

                        float cellHashD =
                            Hash31(
                                float3(
                                    cellID + 113.71,
                                    _ImperfectionSeed * 2.137,
                                    37.19
                                )
                            );

                        float anchorCenter =
                            lerp(
                                0.18,
                                0.82,
                                cellHashB
                            );

                        float signedDistanceToAnchor =
                            localCoordinate -
                            anchorCenter;

                        float spawn =
                            step(
                                _DripThreshold,
                                cellHashA
                            );

                        if (spawn <= 0.0001)
                            return result;

                        float microProximity;
                        float macroProximity;
                        float microFactor;
                        float macroFactor;
                        float nearGrandBoost;

                        GetStyleDistanceFactors(
                            upperSurface.eyeDepth,
                            microProximity,
                            macroProximity,
                            microFactor,
                            macroFactor,
                            nearGrandBoost
                        );

                        float lengthVariation =
                            lerp(
                                1.0 - _DripLengthVariation,
                                1.0 + _DripLengthVariation,
                                cellHashB
                            );

                        float longDrip =
                            step(
                                1.0 - _DripLongChance,
                                cellHashD
                            );

                        float longMultiplier =
                            lerp(
                                1.0,
                                max(_DripLongMultiplier, 1.0),
                                longDrip
                            );

                        float dripLength =
                            _DripLength *
                            lengthVariation *
                            longMultiplier;

                        dripLength =
                            max(
                                dripLength,
                                _DripMinLength
                            );

                        dripLength *=
                            lerp(
                                0.90,
                                1.0,
                                microFactor
                            );

                        dripLength *=
                            lerp(
                                1.0,
                                nearGrandBoost,
                                0.15
                            );

                        dripLength =
                            min(
                                dripLength,
                                searchHeight
                            );

                        float vertical01 =
                            verticalDistancePixels /
                            max(dripLength, 0.001);

                        float textureV =
                            1.0 -
                            vertical01;

                        // Width conservé en world-space, mais amplifié
                        // légèrement en screen-space pour survivre à l'AA.
                        float farWidthBoost =
                            lerp(
                                max(_DripFarWidthBoost, 1.0),
                                1.0,
                                microProximity
                            );

                        float aaWidthBoost =
                            lerp(
                                1.55,
                                1.12,
                                microProximity
                            );

                        float halfWidth =
                            _DripWorldWidth *
                            _DripWidth *
                            farWidthBoost *
                            aaWidthBoost;

                        // Ancienne version : max ~0.48.
                        // Cette limite annulait presque les sliders élevés.
                        halfWidth =
                            clamp(
                                halfWidth,
                                0.04,
                                1.25
                            );

                        float textureU =
                            0.5 +
                            signedDistanceToAnchor /
                            max(
                                halfWidth * 2.0,
                                0.0001
                            );

                        float2 dripUV =
                            float2(
                                textureU,
                                textureV
                            );

                        float textureMask =
                            GetDripTextureMask(
                                dripUV,
                                cellHashC,
                                microProximity
                            );

                        if (textureMask <= 0.0001)
                            return result;

                        float dripShape =
                            boundaryStrength *
                            spawn *
                            textureMask;

                        float maskStrength =
                            _MaskStrength *
                            lerp(
                                0.82,
                                1.0,
                                microFactor
                            );

                        dripShape *=
                            GetWorldMask(
                                anchorPositionWS,
                                anchorNormalWS,
                                maskStrength
                            );

                        dripShape *= _DripOpacity;

                        result.x =
                            saturate(dripShape);

                        float accentSoftness =
                            max(
                                _AccentSoftness,
                                0.0001
                            );

                        float accentMask =
                            smoothstep(
                                _AccentThreshold - accentSoftness,
                                _AccentThreshold + accentSoftness,
                                cellHashC
                            );

                        float tipBias =
                            lerp(
                                0.70,
                                1.20,
                                saturate(vertical01)
                            );

                        result.y =
                            saturate(
                                dripShape *
                                accentMask *
                                _AccentStrength *
                                tipBias *
                                lerp(
                                    0.68,
                                    1.0,
                                    microFactor
                                )
                            );

                        return result;
                    }

                    lowerSurface = upperSurface;
                }

                return result;

                #endif
            }

            float2 EvaluateCombinedOutsideSample(
                float2 centerUV,
                SurfaceData center,
                float2 neighborUV,
                float radiusPixels,
                float2 texel,
                float2 localGradient)
            {
                SurfaceData neighbor =
                    GetVisibleSurface(neighborUV);

                float2 zero = float2(0, 0);

                if (
                    center.valid < 0.5 &&
                    neighbor.valid < 0.5
                )
                    return zero;

                if (
                    center.valid > 0.5 &&
                    neighbor.valid < 0.5
                )
                    return zero;

                float2 directionPixels =
                    (neighborUV - centerUV) /
                    max(
                        texel,
                        float2(
                            0.0000001,
                            0.0000001
                        )
                    );

                if (
                    center.valid < 0.5 &&
                    neighbor.valid > 0.5
                )
                {
                    return ApplyOutlineSurfaceStyle(
                        1,
                        neighborUV,
                        neighbor,
                        radiusPixels,
                        GetThicknessForDepth(neighbor.eyeDepth),
                        directionPixels
                    );
                }

                float difference =
                    center.eyeDepth -
                    neighbor.eyeDepth;

                if (difference <= 0)
                    return zero;

                float relative =
                    difference /
                    max(
                        neighbor.eyeDepth,
                        0.001
                    );

                float slope =
                    max(
                        dot(
                            localGradient,
                            directionPixels
                        ),
                        0
                    );

                float compensated =
                    max(
                        relative -
                        slope *
                        _SlopeCompensation,
                        0
                    );

                float depthEdge =
                    smoothstep(
                        _DepthThreshold,
                        _DepthThreshold +
                        max(
                            _DepthSoftness,
                            0.00001
                        ),
                        compensated
                    );

                if (depthEdge <= 0.0001)
                    return zero;

                return ApplyOutlineSurfaceStyle(
                    depthEdge,
                    neighborUV,
                    neighbor,
                    radiusPixels,
                    GetThicknessForDepth(neighbor.eyeDepth),
                    directionPixels
                );
            }

            float2 RotateOutlineDirection(float2 d, float s, float c)
            {
                return float2(
                    d.x*c-d.y*s,
                    d.x*s+d.y*c
                );
            }

            float2 SampleCombinedOutlineRing(
                float2 uv,
                SurfaceData center,
                float2 texel,
                float2 gradient,
                float radius,
                float rotation)
            {
                float s = sin(rotation);
                float c = cos(rotation);

                const float D = 0.70710678;

                float2 d0 = RotateOutlineDirection(float2(1, 0), s, c);
                float2 d1 = RotateOutlineDirection(float2(D, D), s, c);
                float2 d2 = RotateOutlineDirection(float2(0, 1), s, c);
                float2 d3 = RotateOutlineDirection(float2(-D, D), s, c);
                float2 d4 = RotateOutlineDirection(float2(-1, 0), s, c);
                float2 d5 = RotateOutlineDirection(float2(-D, -D), s, c);
                float2 d6 = RotateOutlineDirection(float2(0, -1), s, c);
                float2 d7 = RotateOutlineDirection(float2(D, -D), s, c);

                float2 edge = float2(0, 0);

                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + d0 * texel * radius, radius, texel, gradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + d1 * texel * radius, radius, texel, gradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + d2 * texel * radius, radius, texel, gradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + d3 * texel * radius, radius, texel, gradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + d4 * texel * radius, radius, texel, gradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + d5 * texel * radius, radius, texel, gradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + d6 * texel * radius, radius, texel, gradient));
                edge = max(edge, EvaluateCombinedOutsideSample(uv, center, uv + d7 * texel * radius, radius, texel, gradient));

                return edge;
            }

            float2 GetCombinedDepthEdge(float2 uv)
            {
                float2 texel = GetScreenTexelSize();
                SurfaceData center = GetVisibleSurface(uv);

                float2 gradient =
                    GetLocalRelativeDepthGradient(
                        uv,
                        center,
                        texel
                    );

                float maximumBase =
                    max(
                        _OutlineThickness *
                        max(
                            _NearThicknessMultiplier,
                            1
                        ),
                        0.5
                    );

                float maxThickness = maximumBase;

                #if defined(_OUTLINE_IMPERFECTIONS)

                float grand = max(_NearGrandBoost, 1);
                float maxJitter = _ThicknessJitter * lerp(1, grand, 0.20);
                float maxChunk = _ChunkBoost * grand;
                float maxSpike = _SpikeLength * grand;
                float maxSpray = _OversprayWidth * lerp(1, grand, 0.20);

                float maxCore =
                    maximumBase *
                    (
                        1 +
                        max(
                            maxJitter * 0.24,
                            0
                        )
                    );

                maxCore += max(maxChunk * 0.16, 0);

                float chunkReach = max(maxChunk * 1.65, 0);
                float spikeReach = max(maxSpike * 2.5 * 1.35, 0);
                float microReach = max(_FarGraffitiShellWidth, 0);

                maxThickness =
                    maxCore +
                    max(
                        microReach,
                        max(
                            chunkReach,
                            spikeReach
                        )
                    )
                    +
                    max(maxSpray, 0);

                #endif

                float2 edge = float2(0, 0);

                [loop]
                for (int ring = 1; ring <= OUTLINE_RINGS; ring++)
                {
                    float ring01 =
                        (float)ring /
                        (float)OUTLINE_RINGS;

                    float radius =
                        maxThickness *
                        ring01 *
                        ring01;

                    float rotation =
                        (float)(ring-1) *
                        0.2399827721;

                    edge =
                        max(
                            edge,
                            SampleCombinedOutlineRing(
                                uv,
                                center,
                                texel,
                                gradient,
                                radius,
                                rotation
                            )
                        );
                }

                #if defined(_OUTLINE_IMPERFECTIONS)

                float2 dripEdge =
                    GetVerticalDripEdge(
                        uv,
                        center,
                        texel
                    );

                edge = max(edge, dripEdge);

                #endif

                edge *= _DepthStrength;

                return saturate(edge);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float4 sceneColor =
                    GetSceneColor(uv);

                float2 channels =
                    GetCombinedDepthEdge(uv);

                float mainEdge =
                    saturate(
                        channels.x *
                        _OutlineOpacity
                    );

                float accentEdge =
                    saturate(
                        channels.y *
                        _OutlineOpacity *
                        _AccentColor.a
                    );

                float3 finalColor =
                    lerp(
                        sceneColor.rgb,
                        _OutlineColor.rgb,
                        mainEdge *
                        _OutlineColor.a
                    );

                finalColor =
                    lerp(
                        finalColor,
                        _AccentColor.rgb,
                        accentEdge
                    );

                return float4(
                    finalColor,
                    sceneColor.a
                );
            }

            ENDHLSL
        }
    }
    CustomEditor "SynthOfRage.Editor.SORShaderGUI"

    FallBack Off
}