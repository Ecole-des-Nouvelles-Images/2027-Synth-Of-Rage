Shader "Custom/Toon/Outline"
{
    Properties
    {
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.01,0.01,0.015,1)
        _OutlineWidth ("Outline Width", Range(0.0001,10)) = 0.012

        // 0 = extrusion par normales
        // 1 = extrusion radiale depuis le pivot, plus robuste aux hard edges
        _OutlineRadialBlend ("Anti Crack / Radial Blend", Range(0,1)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry+10"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Outline"

            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineRadialBlend;
            CBUFFER_END

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS =
                    TransformObjectToWorld(
                        IN.positionOS.xyz
                    );

                float3 normalWS =
                    normalize(
                        TransformObjectToWorldNormal(
                            IN.normalOS
                        )
                    );

                float3 objectCenterWS =
                    TransformObjectToWorld(
                        float3(0,0,0)
                    );

                float3 radialWS =
                    SafeNormalize(
                        positionWS - objectCenterWS
                    );

                float3 extrusionDirection =
                    SafeNormalize(
                        lerp(
                            normalWS,
                            radialWS,
                            _OutlineRadialBlend
                        )
                    );

                positionWS +=
                    extrusionDirection *
                    _OutlineWidth;

                OUT.positionCS =
                    TransformWorldToHClip(
                        positionWS
                    );

                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return _OutlineColor;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
