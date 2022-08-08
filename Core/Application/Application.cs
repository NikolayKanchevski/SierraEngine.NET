using System.Numerics;
using Glfw;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using Camera = SierraEngine.Engine.Components.Camera;
using Cursor = SierraEngine.Engine.Classes.Cursor;
using Window = SierraEngine.Core.Rendering.Window;

namespace SierraEngine.Core.Application;

public class Application
{
    private readonly Window window;
    
    private readonly Camera camera = (new GameObject("Camera").AddComponent(new Camera()) as Camera)!;
    private const float CAMERA_MOVE_SPEED = 15.0f;
    private const float CAMERA_LOOK_SPEED = 0.2f;
    private const float CAMERA_ZOOM_SPEED = 15.0f;
    private float yaw = -90.0f, pitch = 0.0f;

    private Vector2 lastCursorPosition;
    
    public Application(in Window givenWindow)
    {
        this.window = givenWindow;
    }
    
    public void Start()
    {
        StartClasses();
        
        VulkanRenderer vulkanRenderer = new VulkanRenderer(window);
        window.SetRenderer(ref vulkanRenderer);
        
        lastCursorPosition = Cursor.cursorPosition;
        Cursor.HideCursor();

        camera.transform.position = new Vector3(0.0f, -1.0f, 10.0f);

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

        UpdateObjects();

        window.SetTitle($"FPS: { Time.FPS }");
    }
    
    private void HandleCameraMovement()
    {
        if (Input.GetKeyHeld(Key.Minus)) 
        {
            camera.fov += CAMERA_ZOOM_SPEED * camera.fov / 7 * Time.deltaTime;
        }
        if (Input.GetKeyHeld(Key.Equal)) 
        {
            camera.fov -= CAMERA_ZOOM_SPEED * camera.fov / 7 * Time.deltaTime;
        }
        else if (Input.GetKeyHeld(Key.Space)) 
        {
            camera.fov = 45.0f;
        }
        
        camera.fov = Mathematics.Clamp(camera.fov, 5.0f, 90.0f);

        if (Input.GetKeyHeld(Key.W))
        {
            camera.transform.position += CAMERA_MOVE_SPEED * Time.deltaTime * camera.frontDirection;
        }
        if (Input.GetKeyHeld(Key.S))
        {            
            camera.transform.position -= CAMERA_MOVE_SPEED * Time.deltaTime * camera.frontDirection;
        }
        
        if (Input.GetKeyHeld(Key.A))
        {
            camera.transform.position += CAMERA_MOVE_SPEED * Time.deltaTime * Vector3.Normalize(Vector3.Cross(camera.frontDirection, camera.upDirection));
        }
        if (Input.GetKeyHeld(Key.D))
        {
            camera.transform.position -= CAMERA_MOVE_SPEED * Time.deltaTime * Vector3.Normalize(Vector3.Cross(camera.frontDirection, camera.upDirection));
        }
        
        if (Input.GetKeyHeld(Key.Q) || Input.GetKeyHeld(Key.LeftControl))
        {
            camera.transform.position += CAMERA_MOVE_SPEED * Time.deltaTime * camera.upDirection;
        }
        if (Input.GetKeyHeld(Key.E) || Input.GetKeyHeld(Key.Space))
        {
            camera.transform.position -= CAMERA_MOVE_SPEED * Time.deltaTime * camera.upDirection;
        }

        float xCursorOffset = (lastCursorPosition.X - Cursor.cursorPosition.X) * CAMERA_LOOK_SPEED;
        float yCursorOffset = (lastCursorPosition.Y - Cursor.cursorPosition.Y) * CAMERA_LOOK_SPEED;
        lastCursorPosition = Cursor.cursorPosition;

        yaw += xCursorOffset;
        pitch += yCursorOffset;

        pitch = Mathematics.Clamp(pitch, -89.0f, 89.0f);

        Vector3 newCameraFrontDirection;
        newCameraFrontDirection.X = (float) (Math.Cos(Radians(yaw)) * Math.Cos(Radians(pitch)));
        newCameraFrontDirection.Y = (float) (Math.Sin(Radians(pitch)));
        newCameraFrontDirection.Z = (float) (Math.Sin(Radians(yaw)) * Math.Cos(Radians(pitch)));
        camera.frontDirection = Vector3.Normalize(newCameraFrontDirection);
        
        window.vulkanRenderer!.vp.view = Matrix4x4.CreateLookAt(camera.position, camera.position + camera.frontDirection, camera.upDirection);
        window.vulkanRenderer!.vp.projection = Matrix4x4.CreatePerspectiveFieldOfView(Radians(camera.fov), (float) VulkanCore.swapchainExtent.width / VulkanCore.swapchainExtent.height, 0.1f, 100.0f);
        window.vulkanRenderer!.vp.projection.M11 *= -1;

        camera.transform.position = new Vector3(-1.3327036f, -2.042252f, 0.94270474f);
    }

    private void UpdateObjects()
    {
        float upTimeCos = (float) Math.Cos(Time.upTime);
        // World.meshes[0].transform.position = new Vector3(0.0f, (upTimeCos * -0.75f) + 3, 0.0f);
        // World.meshes[0].transform.rotation = new Vector3(0.0f, upTimeCos * 4, 0.0f);
        // World.meshes[0].transform.scale = new Vector3(1.5f - Math.Abs(upTimeCos), 1.5f - Math.Abs(upTimeCos), 1.5f - Math.Abs(upTimeCos));
        
        World.meshes[1].transform.position = new Vector3(2.0f, -5.0f, 2.0f);

        // World.meshes[7].transform.rotation = new Vector3(0.0f, upTimeCos * 0.65f, 0.0f);
        // World.meshes[8].transform.rotation = new Vector3(0.0f, upTimeCos * 0.65f, 0.0f);
    }

    private float Radians(in float degrees)
    {
        return (float) (Math.PI / 180) * degrees;
    }

    private void StartClasses()
    {
        Cursor.Start();
    }

    private void UpdateClasses()
    {
        Time.Update();
    }
}