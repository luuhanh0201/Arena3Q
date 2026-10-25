Shader "Arena3Q/Battlefield/Stylized Environment"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _FactionColor("Faction cloth", Color) = (1,1,1,1)
        _FactionMask("Recolour red cloth", Range(0,1)) = 0
        _Cutoff("Alpha clip", Range(0,1)) = 0
        _BumpScale("Normal strength", Range(0,1)) = .35
        _Smoothness("Smoothness", Range(0,1)) = .1
        _WindBend("Wind bend", Range(0,1)) = 0
        _PlantHeight("Plant height", Float) = 3
        _ArenaVisibility("Visibility",Range(0,1))=1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="AlphaTest" }
        Cull [_Cull]
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST, _BaseColor, _FactionColor;
        float _FactionMask, _Cutoff, _BumpScale, _Smoothness, _WindBend, _PlantHeight, _Cull;
        CBUFFER_END
        UNITY_INSTANCING_BUFFER_START(ArenaInstances)
            UNITY_DEFINE_INSTANCED_PROP(float,_ArenaVisibility)
        UNITY_INSTANCING_BUFFER_END(ArenaInstances)
        float4 _ArenaWind, _ArenaWindNoise;
        float _ArenaWindTime;
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; half3 normal:TEXCOORD2; half4 tangent:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
        float3 Wind(float3 world)
        {
            float3 origin=TransformObjectToWorld(float3(0,0,0));
            float bend=_PlantHeight<=1.5?_ArenaWindNoise.w:_ArenaWindNoise.z+_ArenaWindNoise.w*.6;
            float mask=pow(saturate((world.y-origin.y)/max(.1,_PlantHeight)),1.7)*_WindBend*bend;
            float phase=dot(origin.xz,float2(.73,1.17))*_ArenaWindNoise.y;
            float motion=sin(_ArenaWindTime*_ArenaWind.w*1.6+phase)+.35*sin(_ArenaWindTime*2.7+world.x*.51+world.z*.67);
            float gust=.72+.28*sin(_ArenaWindTime*_ArenaWindNoise.x+phase*.63);
            world.xz+=_ArenaWind.xy*motion*gust*_ArenaWind.z*mask;
            return world;
        }
        Varyings Vert(Attributes input)
        {
            Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(input);UNITY_TRANSFER_INSTANCE_ID(input,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.world=Wind(TransformObjectToWorld(input.positionOS.xyz));
            o.positionCS=TransformWorldToHClip(o.world);o.uv=TRANSFORM_TEX(input.uv,_BaseMap);
            o.normal=TransformObjectToWorldNormal(input.normalOS);o.tangent=half4(TransformObjectToWorldDir(input.tangentOS.xyz),input.tangentOS.w*GetOddNegativeScale());
            return o;
        }
        ENDHLSL
        Pass
        {
            Name "Forward" Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                clip(UNITY_ACCESS_INSTANCED_PROP(ArenaInstances,_ArenaVisibility)-frac(sin(dot(floor(i.positionCS.xy),float2(12.9898,78.233)))*43758.5453));
                half4 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(base.a-_Cutoff);
                half red=saturate((base.r-max(base.g,base.b))/.24)*_FactionMask;
                half luminosity=dot(base.rgb,half3(.3,.59,.11));
                if(_WindBend>.5)base.rgb=lerp(luminosity*half3(1.1,1,.85),base.rgb,.76);
                base.rgb=lerp(base.rgb,_FactionColor.rgb*luminosity*1.8,red);
                half3 n=normalize(i.normal);half3 t=normalize(i.tangent.xyz);half3 b=cross(n,t)*i.tangent.w;
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normalize(mul(normalTS,half3x3(t,b,n)));d.viewDirectionWS=SafeNormalize(GetWorldSpaceViewDir(i.world));d.shadowCoord=TransformWorldToShadowCoord(i.world);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.shadowMask=half4(1,1,1,1);
                d.bakedGI=max(d.bakedGI,half3(.25,.27,.23));
                SurfaceData s=(SurfaceData)0;s.albedo=base.rgb;s.alpha=1;s.occlusion=1;s.smoothness=_Smoothness;s.specular=half3(.04,.04,.04);s.normalTS=normalTS;
                return UniversalFragmentBlinnPhong(d,s);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            float3 _LightDirection;
            Varyings ShadowVert(Attributes input)
            {
                Varyings o=Vert(input);float3 world=ApplyShadowBias(o.world,o.normal,_LightDirection);
                o.positionCS=TransformWorldToHClip(world);
                #if UNITY_REVERSED_Z
                o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #else
                o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 ShadowFrag(Varyings i):SV_Target {UNITY_SETUP_INSTANCE_ID(i);clip(UNITY_ACCESS_INSTANCED_PROP(ArenaInstances,_ArenaVisibility)-frac(sin(dot(floor(i.positionCS.xy),float2(12.9898,78.233)))*43758.5453));clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);return 0;}
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
