Shader "Arena3Q/Battlefield/Vegetation Billboard"
{
    Properties { _BaseMap("Baked approved tree",2D)="white"{} _Cutoff("Cutoff",Range(0,1))=.35 _ArenaVisibility("Visibility",Range(0,1))=1 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout"}
        Cull Off
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) float _Cutoff;float4 _BaseMap_ST; CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(ArenaInstances)
                UNITY_DEFINE_INSTANCED_PROP(float,_ArenaVisibility)
            UNITY_INSTANCING_BUFFER_END(ArenaInstances)
            float4 _ArenaWind;float _ArenaWindTime;
            struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            V Vert(A i)
            {
                UNITY_SETUP_INSTANCE_ID(i);V o;UNITY_TRANSFER_INSTANCE_ID(i,o);float3 origin=TransformObjectToWorld(float3(0,0,0));
                float scale=length(float3(unity_ObjectToWorld._m00,unity_ObjectToWorld._m10,unity_ObjectToWorld._m20));
                float3 right=UNITY_MATRIX_V[0].xyz,up=UNITY_MATRIX_V[1].xyz;
                float3 world=origin+(right*i.positionOS.x+up*i.positionOS.y)*scale;
                world.xz+=_ArenaWind.xy*sin(_ArenaWindTime*1.6+origin.x*.31+origin.z*.43)*_ArenaWind.z*saturate(i.uv.y)*.2;
                o.positionCS=TransformWorldToHClip(world);o.uv=i.uv;return o;
            }
            half4 Frag(V i):SV_Target {UNITY_SETUP_INSTANCE_ID(i);clip(UNITY_ACCESS_INSTANCED_PROP(ArenaInstances,_ArenaVisibility)-frac(sin(dot(floor(i.positionCS.xy),float2(12.9898,78.233)))*43758.5453));half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);clip(color.a-_Cutoff);return half4(color.rgb,1);}
            ENDHLSL
        }
    }
}
