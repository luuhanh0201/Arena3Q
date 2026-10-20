Shader "Arena3Q/MacAnh Flipbook 2D"
{
    // Pre-rendered VFX frames: RGB = light added on top (rendered on black), A = how much a dark underlay covers
    // what is behind, so a glow still reads on the bright sand. Premultiplied blend does both in one pass.
    Properties
    {
        [PerRendererData] _MainTex("Frames", 2D) = "black" {}
        _Glow("Glow", Range(0,4)) = 1
        _Underlay("Underlay", Range(0,1)) = 0.6
        _UnderlayColor("Underlay colour", Color) = (0.2,0.08,0.38,1)
        _Fade("Fade", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend One OneMinusSrcAlpha
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            half _Glow; half _Underlay; half4 _UnderlayColor; half _Fade;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv; o.color = input.color;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // Vertex colour: rgb tints the glow, a is the per-layer fade from the player.
                half fade = _Fade * input.color.a;
                half cover = t.a * _Underlay * fade;
                half3 light = t.rgb * input.color.rgb * _Glow * fade;
                return half4(light + _UnderlayColor.rgb * cover, cover);
            }
            ENDHLSL
        }
    }
}
