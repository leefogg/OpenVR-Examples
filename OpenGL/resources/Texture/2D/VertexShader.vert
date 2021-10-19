#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

out vec2 texCoord;

uniform vec2 scale;
uniform vec2 offset;

void main(void)
{
    gl_Position = vec4(aPosition * vec3(scale, 0.0) + vec3(offset, 0.0), 1.0);

    texCoord = aTexCoord;
}