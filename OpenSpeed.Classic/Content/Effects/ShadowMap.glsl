#version 330

uniform mat4 WorldViewProj;

in vec4 Position;

out float Depth;

void main()
{
    gl_Position = WorldViewProj * Position;
    Depth = gl_Position.z / gl_Position.w;
}