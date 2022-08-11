#version 450

layout(location = 0) in vec3 fromVert_Normal;
layout(location = 1) in vec2 fromVert_TextureCoordinates;
layout(location = 2) in mat4 fromVert_ModelMatrix;

layout(set = 1, binding = 1) uniform UniformBuffer {
        vec3 directionToLight;
} uniformBuffer;

layout(set = 2, binding = 0) uniform sampler2D textureSampler;

layout(location = 0) out vec4 outColor;

//    TODO: Add gamma setting as well as a uniform buffer to store lighting position, intensity, etc
//    layout(location = 0) in vec3 fromVert_Color;
//    outColor = vec4(fromVert_Color, 1.0);
//    outColor = vec4(fromVert_TextureCoordinates, 0.0, 1.0);
//    outColor = vec4(fromVert_Normal, 1.0);

const float AMBIENT = 0.025;
const vec3 DIRECTION_TO_LIGHT = vec3(-0.70710677, -0.70710677, 0); // Light is at -5.0 / -5.0 / 0.0 ( TOP-RIGHT)
//const vec3 DIRECTION_TO_LIGHT = vec3(0, -1, 0); // Light is at 0.0 / -5.0 / 0.0 ( TOP)

void main() {
        // If proportional scale:
        // vec3 normalWorldSpace = normalize(mat3(fromVert_ModelMatrix) * fromVert_Normal);
        
        // else:
        mat3 normalMatrix = transpose(inverse(mat3(fromVert_ModelMatrix)));
        vec3 normalWorldSpace = normalize(normalMatrix * fromVert_Normal);
        
        float lightIntensity = AMBIENT + max(dot(normalWorldSpace, uniformBuffer.directionToLight), 0);
        
        outColor = lightIntensity * texture(textureSampler, fromVert_TextureCoordinates);
//        outColor = texture(textureSampler, fromVert_TextureCoordinates);
}