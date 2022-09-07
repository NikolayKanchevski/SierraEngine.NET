using SierraEngine.Core;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

/// <summary>
/// A component representing a point light in the scene. Derives from both <see cref="Light"/> and <see cref="Component"/>.
/// </summary>
public class PointLight : Light
{
    /// <summary>
    /// Linear value of the light. See <a href="https://learnopengl.com/Lighting/Light-casters">this link</a>
    /// and scroll down to Point Lights if you are not familiar with what this is used for.
    /// </summary>
    public float linear;
    
    /// <summary>
    /// Quadratic value of the light. See <a href="https://learnopengl.com/Lighting/Light-casters">this link</a>
    /// and scroll down to Point Lights if you are not familiar with what this is used for.
    /// </summary>
    public float quadratic;

    public PointLight()
    {
        this.ID = World.RegisterPointLight(this);
    }

    public override void Update()
    {
        base.Update();
        
        if (ID == -1)
        {
            Destroy();
            return;
        }
        
        World.pointLights[ID] = this;
    } 
    
    public static implicit operator UniformPointLight(PointLight givenPointLight)
    {
        return givenPointLight.intensity > 0f ? new UniformPointLight() with
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