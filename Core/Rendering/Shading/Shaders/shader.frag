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

vec3 textureColor;
float specularColor;

vec3 normal;
vec3 viewDirection;

vec3 CalculateDirectionalLight(DirectionalLight directionalLight);
vec3 CalculatePointLight(PointLight directionalLight);

void main() {
        // Get the texture color
//        vec3 textureColor = ub.directionalLight.color * texture(diffuseSampler, fromVert_TextureCoordinates).rgb;
        textureColor = texture(diffuseSampler, fromVert_TextureCoordinates).rgb;
        specularColor = texture(specularSampler, fromVert_TextureCoordinates).r;

        // TODO: Add proportional scale check
        // vec3 normal = normalize(mat3(fromVert_ModelMatrix) * fromVert_Normal);;

        mat3 normalMatrix = transpose(inverse(mat3(pushConstant.model)));
        
        normal = normalize(normalMatrix * fromVert_Normal);
        viewDirection = normalize(-fromVert_Position);
        
        vec3 calculatedColor = vec3(0, 0, 0);
        
        // Directional light data
        if (ub.directionalLight.intensity > 0)  {
                calculatedColor += CalculateDirectionalLight(ub.directionalLight);
        }
        
        if (ub.pointLight.intensity > 0) {
                calculatedColor += CalculatePointLight(ub.pointLight);
        }
        
        outColor = vec4(calculatedColor, 1.0);
}

vec3 CalculateDirectionalLight(DirectionalLight directionalLight) {
        const vec3 reflectionDirection = reflect(directionalLight.direction, normal);

        const float diffuseStrength = max(dot(normal, directionalLight.direction), 0.0);
        const float specularStrength = pow(max(dot(viewDirection, reflectionDirection), 0.0), max(pushConstant.shininess * 512, 1.0));
        
        const vec3 ambient = directionalLight.ambient * textureColor;
        const vec3 diffuse = directionalLight.diffuse * textureColor * diffuseStrength;
        const vec3 specular = directionalLight.specular * specularColor * specularStrength;
        
        return (ambient + diffuse + specular);
} 

vec3 CalculatePointLight(PointLight pointLight) {
        // Calculate required directions
        const vec3 lightDirection = normalize(pointLight.position - fromVert_Position);
        const vec3 reflectionDirection = reflect(lightDirection, normal);

        // Calculate diffuse and base specular values
        const float diffuseStrength = max(dot(normal, lightDirection), 0.0);
        const float specularStrength = pow(max(dot(viewDirection, reflectionDirection), 0.0), max(pushConstant.shininess * 512, 1.0));

        // Calculate final light components
        vec3 ambient = pointLight.ambient * textureColor;
        vec3 diffuse = pointLight.diffuse * textureColor * diffuseStrength;
        vec3 specular = pointLight.specular * specularColor * specularStrength;
        
        const float distance = length(pointLight.position - fromVert_Position);
        const float attenuation = 1.0 / (1.0f + pointLight.linear * distance + pointLight.quadratic * (distance * distance));

        ambient  *= attenuation;
        diffuse  *= attenuation;
        specular *= attenuation;
        
        return (ambient + diffuse + specular);
}