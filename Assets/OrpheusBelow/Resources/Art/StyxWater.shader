Shader "OrpheusBelow/StyxWater"
{
    Properties
    {
        _DeepColor ("Deep teal", Color) = (0.012,0.055,0.064,1)
        _FoamColor ("Silver teal foam", Color) = (0.28,0.52,0.48,1)
        _Surface ("Width, depth, waterfall, opacity", Vector) = (10,10,0,1)
        _FlowTime ("Animation time, ripple time", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "StyxUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _DeepColor, _FoamColor, _Surface, _FlowTime;
            float4 _Ripples[4];
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(cell), hash(cell + float2(1,0)), f.x),
                    lerp(hash(cell + float2(0,1)), hash(cell + 1.0), f.x), f.y);
            }
            float thinLine(float field, float width)
            {
                float aa = max(fwidth(field), .003);
                return 1.0 - smoothstep(width, width + aa, abs(field));
            }
            float4 frag(v2f input) : SV_Target
            {
                float2 p = (input.uv - .5) * _Surface.xy;
                float t = _FlowTime.x;
                float fall = _Surface.z;
                float2 drift = lerp(float2(t * .12, -t * .24), float2(0, t * 1.7), fall);
                float broad = noise(p * .21 + drift * .2);
                float fine = noise(p * float2(.9, 1.3) + drift);
                float2 warped = p + float2(broad - .5, fine - .5) * .65;
                float wave = sin(warped.y * 4.1 + warped.x * .43 - t * .85);
                float crossing = sin(warped.x * 2.4 - warped.y * 1.7 + t * .47);
                float lightBand = pow(saturate(wave * .5 + .5), 12.0)
                    * smoothstep(.28, .8, noise(p * float2(.7, 1.8) + drift * .6));
                // Fine interrupted veins are evaluated per pixel, independent of the mesh resolution.
                float caustic = thinLine(sin(warped.x * 3.3 + fine * 3.0) +
                    sin(warped.y * 4.7 - t * .46 + broad * 2.0), .035);
                caustic *= smoothstep(.36, .76, fine) * .055;
                float current = thinLine(wave, .025) * smoothstep(.52, .82, fine) * .11;
                float edge = min((_Surface.x * .5) - abs(p.x), (_Surface.y * .5) - abs(p.y));
                float foam = (1.0 - smoothstep(.025, .22 + fine * .16, edge))
                    * (.18 + fine * .26);
                float ripple = 0.0;
                [unroll] for (int r = 0; r < 4; r++)
                {
                    float age = _FlowTime.y - _Ripples[r].z;
                    float alive = step(0.0, age) * (1.0 - step(1.7, age));
                    float distanceToCenter = length(p - _Ripples[r].xy);
                    float ring = thinLine(distanceToCenter - age * 2.2, .035);
                    ripple += ring * saturate(1.0 - age / 1.7) * alive * .42;
                }
                float waterfallThread = pow(saturate(sin(p.x * 21.0 + fine * 2.0) * .5 + .5), 9.0)
                    * (.5 + .5 * sin(p.y * 8.0 + t * 9.0 + broad));
                float detail = .025 + broad * .055 + lightBand * .10 + caustic + current;
                detail = lerp(detail, .08 + fine * .09 + waterfallThread * .27, fall);
                float3 deep = lerp(_DeepColor.rgb, float3(.012, .049, .057), .32);
                float3 foamColor = lerp(_FoamColor.rgb, float3(.32, .48, .43), .42);
                float3 color = lerp(deep * (.84 + broad * .3), foamColor,
                    saturate(detail + foam + ripple * (1.0 - fall)));
                return float4(color, lerp(.99, .85, fall) * _Surface.w);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
