#version 450

layout(location = 0) in vec3 fromVert_Color;
layout(location = 1) in vec2 fromVert_TextureCoordinates;

layout(set = 1, binding = 0) uniform sampler2D textureSampler;

layout(location = 0) out vec4 outColor;

void main() {
//    outColor = vec4(fromVert_Color, 1.0);
//    outColor = vec4(fromVert_TextureCoordinates, 0.0, 1.0);
        outColor = texture(textureSampler, fromVert_TextureCoordinates);
}