Shader "Arena3Q/Battlefield/Blended Ground"
{
    Properties
    {
        _Sand("Sand",2D)="white"{} _Road("Road",2D)="white"{}
        _Gravel("Gravel",2D)="white"{} _Paving("Paving",2D)="white"{}
        _Dry("Dry grass",2D)="white"{} _Green("Green grass",2D)="white"{}
        _TileScale("World tile metres",Float)=4
        _MacroVariation("Macro variation",Range(0,1))=.16
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_Sand);SAMPLER(sampler_Sand);TEXTURE2D(_Road);TEXTURE2D(_Gravel);TEXTURE2D(_Paving);TEXTURE2D(_Dry);TEXTURE2D(_Green);
            CBUFFER_START(UnityPerMaterial) float _TileScale,_MacroVariation; CBUFFER_END
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;float2 uv2:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half4 color:COLOR;float2 uv2:TEXCOORD2;};
            V Vert(A i){UNITY_SETUP_INSTANCE_ID(i);V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);o.color=i.color;o.uv2=i.uv2;return o;}
            half4 Frag(V i):SV_Target
            {
                float2 uv=i.world.xz/max(.2,_TileScale);
                half grass=saturate(i.color.a),paving=saturate(i.color.b),road=saturate(i.color.r),gravel=saturate(i.color.g);
                half3 albedo=SAMPLE_TEXTURE2D(_Sand,sampler_Sand,uv).rgb;
                albedo=lerp(albedo,SAMPLE_TEXTURE2D(_Gravel,sampler_Sand,uv*.83).rgb,gravel);
                albedo=lerp(albedo,SAMPLE_TEXTURE2D(_Dry,sampler_Sand,uv*.9).rgb,grass*i.uv2.x);
                albedo=lerp(albedo,SAMPLE_TEXTURE2D(_Green,sampler_Sand,uv*.91).rgb*half3(.42,1.13,.47),grass*(1-i.uv2.x));
                albedo=lerp(albedo,SAMPLE_TEXTURE2D(_Road,sampler_Sand,uv*.77).rgb*half3(1.18,1.15,1.05),road);
                albedo=lerp(albedo,SAMPLE_TEXTURE2D(_Paving,sampler_Sand,uv*.72).rgb,paving);
                float macro=sin(i.world.x*.41+sin(i.world.z*.25))*sin(i.world.z*.33+i.world.x*.11);
                albedo*=1+macro*_MacroVariation;
                float dx=ddx(dot(albedo,float3(.3,.59,.11))),dy=ddy(dot(albedo,float3(.3,.59,.11)));
                InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normalize(i.normal+float3(-dx,0,-dy)*.06);d.viewDirectionWS=SafeNormalize(GetWorldSpaceViewDir(i.world));d.shadowCoord=TransformWorldToShadowCoord(i.world);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.shadowMask=half4(1,1,1,1);
                SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.alpha=1;s.occlusion=1;s.smoothness=.02;s.specular=half3(.02,.02,.02);s.normalTS=half3(0,0,1);
                return UniversalFragmentBlinnPhong(d,s);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
