using System.Numerics;

namespace SierraEngine.Engine.Components;

public class Transform
{
    public static Transform Default = new Transform() { position = Vector3.Zero, rotation = Vector3.Zero, scale = Vector3.One };
    
    public Vector3 position = Vector3.Zero;
    public Vector3 rotation = Vector3.Zero;
    public Vector3 scale = Vector3.One;
}