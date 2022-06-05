using Evergine.Bindings.Vulkan;
using Glfw;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkSurfaceKHR surface;

    private void CreateWindowSurface()
    {
        // Let GLFW create the surface
        Glfw3.CreateWindowSurface(this.instance.Handle, window.GetCoreWindow(), IntPtr.Zero, out ulong surfaceHandle);
        surface = new VkSurfaceKHR(surfaceHandle);
    }
}