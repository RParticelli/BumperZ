Shader "BumperZ/OilPuddle"
{
    Properties
    {
        _BaseColor ("Base", Color) = (0.025, 0.035, 0.05, 1)
        _SheenA ("Sheen A", Color) = (0.02, 0.65, 0.9, 1)
        _SheenB ("Sheen B", Color) = (0.8, 0.08, 0.55, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+5" }
        LOD 180

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _BaseColor;
        fixed4 _SheenA;
        fixed4 _SheenB;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float waveA = sin(IN.worldPos.x * 7.0 + IN.worldPos.z * 5.0 + _Time.y * 0.8);
            float waveB = sin(IN.worldPos.x * -4.0 + IN.worldPos.z * 9.0 - _Time.y * 0.55);
            float band = saturate((waveA + waveB) * 0.22 + 0.42);
            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), float3(0, 1, 0))), 2.0);
            fixed3 sheen = lerp(_SheenA.rgb, _SheenB.rgb, saturate(waveB * 0.5 + 0.5));

            o.Albedo = lerp(_BaseColor.rgb, sheen, band * 0.30 + fresnel * 0.16);
            o.Metallic = 0.72;
            o.Smoothness = 0.98;
            o.Emission = sheen * (band * 0.16 + fresnel * 0.25);
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Standard"
}
