using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core;

public static class EngineCore
{
    public static IntPtr glfwWindow;
    public static VkPhysicalDevice physicalDevice;
    public static VkDevice logicalDevice;
    public static VkCommandPool commandPool;
    public static VkQueue graphicsQueue;
}