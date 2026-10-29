Shader "Arena3Q/Chapter Fog"
{
    Properties
    {
        _CloudTex ("Cloud source", 2D) = "white" {}
        _FogColor ("Fog tint", Color) = (.75,.79,.78,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite Off ZTest Always Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            TEXTURE2D(_CloudTex); SAMPLER(sampler_CloudTex);
            float4 _Regions[16]; // xz centre, visibility, reserved
            float4 _FogColor;
            float _Count, _Boundary, _Clock, _Drift, _Scale, _Opacity, _Darkening, _Layers;
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings Vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); return o; }
            half4 Frag(Varyings v) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(v.positionCS);
                // Regions use the XZ logic plane. Do not depend on when the renderer copies depth.
                float3 aWorld = ComputeWorldSpacePosition(uv,UNITY_NEAR_CLIP_VALUE,UNITY_MATRIX_I_VP);
                float3 bWorld = ComputeWorldSpacePosition(uv,1,UNITY_MATRIX_I_VP);
                #if UNITY_REVERSED_Z
                    aWorld = ComputeWorldSpacePosition(uv,0,UNITY_MATRIX_I_VP);
                    bWorld = ComputeWorldSpacePosition(uv,1,UNITY_MATRIX_I_VP);
                #endif
                float3 direction=bWorld-aWorld;
                float3 world=aWorld-direction*(aWorld.y/direction.y);
                float nearest=1e8, total=0, fog=0;
                for(int i=0;i<(int)_Count;i++) nearest=min(nearest,distance(world.xz,_Regions[i].xy));
                // A soft Voronoi boundary follows the same stage regions used by navigation.
                for(int j=0;j<(int)_Count;j++)
                {
                    float weight=exp2(-(distance(world.xz,_Regions[j].xy)-nearest)/max(.01,_Boundary)*4);
                    total+=weight; fog+=weight*(1-_Regions[j].z);
                }
                fog=smoothstep(.05,.95,fog/max(.001,total));
                float2 drift=float2(_Clock*_Drift,-_Clock*_Drift*.37);
                float2 p=(world.xz+drift)*_Scale;
                float a=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,frac(p)).a;
                float b=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,frac(p*1.73+float2(.37,.61)-drift*_Scale*.5)).a;
                float cloud=(a+b)*.5;
                if(_Layers>2.5) cloud=(a+b+SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,frac(p*.67+float2(.71,.23))).a)/3;
                half3 scene=SampleSceneColor(uv);
                half gray=dot(scene,half3(.2126,.7152,.0722))*(1-_Darkening);
                half3 color=lerp(gray.xxx,_FogColor.rgb,saturate(.4+cloud*.7)*_Opacity);
                return half4(color,fog);
            }
            ENDHLSL
        }
    }
}
