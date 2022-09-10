using System.Numerics;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

/// <summary>
/// A default component, existing in all entities (<see cref="GameObject"/>). Represents its entity's position, rotation, and scale
/// in the 3D world space.
/// </summary>
public class Transform
{
    public static readonly Transform Default = new Transform() { position = Vector3.Zero, rotation = Vector3.Zero, scale = Vector3.One };
    
    public Vector3 position = Vector3.Zero;
    public Vector3 rotation = Vector3.Zero;
    public Vector3 scale = Vector3.One;
}