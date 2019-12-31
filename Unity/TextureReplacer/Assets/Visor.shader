Shader "KSP/TR/Visor"
{
  Properties
  {
    _Color("Main Color", Color)              = (1.0, 1.0, 1.0, 1.0)
    _SpecColor("Specular Color", Color)      = (0.5, 0.5, 0.5, 0.0)
    _Shininess("Shininess", Range(0.01, 1))  = 0.078125
    _ReflectColor("Reflection Color", Color) = (1.0, 1.0, 1.0, 0.5)
    _MainTex("Base (RGB) Gloss (A)", 2D)     = "white" {}
    _Cube("Reflection Cubemap", Cube)        = "_Skybox" {}
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
    samplerCUBE _Cube;

    fixed4 _Color;
    fixed4 _ReflectColor;
    half   _Shininess;

    struct Input
    {
      float2 uv_MainTex;
      float3 worldRefl;
    };

    void surf(Input IN, inout SurfaceOutput o)
    {
      fixed4 tex  = tex2D(_MainTex, IN.uv_MainTex);
      fixed4 refl = texCUBE(_Cube, IN.worldRefl) * tex.a;

      o.Albedo   = tex.rgb * _Color.rgb;
      o.Gloss    = tex.a;
      o.Specular = _Shininess;
      o.Emission = refl.rgb * _ReflectColor.rgb;
      o.Alpha    = refl.a * _Color.a;
    }
    ENDCG
  }

  FallBack "Transparent/Specular"
}
