Shader "BumperZ/CarTrailFire"
{
    SubShader
    {
        Tags { "Queue"="Transparent+15" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=(i.uv-.5)*2.0;
                p.x += sin(p.y*9.0-_Time.y*14.0)*.09*(p.y+.8);
                float width=lerp(.82,.16,saturate(i.uv.y));
                float side=1.0-smoothstep(width*.45,width,abs(p.x));
                float ends=smoothstep(-1.0,-.65,p.y)*(1.0-smoothstep(.35,1.0,p.y));
                float a=side*ends*i.color.a;
                float core=1.0-smoothstep(.03,width*.38,abs(p.x));
                fixed3 rgb=lerp(i.color.rgb,lerp(i.color.rgb,fixed3(1,1,1),.72),core*(1.0-i.uv.y*.55));
                return fixed4(rgb*1.35,a);
            }
            ENDCG
        }
    }
}
