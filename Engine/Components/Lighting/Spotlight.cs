using System.Numerics;
using SierraEngine.Core;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Structures;

namespace SierraEngine.Engine.Components.Lighting;

/// <summary>
/// A component used to render a spot light within a scene. Derives from both <see cref="Light"/> and <see cref="Component"/>.
/// </summary>
public class Spotlight : Light
{
    /// <summary>
    /// In which direction the spot light should be emitting light rays.
    /// </summary>
    public Vector3 direction;
    
    /// <summary>
    /// Specifies the radius of the spot light.
    /// </summary>
    public float radius;

    /// <summary>
    /// How far the light will spread outside of the <see cref="radius"/>, before the light color turns into the <see cref="Material.ambient"/> color. 
    /// </summary>
    public float spreadRadius;
    
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

    public Spotlight()
    {
        this.ID = World.RegisterSpotLight(this);
    }
    
    public override void Update()
    {
        base.Update();
        
        if (ID == -1)
        {
            Destroy();
            return;
        }
        
        World.spotLights[ID] = this;
    } 
    
    public override void Destroy()
    {
        base.Destroy();
        
        World.RemoveSpotLight(this);
    }

    public static implicit operator UniformSpotLight(Spotlight givenSpotlight)
    {
        return givenSpotlight.intensity > 0f ? new UniformSpotLight()
        {
            position = givenSpotlight.transform.position,
            direction = givenSpotlight.direction,
            intensity = givenSpotlight.intensity,
            color = givenSpotlight.color,
            radius =  (float) Math.Cos(Mathematics.ToRadians(givenSpotlight.radius)),
            spreadRadius = (float) Math.Cos(Mathematics.ToRadians(givenSpotlight.spreadRadius)),
            linear = givenSpotlight.linear,
            quadratic = givenSpotlight.quadratic
        } : default;
    }
}

#pragma warning disable CS0169
public struct UniformSpotLight
{
    public Vector3 position;
    public float radius;

    public Vector3 direction;
    public float intensity;

    public Vector3 color;
    public float linear;    
    private readonly float _align1_;
    private readonly float _align2_;

    public float quadratic;
    public float spreadRadius;
}
#pragma warning restore CS0169