#version 450

layout(location = 0) in vec3 fromCode_Position;
layout(location = 1) in vec3 fromCode_Normal;
layout(location = 2) in vec2 fromCode_TextureCoordinates;

layout(set = 0, binding = 0) uniform UniformBuffer {
    /* VERTEX DATA */
    mat4 view;
    mat4 projection;

    /* FRAGMENT DATA */
    vec3 lightPosition;
    float _align1_;

    vec3 lightColor;
    float _align2_;

    vec3 lightAmbient;
    float _align3_;

    vec3 lightDiffuse;
    float _align4_;

    vec3 lightSpecular;
} uniformBuffer; 

layout(push_constant) uniform PushConstant {
    /* VERTEX DATA */
    mat4 model;
    
    /* FRAGMENT DATA */
    float shininess;
} pushConstant;

layout(location = 0) out vec3 toFrag_Position;
layout(location = 1) out vec3 toFrag_Normal;
layout(location = 2) out vec2 toFrag_TextureCoordinates;

void main() {
    // Set the position of the vertex in world space
    gl_Position =  uniformBuffer.projection * uniformBuffer.view * pushConstant.model * vec4(fromCode_Position, 1.0);
    
    // Transfer required data from vertex to fragment shader
    toFrag_Position = fromCode_Position;
    toFrag_Normal = fromCode_Normal;
    toFrag_TextureCoordinates = fromCode_TextureCoordinates;
}