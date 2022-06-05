#version 450

layout(location = 0) in vec3 fromVert_Color;

layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(fromVert_Color, 1.0);
}