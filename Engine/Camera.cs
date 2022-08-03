using System.Numerics;

namespace SierraEngine.Engine;

public class Camera
{
    public Transform transform = new Transform();
    
    public Vector3 position => transform.position;
    public Vector3 rotation => transform.rotation;

    public Vector3 frontDirection = new Vector3(0.0f, 0.0f, -1.0f);
    public Vector3 upDirection = new Vector3(0.0f, 1.0f, 0.0f);
}