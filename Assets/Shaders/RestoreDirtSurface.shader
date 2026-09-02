Shader "Restore/DirtSurface"
{
    Properties
    {
        _MainTex ("Sneaker Texture", 2D) = "white" {}
        _DirtMask ("Dirt Mask", 2D) = "black" {}
        _DirtColor ("Dirt Color", Color) = (0.20,0.13,0.08,1)
        _DirtStrength ("Dirt Strength", Range(0,1)) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.45
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _DirtMask;
        fixed4 _DirtColor;
        half _DirtStrength;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex);
            half dirt = saturate(tex2D(_DirtMask, IN.uv_MainTex).r * _DirtStrength);

            // Keep a little texture variation visible through the dirt so it feels embedded in the material.
            fixed3 dirtyColor = lerp(baseColor.rgb, _DirtColor.rgb, dirt * 0.82);
            o.Albedo = dirtyColor;
            o.Metallic = 0.0;
            o.Smoothness = lerp(_Glossiness, 0.18, dirt);
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Standard"
}
