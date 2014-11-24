
cbuffer cbPerObject {
	float4x4 WorldViewProjection;
	float4x4 World;
	float4x4 gWorldInvTranspose;
};

// new
cbuffer cbPerObjectPS {
	float3 EyePosition;
	float specExp;
	float specIntensity;
};

cbuffer cbDirLightPS {
	float3 AmbientDown;
	float3 AmbientRange;
	float3 DirToLight; // new
	float3 DirLightColor; // new
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

float3 CalcAmbient(float3 normal, float3 color) {
	float up = normal.y * 0.5 + 0.5;

	float3 ambient = AmbientDown + up * AmbientRange;

	return ambient * color;
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

float3 CalcDirectional(float3 position, Material material) {
	// calculate diffuse light
	float NDotL = dot(DirToLight, material.normal);
	float3 finalColor = DirLightColor.rgb * saturate(NDotL);

	// calculate specular light and add to diffuse
	float3 toEye = EyePosition.xyz - position;
	toEye = normalize(toEye);
	float3 halfway = normalize(toEye + DirToLight);
	float NDotH = saturate(dot(halfway, material.normal));
	finalColor += DirLightColor.rgb * pow(NDotH, material.specExp) * material.specIntensity;

	// scale light color by material color
	return finalColor * material.diffuseColor.rgb;
}

float4 DirectionalLightPS(VS_OUTPUT input) :SV_TARGET0{
	Material material = PrepareMaterial(input.Normal, input.UV);

	float3 finalColor = CalcAmbient(material.normal, material.diffuseColor.rgb);

	finalColor += CalcDirectional(input.WorldPos, material);
	
	return float4(finalColor, 1.0);
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
technique11 DepthPrePass {
	pass P0 {
		SetVertexShader(CompileShader(vs_5_0, RenderSceneVS()));
		SetPixelShader(NULL);
	}
}
technique11 Directional {
	pass P0 {
		SetVertexShader(CompileShader(vs_5_0, RenderSceneVS()));
		SetPixelShader(CompileShader(ps_5_0, DirectionalLightPS()));
	}
}