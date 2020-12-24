Shader "KSP/TR/BasicVisor"
{
  Properties
  {
    _Color("Main Color", Color)              = (1.0, 1.0, 1.0, 1.0)
    _SpecColor("Specular Color", Color)      = (0.5, 0.5, 0.5, 0.0)
    _Shininess("Shininess", Range(0.01, 1))  = 0.078125
    _MainTex("Base (RGB) Gloss (A)", 2D)     = "white" {}
  }

  SubShader
  {
    LOD 300
    Tags
    {
      "RenderType"      = "Transparent"
      "Queue"           = "Transparent"
      "IgnoreProjector" = "True"
    }

    CGPROGRAM
    #pragma surface surf BlinnPhong alpha
    #pragma target 3.0

    sampler2D   _MainTex;

    fixed4 _Color;
    half   _Shininess;

    struct Input
    {
      float2 uv_MainTex;
      float3 worldRefl;
    };

    void surf(Input IN, inout SurfaceOutput o)
    {
      fixed4 tex  = tex2D(_MainTex, IN.uv_MainTex);

      o.Albedo   = tex.rgb * _Color.rgb;
      o.Gloss    = tex.a;
      o.Specular = _Shininess;
      o.Alpha    = tex.a * _Color.a;
    }
    ENDCG
  }

  FallBack "Transparent/Specular"
}
