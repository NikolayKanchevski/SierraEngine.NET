using System.Numerics;

namespace SierraEngine.Engine.Components.Lighting;


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

    public int ID { get; protected init; } = -1;
}