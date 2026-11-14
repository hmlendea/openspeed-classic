float4x4 WorldViewProj : WORLDVIEWPROJ;

struct VS_INPUT
{
    float4 Position : POSITION;
};

struct VS_OUTPUT
{
    float4 Position : POSITION;
    float Depth : TEXCOORD0;
};

VS_OUTPUT VS(VS_INPUT input)
{
    VS_OUTPUT output;
    output.Position = mul(input.Position, WorldViewProj);
    output.Depth = output.Position.z / output.Position.w;
    return output;
}

float4 PS(VS_OUTPUT input) : COLOR
{
    return float4(input.Depth, input.Depth, input.Depth, 1.0);
}

technique ShadowMap
{
    pass Pass0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}