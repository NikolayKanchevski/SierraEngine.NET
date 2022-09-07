using System.Numerics;
using SierraEngine.Core;

namespace SierraEngine.Engine.Components;

public class Camera : Component
{
    public float fov = 45.0f;
    public Vector3 frontDirection = new Vector3(0.0f, 0.0f, -1.0f);
    public Vector3 upDirection = new Vector3(0.0f, 1.0f, 0.0f);
    public Vector3 rightDirection => Vector3.Normalize(Vector3.Cross(frontDirection, upDirection));
    public Vector3 leftDirection => Vector3.Normalize(Vector3.Cross(frontDirection, upDirection));
    
    public float nearClip = 0.1f;
    public float farClip = 100.0f;

    public Camera()
    {
        if (World.camera == null)
        {
            SetAsMain();
        }
    }
    
    /// <summary>
    /// Sets the renderer to use this camera to render the scene.
    /// </summary>
    public void SetAsMain()
    {
        World.camera = this;
    }
}