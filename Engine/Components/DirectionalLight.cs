using System.Numerics;
using Evergine.Bindings.Vulkan;
using SierraEngine.Core;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

public class DirectionalLight : Light
{
    public Transform transform = new Transform();
    public float intensity;
    public Vector3 direction;

    public static implicit operator UniformDirectionalLight(DirectionalLight givenDirectionalLight)
    {
        return new UniformDirectionalLight() with
        {
            intensity = givenDirectionalLight.intensity,
            direction = givenDirectionalLight.direction,
            color = givenDirectionalLight.color,
            diffuse = givenDirectionalLight.diffuse,
            ambient = givenDirectionalLight.ambient,
            specular = givenDirectionalLight.specular
        };
    }
}