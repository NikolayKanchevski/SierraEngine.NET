#version 450

layout(location = 0) in vec3 fromCode_Position;
layout(location = 1) in vec3 fromCode_Normal;
layout(location = 2) in vec2 fromCode_TextureCoordinates;

layout(set = 0, binding = 0) uniform MV {
    mat4 view;
    mat4 projection;
} mv; 

// TODO: Put proportional scale in frag push constant

layout(push_constant) uniform PushConstant {
    mat4 model;
} pushConstant;

layout(location = 0) out vec3 toFrag_Normal;
layout(location = 1) out vec2 toFrag_TextureCoordinates;
layout(location = 2) out mat4 foFrag_ModelMatrix;

//  layout(location = 1) in vec3 fromCode_Color;
//  layout(location = 0) out vec3 toFrag_Color;
//  toFrag_Color = fromCode_Color;

void main() {
    gl_Position =  mv.projection * mv.view * pushConstant.model * vec4(fromCode_Position, 1.0);
    
    toFrag_Normal = fromCode_Normal;
    toFrag_TextureCoordinates = fromCode_TextureCoordinates;
    foFrag_ModelMatrix = pushConstant.model;
}