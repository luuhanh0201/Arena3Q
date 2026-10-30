Shader "Arena3Q/Chapter Water"
{
    Properties
    {
        _BaseMap("Water",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
        _Smoothness("Smoothness",Range(0,1))=.75 _Metallic("Metallic",Range(0,1))=0
        _Flow("Primary, secondary, blend",Vector)=(0,0,0,0) _SecondDirection("Secondary direction",Vector)=(.3,-1,0,0)
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_BaseColor,_Flow,_SecondDirection;float _Smoothness,_Metallic;
            CBUFFER_END
            struct A{float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
            struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
            V Vert(A i){V o;o.world=TransformObjectToWorld(i.vertex.xyz);o.position=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normal);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
            half4 Frag(V i):SV_Target
            {
                half3 primary=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv-float2(0,_Flow.x)).rgb;
                half3 secondary=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv-_SecondDirection.xy*_Flow.y).rgb;
                SurfaceData s=(SurfaceData)0;s.albedo=lerp(primary,secondary,_Flow.z)*_BaseColor.rgb;s.alpha=1;s.occlusion=1;s.smoothness=_Smoothness;s.metallic=_Metallic;s.normalTS=half3(0,0,1);
                InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normalize(i.normal);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);d.shadowCoord=TransformWorldToShadowCoord(i.world);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.position);d.shadowMask=half4(1,1,1,1);
                return UniversalFragmentPBR(d,s);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
