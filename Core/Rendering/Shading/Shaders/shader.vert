#version 450

layout(location = 0) in vec3 fromCode_Position;
layout(location = 1) in vec3 fromCode_Color;
layout(location = 2) in vec2 fromCode_TextureCoordinates;

layout(set = 0, binding = 0) uniform MV {
    mat4 view;
    mat4 projection;
} mv; 

layout(push_constant) uniform PushConstant {
    mat4 model;
} pushConstant;

layout(location = 0) out vec3 toFrag_Color;
layout(location = 1) out vec2 toFrag_TextureCoordinates;

void main() {
    gl_Position =  mv.projection * mv.view * pushConstant.model * vec4(fromCode_Position, 1.0);
    
    toFrag_Color = fromCode_Color;
    toFrag_TextureCoordinates = fromCode_TextureCoordinates;
}