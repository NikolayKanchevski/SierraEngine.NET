using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

public class PointLight : Light
{
    public float linear;
    public float quadratic;
    
    public static implicit operator UniformPointLight(PointLight givenPointLight)
    {
        return givenPointLight.intensity > 0 ? new UniformPointLight() with
        {
            position = givenPointLight.transform.position,
            color = givenPointLight.color,
            intensity = givenPointLight.intensity,
            linear = givenPointLight.linear,
            quadratic = givenPointLight.quadratic,
            ambient = givenPointLight.ambient,
            diffuse = givenPointLight.diffuse,
            specular = givenPointLight.specular
        } : new UniformPointLight();
    }
}