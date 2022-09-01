using SierraEngine.Core;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

public class PointLight : Light
{
    public float linear;
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