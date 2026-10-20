Shader "Arena3Q/MacAnh/Cyclone Volume"
{
    Properties
    {
        _VolumeTex("3D turbulent wind",3D)="white"{}
        [HDR] _Color("Magic violet (linear)",Color)=(.3,.095,.86,1)
        _Emission("Energy",Float)=1.3
        _Opacity("Optical density",Float)=3.6
        _Steps("Bounded ray samples",Float)=12
        _Fade("Envelope",Float)=1
        _Age("Local clock",Float)=0
        _Dissolve("Dissolve",Float)=0
        _SoftDistance("Intersection softness",Float)=.12
        _DebugVolume("Temporary diagnostic",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" "DisableBatching"="True"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        TEXTURE3D(_VolumeTex);SAMPLER(sampler_VolumeTex);
        CBUFFER_START(UnityPerMaterial)
        float4 _Color;
        float _Emission,_Opacity,_Steps,_Fade,_Age,_Dissolve,_SoftDistance;
        float _DebugVolume;
        CBUFFER_END
        struct A {float4 pos:POSITION;float3 normal:NORMAL;};
        struct V {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 local:TEXCOORD1;float4 screen:TEXCOORD2;float3 normal:TEXCOORD3;};
        V vert(A v) {V o;o.local=v.pos.xyz;o.normal=v.normal;o.world=TransformObjectToWorld(v.pos.xyz);o.pos=TransformWorldToHClip(o.world);o.screen=ComputeScreenPos(o.pos);return o;}
        half4 frag(V i):SV_Target
        {
            if(_Fade<.0001)return 0;
            if(_DebugVolume>2.5)return half4(.6,.1,.9,.8);
            // A closed, conservative funnel bounds the work. Only its near surface
            // is rasterized; density is sampled INSIDE the funnel, not on its skin.
            float3 worldRay=unity_OrthoParams.w>.5?-UNITY_MATRIX_V[2].xyz:normalize(i.world-_WorldSpaceCameraPos);
            float3 ray=normalize(TransformWorldToObjectDir(worldRay));
            // Reject the far wall before sampling, independent of raster winding
            // differences between the orthographic 2D and perspective renderers.
            clip(dot(i.normal,ray)-.00001);
            if(_DebugVolume>1.5)return half4(abs(ray),1);
            int samples=(int)clamp(_Steps,8,20);
            // Intersect the local volume bounds, rather than assuming the
            // rasterized wall is the entry point. Elevated orthographic views
            // and the battle's nonuniform projection need the full ray segment.
            float3 safeRay=(step(0,ray)*2-1)*max(abs(ray),.00001);
            float3 invRay=rcp(safeRay);
            float3 slabA=(float3(-1.75,-.1,-1.75)-i.local)*invRay;
            float3 slabB=(float3(1.75,2.9,1.75)-i.local)*invRay;
            float3 nearSlab=min(slabA,slabB),farSlab=max(slabA,slabB);
            float entry=max(nearSlab.x,max(nearSlab.y,nearSlab.z));
            float exit=min(farSlab.x,min(farSlab.y,farSlab.z));
            if(exit<=entry)return 0;
            float stepLength=(exit-entry)/samples;
            // A little spatial jitter softens sample bands without introducing
            // a temporal noise clock that would break deterministic seeking.
            float jitter=frac(52.9829189*frac(dot(i.pos.xy,float2(.06711056,.00583715))));
            float3 p=i.local+ray*(entry+stepLength*(.4+.2*jitter));
            if(_DebugVolume>.5)return half4(saturate((exit-entry)/3),0,.5,1);
            // Rotate the noise field with height, using a recurrence along the ray.
            // No atan2 or periodic helix mask per sample: those drew hard rings.
            float rs,rc,ds,dc;
            sincos(_Age*.65-p.y*2.7,rs,rc);
            sincos(-ray.y*stepLength*2.7,ds,dc);
            half alpha=0;half3 rgb=0;
            [loop] for(int s=0;s<20;s++)
            {
                if(s>=samples || alpha>.96) break;
                float y=(p.y-.065)/2.12;
                float h=saturate(y);
                // The cage is only an integration bound. The actual wind has a
                // wandering axis and a ragged crown entirely inside that cage.
                float2 axis=float2(sin(y*2.3+_Age*.7),cos(y*3.1-_Age*.53))*.12*h;
                float2 radial=p.xz-axis;
                float angle=atan2(radial.y,radial.x);
                float crown=.89+.16*sin(angle+_Age*.57)+.105*sin(angle*3-_Age*.83)
                            +.065*sin(radial.x*5+radial.y*4+_Age*.4);
                float radius=(.16+.94*pow(h,.72))*(1+.075*sin(angle*2+y*4+_Age*.4));
                float rho=length(radial)/radius;
                if(y>0 && y<1.34 && rho<1.24)
                {
                    float2 swirl=float2(rc*radial.x-rs*radial.y,rs*radial.x+rc*radial.y);
                    float3 q=float3(swirl.x*.64+y*.12,y*.8-_Age*.105-_Dissolve*.2,swirl.y*.64);
                    half3 n=SAMPLE_TEXTURE3D_LOD(_VolumeTex,sampler_VolumeTex,q,0).rgb;
                    half cloud=smoothstep(.29,.68,n.r*.8+n.b*.2);
                    crown+=(n.b-.5)*.14;
                    half edge=(1-smoothstep(.57+n.g*.09,1.15+n.r*.07,rho))*smoothstep(0,.055,y)
                              *(1-smoothstep(crown-.3,crown+.035,y));
                    // Different pockets disappear at different heights while
                    // continuing to advect upward. The funnel never shrinks.
                    half erosion=_Dissolve*(1.04+(1-y)*.14);
                    half dissolve=smoothstep(erosion-.12,erosion+.18,n.r*.85+.15);
                    // A translucent interior keeps trapped units readable. Most
                    // density lives in the moving outer wind, rather than a plug.
                    half shell=smoothstep(.25,.69,rho);
                    half density=cloud*lerp(.42,1,n.g)*edge*dissolve*lerp(.13,1,shell);
                    half a=1-exp(-density*stepLength*_Opacity);
                    half3 deep=half3(.0908,.0252,.2831);
                    half3 light=lerp(deep,_Color.rgb,saturate((n.g-.25)*1.65+cloud*.22));
                    light*=lerp(.6,1.2,n.g)*_Emission;
                    light*=.65+.55*saturate(.5-p.x*.5/radius-p.z*.35/radius);
                    light+=half3(.12,.07,.24)*cloud*n.b*.3;
                    rgb+=(1-alpha)*light*a;alpha+=(1-alpha)*a;
                }
                p+=ray*stepLength;
                float nextC=rc*dc-rs*ds;rs=rs*dc+rc*ds;rc=nextC;
            }
            half soft=1;
            if(_SoftDistance>0)
            {
                float depth=LinearEyeDepth(SampleSceneDepth(i.screen.xy/i.screen.w),_ZBufferParams);
                float eye=-TransformWorldToView(i.world).z;
                soft=saturate((depth-eye)/max(.001,_SoftDistance));
            }
            return half4(rgb/max(.001,alpha),alpha*_Fade*soft);
        }
        ENDHLSL
        Pass
        {
            Name "BoundedVolumeForward" Tags {"LightMode"="UniversalForward"}
            // Funnel mesh is wound inward, so Cull Front selects its near wall.
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
        Pass
        {
            Name "BoundedVolume2D" Tags {"LightMode"="Universal2D"}
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
