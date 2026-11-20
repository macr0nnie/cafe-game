Shader "Cafe/Toon"
{
    Properties
    {
        _BaseMap        ("Base Map", 2D) = "white" {}
        _BaseColor      ("Base Color", Color) = (1,1,1,1)
        _ShadowTint     ("Shadow Tint", Color) = (0.718,0.651,0.788,1)
        _AmbientLift    ("Ambient Lift", Range(0,1)) = 0.5
        _Bands          ("Bands", Range(2,6)) = 3
        _BandSoftness   ("Band Softness", Range(0,0.25)) = 0.03
        _RimColor       ("Rim Color", Color) = (1,1,1,1)
        _RimPower       ("Rim Power", Range(1,16)) = 6
        _RimStrength    ("Rim Strength", Range(0,1)) = 0
        _EmissionColor  ("Emission", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // PC_Renderer is Forward+, which lights through clusters and never sets _ADDITIONAL_LIGHTS.
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadowTint;
                float4 _RimColor;
                float4 _EmissionColor;
                float  _AmbientLift;
                float  _Bands;
                float  _BandSoftness;
                float  _RimPower;
                float  _RimStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
            };

            // Quantises x into N bands. _BandSoftness 0 gives hard steps.
            float Band(float x, float bands, float soft)
            {
                x = saturate(x);
                float s = x * bands;
                float i = floor(s);
                float f = s - i;
                float e = smoothstep(0.0, max(soft * bands, 1e-4), f);
                return (i + e) / bands;
            }

            Varyings vert (Attributes input)
            {
                Varyings o = (Varyings)0;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   n = GetVertexNormalInputs(input.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS   = n.normalWS;
                o.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogCoord   = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 baseCol = tex.rgb * _BaseColor.rgb;

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    float4 shadowCoord = float4(0,0,0,0);
                #endif

                Light mainLight = GetMainLight(shadowCoord);

                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                float band  = Band(ndotl * atten, _Bands, _BandSoftness);

                // Ambient probe lifts the shadow colour so it never reads dead.
                float3 ambient = SampleSH(normalWS);
                float3 shadowCol = baseCol * _ShadowTint.rgb * (ambient * _AmbientLift + (1.0 - _AmbientLift));
                float3 litCol    = baseCol * mainLight.color;

                float3 col = lerp(shadowCol, litCol, band);

                // Pendant lights and any other additional lights, banded the same way.
                #ifdef _ADDITIONAL_LIGHTS
                    // LIGHT_LOOP_BEGIN walks the clusters in Forward+ (it reads inputData)
                    // and is a plain for loop in Forward.
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    uint count = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light l = GetAdditionalLight(lightIndex, input.positionWS);
                        float nl = saturate(dot(normalWS, l.direction));
                        float la = l.distanceAttenuation * l.shadowAttenuation;
                        col += baseCol * l.color * Band(nl * la, _Bands, _BandSoftness);
                    LIGHT_LOOP_END
                #endif

                float rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                col += _RimColor.rgb * rim * _RimStrength;

                col += _EmissionColor.rgb;

                col = MixFog(col, input.fogCoord);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadowTint;
                float4 _RimColor;
                float4 _EmissionColor;
                float  _AmbientLift;
                float  _Bands;
                float  _BandSoftness;
                float  _RimPower;
                float  _RimStrength;
            CBUFFER_END

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings shadowVert (Attributes input)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = positionCS;
                return o;
            }

            half4 shadowFrag (Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadowTint;
                float4 _RimColor;
                float4 _EmissionColor;
                float  _AmbientLift;
                float  _Bands;
                float  _BandSoftness;
                float  _RimPower;
                float  _RimStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings depthVert (Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 depthFrag (Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
