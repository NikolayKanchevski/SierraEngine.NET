using Evergine.Bindings.Vulkan;
using GLFW;

namespace SierraEngine.Core.Rendering.Vulkan;

public partial class VulkanRenderer
{
    private VkSurfaceKHR surface;

    private void CreateWindowSurface()
    {
        // Let GLFW create the surface
        GLFW.Vulkan.CreateWindowSurface(this.instance.Handle, window.GetCoreWindow(), IntPtr.Zero, out var surfaceHandle);
        surface = new VkSurfaceKHR((ulong) surfaceHandle);
    }
}