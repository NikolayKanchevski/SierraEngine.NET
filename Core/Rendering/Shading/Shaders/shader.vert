#version 450

layout(location = 0) in vec3 fromCode_Position;
layout(location = 1) in vec3 fromCode_Color;

layout(binding = 0) uniform MVP {
    mat4 model;
    mat4 view;
    mat4 projection;
} mvp;

layout(location = 0) out vec3 toFrag_Color;

void main() {
    gl_Position =  mvp.projection * mvp.view * mvp.model * vec4(fromCode_Position, 1.0);
    toFrag_Color = fromCode_Color;
}