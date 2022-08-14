#version 450

layout(location = 0) in vec3 fromVert_Position;
layout(location = 1) in vec3 fromVert_Normal;
layout(location = 2) in vec2 fromVert_TextureCoordinates;
layout(location = 3) in mat4 fromVert_ModelMatrix;

layout(set = 0, binding = 0) uniform UniformBuffer {
        /* VERTEX DATA */
        mat4 view;
        mat4 projection;

        /* FRAGMENT DATA */
        vec3 lightPosition;
        float lightIntensity;
        vec3 lightColor;
} uniformBuffer;

layout(set = 1, binding = 1) uniform sampler2D textureSampler;

//layout(push_constant) uniform PushConstant {
//        vec4 data;
//} pushConstant;

layout(location = 0) out vec4 outColor;

const float AMBIENT_STRENGTH = 0.1f;
const float SPECULAR_STRENGTH = 0.75f;

void main() {
        // TODO: Add proportional scale check
        // vec3 normalWorldSpace = normalize(mat3(fromVert_ModelMatrix) * fromVert_Normal);
        // mat3 normalMatrix = transpose(inverse(mat3(fromVert_ModelMatrix)));
        // vec3 normalWorldSpace = normalize(normalMatrix * fromVert_Normal);

//        if (pushConstant.data.x == 1.0) {
//                outColor = vec4(1.0, 0.0, 0.0, 1.0);
//                return;
//        }
        
        // Get the texture color
        vec3 textureColor = (uniformBuffer.lightColor * texture(textureSampler, fromVert_TextureCoordinates).rgb) * uniformBuffer.lightIntensity;
        
        // Calculate required directions
        vec3 normal = normalize(fromVert_Normal);
        vec3 lightDirection = normalize(uniformBuffer.lightPosition - fromVert_Position);
        vec3 viewDirection = normalize(-fromVert_Position);
        vec3 reflectionDirection = reflect(lightDirection, normal);
        
        // Calculate diffuse and base specular values
        float DIFFUSE_STRENTH = (max(dot(normal, lightDirection), 0.0));
        
        // Calculate final light components
        vec3 diffuse = DIFFUSE_STRENTH * uniformBuffer.lightColor;
        vec3 ambient = AMBIENT_STRENGTH * uniformBuffer.lightColor;
        vec3 specular = SPECULAR_STRENGTH * pow(max(dot(viewDirection, reflectionDirection), 0.0), 32) * uniformBuffer.lightColor * uniformBuffer.lightIntensity;
        
        // Combine everything into a single color vector and send it
        vec3 result = (AMBIENT_STRENGTH + diffuse + specular) * textureColor;
        outColor = vec4(result, 1.0);
}