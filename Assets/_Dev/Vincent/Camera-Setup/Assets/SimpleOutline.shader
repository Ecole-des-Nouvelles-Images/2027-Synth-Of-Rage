Shader "Custom/Toon/Outline"
{
    Properties
    {
        [Header(Outline)]

        _OutlineColor
        (
            "Outline Color",
            Color
        ) = (0.015, 0.015, 0.02, 1)

        _OutlineWidth
        (
            "Outline Width",
            Range(0.0001, 15)
        ) = 0.015

        _OutlineRadialBlend
        (
            "Anti Crack / Radial Blend",
            Range(0, 1)
        ) = 1
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


            // Inverted hull.
            Cull Front

            // Le vrai mesh a déjà écrit la profondeur.
            ZWrite Off

            ZTest LEqual


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment Frag

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


            Varyings Vert(
                Attributes IN
            )
            {
                Varyings OUT =
                    (Varyings)0;


                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);


                // ----------------------------------------------------
                // Position originale
                // ----------------------------------------------------

                float3 positionWS =
                    TransformObjectToWorld(
                        IN.positionOS.xyz
                    );


                // ----------------------------------------------------
                // Normale classique
                // ----------------------------------------------------

                float3 normalWS =
                    normalize(
                        TransformObjectToWorldNormal(
                            IN.normalOS
                        )
                    );


                // ----------------------------------------------------
                // Direction radiale
                //
                // Deux vertices ayant la même position obtiennent
                // exactement la même direction, même avec des
                // normales différentes.
                // ----------------------------------------------------

                float3 objectCenterWS =
                    TransformObjectToWorld(
                        float3(0,0,0)
                    );


                float3 radialWS =
                    normalize(
                        positionWS
                        - objectCenterWS
                    );


                // ----------------------------------------------------
                // Mix
                //
                // 0 = normale mesh
                // 1 = radial anti-crack
                // ----------------------------------------------------

                float3 extrusionDirection =
                    normalize(
                        lerp(
                            normalWS,
                            radialWS,
                            _OutlineRadialBlend
                        )
                    );


                positionWS +=
                    extrusionDirection
                    * _OutlineWidth;


                OUT.positionCS =
                    TransformWorldToHClip(
                        positionWS
                    );


                return OUT;
            }


            half4 Frag(
                Varyings IN
            ) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }
    }

    FallBack Off
}