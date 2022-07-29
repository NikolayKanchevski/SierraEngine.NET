using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine;
using Glfw;
using GlmSharp;
using StbImageSharp;
using Window = SierraEngine.Core.Rendering.Window;

namespace SierraEngine;

public static class Program
{
    private static IntPtr glfwWindow;
    private static Window window = null!;
    // public static string ROOT_FOLDER_PATH = "";
    
    public static void Main()
    {
        // if (OperatingSystem.IsWindows())
        // {
        //     // TODO: Set path!
        //     ROOT_FOLDER_PATH = "";
        // }
        // else if (OperatingSystem.IsMacOS())
        // {
        //     ROOT_FOLDER_PATH = "/Users/nikolay/RiderProjects/SierraEngine.NET/";
        // }
        
        window = new Window(800, 600, "Hello, Vulkan!", true);
        glfwWindow = window.GetCoreWindow();

        // if (OperatingSystem.IsWindows())
        // {
        //     using var stream = System.IO.File.OpenRead("C:/Users/Niki/RiderProjects/SierraEngine/Other/terminal_icon.png");
        //     ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        //     window.SetIcon(image.Width, image.Height, image.Data);
        // }
            
        VulkanRenderer vulkanRenderer = new VulkanRenderer(ref window);
        window.SetRenderer(ref vulkanRenderer);

        while (!window.closed)
        {
            UpdateClasses();

            ProgramLoop();
            
            window.SetTitle($"FPS: { Time.FPS }"); 
            
            window.Update();
        }
        
        window.Destroy();
        
        Glfw3.Terminate();
    }

    private static void ProgramLoop()
    {
        const float RADIUS = 8.0f;
        float camX = (float) Math.Sin(Time.upTime) * RADIUS;
        float camZ = (float) Math.Cos(Time.upTime) * RADIUS;
        
        window.vulkanRenderer!.vp.model = mat4.Rotate(glm.Radians(90.0f), new vec3(0.0f, 0.0f, 1.0f));
        window.vulkanRenderer!.vp.model = mat4.Rotate((float) Math.Cos(Time.upTime), new vec3(0.0f, 0.0f, 1.0f));
        window.vulkanRenderer!.vp.view = mat4.LookAt(new vec3(camX, 0.0f, camZ), vec3.Zero, new vec3(0.0f, 1.0f, 0.0f));
        window.vulkanRenderer!.vp.projection = mat4.Perspective(glm.Radians(45.0f), (float) window.width / window.height, 0.1f, 100.0f);
        window.vulkanRenderer!.vp.projection[1, 1] *= -1;
    }

    private static void UpdateClasses()
    {
        Time.Update();
        Input.Update();
    }
}