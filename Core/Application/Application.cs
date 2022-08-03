using Glfw;
using GlmSharp;
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

    private vec2 lastCursorPosition;
    
    public Application(in Window givenWindow)
    {
        this.window = givenWindow;
    }
    
    public void Start()
    {
        VulkanRenderer vulkanRenderer = new VulkanRenderer(window);
        window.SetRenderer(ref vulkanRenderer);

        Glfw3.GetCursorPosition(VulkanCore.glfwWindow, out double cursorX, out double cursorY);
        Cursor.SetCursorPosition(new vec2((float) cursorX, (float) cursorY));
        lastCursorPosition = Cursor.cursorPosition;
        Cursor.HideCursor();

        camera.transform.position = new vec3(0.0f, 0.0f, -10.0f);
        window.vulkanRenderer!.vp.view = mat4.LookAt(new vec3(camera.position), new vec3(0.0f, 0.0f, 0.0f), new vec3(0.0f, 1.0f, 0.0f));


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
        // HandleCameraMovement();
        
        // window.vulkanRenderer!.vp.model = mat4.Rotate(glm.Radians(90.0f), new vec3(0.0f, 0.0f, 1.0f));
        window.vulkanRenderer!.vp.model = mat4.Rotate((float) Math.Cos(Time.upTime) * 4, new vec3(0.0f, 0.0f, 1.0f));
        // window.vulkanRenderer!.vp.model = mat4.Translate(0, 0, 0);
        // window.vulkanRenderer!.vp.view = mat4.LookAt(new vec3(0.0f, 0.0f, 10.0f), vec3.Zero, new vec3(0.0f, 1.0f, 0.0f));
        window.vulkanRenderer!.vp.projection = Perspective(glm.Radians(45.0f), (float) VulkanCore.swapchainExtent.width / VulkanCore.swapchainExtent.height, 0.1f, 100.0f);
        window.vulkanRenderer!.vp.projection[1, 1] *= -1;
            
        window.SetTitle($"FPS: { Time.FPS }");
    }

    private void HandleCameraMovement()
    {
        if (Input.GetKeyHeld(Key.W))
        {
            camera.transform.position += Time.deltaTime * CAMERA_MOVE_SPEED * camera.frontDirection;
        }
        if (Input.GetKeyHeld(Key.S))
        {
            camera.transform.position -= Time.deltaTime * CAMERA_MOVE_SPEED * camera.frontDirection;
        }
        
        if (Input.GetKeyHeld(Key.A))
        {
            camera.transform.position -= Time.deltaTime * CAMERA_MOVE_SPEED * glm.Normalized(glm.Cross(camera.frontDirection, camera.upDirection));
        }
        if (Input.GetKeyHeld(Key.D))
        {
            camera.transform.position += Time.deltaTime * CAMERA_MOVE_SPEED * glm.Normalized(glm.Cross(camera.frontDirection, camera.upDirection));
        }
        
        if (Input.GetKeyHeld(Key.Q) || Input.GetKeyHeld(Key.LeftControl))
        {
            camera.transform.position -= Time.deltaTime * CAMERA_MOVE_SPEED * camera.upDirection;
        }
        if (Input.GetKeyHeld(Key.E) || Input.GetKeyHeld(Key.Space))
        {
            camera.transform.position += Time.deltaTime * CAMERA_MOVE_SPEED * camera.upDirection;
        }
        
        // Rotation
        vec2 currentCursorPosition = Cursor.cursorPosition;
        float xOffset = (currentCursorPosition.x - lastCursorPosition.x) * CAMERA_LOOK_SPEED;
        float yOffset = (currentCursorPosition.y - lastCursorPosition.y) * CAMERA_LOOK_SPEED;
        lastCursorPosition = currentCursorPosition;

        yaw += xOffset;
        pitch += yOffset;
        
        vec3 newCameraFrontDirection;
        newCameraFrontDirection.x = glm.Cos(glm.Radians(yaw)) * glm.Cos(glm.Radians(pitch));
        newCameraFrontDirection.y = glm.Sin(glm.Radians(pitch));
        newCameraFrontDirection.z = glm.Sin(glm.Radians(yaw)) * glm.Cos(glm.Radians(pitch));
        camera.frontDirection = glm.Normalized(newCameraFrontDirection);

        camera.transform.rotation = new vec3(-yaw, pitch, camera.transform.rotation.z);

        var a = mat4.LookAt(camera.position, camera.position + camera.frontDirection, camera.upDirection);

        // if (window.vulkanRenderer!.vp.view != a)
        {
            Console.WriteLine($"Camera position: { camera.position.ToString() }");
            Console.WriteLine($"Camera rotation: { camera.rotation.ToString() }");
            Console.WriteLine($"Camera yaw pitch: { yaw } { pitch }");
        } 
        
        window.vulkanRenderer!.vp.view = a;
    }
    
    private static mat4 Perspective(float fovy, float aspect, float zNear, float zFar)
    {
        double num = Math.Tan(fovy / 2.0);
        return mat4.Zero with
        {
            m00 = (float) (1.0 / (aspect * num)),
            m11 = (float) (1.0 / num),
            m22 = zFar / (zNear - zFar),
            m23 = -1f,
            m32 = -(zFar * zNear) / (zFar - zNear)
            
            // m22 = (float) (-((double) zFar + (double) zNear) / ((double) zFar - (double) zNear)),
            // m32 = (float) (-(2.0 * (double) zFar * (double) zNear) / ((double) zFar - (double) zNear))
        };
    }

    private void UpdateClasses()
    {
        Time.Update();
        // Input.Update();
    }
}