Shader "Arena3Q/MacAnh/Cyclone Wisps"
{
    Properties
    {
        _MainTex("Rune mask",2D)="white"{}
        _CloudTex("Wind density",2D)="white"{}
        _NoiseTex("Turbulence",2D)="gray"{}
        [HDR] _Color("Linear tint",Color)=(.65,.48,1,1)
        _Emission("Radiance",Float)=1.2
        _Opacity("Optical density",Range(0,1))=.7
        _Rune("Independent rune orbits",Float)=0
        _Age("Explicit clock",Float)=0
        _Fade("Envelope",Float)=1
        _Dissolve("Dissolve",Float)=0
        _SoftDistance("Intersection softness",Float)=.12
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
        TEXTURE2D(_CloudTex);SAMPLER(sampler_CloudTex);
        TEXTURE2D(_NoiseTex);SAMPLER(sampler_NoiseTex);
        CBUFFER_START(UnityPerMaterial)
        float4 _Color;
        float _Emission,_Opacity,_Rune,_Age,_Fade,_Dissolve,_SoftDistance;
        CBUFFER_END
        struct A {float4 pos:POSITION;float2 uv:TEXCOORD0;float4 motion:TEXCOORD1;half4 color:COLOR;};
        struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float4 screen:TEXCOORD2;half4 color:COLOR;float2 motion:TEXCOORD3;};
        V vert(A v)
        {
            V o;float seed=v.motion.x;
            if(_Rune>.5)
            {
                float s,c;sincos(_Age*(.48+seed*.55),s,c);
                v.pos.xz=float2(c*v.pos.x-s*v.pos.z,s*v.pos.x+c*v.pos.z);
                v.pos.y+=.085*(sin(_Age*(.8+seed*.4)+seed*6.283)-sin(seed*6.283));
            }
            else
            {
                float envelope=sin(saturate(v.uv.x)*3.141593);
                float ripple=sin(v.uv.x*9+_Age*(1.1+seed)+seed*7)*.026+sin(v.pos.y*8-_Age*.75)*.014;
                v.pos.xz+=normalize(v.pos.xz+float2(.001,0))*ripple*envelope;
                v.pos.y+=sin(v.uv.x*7-_Age*(.8+seed)+seed*4)*.026*envelope;
            }
            o.world=TransformObjectToWorld(v.pos.xyz);o.pos=TransformWorldToHClip(o.world);o.screen=ComputeScreenPos(o.pos);
            o.uv=v.uv;o.color=v.color;o.motion=v.motion.xy;return o;
        }
        half4 frag(V i):SV_Target
        {
            if(_Fade<.0001)return 0;
            half alpha;
            if(_Rune>.5)
            {
                alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                alpha*=1-smoothstep(.18+i.motion.x*.28,.64+i.motion.x*.28,_Dissolve);
            }
            else
            {
                float2 uv=i.uv;float seed=i.motion.x;
                half n=SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,float2(uv.x*2-_Age*.13,uv.y*.6+seed)).r;
                half cloud=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,float2(uv.x*1.3-_Age*(.11+seed*.05)+n*.08,uv.y)).a;
                half ends=smoothstep(0,.2,uv.x)*(1-smoothstep(.74,1,uv.x));
                half width=exp2(-14*(uv.y-.5)*(uv.y-.5))*(1-smoothstep(.36,.5,abs(uv.y-.5)));
                half breakup=smoothstep(.26,.68,n+cloud*.16);
                alpha=ends*width*lerp(.2,1,cloud)*breakup*smoothstep(_Dissolve-.18,_Dissolve+.22,n*.8+.2);
            }
            half soft=1;
            if(_SoftDistance>0)
            {
                float depth=LinearEyeDepth(SampleSceneDepth(i.screen.xy/i.screen.w),_ZBufferParams);
                soft=saturate((depth+TransformWorldToView(i.world).z)/max(.001,_SoftDistance));
            }
            return half4(_Color.rgb*i.color.rgb*_Emission,saturate(alpha*_Color.a*i.color.a*_Opacity*_Fade*soft));
        }
        ENDHLSL
        Pass
        {
            Name "CycloneWispsForward" Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
        Pass
        {
            Name "CycloneWisps2D" Tags {"LightMode"="Universal2D"}
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
