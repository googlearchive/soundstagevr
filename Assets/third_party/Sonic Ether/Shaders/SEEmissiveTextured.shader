// This is a placeholder script so that SoundStage runs without Sonic Ether Natural Bloom & Dirty Lens from the Unity Asset Store
// You can purchase the asset here: http://u3d.as/7v5

Shader "Sonic Ether/Emissive/Textured" {
Properties {
	_EmissionColor ("Emission Color", Color) = (1,1,1,1)
	_DiffuseColor ("Diffuse Color", Color) = (1, 1, 1, 1)
	_MainTex ("Diffuse Texture", 2D) = "White" {}
	_Illum ("Emission Texture", 2D) = "white" {}
	_EmissionGain ("Emission Gain", Range(0, 1)) = 0.5
	_EmissionTextureContrast ("Emission Texture Contrast", Range(1, 3)) = 1.0
}

SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 200
	
CGPROGRAM
#pragma surface surf Lambert

sampler2D _MainTex;
sampler2D _Illum;
fixed4 _DiffuseColor;
fixed4 _EmissionColor;
float _EmissionGain;
float _EmissionTextureContrast;

struct Input {
	float2 uv_MainTex;
	float2 uv_Illum;
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
	fixed4 c = tex * _DiffuseColor;
	o.Albedo = c.rgb;
	
	o.Emission = _EmissionColor * (exp(_EmissionGain * 10.0f));
	o.Alpha = c.a;
}
ENDCG
} 
FallBack "Self-Illumin/VertexLit"
}
