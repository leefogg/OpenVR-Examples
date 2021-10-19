#version 330

in vec2 texCoord;

out vec4 outputColor;

uniform sampler2D texture0;
uniform float opacity = 1;

void main()
{
    if (opacity > 0) {
        outputColor = texture(texture0, texCoord);
    } else {
        outputColor = vec4(0.0);
    }
}