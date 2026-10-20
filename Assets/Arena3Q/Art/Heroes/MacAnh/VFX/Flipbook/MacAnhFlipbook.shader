Shader "Arena3Q/MacAnh Flipbook 2D"
{
    // Pre-rendered VFX frames: RGB = light added on top (rendered on black), A = how much a dark underlay covers
    // what is behind, so a glow still reads on the bright sand. Premultiplied blend does both in one pass.
    // Qi look (user review 2026-10-20): a slow drifting noise varies the density everywhere (_Breath), and appearing /
    // vanishing erodes along the same noise with a soft glowing edge (_Dissolve) instead of a flat alpha fade.
    // The noise lives in the sprite's local space, so it does not jump between flipbook frames.
    // Frame blending (spec §1, 2026-10-21): MacAnhVfxPlayer draws one quad covering the current and the next frame
    // (they may sit on different atlas pages with different crops and pivots); each frame is sampled through its own
    // UV set and masked to its own atlas rect, then mixed by _FrameBlend on the effect clock. A plain SpriteRenderer
    // (afterimage glow, screen dim, references) leaves _FrameBlend at 0 and the masks at "everything".
    // Two passes with the same code: Universal2D for the 2D Renderer, UniversalForward for the 3D (Forward) renderer.
    Properties
    {
        [PerRendererData] _MainTex("Frames", 2D) = "black" {}
        _Glow("Glow", Range(0,4)) = 1
        _Underlay("Underlay", Range(0,1)) = 0.6
        _UnderlayColor("Underlay colour", Color) = (0.2,0.08,0.38,1)
        _Fade("Fade", Range(0,1)) = 1
        _Dissolve("Dissolve (0 = whole, 1 = gone)", Range(0,1)) = 0
        _Breath("Density variation", Range(0,1)) = 0
        _NoiseScale("Noise scale (per metre)", Float) = 2.5
        _Drift("Noise drift (m/s)", Float) = 0.25
        _EdgeGlow("Dissolve edge glow", Range(0,3)) = 1
        _EdgeColor("Dissolve edge colour", Color) = (0.84,0.73,1,1)
        _Seed("Seed", Float) = 0
        [PerRendererData] _EffectTime("Effect time (seconds)", Float) = 0
        [PerRendererData] _NextTex("Next frame", 2D) = "black" {}
        [PerRendererData] _FrameBlend("Next frame weight", Range(0,1)) = 0
        [PerRendererData] _RectA("Current frame atlas rect (min xy, max zw)", Vector) = (-1,-1,2,2)
        [PerRendererData] _RectB("Next frame atlas rect (min xy, max zw)", Vector) = (-1,-1,2,2)
        [PerRendererData] _Tint("Tint and fade (mesh layers; sprites use the vertex colour)", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float2 uvNext : TEXCOORD1; half4 color : COLOR; };
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float2 local : TEXCOORD1; float2 uvNext : TEXCOORD2; };
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_NextTex); SAMPLER(sampler_NextTex);
        CBUFFER_START(UnityPerMaterial)
        half _Glow; half _Underlay; half4 _UnderlayColor; half _Fade;
        half _Dissolve; half _Breath; float _NoiseScale; float _Drift; half _EdgeGlow; half4 _EdgeColor; float _Seed; float _EffectTime;
        half _FrameBlend; float4 _RectA; float4 _RectB; half4 _Tint;
        CBUFFER_END

        float Hash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }
        float ValueNoise(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            float2 u = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
        }
        float Fbm(float2 p)
        {
            return 0.55 * ValueNoise(p) + 0.3 * ValueNoise(p * 2.07 + 7.3) + 0.15 * ValueNoise(p * 4.31 + 3.1);
        }
        // 1 inside the frame's own atlas rect, 0 outside (the quad also covers the other frame's area).
        half InRect(float2 uv, float4 rect)
        {
            return step(rect.x, uv.x) * step(uv.x, rect.z) * step(rect.y, uv.y) * step(uv.y, rect.w);
        }

        Varyings Vert(Attributes input)
        {
            Varyings o;
            o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            o.uv = input.uv; o.uvNext = input.uvNext; o.color = input.color * _Tint;
            o.local = input.positionOS.xy;
            return o;
        }
        half4 Frag(Varyings input) : SV_Target
        {
            half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * InRect(input.uv, _RectA);
            if (_FrameBlend > 0.0005)
                t = lerp(t, SAMPLE_TEXTURE2D(_NextTex, sampler_NextTex, input.uvNext) * InRect(input.uvNext, _RectB), _FrameBlend);
            float2 p = input.local * _NoiseScale + _Seed + float2(_EffectTime * _Drift, _EffectTime * _Drift * 0.6);
            float n = Fbm(p);
            // Density: thicker and thinner patches that drift (qi, not ink lines).
            half density = lerp(1.0, saturate(0.35 + 1.15 * n), _Breath);
            // Erosion threshold sweeps the noise: 0 = everything visible, 1 = nothing left.
            float threshold = _Dissolve * 1.3 - 0.3;
            half keep = smoothstep(threshold, threshold + 0.22, n);
            half edge = _Dissolve > 0.001 ? saturate(keep * (1.0 - keep) * 4.0) : 0.0;
            // Vertex colour: rgb tints the glow, a is the per-layer fade from the player.
            half fade = _Fade * input.color.a * keep;
            half cover = t.a * _Underlay * fade * density;
            half3 light = t.rgb * input.color.rgb * _Glow * fade * density
                        + _EdgeColor.rgb * edge * _EdgeGlow * t.a * _Fade * input.color.a;
            return half4(light + _UnderlayColor.rgb * cover, cover);
        }
        ENDHLSL
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend One OneMinusSrcAlpha
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
