// UV Distortion: a second noise texture scrolls over time and its value displaces the UVs used
// to sample the main brew texture, giving the cauldron liquid a blobby, bubbling wobble.
Shader "ArcaneWorkshop/Distorted Brew"
{
    Properties
    {
        _MainTex ("Brew Texture", 2D) = "white" {}
        _NoiseTex ("Distortion Noise", 2D) = "gray" {}
        _Color ("Tint", Color) = (0.25, 0.95, 1, 1)
        _Emission ("Glow Strength", Range(0, 4)) = 1.4
        _NoiseScroll ("Noise Scroll (xy)", Vector) = (0.07, 0.11, 0, 0)
        _Distortion ("Distortion Strength", Range(0, 0.5)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            fixed4 _Color;
            float _Emission;
            float4 _NoiseScroll;
            float _Distortion;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 noiseUv : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.noiseUv = TRANSFORM_TEX(v.uv, _NoiseTex) + _NoiseScroll.xy * _Time.y;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 noise = tex2D(_NoiseTex, i.noiseUv).rg * 2.0 - 1.0;
                float2 noise2 = tex2D(_NoiseTex, i.noiseUv * 0.6 - _NoiseScroll.yx * _Time.y).gb * 2.0 - 1.0;
                float2 uv = i.uv + (noise + noise2) * _Distortion;
                fixed4 col = tex2D(_MainTex, uv) * _Color;
                col.rgb *= _Emission;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
