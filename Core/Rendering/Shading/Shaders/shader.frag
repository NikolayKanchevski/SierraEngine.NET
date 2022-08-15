#version 450

layout(location = 0) in vec3 fromVert_Position;
layout(location = 1) in vec3 fromVert_Normal;
layout(location = 2) in vec2 fromVert_TextureCoordinates;

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

layout(set = 1, binding = 1) uniform sampler2D diffuseSampler;
layout(set = 2, binding = 2) uniform sampler2D specularSampler;

layout(location = 0) out vec4 outColor;

void main() {
        // Get the texture color
        vec3 textureColor = uniformBuffer.lightColor * texture(diffuseSampler, fromVert_TextureCoordinates).rgb;

        // Calculate required directions
        vec3 normal = normalize(fromVert_Normal);
        vec3 lightDirection = normalize(uniformBuffer.lightPosition - fromVert_Position);
        vec3 viewDirection = normalize(-fromVert_Position);
        vec3 reflectionDirection = reflect(lightDirection, normal);

        // Calculate diffuse and base specular values
        const float DIFFUSE_STRENTH = (max(dot(normal, lightDirection), 0.0));
        const float SPECULAR_STRENGTH = pow(max(dot(viewDirection, reflectionDirection), 0.0), pushConstant.shininess);

        // Calculate final light components
        vec3 ambient = uniformBuffer.lightAmbient * textureColor * uniformBuffer.lightColor;
        vec3 diffuse = uniformBuffer.lightDiffuse * DIFFUSE_STRENTH * textureColor * uniformBuffer.lightColor;
        vec3 specular = uniformBuffer.lightSpecular * SPECULAR_STRENGTH * texture(specularSampler, fromVert_TextureCoordinates).rgb * uniformBuffer.lightColor;

        // Combine everything into a single color vector and send it
        vec3 result = ambient + diffuse + specular;
        outColor = vec4(result, 1.0);
}