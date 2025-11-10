Shader "Custom/InnerCore_Deform"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _EmissionColor ("Emission", Color) = (1,1,1,1)
        _Amplitude ("Deform Amplitude", Range(0,0.2)) = 0.05
        _Frequency ("Deform Frequency", Range(0,10)) = 3.0
        _Speed ("Deform Speed", Range(0,5)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _Color;
        float _Amplitude;
        float _Frequency;
        float _Speed;

        struct Input {
            float2 uv_MainTex;
        };

        void vert(inout appdata_full v)
        {
            float3 noise = float3(
                sin(_Frequency * v.vertex.x + _Time.y * _Speed),
                sin(_Frequency * v.vertex.y + _Time.y * _Speed * 1.1),
                sin(_Frequency * v.vertex.z + _Time.y * _Speed * 1.2)
            );
            v.vertex.xyz += normalize(noise) * _Amplitude;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Emission = c.rgb * 2;
            o.Metallic = 0;
            o.Smoothness = 0.6;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
