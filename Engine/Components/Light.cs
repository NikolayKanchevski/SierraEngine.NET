using System.Numerics;

namespace SierraEngine.Engine.Components;

public class Light : Component
{
    public Vector3 color = Vector3.One;
    public float intensity = 1.0f;
    
    public Vector3 ambient = Vector3.One;
    public Vector3 diffuse = Vector3.One;
    public Vector3 specular = Vector3.One;

    public int ID { get; protected init; } = -1;
}