Shader "Arena3Q/Chapter Details"
{
    Properties{_BaseMap("Approved detail atlas",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _Visibility("Visibility",Range(0,1))=1}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-10"}
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) float4 _BaseColor;float _Visibility;CBUFFER_END
            struct A {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float4 color:TEXCOORD3;};
            V Vert(A i){V o;o.world=TransformObjectToWorld(i.vertex.xyz);o.position=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normal);o.uv=i.uv;o.color=i.color;return o;}
            half4 Frag(V i):SV_Target
            {
                half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor*i.color;
                tex.a*=_Visibility;clip(tex.a-.002);
                SurfaceData s=(SurfaceData)0;s.albedo=tex.rgb;s.alpha=1;s.occlusion=1;s.smoothness=.02;s.specular=half3(.02,.02,.02);s.normalTS=half3(0,0,1);
                InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normalize(i.normal);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);d.shadowCoord=TransformWorldToShadowCoord(i.world);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.position);d.shadowMask=half4(1,1,1,1);
                half4 c=UniversalFragmentBlinnPhong(d,s);c.a=tex.a;return c;
            }
            ENDHLSL
        }
    }
}
