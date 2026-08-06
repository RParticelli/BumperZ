Shader "BumperZ/AbyssFog"
{
    Properties
    {
        _Color ("Fog Color", Color) = (0.10, 0.20, 0.34, 0.55)
        _Scale ("Cloud Scale", Float) = 7
        _Speed ("Movement", Float) = 0.08
        _Density ("Density", Range(0, 1)) = 0.65
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Scale;
            float _Speed;
            float _Density;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * _Scale;
                float t = _Time.y * _Speed;
                float n1 = sin(p.x * 1.17 + t * 3.1 + sin(p.y * 0.73 - t));
                float n2 = sin(p.y * 1.41 - t * 2.2 + sin(p.x * 0.61 + t * 1.7));
                float n3 = sin((p.x + p.y) * 0.48 + t * 1.2);
                float cloud = saturate(0.52 + n1 * 0.19 + n2 * 0.17 + n3 * 0.12);
                float soft = smoothstep(0.20, 0.86, cloud);
                fixed4 color = _Color;
                color.rgb *= 0.72 + soft * 0.42;
                color.a *= lerp(0.32, 1.0, soft) * _Density;
                return color;
            }
            ENDCG
        }
    }
}
