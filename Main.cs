using Glfw;
using SierraEngine.Core;
using SierraEngine.Core.Application;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using Window = SierraEngine.Core.Rendering.Window;

namespace SierraEngine;

public static class Program
{
    public static void Main()
    {
        // GameObject root = new GameObject("ROOT");
        // GameObject root_child_1 = new GameObject("ROOT_CHILD_1");
        // GameObject root_child_1_1 = new GameObject("ROOT_CHILD_1_1");
        // GameObject root_child_1_2 = new GameObject("ROOT_CHILD_1_2");
        // GameObject root_child_1_2_1 = new GameObject("ROOT_CHILD_1_2_1");
        // GameObject root_child_2 = new GameObject("ROOT_CHILD_2");
        //
        // root_child_1.SetParent(root);
        // root_child_1_1.SetParent(root_child_1);
        // root_child_1_2.SetParent(root_child_1);
        // root_child_1_2_1.SetParent(root_child_1_2);
        // root_child_2.SetParent(root);
        //
        // World.PrintHierarchy();
        // return;
        
        // if (OperatingSystem.IsWindows())
        // {
        //     using var stream = System.IO.File.OpenRead("C:/Users/Niki/RiderProjects/SierraEngine/Other/terminal_icon.png");
        //     ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        //     window.SetIcon(image.Width, image.Height, image.Data);
        // }

        Application application = new Application();
        application.Start();
        
        // World.PrintHierarchy();
    }
}