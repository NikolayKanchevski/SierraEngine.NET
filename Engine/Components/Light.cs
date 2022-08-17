using System.Numerics;

namespace SierraEngine.Engine.Components;

public class Light : Component
{
    public Transform transform = new Transform();

    public Vector3 color;
    public float intensity;
    
    public Vector3 ambient;
    public Vector3 diffuse;
    public Vector3 specular;
}