#version 450

layout(location = 0) in vec3 fromVert_Position;
layout(location = 1) in vec3 fromVert_Normal;
layout(location = 2) in vec2 fromVert_TextureCoordinates;

struct DirectionalLight {
        vec3 direction;
        float intensity;

        vec3 color;
        float _align1_;

        vec3 ambient;
        float _align2_;

        vec3 diffuse;
        float _align3_;

        vec3 specular;
        float _align4_;
};

struct PointLight {
        vec3 position;
        float _align1_;

        vec3 color;
        float intensity;

        vec3 ambient;
        float _align2_;
        
        vec3 diffuse;
        float linear;
        
        vec3 specular;
        float quadratic;
};

layout(set = 0, binding = 0) uniform UniformBuffer {
        /* VERTEX DATA */
        mat4 view;
        mat4 projection;

        /* FRAGMENT DATA */
        DirectionalLight directionalLight;
        PointLight pointLight;
} ub;

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
//        vec3 textureColor = ub.directionalLight.color * texture(diffuseSampler, fromVert_TextureCoordinates).rgb;
        vec3 textureColor = texture(diffuseSampler, fromVert_TextureCoordinates).rgb;
        vec3 specularColor = texture(specularSampler, fromVert_TextureCoordinates).rgb;
        
        // Get ambient of directional light
        
        vec3 ambient;
        vec3 diffuse;
        vec3 specular;
        
        // Directional light data
//        if (ub.directionalLight.intensity > 0) 
//        {
//                // Calculate required directions
//                vec3 normal = normalize(fromVert_Normal);
//                vec3 viewDirection = normalize(-fromVert_Position);
//                vec3 reflectionDirection = reflect(ub.directionalLight.direction, normal);
//
//                // Calculate diffuse and base specular values
//                const float DIFFUSE_STRENTH = (max(dot(normal, ub.directionalLight.direction), 0.0));
//                const float SPECULAR_STRENGTH = pow(max(dot(viewDirection, reflectionDirection), 0.0), pushConstant.shininess);
//
//                // Calculate final light components
//                ambient = ub.directionalLight.ambient * textureColor * ub.directionalLight.color;
//                diffuse = ub.directionalLight.diffuse * DIFFUSE_STRENTH * textureColor * ub.directionalLight.color * ub.directionalLight.intensity;
//                specular = ub.directionalLight.specular * SPECULAR_STRENGTH * texture(specularSampler, fromVert_TextureCoordinates).rgb * ub.directionalLight.color * ub.directionalLight.intensity;
//        }
        
        // Point light data
        if (ub.pointLight.intensity > 0) {
                // Calculate required directions
                vec3 normal = normalize(fromVert_Normal);
                vec3 viewDirection = normalize(-fromVert_Position);
                vec3 lightDir = normalize(ub.pointLight.position - fromVert_Position);
                vec3 reflectionDirection = reflect(lightDir, normal);

                // Calculate diffuse and base specular values
                const float DIFFUSE_STRENTH = (max(dot(normal, lightDir), 0.0));
                const float SPECULAR_STRENGTH = pow(max(dot(viewDirection, reflectionDirection), 0.0), pushConstant.shininess);

                // Calculate final light components
                ambient = ub.pointLight.ambient * textureColor;
                diffuse = ub.pointLight.diffuse * DIFFUSE_STRENTH * textureColor * ub.pointLight.intensity;
                specular = ub.pointLight.specular * SPECULAR_STRENGTH * specularColor * ub.pointLight.intensity;

                float distance    = length(ub.pointLight.position - fromVert_Position);
                float attenuation = 1.0 / (1.0f + ub.pointLight.linear * distance + ub.pointLight.quadratic * (distance * distance));
                
                ambient  *= attenuation;
                diffuse  *= attenuation;
                specular *= attenuation;
        }
        
//        outColor = vec4(ub.pointLight.specular, 1.0);
        outColor = vec4(ambient + diffuse + specular, 1.0);
}