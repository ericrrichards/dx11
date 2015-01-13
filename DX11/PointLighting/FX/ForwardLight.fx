#include "ForwardLightCommon.fx"


cbuffer cbDirLightPS {
	float3 AmbientDown;
	float3 AmbientRange;
	float3 DirToLight; // new
	float3 DirLightColor; // new
};

cbuffer PointLightConstants {
	float3 PointLightPosition;
	float PointLightRangeRcp;
	float3 PointLightColor;
};

float3 CalcAmbient(float3 normal, float3 color) {
	float up = normal.y * 0.5 + 0.5;

	float3 ambient = AmbientDown + up * AmbientRange;

		return ambient * color;
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

float3 CalcPoint(float3 position, Material material) {
	float3 ToLight = PointLightPosition.xyz - position;
		float3 ToEye = EyePosition.xyz - position;
		float DistToLight = length(ToLight);

	// Phong diffuse
	ToLight /= DistToLight; // Normalize
	float NDotL = saturate(dot(ToLight, material.normal));
	float3 finalColor = PointLightColor.rgb * NDotL;

		// Blinn specular
		ToEye = normalize(ToEye);
	float3 HalfWay = normalize(ToEye + ToLight);
		float NDotH = saturate(dot(HalfWay, material.normal));
	finalColor += PointLightColor.rgb * pow(NDotH, material.specExp) * material.specIntensity;

	// Attenuation
	float DistToLightNorm = 1.0 - saturate(DistToLight * PointLightRangeRcp);
	float Attn = DistToLightNorm * DistToLightNorm;
	finalColor *= material.diffuseColor * Attn;

	return finalColor;
}

float4 DirectionalLightPS(VS_OUTPUT input) :SV_TARGET0{
	Material material = PrepareMaterial(input.Normal, input.UV);

	float3 finalColor = CalcAmbient(material.normal, material.diffuseColor.rgb);

		finalColor += CalcDirectional(input.WorldPos, material);

	return float4(finalColor, 1.0);
}
float4 PointLightPS(VS_OUTPUT In) : SV_TARGET0 {
	// Prepare the material structure
	Material material = PrepareMaterial(In.Normal, In.UV);

	// Calculate the point light color
	float3 finalColor = CalcPoint(In.WorldPos, material);

		// Return the final color
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
technique11 Point {
	pass P0 {
		SetVertexShader(CompileShader(vs_5_0, RenderSceneVS()));
		SetPixelShader(CompileShader(ps_5_0, PointLightPS()));
	}
}