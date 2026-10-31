Shader "Arena3Q/Chapter Bamboo"
{
    Properties
    {
        _BaseMap("Albedo",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
        _Cutoff("Cutoff",Range(0,1))=.5 _Smoothness("Smoothness",Range(0,1))=.1 _Metallic("Metallic",Range(0,1))=0
        _Cull("Cull",Float)=0 _Motion("Clock, amplitude, frequency",Vector)=(0,.1,1.5,0)
        _ChapterPlantBounds("World bottom and height",Vector)=(0,7,0,0)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
        Cull [_Cull]
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST,_BaseColor,_Motion;float _Cutoff,_Smoothness,_Metallic,_Cull;
        CBUFFER_END
        UNITY_INSTANCING_BUFFER_START(ChapterPlants)
            UNITY_DEFINE_INSTANCED_PROP(float4,_ChapterPlantBounds)
        UNITY_INSTANCING_BUFFER_END(ChapterPlants)
        float4 _ArenaWind;
        struct A{float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
        struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
        V Vert(A i)
        {
            UNITY_SETUP_INSTANCE_ID(i);V o;UNITY_TRANSFER_INSTANCE_ID(i,o);
            float4 bounds=UNITY_ACCESS_INSTANCED_PROP(ChapterPlants,_ChapterPlantBounds);
            float3 world=TransformObjectToWorld(i.vertex.xyz);
            float height=saturate((world.y-bounds.x)/max(.1,bounds.y));
            // Smooth, low-frequency variation across the clump: nearby vertices remain coherent.
            float phase=dot(bounds.zw,float2(.73,1.17))+dot(world.xz-bounds.zw,float2(.19,.23));
            float sway=(sin(_Motion.x*_Motion.z+phase)+.25*sin(_Motion.x*_Motion.z*1.73+phase*.61))/1.25;
            world.xz+=_ArenaWind.xy*sway*_Motion.y*height*height;
            o.world=world;o.position=TransformWorldToHClip(world);o.normal=TransformObjectToWorldNormal(i.normal);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;
        }
        ENDHLSL
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 Frag(V i):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(color.a-_Cutoff);
                SurfaceData s=(SurfaceData)0;s.albedo=color.rgb;s.alpha=1;s.occlusion=1;s.smoothness=_Smoothness;s.metallic=_Metallic;s.normalTS=half3(0,0,1);
                InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normalize(i.normal);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);d.shadowCoord=TransformWorldToShadowCoord(i.world);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.position);d.shadowMask=half4(1,1,1,1);
                return UniversalFragmentPBR(d,s);
            }
            ENDHLSL
        }
        Pass
        {
            Tags {"LightMode"="ShadowCaster"} ColorMask 0 ZWrite On
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            float3 _LightDirection;
            V ShadowVert(A i){V o=Vert(i);o.position=TransformWorldToHClip(ApplyShadowBias(o.world,o.normal,_LightDirection));
            #if UNITY_REVERSED_Z
            o.position.z=min(o.position.z,UNITY_NEAR_CLIP_VALUE);
            #else
            o.position.z=max(o.position.z,UNITY_NEAR_CLIP_VALUE);
            #endif
            return o;}
            half4 ShadowFrag(V i):SV_Target{clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a*_BaseColor.a-_Cutoff);return 0;}
            ENDHLSL
        }
        Pass
        {
            Tags {"LightMode"="DepthOnly"} ColorMask R ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            half4 DepthFrag(V i):SV_Target{clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a*_BaseColor.a-_Cutoff);return i.position.z;}
            ENDHLSL
        }
    }
}
