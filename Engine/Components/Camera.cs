using System.Numerics;

namespace SierraEngine.Engine.Components;

public class Camera : Component
{
    public float fov = 45.0f;
    public Vector3 frontDirection = new Vector3(0.0f, 0.0f, -1.0f);
    public Vector3 upDirection = new Vector3(0.0f, 1.0f, 0.0f);
}