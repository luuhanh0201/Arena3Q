Shader "Arena3Q/MacAnh/Arcane Flow 3D"
{
    Properties
    {
        _MainTex("Shape", 2D) = "white" {}
        _NoiseTex("Flow noise", 2D) = "gray" {}
        _CloudTex("Organic wind density", 2D) = "white" {}
        [HDR] _Color("Energy", Color) = (0.588,0.341,0.937,1)
        _Emission("Radiance", Float) = 1.6
        _Opacity("Density", Range(0,1)) = 0.7
        _Mode("0 volume / 1 ribbon / 2 glyph", Float) = 0
        _Flow("UV flow", Vector) = (0.25,-0.7,0,0)
        _SoftDistance("Soft intersection metres", Float) = 0.12
        _Fade("Envelope", Range(0,1)) = 1
        _Age("Local clock", Float) = 0
        _Dissolve("Dissolve", Range(0,1)) = 0
        _SoftShape("Feather billboard edges", Range(0,1)) = 0
        _CoreHighlight("Core whitening", Range(0,1)) = .72
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination blend",Float)=10
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_CloudTex); SAMPLER(sampler_CloudTex);
        CBUFFER_START(UnityPerMaterial)
        float4 _Color, _Flow;
        float _Emission, _Opacity, _Mode, _SoftDistance, _Fade, _Age, _Dissolve, _SoftShape, _CoreHighlight;
        CBUFFER_END
        struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 motion:TEXCOORD1; half4 color:COLOR; };
        struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float3 normal:TEXCOORD1; float3 world:TEXCOORD2; float4 screen:TEXCOORD3; float seed:TEXCOORD4; };
        V vert(A v)
        {
            V o;
            if(_Mode<1.5)
            {
                float angle=atan2(v.pos.z,v.pos.x);
                float ripple=(sin(v.pos.y*9-angle*3+_Age*4)*.035+sin(v.pos.y*17+angle*2-_Age*3)*.014)*sin(saturate(v.uv.x)*3.14159);
                v.pos.xz+=normalize(v.pos.xz+float2(.001,0))*ripple;
            }
            o.world=TransformObjectToWorld(v.pos.xyz); o.pos=TransformWorldToHClip(o.world);
            o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; o.color=v.color;
            o.screen=ComputeScreenPos(o.pos);o.seed=v.motion.x; return o;
        }
        half4 frag(V i):SV_Target
        {
            float2 uv=i.uv;
            half n=SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,uv*float2(3,2)+_Flow.xy*_Age).r;
            half m=SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,uv*float2(7,3)-_Flow.yx*_Age*.63).g;
            half density=1;
            half highlight=0;
            if (_Mode<.5)
            {
                half spiral=sin((uv.y*5.4-uv.x*3.0+_Age*1.8+n*.45)*6.283)*.5+.5;
                half cloud=saturate(SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,float2(frac(uv.x+_Age*.11+n*.055),frac(uv.y*1.25-_Age*.19))).a);
                density=smoothstep(.06,.76,n*.45+m*.2+spiral*.25)*pow(cloud,.42)*1.45;
                density*=smoothstep(0,.09,uv.y)*(1-smoothstep(.69+n*.11,.9+m*.095,uv.y));
                half rim=pow(saturate(1-abs(dot(normalize(i.normal),GetWorldSpaceNormalizeViewDir(i.world)))),2);
                highlight=spiral*.12+rim*.18;
            }
            else if (_Mode<1.5)
            {
                density=pow(saturate(sin(uv.y*3.141593)),1.6)*smoothstep(0,.07,uv.x)*(1-smoothstep(.87,1,uv.x));
                half cloud=saturate(SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,float2(frac(uv.x*1.5-_Age*.25+n*.04),uv.y)).a);
                density*=lerp(.3,1,n*.65+m*.35)*lerp(.05,1,pow(cloud,.65));
                highlight=pow(saturate(1-abs(uv.y-.48)*4),3)*.25;
            }
            else if(_Mode<2.5)
            {
                half4 shape=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                float2 centred=uv*2-1;
                half border=pow(saturate(1-dot(centred,centred)),1.5);
                density=shape.a*lerp(1,border,_SoftShape);
            }
            else if(_Mode<3.5)
            {
                half rim=pow(saturate(1-abs(dot(normalize(i.normal),GetWorldSpaceNormalizeViewDir(i.world)))),2);
                float2 cell=uv*float2(16,10);cell.x+=fmod(floor(cell.y),2)*.5;
                float2 p=abs(frac(cell)-.5);
                float hex=max(p.x*.866+p.y*.5,p.y);
                half grid=1-smoothstep(.015,.045,abs(hex-.42));
                density=.035+rim*.72+grid*.35;highlight=rim*.3+grid*.12;
            }
            else if(_Mode<4.5)
            {
                // A luminous core must remain readable when a cloud-noise
                // sample happens to be transparent, including over bright sand.
                density=pow(saturate(sin(uv.y*3.141593)),.65)
                        *smoothstep(0,.04,uv.x)*(1-smoothstep(.94,1,uv.x));
                highlight=_CoreHighlight;
            }
            else
            {
                float localAge=_Age-i.seed;
                half pulse=smoothstep(0,.1,localAge)*(1-smoothstep(.72,1.2,localAge));
                density=smoothstep(0,.06,uv.y)*(1-smoothstep(.8+n*.08,.98,uv.y))*pulse;
                density*=lerp(.72,1,n);highlight=.8;
            }
            // Solid rune/circle masks fade smoothly; thresholding their sparse
            // pixels was making the final frames flicker and disappear in chunks.
            half dissolve=_Mode>1.5?1:smoothstep(_Dissolve-.07,_Dissolve+.13,n*.8+.2);
            float depth=LinearEyeDepth(SampleSceneDepth(i.screen.xy/i.screen.w),_ZBufferParams);
            float eye=-TransformWorldToView(i.world).z;
            half soft=_SoftDistance>0 ? saturate((depth-eye)/max(.001,_SoftDistance)):1;
            half alpha=saturate(density*_Opacity*_Color.a*i.color.a*_Fade*dissolve*soft);
            half3 rgb=lerp(_Color.rgb*i.color.rgb,half3(.839,.725,1),highlight)*_Emission;
            if(_Mode<.5) rgb*=.65+.35*saturate(dot(normalize(i.normal),normalize(float3(-.6,.5,-.4))));
            return half4(rgb,alpha);
        }
        ENDHLSL
        Pass
        {
            Name "ArcaneForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha [_DstBlend]
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            ENDHLSL
        }
        // The battle still uses the project's 2D renderer. The same real mesh is
        // supported there; the isolated inspection camera uses UniversalRenderer.
        Pass
        {
            Name "Arcane2D"
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha [_DstBlend]
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            ENDHLSL
        }
    }
}
