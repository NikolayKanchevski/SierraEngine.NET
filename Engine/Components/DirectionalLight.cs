using System.Numerics;
using SierraEngine.Core;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

/// <summary>
/// A component representing a directional light in the scene. Derives from both <see cref="Light"/> and <see cref="Component"/>.
/// </summary>
public class DirectionalLight : Light
{
    public Vector3 direction;

    public DirectionalLight()
    {
        this.ID = World.RegisterDirectionalLight(this);
    }

    public override void Update()
    {
        base.Update();
        
        if (ID == -1)
        {
            Destroy();
            return;
        }
        
        World.directionalLights[ID] = this;
    }

    public static implicit operator UniformDirectionalLight(DirectionalLight givenDirectionalLight)
    {
        return givenDirectionalLight.intensity > 0f ? new UniformDirectionalLight() with
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