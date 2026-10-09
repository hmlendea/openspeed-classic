float4x4 WorldViewProj : WORLDVIEWPROJ;
float4x4 ShadowViewProj : SHADOWVIEWPROJ;
texture ShadowMap : SHADOWMAP;
sampler ShadowSampler = sampler_state
{
    Texture = <ShadowMap>;
    AddressU = Clamp;
    AddressV = Clamp;
    MagFilter = Linear;
    MinFilter = Linear;
    MipFilter = Linear;
};

struct VS_INPUT
{
    float4 Position : POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VS_OUTPUT
{
    float4 Position : POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
    float4 ShadowPos : TEXCOORD1;
};

VS_OUTPUT VS(VS_INPUT input)
{
    VS_OUTPUT output;
    output.Position = mul(input.Position, WorldViewProj);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    output.ShadowPos = mul(input.Position, ShadowViewProj);
    return output;
}

float4 PS(VS_OUTPUT input) : COLOR
{
    float4 color = input.Color;

    // Shadow mapping
    float3 shadowCoord = input.ShadowPos.xyz / input.ShadowPos.w;
    shadowCoord.x = shadowCoord.x * 0.5 + 0.5;
    shadowCoord.y = -shadowCoord.y * 0.5 + 0.5;

    if (shadowCoord.x >= 0 && shadowCoord.x <= 1 &&
        shadowCoord.y >= 0 && shadowCoord.y <= 1 &&
        shadowCoord.z >= 0 && shadowCoord.z <= 1)
    {
        float shadowDepth = tex2D(ShadowSampler, shadowCoord.xy).r;
        float bias = 0.005;
        if (shadowCoord.z > shadowDepth + bias)
        {
            color.rgb *= 0.5; // Shadow intensity
        }
    }

    return color;
}

technique ShadowMap
{
    pass Pass0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}