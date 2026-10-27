// Stylised moonlit night sky for Michael Island: zenith/horizon gradient, hashed star field,
// soft moon disc with halo, and a faint violet nebula band. One pass, no textures (Quest friendly).
Shader "ArcaneWorkshop/Night Sky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.015, 0.02, 0.07, 1)
        _HorizonColor ("Horizon", Color) = (0.16, 0.12, 0.32, 1)
        _GroundColor ("Below Horizon", Color) = (0.03, 0.025, 0.07, 1)
        _NebulaColor ("Nebula", Color) = (0.42, 0.22, 0.75, 1)
        _MoonColor ("Moon", Color) = (0.86, 0.92, 1, 1)
        _MoonDirection ("Moon Direction", Vector) = (-0.35, 0.55, 0.75, 0)
        _MoonSize ("Moon Size", Range(0.005, 0.1)) = 0.035
        _StarDensity ("Star Density", Range(50, 600)) = 260
        _StarBrightness ("Star Brightness", Range(0, 3)) = 1.4
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _ZenithColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            fixed4 _NebulaColor;
            fixed4 _MoonColor;
            float4 _MoonDirection;
            float _MoonSize;
            float _StarDensity;
            float _StarBrightness;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash31(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash31(i);
                float n100 = hash31(i + float3(1, 0, 0));
                float n010 = hash31(i + float3(0, 1, 0));
                float n110 = hash31(i + float3(1, 1, 0));
                float n001 = hash31(i + float3(0, 0, 1));
                float n101 = hash31(i + float3(1, 0, 1));
                float n011 = hash31(i + float3(0, 1, 1));
                float n111 = hash31(i + float3(1, 1, 1));
                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float up = dir.y;

                float skyT = saturate(pow(saturate(up), 0.45));
                float3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, skyT);
                color = lerp(color, _GroundColor.rgb, saturate(-up * 3.0));

                // Nebula band tilted across the sky.
                float band = 1.0 - saturate(abs(dot(dir, normalize(float3(0.35, 0.55, -0.75)))) * 2.6);
                float cloud = noise3(dir * 3.2) * 0.6 + noise3(dir * 7.5) * 0.4;
                color += _NebulaColor.rgb * band * band * cloud * 0.45 * saturate(up * 4.0 + 0.3);

                // Stars: one candidate per cell, twinkling slowly.
                float3 cell = floor(dir * _StarDensity);
                float rnd = hash31(cell);
                float3 starPos = (cell + 0.5 + (float3(hash31(cell + 3.1), hash31(cell + 7.7), hash31(cell + 11.3)) - 0.5) * 0.7) / _StarDensity;
                float d = length(dir - normalize(starPos)) * _StarDensity;
                float star = smoothstep(0.35, 0.0, d) * step(0.965, rnd);
                float twinkle = 0.65 + 0.35 * sin(_Time.y * (1.5 + rnd * 3.0) + rnd * 40.0);
                color += star * twinkle * _StarBrightness * saturate(up * 6.0) * lerp(float3(0.8, 0.85, 1), float3(1, 0.9, 0.8), rnd);

                // Moon disc and halo.
                float3 moonDir = normalize(_MoonDirection.xyz);
                float moonDot = dot(dir, moonDir);
                float moonDist = acos(clamp(moonDot, -1.0, 1.0));
                float disc = smoothstep(_MoonSize, _MoonSize * 0.8, moonDist);
                float crater = noise3(dir * 90.0) * 0.18;
                color = lerp(color, _MoonColor.rgb * (1.0 - crater), disc);
                color += _MoonColor.rgb * exp(-moonDist * 9.0) * 0.22;

                return fixed4(color, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
