#version 450
#extension GL_OVR_multiview2 : enable
layout(num_views = 2) in;

in vec3 aPosition;
in vec2 aTexCoord;

uniform mat4 ViewMatrix[2];
uniform mat4 ProjectionMatrix[2];
uniform mat4 ModelMatrix;

out vec2 texCoord;

void main(void)
{
    vec4 worldspacePos = ModelMatrix * vec4(aPosition, 1.0);
	gl_Position = ProjectionMatrix[gl_ViewID_OVR] * ViewMatrix[gl_ViewID_OVR] * worldspacePos;

    texCoord = aTexCoord;
}