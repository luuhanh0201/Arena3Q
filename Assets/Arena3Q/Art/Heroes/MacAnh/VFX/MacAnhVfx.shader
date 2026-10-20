Shader "Arena3Q/MacAnh VFX 2D"
{
    Properties { _Fade("Fade", Range(0,1))=1 _Tint("Tint", Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
            half _Fade; half4 _Tint;
            CBUFFER_END
            Varyings Vert(Attributes input) { Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz); o.color=input.color*_Tint; return o; }
            half4 Frag(Varyings input) : SV_Target { input.color.a*=_Fade; return input.color; }
            ENDHLSL
        }
    }
}
