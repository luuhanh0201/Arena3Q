Shader "Arena3Q/Chapter Road Guide"
{
    Properties
    {
        _RouteColor ("Route", Color) = (1,.78,.32,1)
        _OutlineColor ("Outline", Color) = (.17,.09,.035,1)
        _DashLength ("Dash length", Float) = 1.35
        _DashFill ("Dash fill", Range(0,1)) = .62
        _StartDistance ("Start", Float) = 0
        _ActiveStart ("Active start", Float) = 0
        _EndDistance ("End", Float) = 1000
        _Clock ("Map clock", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _RouteColor, _OutlineColor;
            float _DashLength, _DashFill, _StartDistance, _ActiveStart, _EndDistance, _Clock;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionCS = TransformObjectToHClip(input.positionOS.xyz); output.uv = input.uv; return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float d = input.uv.x;
                clip(d - _StartDistance); clip(_EndDistance - d);
                float moving = smoothstep(_ActiveStart, _ActiveStart + _DashLength, d);
                float phase = frac(d / max(.01, _DashLength) - _Clock * moving);
                float dash = smoothstep(0, .06, phase) * (1 - smoothstep(_DashFill - .06, _DashFill, phase));
                float edge = abs(input.uv.y);
                half4 color = lerp(_RouteColor, _OutlineColor, smoothstep(.55, .75, edge));
                color.a *= dash * (1 - smoothstep(.8, 1, edge));
                return color;
            }
            ENDHLSL
        }
    }
}
