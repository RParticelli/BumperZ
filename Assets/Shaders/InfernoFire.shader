Shader "BumperZ/InfernoFire"
{
    Properties { _Intensity ("Intensity", Range(0.5, 3)) = 1.35 }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float seed : TEXCOORD1;
            };

            float _Intensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.seed = frac(dot(v.vertex.xyz, float3(12.9898, 78.233, 37.719)));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float y = saturate(i.uv.y);
                float wave = sin(y * 17.0 - _Time.y * 12.0 + i.seed * 6.28) * (0.035 + y * 0.055);
                float x = abs((i.uv.x + wave - 0.5) * 2.0);
                float halfWidth = lerp(0.86, 0.10, pow(y, 0.72));
                float side = 1.0 - smoothstep(halfWidth * 0.52, halfWidth, x);
                float bottom = smoothstep(0.0, 0.12, y);
                float top = 1.0 - smoothstep(0.68, 1.0, y);
                float alpha = side * bottom * top * i.color.a;

                float core = 1.0 - smoothstep(0.05, halfWidth * 0.48, x);
                fixed3 outer = fixed3(1.0, 0.075, 0.004);
                fixed3 middle = fixed3(1.0, 0.32, 0.015);
                fixed3 inner = fixed3(1.0, 0.92, 0.42);
                fixed3 flame = lerp(outer, middle, saturate(side * 1.25));
                flame = lerp(flame, inner, core * (1.0 - y * 0.65));
                flame *= i.color.rgb * _Intensity;
                return fixed4(flame, alpha);
            }
            ENDCG
        }
    }
}
