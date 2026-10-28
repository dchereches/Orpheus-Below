Shader "OrpheusBelow/OrpheusOilSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Orpheus sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer tint", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Renderer flip", Vector) = (1,1,1,1)
        [PerRendererData] _FrameRect ("Current sprite UV rectangle", Vector) = (0,0,1,1)
        [PerRendererData] _RimDirection ("Screen-space light direction", Vector) = (0.707,0.707,0,0)
        [PerRendererData] _RimColor ("Authored rim colour", Color) = (1,0.62,0.28,1)
        [PerRendererData] _RimIntensity ("Authored rim intensity", Range(0,2)) = 1
        _PaintStrength ("Broad oil strength", Range(0,1)) = 0.88
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off ZWrite On ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            float4 _FrameRect;
            float4 _RimDirection;
            float4 _RimColor;
            float _RimIntensity;
            float _PaintStrength;

            float2 FrameUV(float2 uv)
            {
                float2 inset = min(abs(_MainTex_TexelSize.xy) * 0.5, _FrameRect.zw * 0.49);
                return clamp(uv, _FrameRect.xy + inset, _FrameRect.xy + _FrameRect.zw - inset);
            }

            void PaintTap(float2 uv, float3 centre, float spatialWeight, inout float3 sum, inout float weight)
            {
                float4 tap = tex2D(_MainTex, FrameUV(uv));
                float3 difference = tap.rgb - centre;
                float bilateral = exp2(-10.0 * dot(difference, difference));
                float contribution = spatialWeight * bilateral * smoothstep(0.08, 0.72, tap.a);
                sum += tap.rgb * contribution;
                weight += contribution;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float4 source = tex2D(_MainTex, uv);
                clip(source.a - 0.1);
                float2 local = saturate((uv - _FrameRect.xy) / max(_FrameRect.zw, float2(0.000001, 0.000001)));
                float3 sum = source.rgb * 2.2;
                float weight = 2.2;
                float2 sweep = _FrameRect.zw * float2(0.012, 0.050);
                PaintTap(uv - sweep * 3.0, source.rgb, 0.55, sum, weight);
                PaintTap(uv - sweep * 2.0, source.rgb, 0.80, sum, weight);
                PaintTap(uv - sweep, source.rgb, 1.0, sum, weight);
                PaintTap(uv + sweep, source.rgb, 1.0, sum, weight);
                PaintTap(uv + sweep * 2.0, source.rgb, 0.80, sum, weight);
                PaintTap(uv + sweep * 3.0, source.rgb, 0.55, sum, weight);
                PaintTap(uv + _FrameRect.zw * float2(-0.035, -0.055), source.rgb, 0.65, sum, weight);
                PaintTap(uv + _FrameRect.zw * float2(0.035, 0.055), source.rgb, 0.65, sum, weight);

                float facePreservation = 1.0 - smoothstep(0.68, 0.77, local.y);
                float skin = smoothstep(0.05, 0.12, source.r - source.g) *
                    smoothstep(0.01, 0.07, source.g - source.b) * smoothstep(0.12, 0.24, source.g);
                float finish = _PaintStrength * facePreservation * (1.0 - 0.86 * skin) * smoothstep(0.25, 0.9, source.a);
                float bristle = 0.035 * sin(local.x * 54.0 + local.y * 2.0) + 0.018 * sin(local.x * 151.0);
                float3 paint = (sum / weight) * (1.0 + bristle);
                float3 rgb = lerp(source.rgb, paint, finish);

                float2 lightStep = normalize(_RimDirection.xy + float2(0.0001, 0.0001)) *
                    abs(_MainTex_TexelSize.xy) * 4.0;
                float inwardAlpha = tex2D(_MainTex, FrameUV(uv - lightStep)).a;
                float rim = source.a * saturate(1.0 - inwardAlpha) * saturate(_RimIntensity);
                rim *= 0.55 + 0.45 * smoothstep(0.0, 1.0, dot(local - 0.5, normalize(_RimDirection.xy)) + 0.5);
                rgb += _RimColor.rgb * rim * _RimColor.a;
                return float4(rgb * i.color.rgb, source.a * i.color.a);
            }
            ENDCG
        }
    }
}
