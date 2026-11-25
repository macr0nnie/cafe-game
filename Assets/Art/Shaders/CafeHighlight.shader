// Soft outline + glow drawn over an interactable, on a copy of its mesh.
// Pass 1 is an inverted hull: back faces pushed out along the normal, so only a
// rim outside the silhouette shows. Width is in metres per metre of distance, so
// it stays about the same on screen near and far. Pass 2 is a faint fill with a
// fresnel rim on the front faces. Both are unlit and alpha blended.
Shader "Cafe/Highlight"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.557,0.498,0.604,0.6)
        _Width        ("Outline Width", Range(0,0.02)) = 0.003
        _GlowColor    ("Glow Color", Color) = (1,0.98,0.95,1)
        _Fill         ("Glow Fill", Range(0,1)) = 0
        _Rim          ("Glow Rim", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" "IgnoreProjector"="True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float4 _GlowColor;
            float  _Width;
            float  _Fill;
            float  _Rim;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
        };
        ENDHLSL

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(Attributes v) : SV_POSITION
            {
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                float3 n = normalize(TransformObjectToWorldNormal(v.normalOS));
                float d = max(distance(p, _WorldSpaceCameraPos), 0.5);
                return TransformWorldToHClip(p + n * _Width * d);
            }

            half4 frag() : SV_Target { return _OutlineColor; }
            ENDHLSL
        }

        Pass
        {
            Name "Glow"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewWS     : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(p);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = _WorldSpaceCameraPos - p;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float facing = saturate(dot(normalize(i.normalWS), normalize(i.viewWS)));
                float a = _Fill + _Rim * pow(1.0 - facing, 3.0);
                return half4(_GlowColor.rgb, saturate(a) * _GlowColor.a);
            }
            ENDHLSL
        }
    }
}
