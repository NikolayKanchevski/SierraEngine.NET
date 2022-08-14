using System.Numerics;
using Glfw;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Components;
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
    private float yaw = -90.0f, pitch;

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

        window.SetTitle($"Sierra Engine | FPS: { Time.FPS.ToString().PadLeft(4, '0') } | GPU Draw Time: { VulkanRendererInfo.drawTime.ToString("n9") }ms");
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
        else if (Input.GetKeyHeld(Key.Tab)) 
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

        window.vulkanRenderer!.uniformData.view = Matrix4x4.CreateLookAt(camera.position, camera.position + camera.frontDirection, camera.upDirection);
        window.vulkanRenderer!.uniformData.projection = Matrix4x4.CreatePerspectiveFieldOfView(Radians(camera.fov), (float) VulkanCore.swapchainExtent.width / VulkanCore.swapchainExtent.height, 0.1f, 100.0f);
        window.vulkanRenderer!.uniformData.projection.M11 *= -1;
    }

    private void UpdateObjects()
    {
        float upTimeCos = (float) Math.Cos(Time.upTime);

        const float RADIUS = 8.0f;
        float lightX = (float) Math.Sin(Time.upTime) * RADIUS;
        float lightY= (float) -Math.Sin(Time.upTime) * RADIUS;
        float lightZ = (float) Math.Cos(Time.upTime) * RADIUS;
        
        Vector3 lightPosition = new Vector3(lightX, lightY, lightZ);

        World.meshes[0].transform.position = lightPosition;
        
        window.vulkanRenderer!.uniformData.directionToLight = lightPosition;
        window.vulkanRenderer!.uniformData.lightIntensity = 1.0f;
        window.vulkanRenderer!.uniformData.lightColor = Vector3.One;

        World.meshes[4].transform.rotation = new Vector3(0.0f, upTimeCos * 0.65f, 0.0f);
        World.meshes[5].transform.rotation = new Vector3(0.0f, upTimeCos * 0.65f, 0.0f);
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