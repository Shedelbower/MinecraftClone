
Shader "Custom/Fluid"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _BreakColor ("Break Color", Color) = (1,1,1,1)
        _ReflectColor ("Reflect Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _WindDir ("Wind Direction", Vector) = (1.0, 0, 0, 0)
        _NoiseTex ("Noise (RGB)", 2D) = "black" {}
        _NoiseScale ("Noise Scale", Range(0.001, 10000.0)) = 100.0
        _NoiseMin ("Noise Min", Range(0,1)) = 0.0
        _NoiseMax ("Noise Max", Range(00,1)) = 1
        _TimeScale ("Time Scale", Range(0.001, 2.0)) = 0.5
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 200
        Cull Off // Make double sided
        ZTest Less

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        //#pragma surface surf Standard fullforwardshadows
        #pragma surface surf Standard vertex:vert addshadow alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NoiseTex;
        
        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
            float3 viewDir;
            float3 worldNormal;
        };

        float _NoiseScale;
        float _NoiseMin;
        float _NoiseMax;
        float _TimeScale;
        float4 _WindDir;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _BreakColor;
        fixed4 _ReflectColor;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v)
        {     
            if (v.color.b > 0.0001) { // Use blue vertex color as mask for whether this vertex should be wavy
                
                float3 worldPos = mul(unity_ObjectToWorld,v.vertex);
                float2 uv_NoiseTex = worldPos.xz / _NoiseScale + float2(_Time.y * _TimeScale, _Time.y * _TimeScale);

                float noise = tex2Dlod (_NoiseTex, float4(uv_NoiseTex, 0, 0)).r;

                v.color = half4((_BreakColor * noise).rgb,0.0);

                noise = (noise - _NoiseMin) / (_NoiseMax - _NoiseMin);
                v.vertex += (_WindDir * noise);
                
                v.vertex -= 0.01;
            }
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color + IN.color;
            
            // Reflection
            //float t = 1.0 - abs(dot(normalize(IN.viewDir), IN.worldNormal));
            //float alpha = lerp(0.2,1.0,t);
            float alpha = 0.6;

            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = alpha;

            
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/VertexLit"
}
