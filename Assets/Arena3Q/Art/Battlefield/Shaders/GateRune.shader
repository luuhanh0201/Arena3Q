Shader "Arena3Q/Battlefield/Gate Rune"
{
    Properties { _BaseColor("Rune colour",Color)=(1,.5,.15,1) }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha One ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _BaseColor; CBUFFER_END
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv*2-1;return o;}
            half4 Frag(V i):SV_Target
            {
                float r=length(i.uv),a=atan2(i.uv.y,i.uv.x);
                float rings=exp(-pow((r-.78)*95,2))+exp(-pow((r-.88)*90,2));
                float marks=step(.86,cos(a*8))*exp(-pow((r-.65)*50,2));
                float bars=step(abs(i.uv.x),.34)*exp(-pow((abs(i.uv.y)-.19)*65,2));
                float mid=step(abs(i.uv.x),.28)*exp(-pow(i.uv.y*65,2));
                float glow=exp(-pow((r-.82)*8,2))*.2;
                float alpha=saturate(rings+marks+bars+mid+glow)*_BaseColor.a;
                return half4(_BaseColor.rgb*1.45,alpha);
            }
            ENDHLSL
        }
    }
}
