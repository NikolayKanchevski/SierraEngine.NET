using System.Numerics;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

public class DirectionalLight : Light
{
    public Vector3 direction;

    public static implicit operator UniformDirectionalLight(DirectionalLight givenDirectionalLight)
    {
        return givenDirectionalLight.intensity > 0 ? new UniformDirectionalLight() with
        {
            intensity = givenDirectionalLight.intensity,
            direction = givenDirectionalLight.direction,
            color = givenDirectionalLight.color,
            diffuse = givenDirectionalLight.diffuse,
            ambient = givenDirectionalLight.ambient,
            specular = givenDirectionalLight.specular
        } : new UniformDirectionalLight();
    }
}