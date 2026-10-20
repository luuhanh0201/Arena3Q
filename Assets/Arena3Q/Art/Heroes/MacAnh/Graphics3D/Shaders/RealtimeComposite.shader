Shader "Arena3Q/MacAnh/Realtime Composite"
{
    Properties
    {
        _MainTex("Live 3D capture",2D)="white"{}
        _Opacity("Opacity",Range(0,1))=1
        _Shadow("Soft ground shadow",Float)=0
        _Tint("Tint",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite Off Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float _Opacity,_Shadow;
            float4 _Tint;
            CBUFFER_END
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
            half4 frag(V i):SV_Target
            {
                if(_Shadow>.5)
                {
                    float2 p=i.uv*2-1;half alpha=pow(saturate(1-dot(p,p)),2)*.22*_Opacity;
                    return half4(half3(.025,.017,.04)*alpha,alpha);
                }
                half4 colour=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                return half4(colour.rgb*_Tint.rgb,colour.a)*(_Opacity*_Tint.a);
            }
            ENDHLSL
        }
    }
}
