

cbuffer cbPerObjectVS {
	float4x4 WorldViewProjection;
	float4x4 World;
	float4x4 gWorldInvTranspose;
};

cbuffer cbPerObjectPS {
	float3 EyePosition;
	float specExp;
	float specIntensity;
};

Texture2D DiffuseTexture;
SamplerState LinearSampler {
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = WRAP;
	AddressV = WRAP;
	AddressW = WRAP;
	MaxAnisotropy = 1;
};

struct VS_INPUT {
	float3 Position : POSITION;
	float3 Normal : NORMAL;
	float2 UV : TEXCOORD0;
};

struct VS_OUTPUT {
	float4 Position : SV_POSITION;
	float2 UV : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	float3 WorldPos : TEXCOORD2; // new
};

float4 DepthPrePassVS(float4 Position : POSITION) :SV_POSITION{
	return mul(Position, WorldViewProjection);
}

VS_OUTPUT RenderSceneVS(VS_INPUT input) {
	VS_OUTPUT output;
	float3 vNormalWorldSpace;
	output.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);

	// new
	output.WorldPos = mul(float4(input.Position, 1.0f), World).xyz;

	output.UV = input.UV;

	output.Normal = mul(input.Normal, (float3x3)gWorldInvTranspose);

	return output;
}

struct Material {
	float3 normal;
	float4 diffuseColor;
	float specExp;
	float specIntensity;
};

Material PrepareMaterial(float3 normal, float2 UV) {
	Material material;

	material.normal = normalize(normal);
	material.diffuseColor = DiffuseTexture.Sample(LinearSampler, UV);

	// gamma correct input texture diffuse color to linear-space
	material.diffuseColor.rgb *= material.diffuseColor.rgb;

	material.specExp = specExp;
	material.specIntensity = specIntensity;

	return material;
}