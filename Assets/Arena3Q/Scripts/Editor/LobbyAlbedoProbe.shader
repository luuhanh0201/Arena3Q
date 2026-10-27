Shader "Hidden/Arena3Q/Lobby Albedo Probe"
{
    Properties
    {
        _BaseMap("Base", 2D) = "white" {}
        _BaseColor("Multiplier", Color) = (1,1,1,1)
        _BaseUV1("Base UV1", Float) = 0
        _Cull("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor; float _BaseUV1;
            CBUFFER_END
            struct A { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 uv1:TEXCOORD1; };
            struct V { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            V vert(A input)
            {
                V output; output.vertex = TransformObjectToHClip(input.vertex.xyz);
                output.uv = TRANSFORM_TEX(lerp(input.uv, input.uv1, _BaseUV1), _BaseMap); return output;
            }
            half4 frag(V input):SV_Target
            {
                return half4(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}
