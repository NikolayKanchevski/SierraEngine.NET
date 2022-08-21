using System.Numerics;
using Glfw;
using ImGuiNET;
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
    private readonly UserInterface ui = new UserInterface();

    private readonly Camera camera = (new GameObject("Camera").AddComponent(new Camera()) as Camera)!;
    private readonly DirectionalLight directionalLight = (new GameObject("Directional Light").AddComponent(new DirectionalLight()) as DirectionalLight)!;
    private readonly PointLight pointLight = (new GameObject("Point Light").AddComponent(new PointLight()) as PointLight)!;
    
    private const float CAMERA_MOVE_SPEED = 15.0f;
    private const float CAMERA_LOOK_SPEED = 60.0f;
    private const float CAMERA_ZOOM_SPEED = 3000.0f;
    private float yaw = -90.0f, pitch;

    private bool cursorShown = true;
    
    public Application()
    {
        window = new Window("Sierra Engine v1.0.0", true, true);
        
        VulkanRenderer vulkanRenderer = new VulkanRenderer(window);
        window.SetRenderer(ref vulkanRenderer);
        
        window.Show();
    }
    
    public void Start()
    {
        StartClasses();
        
        Cursor.SetCursorVisibility(cursorShown);

        camera.transform.position = new Vector3(0.0f, -3.0f, 10.0f);
        pointLight.transform.position = new Vector3(0.0f, -6.0f, 0.0f);
        directionalLight.direction = Vector3.Normalize(camera.transform.position - Vector3.Zero);

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

        ui.Update(window);
    }
    
    private void HandleCameraMovement()
    {
        if (Input.GetKeyPressed(Key.Escape))
        {
            cursorShown = !cursorShown;
            Cursor.SetCursorVisibility(cursorShown);
        }
        
        if (cursorShown) return;
        
        camera.fov -= Input.GetVerticalMouseScroll() * CAMERA_ZOOM_SPEED * Time.deltaTime;

        if (Input.GetKeyHeld(Key.Tab)) 
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

        yaw += Cursor.GetHorizontalCursorOffset() * CAMERA_LOOK_SPEED * Time.deltaTime;
        pitch += Cursor.GetVerticalCursorOffset() * CAMERA_LOOK_SPEED * Time.deltaTime;

        pitch = Mathematics.Clamp(pitch, -89.0f, 89.0f);

        Vector3 newCameraFrontDirection;
        newCameraFrontDirection.X = (float) (Math.Cos(Mathematics.ToRadians(yaw)) * Math.Cos(Mathematics.ToRadians(pitch)));
        newCameraFrontDirection.Y = (float) (Math.Sin(Mathematics.ToRadians(pitch)));
        newCameraFrontDirection.Z = (float) (Math.Sin(Mathematics.ToRadians(yaw)) * Math.Cos(Mathematics.ToRadians(pitch)));
        camera.frontDirection = Vector3.Normalize(newCameraFrontDirection);
    }

    private void UpdateObjects()
    {
        float upTimeCos = (float) Math.Cos(Time.upTime);
        float upTimeSin = (float) Math.Sin(Time.upTime);
        
        // Light data calculation
        const float RADIUS = 12.0f;
        float lightX = upTimeSin * RADIUS;
        float lightY = -3f;
        float lightZ = upTimeCos * RADIUS;
        
        Vector3 lightPosition = new Vector3(lightX, lightY, lightZ);
        
        // Directional light
        directionalLight.intensity = 0f;
        directionalLight.color = Vector3.One;
        
        directionalLight.ambient = new Vector3(0.005f);
        directionalLight.diffuse = new Vector3(0.5f);
        directionalLight.specular = new Vector3(0.5f);
        
        // Point light
        pointLight.intensity = 3f;
        // pointLight.color = new Vector3(1.0f, 0.0f, 1.0f);
        // pointLight.color = new Vector3(
        //     (float) Math.Sin(Time.upTime * 2.0f),    
        //     (float) Math.Sin(Time.upTime * 0.7f),    
        //     (float) Math.Sin(Time.upTime * 1.3f) 
        // );

        // pointLight.transform.position = new Vector3(0f, -7.5f, upTimeSin * 10f);
        pointLight.linear = 0.09f;
        pointLight.quadratic = 0.032f;
        
        pointLight.ambient = new Vector3(0.2f);
        pointLight.diffuse = new Vector3(0.5f);
        pointLight.specular = new Vector3(0.5f);
        
        // Object transformations
        World.meshes[0].transform.position = pointLight.transform.position;
        World.meshes[0].transform.scale = new Vector3(0.5f);
        
        World.meshes[4].transform.rotation = new Vector3(0.0f, upTimeSin * 45f, 0.0f);
        World.meshes[5].transform.rotation = new Vector3(0.0f, upTimeSin * 45f, 0.0f);
    }
    
    private void UpdateRenderer()
    {
        window.vulkanRenderer!.imGuiController.Update();
        
        window.vulkanRenderer!.uniformData.view = Matrix4x4.CreateLookAt(camera.position, camera.position + camera.frontDirection, camera.upDirection);
        window.vulkanRenderer!.uniformData.projection = Matrix4x4.CreatePerspectiveFieldOfView(Mathematics.ToRadians(camera.fov), (float) VulkanCore.swapchainExtent.width / VulkanCore.swapchainExtent.height, 0.1f, 100.0f);
        window.vulkanRenderer!.uniformData.projection.M11 *= -1;

        window.vulkanRenderer!.uniformData.directionalLight = directionalLight;
        window.vulkanRenderer!.uniformData.pointLight = pointLight;
    }

    private void StartClasses()
    {
        Cursor.Start();
    }

    private void UpdateClasses()
    {
        Time.Update();
        Input.Update();
        Cursor.Update();
    }
}