Shader "OrpheusBelow/EurydiceOilSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Original committed sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer tint", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Renderer flip", Vector) = (1,1,1,1)
        [PerRendererData] _FrameRect ("Current sprite UV rectangle", Vector) = (0,0,1,1)
        _PaintStrength ("Interior paint strength", Range(0,1)) = 0.92
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off ZWrite Off ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            float4 _FrameRect;
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
                // Colour edges and transparent gutters never contribute neighbouring hair/skin/cloth.
                float bilateral = exp2(-12.0 * dot(difference, difference));
                float contribution = spatialWeight * bilateral * smoothstep(0.1, 0.8, tap.a);
                sum += tap.rgb * contribution;
                weight += contribution;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // The original centre sample alone supplies alpha: no filtered alpha or UV deformation.
                float2 uv = i.texcoord;
                float4 source = tex2D(_MainTex, uv);
                float2 local = saturate((uv - _FrameRect.xy) / max(_FrameRect.zw, float2(0.000001, 0.000001)));
                float3 sum = source.rgb * 2.0;
                float weight = 2.0;
                // Anisotropic connected swipes, long in the cloth/hair direction rather than a box blur.
                float2 stroke = _FrameRect.zw * float2(0.009, 0.045);
                PaintTap(uv - stroke * 3.0, source.rgb, 0.65, sum, weight);
                PaintTap(uv - stroke * 2.0, source.rgb, 0.85, sum, weight);
                PaintTap(uv - stroke, source.rgb, 1.0, sum, weight);
                PaintTap(uv + stroke, source.rgb, 1.0, sum, weight);
                PaintTap(uv + stroke * 2.0, source.rgb, 0.85, sum, weight);
                PaintTap(uv + stroke * 3.0, source.rgb, 0.65, sum, weight);
                PaintTap(uv + _FrameRect.zw * float2(-0.028, -0.05), source.rgb, 0.6, sum, weight);
                PaintTap(uv + _FrameRect.zw * float2(0.028, 0.05), source.rgb, 0.6, sum, weight);

                float facePreservation = 1.0 - smoothstep(0.70, 0.75, local.y);
                float skin = smoothstep(0.055, 0.12, source.r - source.g)
                    * smoothstep(0.015, 0.065, source.g - source.b)
                    * smoothstep(0.12, 0.22, source.g);
                float finish = _PaintStrength * facePreservation * (1.0 - 0.85 * skin)
                    * smoothstep(0.3, 0.9, source.a);
                float3 paint = sum / weight;
                // Stable UV-anchored bristles run along the swipe; no frame-time noise or shimmer.
                float bristles = 0.024 * sin(local.x * 140.0 + sin(local.y * 5.0) * 1.2)
                    + 0.045 * sin(local.x * 48.0 + local.y * 1.5);
                paint *= 1.0 + bristles;
                float3 rgb = lerp(source.rgb, paint, finish);
                // SpriteVert already includes vertex, material and renderer colour, plus requested flip.
                float4 tint = i.color;
                return float4(rgb * tint.rgb, source.a * tint.a);
            }
            ENDCG
        }
    }
}
