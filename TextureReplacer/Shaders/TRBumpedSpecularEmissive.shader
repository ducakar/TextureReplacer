Shader "KSP/TR/Bumped Specular Emissive"
{
  Properties
  {
    [Header(Texture Maps)]
    _MainTex("_MainTex (RGB spec(A))", 2D) = "gray" {}
    _BumpMap("_BumpMap", 2D) = "bump" {}
    _Color ("_Color", Color) = (1,1,1,1)

    [Header(Shininess)]
    _SpecColor ("_SpecColor", Color) = (0.5, 0.5, 0.5, 1)
    //_Shininess ("_Shininess", Range (0.03, 1)) = 0.4 // don't need that shit, getting it from spec mask instead looks more realistic
    _ReflectionMask("_ReflectionMask (RGB)", 2D) = "black" {}
    _Cube("Reflection Cubemap", Cube) = "_Skybox" { }
    [Header(Emissive)]
    _Emissive("_Emissive (RGB)", 2D) = "white" {}
    _EmissiveColor("_EmissiveColor", Color) = (0,0,0,1)

    [Header(Effects)]
    _Opacity("_Opacity", Range(0,1) ) = 1
    _RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
    _RimColor("_RimColor", Color) = (0,0,0,0)
    _TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
    _BurnColor ("Burn Color", Color) = (1,1,1,1)
    _UnderwaterFogFactor ("Underwater Fog Factor", Range(0,1)) = 0
  }

  SubShader
  {
    Tags { "RenderType"="Opaque" }
    ZWrite On
    ZTest LEqual
    Blend SrcAlpha OneMinusSrcAlpha

    CGPROGRAM

    #pragma surface surf BlinnPhongSmooth keepalpha
    #pragma target 3.0

    sampler2D _MainTex;
    sampler2D _BumpMap;
    samplerCUBE _Cube;
    sampler2D _ReflectionMask;

    float4 _EmissiveColor;
    sampler2D _Emissive;

    float _Opacity;
    float _RimFalloff;
    float4 _RimColor;
    float4 _TemperatureColor;
    float4 _BurnColor;
    float4 _Color;

    struct Input
    {
      float2 uv_MainTex;
      float2 uv_BumpMap;
      float2 uv_Emissive;
      float2 uv_ReflectionMask;
      float3 viewDir;
      float3 worldPos;
      float3 worldRefl;
      INTERNAL_DATA
    };

    inline fixed4 LightingBlinnPhongSmooth(SurfaceOutput s, fixed3 lightDir, half3 viewDir, fixed atten)
    {
      s.Normal = normalize(s.Normal);
      half3 h = normalize(lightDir + viewDir);

      fixed diff = max(0, dot(s.Normal, lightDir));

      float nh = max(0, dot(s.Normal, h));
      float spec = pow(nh, s.Specular*128.0) * s.Gloss;

      fixed4 c;
      c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * _SpecColor.rgb * spec) * (atten);
      c.a = s.Alpha + _LightColor0.a * _SpecColor.a * spec * atten;
      return c;
    }

    float4 _LocalCameraPos;
    float4 _LocalCameraDir;
    float4 _UnderwaterFogColor;
    float _UnderwaterMinAlphaFogDistance;
    float _UnderwaterMaxAlbedoFog;
    float _UnderwaterMaxAlphaFog;
    float _UnderwaterAlbedoDistanceScalar;
    float _UnderwaterAlphaDistanceScalar;
    float _UnderwaterFogFactor;

    float4 UnderwaterFog(float3 worldPos, float3 color)
    {
      float3 toPixel = worldPos - _LocalCameraPos.xyz;
      float toPixelLength = length(toPixel);
      float underwaterDetection = _UnderwaterFogFactor * _LocalCameraDir.w;
      float albedoLerpValue = underwaterDetection * (_UnderwaterMaxAlbedoFog * saturate(toPixelLength * _UnderwaterAlbedoDistanceScalar));
      float alphaFactor = 1 - underwaterDetection * (_UnderwaterMaxAlphaFog * saturate((toPixelLength - _UnderwaterMinAlphaFogDistance) * _UnderwaterAlphaDistanceScalar));
      return float4(lerp(color, _UnderwaterFogColor.rgb, albedoLerpValue), alphaFactor);
    }

    void surf (Input IN, inout SurfaceOutput o)
    {
      float4 color = tex2D(_MainTex,(IN.uv_MainTex)) * _BurnColor * _Color;
      float3 emissive = tex2D(_Emissive, (IN.uv_Emissive));
      float4 reftint = tex2D(_ReflectionMask, (IN.uv_ReflectionMask));
      float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

      half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));

      float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
      emission += _TemperatureColor.rgb * _TemperatureColor.a;
      emission += (emissive.rgb * _EmissiveColor.rgb) * _EmissiveColor.a;

      float4 fog = UnderwaterFog(IN.worldPos, color);

      o.Albedo = fog.rgb;
      //o.Emission = emission;
      o.Gloss = color.a;
      o.Specular = color.a;
      o.Normal = normal;

      float3 worldRefl = WorldReflectionVector(IN, o.Normal);
      fixed4 reflcol = texCUBE(_Cube, worldRefl);
      //reflcol *= color.a;

      o.Emission = emission + reflcol.rgb * reftint.rgb;

      o.Emission *= _Opacity * fog.a;
      o.Alpha = _Opacity * fog.a;
    }

    ENDCG
  }
  Fallback "Diffuse"
}
