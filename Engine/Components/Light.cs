using System.Numerics;

namespace SierraEngine.Engine.Components;


/// <summary>
/// A component representing any type of light in the scene.
/// </summary>
public class Light : Component
{
    /// <summary>
    /// Color of the light.
    /// </summary>
    public Vector3 color = Vector3.One;
    
    /// <summary>
    /// Intensity of the light color.
    /// </summary>
    public float intensity = 1.0f;
    
    /// <summary>
    /// Ambient value of light. See <a href="https://learnopengl.com/Lighting/Basic-Lighting">this link</a> if you are not familiar with how light works in rendering.
    /// </summary>
    public Vector3 ambient = Vector3.One;
    
    /// <summary>
    /// Diffuse value of light. See <a href="https://learnopengl.com/Lighting/Basic-Lighting">this link</a> if you are not familiar with how light works in rendering.
    /// </summary>
    public Vector3 diffuse = Vector3.One;
    
    /// <summary>
    /// Specular value of light. See <a href="https://learnopengl.com/Lighting/Basic-Lighting">this link</a> if you are not familiar with how light works in rendering.
    /// </summary>
    public Vector3 specular = Vector3.One;

    public int ID { get; protected init; } = -1;
}