Shader "Custom/URP/Debris Dissolve"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0
        _NoiseScale("Noise Scale", Range(0.1, 20)) = 3
        _EdgeWidth("Edge Width", Range(0.001, 0.5)) = 0.08
        [HDR] _EdgeColor("Edge Color", Color) = (1, 0.35, 0.05, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _DissolveAmount;
                float _NoiseScale;
                float _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float Hash31(float3 value)
            {
                value = frac(value * 0.1031);
                value += dot(value, value.yzx + 33.33);
                return frac((value.x + value.y) * value.z);
            }

            float ValueNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 local = frac(position);
                float3 smoothLocal = local * local * (3.0 - 2.0 * local);

                float lowerBack = lerp(Hash31(cell), Hash31(cell + float3(1, 0, 0)), smoothLocal.x);
                float lowerFront = lerp(Hash31(cell + float3(0, 0, 1)), Hash31(cell + float3(1, 0, 1)), smoothLocal.x);
                float upperBack = lerp(Hash31(cell + float3(0, 1, 0)), Hash31(cell + float3(1, 1, 0)), smoothLocal.x);
                float upperFront = lerp(Hash31(cell + float3(0, 1, 1)), Hash31(cell + float3(1, 1, 1)), smoothLocal.x);

                return lerp(
                    lerp(lowerBack, lowerFront, smoothLocal.z),
                    lerp(upperBack, upperFront, smoothLocal.z),
                    smoothLocal.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionOS = input.positionOS.xyz;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float noise = ValueNoise(input.positionOS * _NoiseScale);
                clip(noise - _DissolveAmount);

                float edge = 1.0 - smoothstep(0.0, _EdgeWidth, noise - _DissolveAmount);
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lighting = SampleSH(normalWS);
                lighting += LightingLambert(
                    mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation),
                    mainLight.direction,
                    normalWS);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0; lightIndex < lightCount; lightIndex++)
                    {
                        Light additionalLight = GetAdditionalLight(lightIndex, input.positionWS);
                        lighting += LightingLambert(
                            additionalLight.color * (additionalLight.distanceAttenuation * additionalLight.shadowAttenuation),
                            additionalLight.direction,
                            normalWS);
                    }
                #endif

                half3 color = lerp(baseColor.rgb * lighting, _EdgeColor.rgb, edge);
                return half4(MixFog(color, input.fogFactor), baseColor.a);
            }
            ENDHLSL
        }
    }
}
