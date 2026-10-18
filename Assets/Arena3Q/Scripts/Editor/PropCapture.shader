// Used only by PropSpriteRenderer (editor) to draw 3D props into a render texture through a CommandBuffer,
// independent of the project's 2D Renderer. Matrices come from material properties, not from a camera.
Shader "Hidden/Arena3Q/PropCapture"
{
    Properties
    {
        _BaseMap ("Base colour", 2D) = "white" {}
        _NormalMap ("Normal (linear RGB)", 2D) = "black" {}
    }
    SubShader
    {
        // Pass 0: soft-lit colour.
        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4x4 _PropModel;
            float4x4 _PropViewProj;
            float3 _LightDir;
            float _Ambient;
            float _Diffuse;
            float _LightWrap;
            float _HasNormalMap;
            Texture2D _BaseMap;
            SamplerState sampler_BaseMap;
            Texture2D _NormalMap;
            SamplerState sampler_NormalMap;

            struct Attributes
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 tangent : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 world = mul(_PropModel, float4(input.position.xyz, 1.0));
                output.position = mul(_PropViewProj, world);
                output.uv = input.uv;
                output.normal = mul((float3x3)_PropModel, input.normal);
                output.tangent = float4(mul((float3x3)_PropModel, input.tangent.xyz), input.tangent.w);
                return output;
            }

            float4 Frag(Varyings input, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                float3 normal = normalize(input.normal);
                if (!frontFace)
                    normal = -normal;
                if (_HasNormalMap > 0.5)
                {
                    float3 tangent = normalize(input.tangent.xyz - normal * dot(normal, input.tangent.xyz));
                    float3 bitangent = cross(normal, tangent) * (input.tangent.w < 0.0 ? -1.0 : 1.0);
                    float3 local = _NormalMap.Sample(sampler_NormalMap, input.uv).rgb * 2.0 - 1.0;
                    normal = normalize(local.x * tangent + local.y * bitangent + local.z * normal);
                }

                float wrapped = saturate((dot(normal, _LightDir) + _LightWrap) / (1.0 + _LightWrap));
                float sky = 0.85 + 0.15 * normal.y;
                float3 colour = _BaseMap.Sample(sampler_BaseMap, input.uv).rgb;
                return float4(colour * (_Ambient * sky + _Diffuse * wrapped), 1.0);
            }
            ENDHLSL
        }

        // Pass 1: shadow mask, the mesh flattened onto the ground (y = 0) along the light.
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4x4 _PropModel;
            float4x4 _PropViewProj;
            float3 _LightDir;

            float4 Vert(float4 position : POSITION) : SV_POSITION
            {
                float3 world = mul(_PropModel, float4(position.xyz, 1.0)).xyz;
                world -= _LightDir * (max(world.y, 0.0) / _LightDir.y);
                return mul(_PropViewProj, float4(world.x, 0.0, world.z, 1.0));
            }

            float4 Frag() : SV_Target
            {
                return float4(1.0, 1.0, 1.0, 1.0);
            }
            ENDHLSL
        }
    }
}
