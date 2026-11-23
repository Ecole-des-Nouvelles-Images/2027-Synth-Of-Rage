
Shader "FX/Waterfall Interactive URP"
{
    Properties
    {
        [Space]
        [Header(Water)]
        _TColor ("Deep Tint", Color) = (0,1,1,1)
        _WaterColor ("Edge Tint", Color) = (0,0.6,1,1)
        _DepthOffset ("Depth Offset", Range(-10,10)) = 0
        _Stretch ("Depth Stretch", Range(0,5)) = 2
        _Brightness ("Water Brightness", Range(0.5,2)) = 1.2

        [Space]
        [Header(Surface Noise and Movement)]
        _SideNoiseTex ("Side Water Texture", 2D) = "white" {}
        _TopNoiseTex ("Top Water Texture", 2D) = "white" {}
        _HorSpeed ("Horizontal Flow Speed", Range(0,4)) = 0.14
        _VertSpeed ("Vertical Flow Speed", Range(0,60)) = 6.8
        _TopScale ("Top Noise Scale", Range(0,1)) = 0.4
        _NoiseScale ("Side Noise Scale", Range(0,1)) = 0.04

        [Toggle(VERTEX)]
        _VERTEX ("Use Vertex Colors", Float) = 0

        [Space]
        [Header(Foam)]
        _FoamColor ("Foam Tint", Color) = (1,1,1,1)
        _Foam ("Edgefoam Width", Range(1,50)) = 2.35
        _TopSpread ("Foam Position", Range(-1,6)) = 0.05
        _Softness ("Foam Softness", Range(0,0.5)) = 0.1
        _EdgeWidth ("Foam Width", Range(0,2)) = 0.4

        [Space]
        [Header(Bottom Waterfall Foam)]
        _BottomFoamWidth ("Bottom Foam Width", Range(0.001,0.5)) = 0.08
        _BottomFoamStrength ("Bottom Foam Strength", Range(0,2)) = 1
        _BottomFoamScale ("Bottom Foam Noise Scale", Range(0.1,10)) = 2
        _BottomFoamSpeed ("Bottom Foam Speed", Range(0,5)) = 0.5

        [Space]
        [Header(Rim Light)]
        _RimPower ("Rim Power", Range(1,20)) = 18
        _RimColor ("Rim Color", Color) = (0,0.5,0.25,1)

        [Space]
        [Header(Vertex Movement)]
        _Amount ("Wave Amount", Range(0,10)) = 0.6
        _SpeedV ("Speed", Range(0,10)) = 0.5
        _Height ("Wave Height", Range(0,1)) = 0.1

        [Space]
        [Header(Water Surface Waves)]
        _WaveStrength ("Wave Strength", Range(0,0.5)) = 0.04
        _WaveScale ("Wave Scale", Range(0.1,10)) = 1
        _WaveSpeed ("Wave Speed", Range(0,5)) = 0.5
        _WaveDetail ("Wave Detail", Range(0,1)) = 0.5
        _WaterfallJoinWidth ("Waterfall Join Width", Range(0.001,0.5)) = 0.08

        [Space]
        [Header(Reflections)]
        _ReflectionTex ("Refl Texture", 2D) = "black" {}
        _Reflectivity ("Reflectivity", Range(0,1)) = 0.6

        [Space]
        [Header(Emission)]
        [HDR]
        _EdgeEmissionColor ("Edge Emission", Color) = (0,0,0,1)

        [HDR]
        _DeepEmissionColor ("Deep Emission", Color) = (0,0,0,1)

        [HDR]
        _FoamEmissionColor ("Foam Emission", Color) = (0,0,0,1)

        [Space]
        [Header(Halftone)]
        _HalftoneStrength ("Halftone Strength", Range(0,1)) = 0.8
        _HalftoneColor ("Halftone Color", Color) = (0.03,0.03,0.04,1)
        _HalftoneSize ("Dot Cell Size (pixels)", Range(2,64)) = 10
        _HalftoneLevels ("Halftone Levels", Range(2,8)) = 4
        _HalftoneMinRadius ("Min Dot Radius", Range(0,0.7)) = 0.04
        _HalftoneMaxRadius ("Max Dot Radius", Range(0,0.7)) = 0.46
        _HalftoneAngle ("Halftone Angle", Range(0,180)) = 45

        [Space]
        [Header(Halftone Anchoring)]
        [Toggle(_HALFTONE_WORLD_SPACE)]
        _HalftoneWorldSpace ("Lock Dots To World", Float) = 0
        _HalftoneWorldScale ("World Dot Density", Range(0.05,20)) = 2
        _HalftoneWorldOffset ("World Dot Offset", Vector) = (0,0,0,0)
        _HalftoneWorldProjectionBlend ("World Projection Sharpness", Range(1,16)) = 6
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        LOD 300

        Pass
        {
            Name "WaterfallForward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _ VERTEX
            #pragma multi_compile_local_fragment _ _HALFTONE_WORLD_SPACE

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_SideNoiseTex);
            SAMPLER(sampler_SideNoiseTex);

            TEXTURE2D(_TopNoiseTex);
            SAMPLER(sampler_TopNoiseTex);

            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            TEXTURE2D(_GlobalEffectRT);
            SAMPLER(sampler_GlobalEffectRT);

            CBUFFER_START(UnityPerMaterial)

                float4 _TColor;
                float4 _WaterColor;
                float4 _FoamColor;
                float4 _RimColor;

                float4 _EdgeEmissionColor;
                float4 _DeepEmissionColor;
                float4 _FoamEmissionColor;

                float _DepthOffset;
                float _Stretch;
                float _Brightness;

                float _HorSpeed;
                float _VertSpeed;
                float _TopScale;
                float _NoiseScale;

                float _Foam;
                float _TopSpread;
                float _Softness;
                float _EdgeWidth;

                float _BottomFoamWidth;
                float _BottomFoamStrength;
                float _BottomFoamScale;
                float _BottomFoamSpeed;

                float _RimPower;

                float _Amount;
                float _SpeedV;
                float _Height;

                float _WaveStrength;
                float _WaveScale;
                float _WaveSpeed;
                float _WaveDetail;
                float _WaterfallJoinWidth;

                float _Reflectivity;

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

            CBUFFER_END

            float3 _Position;
            float _OrthographicCamSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;

                float4 color : TEXCOORD2;
                float2 uv : TEXCOORD3;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ====================================================
            // HALFTONE FUNCTIONS
            // ====================================================

            float QuantizeHalftone(float value, float levels)
            {
                levels = max(2.0, round(levels));
                value = saturate(value);

                float denominator = levels - 1.0;

                return floor(value * denominator + 0.5) / denominator;
            }

            float2 RotateHalftone2D(float2 p, float angleRad)
            {
                float s = sin(angleRad);
                float c = cos(angleRad);

                return float2(
                    c * p.x - s * p.y,
                    s * p.x + c * p.y
                );
            }

            float HalftoneDotMask(float2 patternPosition, float darkness)
            {
                darkness = QuantizeHalftone(darkness, _HalftoneLevels);

                if (_HalftoneStrength <= 0.0001 || darkness <= 0.0001)
                    return 0.0;

                float2 cell = frac(patternPosition) - 0.5;
                float distToCenter = length(cell);

                float radius = lerp(
                    _HalftoneMinRadius,
                    _HalftoneMaxRadius,
                    darkness
                );

                float aa = max(fwidth(distToCenter), 0.0001);

                return 1.0 - smoothstep(
                    radius - aa,
                    radius + aa,
                    distToCenter
                );
            }

            float HalftoneMaskScreen(float2 pixelPosition, float darkness)
            {
                float2 p = RotateHalftone2D(
                    pixelPosition,
                    radians(_HalftoneAngle)
                );

                p /= max(_HalftoneSize, 1.0);

                return HalftoneDotMask(p, darkness) * _HalftoneStrength;
            }

            float HalftoneMaskWorld(
                float3 positionWS,
                float3 geometricNormalWS,
                float darkness
            )
            {
                if (_HalftoneStrength <= 0.0001)
                    return 0.0;

                float3 p = (
                    positionWS + _HalftoneWorldOffset.xyz
                ) * max(_HalftoneWorldScale, 0.0001);

                float angle = radians(_HalftoneAngle);

                float3 weights = abs(normalize(geometricNormalWS));

                weights = pow(
                    max(weights, float3(0.0001, 0.0001, 0.0001)),
                    max(_HalftoneWorldProjectionBlend, 1.0)
                );

                weights /= max(
                    weights.x + weights.y + weights.z,
                    0.0001
                );

                float maskX = HalftoneDotMask(
                    RotateHalftone2D(p.zy, angle),
                    darkness
                );

                float maskY = HalftoneDotMask(
                    RotateHalftone2D(p.xz, angle),
                    darkness
                );

                float maskZ = HalftoneDotMask(
                    RotateHalftone2D(p.xy, angle),
                    darkness
                );

                return (
                    maskX * weights.x +
                    maskY * weights.y +
                    maskZ * weights.z
                ) * _HalftoneStrength;
            }

            float HalftoneMask(
                float2 pixelPosition,
                float3 positionWS,
                float3 geometricNormalWS,
                float darkness
            )
            {
                #if defined(_HALFTONE_WORLD_SPACE)

                    return HalftoneMaskWorld(
                        positionWS,
                        geometricNormalWS,
                        darkness
                    );

                #else

                    return HalftoneMaskScreen(
                        pixelPosition,
                        darkness
                    );

                #endif
            }

            // ====================================================
            // SHARED WATER WAVE
            // ====================================================

            float EvaluateWaterWave(float3 worldPos)
            {
                float2 waveUV = worldPos.xz * _WaveScale;

                float waveA = sin(
                    dot(waveUV, float2(1.0, 0.73)) +
                    _Time.y * _WaveSpeed
                );

                float waveB = sin(
                    dot(waveUV, float2(-0.61, 1.17)) +
                    _Time.y * (_WaveSpeed * 0.73) +
                    1.7
                );

                float waveC = sin(
                    dot(waveUV, float2(1.31, -0.42)) +
                    _Time.y * (_WaveSpeed * 1.31) +
                    3.1
                );

                float combinedWave = lerp(
                    waveA * 0.7 + waveB * 0.3,
                    waveA * 0.4 + waveB * 0.3 + waveC * 0.3,
                    _WaveDetail
                );

                return combinedWave * _WaveStrength;
            }

            // ====================================================
            // VERTEX SHADER
            // ====================================================

            Varyings Vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                float3 worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                float3 tex = SAMPLE_TEXTURE2D_LOD(
                    _SideNoiseTex,
                    sampler_SideNoiseTex,
                    worldPos.xz * _TopScale,
                    1
                ).rgb;

                float3 movement = sin(
                    _Time.y * _SpeedV +
                    (
                        IN.positionOS.x *
                        IN.positionOS.z *
                        _Amount *
                        tex
                    )
                ) * _Height * (1.0 - worldNormal.y);

                IN.positionOS.xyz += movement;

                worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                // ------------------------------------------------
                // SHARED WATER SURFACE DEFORMATION
                // ------------------------------------------------

                float horizontalMask = smoothstep(
                    0.55,
                    0.9,
                    abs(worldNormal.y)
                );

                float verticalMask = 1.0 - horizontalMask;

                float bottomDistance = 1.0 - IN.uv.y;

                float joinMask = 1.0 - smoothstep(
                    0.0,
                    max(_WaterfallJoinWidth, 0.001),
                    bottomDistance
                );

                float deformationMask = saturate(
                    horizontalMask +
                    verticalMask * joinMask
                );

                float waveHeight = EvaluateWaterWave(worldPos);

                worldPos.y += waveHeight * deformationMask;

                OUT.positionCS = TransformWorldToHClip(worldPos);

                OUT.positionWS = worldPos;
                OUT.normalWS = worldNormal;

                OUT.color = IN.color;
                OUT.uv = IN.uv;

                return OUT;
            }

            // ====================================================
            // FRAGMENT SHADER
            // ====================================================

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 worldPos = IN.positionWS;
                float3 worldNormal = normalize(IN.normalWS);

                float3 viewDir = GetWorldSpaceNormalizeViewDir(worldPos);

                // ------------------------------------------------
                // VERTEX COLORS
                // ------------------------------------------------

                float3 vertexColors = IN.color.rgb;

                float3 blendNormal = saturate(
                    pow(worldNormal * 1.4, 4.0)
                );

                // ------------------------------------------------
                // FLOW DIRECTION
                // ------------------------------------------------

                #if defined(VERTEX)

                    float3 flowDir = (vertexColors * 2.0) - 1.0;

                #else

                    float3 flowDir = -(worldNormal * 2.0) - 1.0;

                #endif

                flowDir *= _HorSpeed;

                // ------------------------------------------------
                // FLOW TIMING
                // ------------------------------------------------

                float timing = frac(_Time.y * 0.5 + 0.5);
                float timing2 = frac(_Time.y * 0.5);

                float timingLerp = abs((0.5 - timing) / 0.5);

                // ------------------------------------------------
                // TOP WATER TEXTURES
                // ------------------------------------------------

                half3 topTex1 = SAMPLE_TEXTURE2D(
                    _TopNoiseTex,
                    sampler_TopNoiseTex,
                    worldPos.xz * _TopScale + flowDir.xz * timing
                ).rgb;

                half3 topTex2 = SAMPLE_TEXTURE2D(
                    _TopNoiseTex,
                    sampler_TopNoiseTex,
                    worldPos.xz * _TopScale + flowDir.xz * timing2
                ).rgb;

                float vertFlow = _Time.y * _VertSpeed;

                // ------------------------------------------------
                // GLOBAL RIPPLE TEXTURE
                // ------------------------------------------------

                float2 rippleUV = worldPos.xz - _Position.xz;

                rippleUV /= max(_OrthographicCamSize * 2.0, 0.001);
                rippleUV += 0.5;

                float ripples = SAMPLE_TEXTURE2D(
                    _GlobalEffectRT,
                    sampler_GlobalEffectRT,
                    rippleUV
                ).b;

                // ------------------------------------------------
                // SIDE NOISE
                // ------------------------------------------------

                float3 SideFoamNoiseZ = SAMPLE_TEXTURE2D(
                    _SideNoiseTex,
                    sampler_SideNoiseTex,
                    float2(
                        worldPos.z * 10,
                        worldPos.y + vertFlow
                    ) * _NoiseScale
                ).rgb;

                float3 SideFoamNoiseX = SAMPLE_TEXTURE2D(
                    _SideNoiseTex,
                    sampler_SideNoiseTex,
                    float2(
                        worldPos.x * 10,
                        worldPos.y + vertFlow
                    ) * _NoiseScale
                ).rgb;

                float3 SideFoamNoiseZE = SAMPLE_TEXTURE2D(
                    _TopNoiseTex,
                    sampler_TopNoiseTex,
                    float2(
                        worldPos.z * 10,
                        worldPos.y + vertFlow
                    ) * _NoiseScale / 3.0
                ).rgb;

                float3 SideFoamNoiseXE = SAMPLE_TEXTURE2D(
                    _TopNoiseTex,
                    sampler_TopNoiseTex,
                    float2(
                        worldPos.x * 10,
                        worldPos.y + vertFlow
                    ) * _NoiseScale / 3.0
                ).rgb;

                // ------------------------------------------------
                // TRIPLANAR NOISE
                // ------------------------------------------------

                float3 noisetexture = (
                    SideFoamNoiseX +
                    SideFoamNoiseXE
                ) / 2.0;

                noisetexture = lerp(
                    noisetexture,
                    (
                        SideFoamNoiseZ +
                        SideFoamNoiseZE
                    ) / 2.0,
                    blendNormal.x
                );

                float3 TopFoamNoise = lerp(
                    topTex1,
                    topTex2,
                    timingLerp
                ) + ripples;

                noisetexture = lerp(
                    noisetexture,
                    TopFoamNoise,
                    blendNormal.y
                );

                // ------------------------------------------------
                // WATER NORMAL DISTORTION
                // ------------------------------------------------

                float noiseLuminance = dot(
                    noisetexture,
                    float3(0.299, 0.587, 0.114)
                );

                float worldNormalDotNoise = dot(
                    worldNormal,
                    float3(
                        worldNormal.y + 0.3,
                        worldNormal.y + 0.3,
                        worldNormal.y + 0.3
                    )
                );

                worldNormalDotNoise *= noiseLuminance;

                float3 normalWS = normalize(
                    worldNormal +
                    (noisetexture * 2.0 - 1.0) * 0.15
                );

                normalWS = normalize(
                    normalWS +
                    (ripples * 2.0 - 1.0) * 0.05
                );

                // ------------------------------------------------
                // VERTICAL SURFACE DETECTION
                // ------------------------------------------------

                // Detection commune aux cascades verticales.
                // On la reutilise pour le foam et le bottom foam.
                float verticalSurfaceMask = 1.0 - smoothstep(
                    0.55,
                    0.9,
                    abs(worldNormal.y)
                );

                // ------------------------------------------------
                // SCENE DEPTH
                // ------------------------------------------------

                float2 screenUV = GetNormalizedScreenSpaceUV(
                    IN.positionCS
                );

                float rawDepth = SampleSceneDepth(screenUV);

                float sceneEyeDepth = LinearEyeDepth(
                    rawDepth,
                    _ZBufferParams
                );

                float fragmentEyeDepth = -TransformWorldToView(
                    worldPos
                ).z;

                float depthDifference = max(
                    sceneEyeDepth - fragmentEyeDepth,
                    0.0
                );

                // ------------------------------------------------
                // EDGE FOAM
                // ------------------------------------------------

                // Le foam d'intersection conserve son comportement original.
                // Il n'est plus utilise pour generer le foam des cascades.
                float foamDepth = saturate(
                    _Foam *
                    noiseLuminance *
                    depthDifference
                );

                float foamLineS = 1.0 - foamDepth;

                float foamLine = smoothstep(
                    0.5,
                    0.8,
                    foamLineS
                );

                // ------------------------------------------------
                // RIM LIGHT
                // ------------------------------------------------

                float rim = 1.0 - saturate(
                    dot(viewDir, normalWS)
                );

                float3 colorRim = _RimColor.rgb * pow(
                    rim,
                    _RimPower
                );

                // ------------------------------------------------
                // SURFACE FOAM
                // ------------------------------------------------

                // Foam horizontal : calcul original conserve.
                float horizontalFoamS = smoothstep(
                    worldNormalDotNoise,
                    worldNormalDotNoise + max(_Softness, 0.001),
                    _TopSpread + _EdgeWidth
                );

                horizontalFoamS *= saturate(
                    1.0 - worldNormal.y
                );

                // Foam vertical :
                // reutilise directement le bruit lateral deja calcule.
                // SideNoise est deja etire et anime avec vertFlow.
                // Aucun nouvel echantillonnage ni UV supplementaire.
                float verticalFoamS = smoothstep(
                    noiseLuminance,
                    noiseLuminance + max(_Softness, 0.001),
                    _TopSpread + _EdgeWidth
                );

                // Selection progressive entre le foam horizontal
                // et le foam issu du bruit d'ecoulement vertical.
                float foamS = saturate(
                    lerp(
                        horizontalFoamS,
                        verticalFoamS,
                        verticalSurfaceMask
                    )
                );

                // ------------------------------------------------
                // BOTTOM WATERFALL FOAM
                // ------------------------------------------------

                float bottomDistance = 1.0 - IN.uv.y;

                float bottomBand = 1.0 - smoothstep(
                    0.0,
                    max(_BottomFoamWidth, 0.001),
                    bottomDistance
                );

                float2 bottomFoamUV = IN.uv * _BottomFoamScale;

                bottomFoamUV.y += _Time.y * _BottomFoamSpeed;

                float bottomFoamNoise = SAMPLE_TEXTURE2D(
                    _TopNoiseTex,
                    sampler_TopNoiseTex,
                    bottomFoamUV
                ).r;

                float bottomFoamPattern = smoothstep(
                    0.35,
                    0.75,
                    bottomFoamNoise
                );

                float bottomFoamMask = saturate(
                    bottomBand *
                    verticalSurfaceMask *
                    bottomFoamPattern *
                    _BottomFoamStrength
                );

                // ------------------------------------------------
                // COMBINED FOAM
                // ------------------------------------------------

                float foamMask = saturate(
                    foamS +
                    foamLine +
                    ripples * 0.25 +
                    bottomFoamMask
                );

                // ------------------------------------------------
                // WATER COLOR
                // ------------------------------------------------

                float depthColor = saturate(
                    (
                        depthDifference +
                        _DepthOffset * noisetexture.r
                    ) * _Stretch
                );

                float4 waterColor = lerp(
                    _WaterColor,
                    _TColor,
                    depthColor
                ) * _Brightness;

                float3 waterAlbedo = lerp(
                    waterColor.rgb,
                    _FoamColor.rgb,
                    foamMask
                );

                // ------------------------------------------------
                // REFLECTIONS
                // ------------------------------------------------

                float3 reflections = SAMPLE_TEXTURE2D(
                    _ReflectionTex,
                    sampler_ReflectionTex,
                    screenUV
                ).rgb;

                reflections *= _Reflectivity;

                // ====================================================
                // EMISSION
                // ====================================================

                float3 waterEmission = lerp(
                    _EdgeEmissionColor.rgb,
                    _DeepEmissionColor.rgb,
                    depthColor
                );

                float3 foamEmission =
                    _FoamEmissionColor.rgb * foamMask;

                float3 finalEmission =
                    waterEmission +
                    foamEmission +
                    colorRim * 0.5 +
                    reflections * 0.25;

                // ------------------------------------------------
                // URP PBR LIGHTING
                // ------------------------------------------------

                InputData inputData = (InputData)0;

                inputData.positionWS = worldPos;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDir;

                inputData.shadowCoord =
                    TransformWorldToShadowCoord(worldPos);

                inputData.fogCoord = 0;
                inputData.vertexLighting = 0;

                inputData.bakedGI = SampleSH(normalWS);

                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;

                surfaceData.albedo = waterAlbedo;
                surfaceData.metallic = 0.5;
                surfaceData.specular = 0.5;
                surfaceData.smoothness = 0.85;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.emission = finalEmission;

                surfaceData.alpha = saturate(
                    waterColor.a * 0.7 +
                    foamMask * 0.25 +
                    ripples * 0.05
                );

                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                half4 finalColor = UniversalFragmentPBR(
                    inputData,
                    surfaceData
                );

                // ====================================================
                // HALFTONE APPLICATION
                // ====================================================

                Light mainLight = GetMainLight(
                    inputData.shadowCoord
                );

                float mainLightNdotL = saturate(
                    dot(
                        normalWS,
                        mainLight.direction
                    )
                );

                float mainLightAttenuation =
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                float halftoneDarkness = 1.0 - saturate(
                    mainLightNdotL *
                    mainLightAttenuation
                );

                float halftone = HalftoneMask(
                    IN.positionCS.xy,
                    worldPos,
                    worldNormal,
                    halftoneDarkness
                );

                finalColor.rgb = lerp(
                    finalColor.rgb,
                    finalColor.rgb * _HalftoneColor.rgb,
                    halftone
                );

                // ------------------------------------------------
                // FOG
                // ------------------------------------------------

                finalColor.rgb = MixFog(
                    finalColor.rgb,
                    inputData.fogCoord
                );

                return finalColor;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
