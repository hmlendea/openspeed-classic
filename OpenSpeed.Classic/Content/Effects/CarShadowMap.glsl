#version 330

in vec4 Position;
in vec4 Color;
in vec2 TexCoord;
in vec4 ShadowPos;

out vec4 FragColor;

uniform mat4 WorldViewProj;
uniform mat4 ShadowViewProj;
uniform sampler2D ShadowMap;

void main()
{
    vec4 color = Color;

    vec3 shadowCoord = ShadowPos.xyz / ShadowPos.w;
    shadowCoord.x = shadowCoord.x * 0.5 + 0.5;
    shadowCoord.y = -shadowCoord.y * 0.5 + 0.5;

    if (shadowCoord.x >= 0.0 && shadowCoord.x <= 1.0 &&
        shadowCoord.y >= 0.0 && shadowCoord.y <= 1.0 &&
        shadowCoord.z >= 0.0 && shadowCoord.z <= 1.0)
    {
        float shadowDepth = texture(ShadowMap, shadowCoord.xy).r;
        float bias = 0.005;
        if (shadowCoord.z > shadowDepth + bias)
        {
            color.rgb *= 0.5;
        }
    }

    FragColor = color;
}