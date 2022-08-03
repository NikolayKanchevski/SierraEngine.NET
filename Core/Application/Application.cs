using System.Numerics;
using Glfw;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine;
using Camera = SierraEngine.Engine.Camera;
using Cursor = SierraEngine.Engine.Cursor;
using Window = SierraEngine.Core.Rendering.Window;

namespace SierraEngine.Core.Application;

public class Application
{
    private readonly Window window;
    
    private readonly Camera camera = new Camera();
    private const float CAMERA_MOVE_SPEED = 15.0f;
    private const float CAMERA_LOOK_SPEED = 0.2f;
    private const float CAMERA_ZOOM_SPEED = 15.0f;
    private float yaw = 90.0f, pitch = 0.0f, fov = 45.0f;

    private Vector2 lastCursorPosition;
    
    public Application(in Window givenWindow)
    {
        this.window = givenWindow;
    }
    
    public void Start()
    {
        VulkanRenderer vulkanRenderer = new VulkanRenderer(window);
        window.SetRenderer(ref vulkanRenderer);

        Glfw3.GetCursorPosition(VulkanCore.glfwWindow, out double cursorX, out double cursorY);
        Cursor.SetCursorPosition(new Vector2((float) cursorX, (float) cursorY));
        lastCursorPosition = Cursor.cursorPosition;
        Cursor.HideCursor();

        camera.transform.position = new Vector3(0.0f, 0.0f, -10.0f);
        window.vulkanRenderer!.vp.view = Matrix4x4.CreateLookAt(camera.position, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f));


        while (!window.closed)
        {
            UpdateClasses();
            
            Update();
            
            window.Update();
        }
        
        window.Destroy();
        
        Glfw3.Terminate();
    }

    private void Update()
    {
        HandleCameraMovement();
        
        // window.vulkanRenderer!.vp.model = Matrix4x4.Rotate(glm.Radians(90.0f), new Vector3(0.0f, 0.0f, 1.0f));
        // window.vulkanRenderer!.vp.model = Matrix4x4.CreateRotationZ((float) Math.Cos(Time.upTime) * 4, new Vector3(0.0f, 0.0f, 1.0f));
        window.vulkanRenderer!.vp.model = Matrix4x4.CreateTranslation(0, 0, 0);
        // window.vulkanRenderer!.vp.view = Matrix4x4.CreateLookAt(new Vector3(0.0f, 0.0f, 10.0f), Vector3.Zero, new Vector3(0.0f, 1.0f, 0.0f));
        window.vulkanRenderer!.vp.projection = Matrix4x4.CreatePerspectiveFieldOfView(Radians(45.0f), (float) VulkanCore.swapchainExtent.width / VulkanCore.swapchainExtent.height, 0.1f, 100.0f);
        window.vulkanRenderer!.vp.projection.M11 *= -1;
            
        window.SetTitle($"FPS: { Time.FPS }");
    }
    
    private void HandleCameraMovement()
    {
        
    }

    private float Radians(in float degrees)
    {
        return (float) (Math.PI / 180) * degrees;
    }

    private void UpdateClasses()
    {
        Time.Update();
        // Input.Update();
    }
}