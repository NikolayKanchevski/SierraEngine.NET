#version 450

layout(location = 0) in vec3 fromVert_Position;
layout(location = 1) in vec3 fromVert_Normal;
layout(location = 2) in vec2 fromVert_TextureCoordinates;

struct DirectionalLight {
        vec3 direction;
        float intensity;

        vec3 color;
        float _align2_;

        vec3 ambient;
        float _align3_;

        vec3 diffuse;
        float _align4_;

        vec3 specular;
};

layout(set = 0, binding = 0) uniform UniformBuffer {
        /* VERTEX DATA */
        mat4 view;
        mat4 projection;

        /* FRAGMENT DATA */
        DirectionalLight directionalLight;
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
        vec3 textureColor = uniformBuffer.directionalLight.color * texture(diffuseSampler, fromVert_TextureCoordinates).rgb;
        
        // Get ambient
        vec3 ambient = uniformBuffer.directionalLight.ambient * textureColor * uniformBuffer.directionalLight.color * uniformBuffer.directionalLight.intensity;
        
        vec3 directionalLightResult = ambient;
        if (uniformBuffer.directionalLight.intensity > 0) 
        {
                // Calculate required directions
                vec3 normal = normalize(fromVert_Normal);
                vec3 viewDirection = normalize(-fromVert_Position);
                vec3 reflectionDirection = reflect(uniformBuffer.directionalLight.direction, normal);

                // Calculate diffuse and base specular values
                const float DIFFUSE_STRENTH = (max(dot(normal, uniformBuffer.directionalLight.direction), 0.0));
                const float SPECULAR_STRENGTH = pow(max(dot(viewDirection, reflectionDirection), 0.0), pushConstant.shininess);

                // Calculate final light components
                vec3 diffuse = uniformBuffer.directionalLight.diffuse * DIFFUSE_STRENTH * textureColor * uniformBuffer.directionalLight.color * uniformBuffer.directionalLight.intensity;
                vec3 specular = uniformBuffer.directionalLight.specular * SPECULAR_STRENGTH * texture(specularSampler, fromVert_TextureCoordinates).rgb * uniformBuffer.directionalLight.color * uniformBuffer.directionalLight.intensity;

                // Combine everything into a single color vector and send it
                directionalLightResult += diffuse + specular;
        }
        
        outColor = vec4(directionalLightResult, 1.0);
}