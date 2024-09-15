Shader "Custom/CheckerboardShader"
{
    Properties
    {
        _MainTex ("UV Map", 2D) = "white" {}
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (0,0,0,1)
        _Frequency ("Frequency", float) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _ColorA;
        fixed4 _ColorB;
        float _Frequency;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void Unity_Checkerboard_float(float2 UV, float3 ColorA, float3 ColorB, float2 Frequency, out float3 Out)
        {
            UV = (UV.xy + 0.5) * Frequency;
            float4 derivatives = float4(ddx(UV), ddy(UV));
            float2 duv_length = sqrt(float2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));
            float width = 1.0;
            float2 distance3 = 4.0 * abs(frac(UV + 0.25) - 0.5) - width;
            float2 scale = 0.35 / duv_length.xy;
            float freqLimiter = sqrt(clamp(1.1f - max(duv_length.x, duv_length.y), 0.0, 1.0));
            float2 vector_alpha = clamp(distance3 * scale.xy, -1.0, 1.0);
            float alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y * freqLimiter);
            Out = lerp(ColorA, ColorB, alpha.xxx);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from the checkerboard function
            Unity_Checkerboard_float(IN.uv_MainTex, _ColorA, _ColorB, float2(_Frequency, _Frequency), o.Albedo);

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.f;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
