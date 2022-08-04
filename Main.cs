using SierraEngine.Core.Application;
using SierraEngine.Core.Rendering.Vulkan;
using Window = SierraEngine.Core.Rendering.Window;

namespace SierraEngine;

public static class Program
{
    public static void Main()
    {
        Window window = new Window(800, 600, "Hello, Vulkan!", true);
        VulkanCore.glfwWindow = window.GetCoreWindow();
        VulkanCore.window = window;
        
        // if (OperatingSystem.IsWindows())
        // {
        //     using var stream = System.IO.File.OpenRead("C:/Users/Niki/RiderProjects/SierraEngine/Other/terminal_icon.png");
        //     ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        //     window.SetIcon(image.Width, image.Height, image.Data);
        // }

        Application application = new Application(window);
        application.Start();
    }
}