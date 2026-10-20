Shader "Arena3Q/MacAnh/Original PBR"
{
    Properties
    {
        _BaseMap("Unchanged base colour",2D)="white"{}
        _BaseColor("Multiplier",Color)=(1,1,1,1)
        [Normal] _BumpMap("Original tangent normal",2D)="bump"{}
        _BumpScale("Normal strength",Float)=1
        _ORMMap("Original ORM (G roughness, B metallic)",2D)="white"{}
        _UseORM("Use source ORM",Float)=0
        _Metallic("Metallic",Range(0,1))=0
        _Smoothness("Smoothness",Range(0,1))=.4
        [HDR] _EmissionColor("Source emission",Color)=(0,0,0,0)
        _Cutoff("Cutoff",Float)=.5
        _Cull("Cull",Float)=2
        _BaseUV1("Colour uses added UV1",Float)=0
        _NormalUV1("Normal uses added UV1",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_ORMMap); SAMPLER(sampler_ORMMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor, _EmissionColor;
            float _BumpScale,_UseORM,_Metallic,_Smoothness,_Cutoff,_Cull,_BaseUV1,_NormalUV1;
            CBUFFER_END
            struct A { float4 position:POSITION; float3 normal:NORMAL; float4 tangent:TANGENT; float2 uv:TEXCOORD0;float2 uv1:TEXCOORD1; };
            struct V {float4 position:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; half3 normal:TEXCOORD2; half4 tangent:TEXCOORD3; float4 shadow:TEXCOORD4;float2 normalUV:TEXCOORD5;};
            V vert(A i)
            {
                V o; VertexPositionInputs p=GetVertexPositionInputs(i.position.xyz); VertexNormalInputs n=GetVertexNormalInputs(i.normal,i.tangent);
                o.position=p.positionCS;o.world=p.positionWS;o.normal=n.normalWS;o.tangent=half4(n.tangentWS,i.tangent.w*GetOddNegativeScale());
                o.uv=TRANSFORM_TEX(lerp(i.uv,i.uv1,_BaseUV1),_BaseMap);o.normalUV=lerp(i.uv,i.uv1,_NormalUV1);o.shadow=GetShadowCoord(p);return o;
            }
            half4 frag(V i,FRONT_FACE_TYPE frontFace:FRONT_FACE_SEMANTIC):SV_Target
            {
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.normalUV),_BumpScale);
                float3 geometric=SafeNormalize(i.normal);
                float3 sourceTangent=SafeNormalize(i.tangent.xyz);
                float3 normal=SafeNormalize(TransformTangentToWorld(normalTS,float3x3(sourceTangent,cross(geometric,sourceTangent)*i.tangent.w,geometric)));
                if(_NormalUV1>.5)
                {
                    float3 dx=ddx(i.world),dy=ddy(i.world);float2 ux=ddx(i.normalUV),uy=ddy(i.normalUV);
                    float determinant=ux.x*uy.y-ux.y*uy.x;
                    if(abs(determinant)>1e-10)
                    {
                        float3 tangent=(dx*uy.y-dy*ux.y)/determinant;
                        float3 bitangent=(dy*ux.x-dx*uy.x)/determinant;
                        tangent=SafeNormalize(tangent-geometric*dot(tangent,geometric));
                        bitangent=SafeNormalize(cross(geometric,tangent))*sign(dot(cross(geometric,tangent),bitangent));
                        normal=SafeNormalize(tangent*normalTS.x+bitangent*normalTS.y+geometric*normalTS.z);
                    }
                }
                normal*=IS_FRONT_VFACE(frontFace,1,-1);
                half4 map=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
                half3 orm=SAMPLE_TEXTURE2D(_ORMMap,sampler_ORMMap,i.uv).rgb;
                SurfaceData s=(SurfaceData)0;s.albedo=map.rgb;s.alpha=1;s.normalTS=normalTS;
                s.metallic=lerp(_Metallic,orm.b,_UseORM);s.smoothness=lerp(_Smoothness,1-orm.g,_UseORM);s.occlusion=1;s.emission=_EmissionColor.rgb;
                InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normal;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);
                d.shadowCoord=i.shadow;d.bakedGI=SampleSH(normal);d.vertexLighting=VertexLighting(i.world,normal);
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.position);d.shadowMask=half4(1,1,1,1);
                return UniversalFragmentPBR(d,s);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
