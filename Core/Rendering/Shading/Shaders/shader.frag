#version 450

layout(location = 0) in vec3 fromVert_Normal;
layout(location = 1) in vec2 fromVert_TextureCoordinates;
layout(location = 2) in mat4 fromVert_ModelMatrix;

layout(set = 1, binding = 1) uniform UniformBuffer {
        vec3 directionToLight;
        float lightIntensity; 
        vec3 lightColor;
} uniformBuffer;

layout(set = 2, binding = 0) uniform sampler2D textureSampler;

layout(location = 0) out vec4 outColor;

const float AMBIENT = 0.025;

void main() {
        // TODO: Add proportional scale check
        // vec3 normalWorldSpace = normalize(mat3(fromVert_ModelMatrix) * fromVert_Normal);;

        mat3 normalMatrix = transpose(inverse(mat3(fromVert_ModelMatrix)));
        vec3 normalWorldSpace = normalize(normalMatrix * fromVert_Normal);
        
        float lightIntensity = (AMBIENT + max(dot(normalWorldSpace, uniformBuffer.directionToLight), 0)) * uniformBuffer.lightIntensity;
        outColor = lightIntensity * vec4(uniformBuffer.lightColor * texture(textureSampler, fromVert_TextureCoordinates).rgb, 1.0);
}