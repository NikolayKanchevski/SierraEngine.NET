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
    private readonly DirectionalLight directionalLight = (new GameObject("Directional Light").AddComponent(new DirectionalLight()) as DirectionalLight)!;
    
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
        
        UpdateRenderer();

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
    }

    private void UpdateObjects()
    {
        float upTimeCos = (float) Math.Cos(Time.upTime);
        float upTimeSin = (float) Math.Sin(Time.upTime);

        const float RADIUS = 12.0f;
        float lightX = upTimeSin * RADIUS;
        float lightY = -upTimeSin * RADIUS;
        float lightZ = upTimeCos * RADIUS;
        
        Vector3 lightPosition = new Vector3(lightX, lightY, lightZ);
        
        directionalLight.transform.position = lightPosition;
        directionalLight.color = Vector3.One;
        
        directionalLight.ambient = new Vector3(0.125f);
        directionalLight.diffuse = new Vector3(0.5f);
        directionalLight.specular = new Vector3(0.25f);

        World.meshes[0].transform.position = lightPosition;
        
        World.meshes[4].transform.rotation = new Vector3(0.0f, upTimeCos * 0.65f, 0.0f);
        World.meshes[5].transform.rotation = new Vector3(0.0f, upTimeCos * 0.65f, 0.0f);
        
        // Screenshot positions:
        // camera.transform.position = new Vector3(-5.6217723f, -2.8457499f, 10.456295f);
        // camera.frontDirection = new Vector3(0.4579284f, 0.1376336f, -0.8782702f);
        // directionalLight.transform.position = new Vector3(7.0500026f, -7.0500026f, 9.710688f);
        // World.meshes[0].transform.position = new Vector3(1000f, 0.0f, 0.0f);
        // World.meshes[4].transform.rotation = new Vector3(0.0f, 1 * 0.65f, 0.0f);
        // World.meshes[5].transform.rotation = new Vector3(0.0f, 1 * 0.65f, 0.0f);
    }

    private void UpdateRenderer()
    {
        window.vulkanRenderer!.uniformData.view = Matrix4x4.CreateLookAt(camera.position, camera.position + camera.frontDirection, camera.upDirection);
        window.vulkanRenderer!.uniformData.projection = Matrix4x4.CreatePerspectiveFieldOfView(Radians(camera.fov), (float) VulkanCore.swapchainExtent.width / VulkanCore.swapchainExtent.height, 0.1f, 100.0f);
        window.vulkanRenderer!.uniformData.projection.M11 *= -1;
        
        window.vulkanRenderer!.uniformData.lightPosition = directionalLight.transform.position;
        window.vulkanRenderer!.uniformData.lightColor = directionalLight.color;
        window.vulkanRenderer!.uniformData.lightDiffuse = directionalLight.diffuse;
        window.vulkanRenderer!.uniformData.lightAmbient = directionalLight.ambient;
        window.vulkanRenderer!.uniformData.lightSpecular = directionalLight.specular;
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