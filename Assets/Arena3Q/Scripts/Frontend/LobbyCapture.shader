Shader "Arena3Q/UI/Premultiplied Capture"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _StencilComp("Stencil comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil operation", Float) = 0
        _StencilWriteMask("Stencil write mask", Float) = 255
        _StencilReadMask("Stencil read mask", Float) = 255
        _ColorMask("Color mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Alpha clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float4 local:TEXCOORD1; };
            sampler2D _MainTex; fixed4 _Color; float4 _ClipRect;
            v2f vert(appdata input)
            {
                v2f output; output.local = input.vertex; output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv; output.color = input.color * _Color; return output;
            }
            fixed4 frag(v2f input):SV_Target
            {
                fixed4 result = tex2D(_MainTex, input.uv);
                result.rgb *= input.color.rgb * input.color.a; result.a *= input.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                fixed mask = UnityGet2DClipping(input.local.xy, _ClipRect); result *= mask;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - .001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
