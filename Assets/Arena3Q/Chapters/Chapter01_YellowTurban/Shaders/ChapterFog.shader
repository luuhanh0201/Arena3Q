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
            float _RegionRadius, _RevealEdge;
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
                float dOpen=1e8, dLocked=1e8, dOpenNext=1e8, dLockedNext=1e8, reveal=0;
                float2 centre=0;
                for(int j=0;j<(int)_Count;j++)
                {
                    float d=distance(world.xz,_Regions[j].xy);
                    float visibility=_Regions[j].z;
                    if(visibility>=1) dOpen=min(dOpen,d); else dLocked=min(dLocked,d);
                    if(visibility>0) dOpenNext=min(dOpenNext,d); else dLockedNext=min(dLockedNext,d);
                    // Reveals are sequential. Use the same transition on both sides of its border,
                    // rather than switching progress when a different locked centre is nearest.
                    if(visibility>0 && visibility<1){reveal=visibility;centre=_Regions[j].xy;}
                }
                // The whole blend lies on the locked side: open ground and the border have zero fog.
                float fogNow=smoothstep(0,max(.01,_Boundary),dOpen-dLocked);
                float fogNext=smoothstep(0,max(.01,_Boundary),dOpenNext-dLockedNext);
                float2 p=world.xz*_Scale;
                float revealNoise=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,p*1.31+float2(.17,.43)).r;
                float field=revealNoise*.6+saturate(distance(world.xz,centre)/_RegionRadius)*.4;
                // Move the threshold beyond the complete [0,1] field at each endpoint.
                float threshold=reveal*(1+2*_RevealEdge)-_RevealEdge;
                float remaining=smoothstep(threshold-_RevealEdge,threshold+_RevealEdge,field);
                float fog=lerp(fogNext,fogNow,remaining);
                // Seamless texture; sampler Repeat handles derivatives across tile edges.
                // Different rotations, scales and velocities keep layers from repeating together.
                float time=_Clock*_Drift*_Scale;
                float a=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,p+float2(time,-time*.37)).r;
                float2 rotated=float2(p.x*.8-p.y*.6,p.x*.6+p.y*.8);
                float b=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,rotated*1.73+float2(.37-time*.41,.61+time*.53)).r;
                float cloud=(a+b)*.5;
                if(_Layers>2.5) cloud=(a+b+SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,float2(-p.y,p.x)*.67+float2(.71+time*.19,.23+time*.31)).r)/3;
                half3 scene=SampleSceneColor(uv);
                half gray=dot(scene,half3(.2126,.7152,.0722))*(1-_Darkening);
                half3 color=lerp(gray.xxx,_FogColor.rgb,saturate(.4+cloud*.7)*_Opacity);
                return half4(color,fog);
            }
            ENDHLSL
        }
    }
}
