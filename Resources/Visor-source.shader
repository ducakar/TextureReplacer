Shader "TR/VertexLit" {
Properties {
 _Color ("Main Color", Color) = (1,1,1,1)
 _SpecColor ("Spec Color", Color) = (1,1,1,0)
 _Shininess ("Shininess", Range(0.1,1)) = 0.7
 _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
 _Cube ("Reflection Cubemap", Cube) = "_Skybox" { TexGen CubeReflect }
}
SubShader {
 LOD 100
 Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="True" "RenderType"="Transparent" }
 Pass {
  Name "BASE"
  Tags { "LIGHTMODE"="Always" }
  ZWrite Off
  Blend One One
  AlphaTest Greater 0
  ColorMask RGB
CGPROGRAM
#pragma exclude_renderers gles xbox360 ps3 gles3
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f {
  float4 pos : SV_POSITION;
  float2 uv  : TEXCOORD0;
  float3 I   : TEXCOORD1;
};

uniform float4 _MainTex_ST;

v2f vert(appdata_tan v)
{
  v2f o;

  // calculate world space reflection vector
  float3 viewDir = WorldSpaceViewDir(v.vertex);
  float3 worldN  = mul((float3x3)_Object2World, v.normal * unity_Scale.w);

  o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
  o.uv  = TRANSFORM_TEX(v.texcoord,_MainTex);
  o.I   = reflect(-viewDir, worldN);
  return o;
}

uniform fixed4      _ReflectColor;
uniform sampler2D   _MainTex;
uniform samplerCUBE _Cube;

fixed4 frag (v2f i) : COLOR
{
  fixed3 reflection = _ReflectColor.xyz * texCUBE(_Cube, i.I).xyz;
  return fixed4(reflection, 1.0);
}
ENDCG
 }
 Pass {
  Tags { "LIGHTMODE"="Vertex" }
  Lighting On
  SeparateSpecular On
  Material {
   Ambient [_Color]
   Diffuse [_Color]
   Specular [_SpecColor]
   Shininess [_Shininess]
  }
  ZWrite Off
  Blend SrcAlpha OneMinusSrcAlpha
  AlphaTest Greater 0
  ColorMask RGB
  SetTexture [_MainTex] { combine texture * primary double, texture alpha * primary alpha }
 }
 Pass {
  Tags { "LIGHTMODE"="VertexLM" }
  BindChannels {
   Bind "vertex", Vertex
   Bind "normal", Normal
   Bind "texcoord1", TexCoord0
   Bind "texcoord", TexCoord1
  }
  ZWrite Off
  Blend SrcAlpha OneMinusSrcAlpha
  AlphaTest Greater 0
  ColorMask RGB
  SetTexture [unity_Lightmap] { Matrix [unity_LightmapMatrix] ConstantColor [_Color] combine texture * constant }
  SetTexture [_MainTex] { combine texture * previous double, texture alpha * primary alpha }
 }
 Pass {
  Tags { "LIGHTMODE"="VertexLMRGBM" }
  BindChannels {
   Bind "vertex", Vertex
   Bind "normal", Normal
   Bind "texcoord1", TexCoord0
   Bind "texcoord1", TexCoord1
   Bind "texcoord", TexCoord2
  }
  ZWrite Off
  Blend SrcAlpha OneMinusSrcAlpha
  AlphaTest Greater 0
  ColorMask RGB
  SetTexture [unity_Lightmap] { Matrix [unity_LightmapMatrix] combine texture * texture alpha double }
  SetTexture [unity_Lightmap] { ConstantColor [_Color] combine previous * constant }
  SetTexture [_MainTex] { combine texture * previous quad, texture alpha * primary alpha }
 }
}
SubShader {
 LOD 100
 Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="True" "RenderType"="Transparent" }
 Pass {
  Tags { "LIGHTMODE"="Always" }
  Lighting On
  SeparateSpecular On
  Material {
   Ambient [_Color]
   Diffuse [_Color]
   Specular [_SpecColor]
   Shininess [_Shininess]
  }
  ZWrite Off
  Blend SrcAlpha OneMinusSrcAlpha
  AlphaTest Greater 0
  ColorMask RGB
  SetTexture [_MainTex] { combine texture * primary double, texture alpha * primary alpha }
 }
}
}
