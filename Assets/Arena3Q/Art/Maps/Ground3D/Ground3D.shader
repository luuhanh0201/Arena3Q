Shader "Arena3Q/Ground 3D"
{
    // The painted battlefield on the 3D ground mesh (docs/KeHoach/ChuyenSang3D.md P2). The painting already carries its
    // light, so flat ground shows it exactly as painted; only slopes (the low hills around the map) get lighter or
    // darker with the battle sun. Drawn first and without depth, so it never hides anything standing on it.
    Properties
    {
        [MainTexture] _BaseMap("Painting", 2D) = "white" {}
        _Relief("Relief shading", Range(0, 2)) = 0.8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half _Relief;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 colour = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                Light sun = GetMainLight();
                // Battle space: the ground is XY and "up" is -Z.
                half flat = saturate(dot(float3(0, 0, -1), sun.direction));
                half lit = saturate(dot(normalize(input.normalWS), sun.direction));
                return half4(colour * max(0.2h, 1.0h + _Relief * (lit - flat)), 1.0h);
            }
            ENDHLSL
        }
    }
}
