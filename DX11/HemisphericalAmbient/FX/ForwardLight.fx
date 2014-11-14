cbuffer cbPerObject {
	float4x4 WorldViewProjection;
	float4x4 World;
	float4x4 gWorldInvTranspose;
}; 

cbuffer cbDirLightPS {
	float3 AmbientDown;
	float3 AmbientRange;
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
};

VS_OUTPUT RenderSceneVS(VS_INPUT input) {
	VS_OUTPUT output;
	float3 vNormalWorldSpace;
	output.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);

	output.UV = input.UV;

	output.Normal = mul(input.Normal, (float3x3)gWorldInvTranspose);

	return output;
}

float3 CalcAmbient(float3 normal, float3 color) {
	float up = normal.y * 0.5 + 0.5;

	float3 ambient = AmbientDown + up * AmbientRange;

	return ambient * color;
}

float4 AmbientLightPS(VS_OUTPUT input) : SV_TARGET0{

	input.Normal = normalize(input.Normal);

	float3 diffuse = DiffuseTexture.Sample(LinearSampler, input.UV).rgb;
	diffuse *= diffuse;

	float3 ambient = CalcAmbient(input.Normal, diffuse);
		return float4(ambient, 1.0);
}

technique11 Ambient {
	pass P0 {
		SetVertexShader(CompileShader(vs_5_0, RenderSceneVS()));
		SetPixelShader(CompileShader(ps_5_0, AmbientLightPS()));
	}
}