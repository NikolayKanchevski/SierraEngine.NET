using GlmSharp;

namespace SierraEngine.Engine;

public class Camera
{
    public Transform transform = new Transform();
    
    public vec3 position => transform.position;
    public vec3 rotation => transform.rotation;

    public vec3 frontDirection = new vec3(0.0f, -0.0f, 1.0f);
    public vec3 upDirection = new vec3(0.0f, 1.0f, 0.0f);
}