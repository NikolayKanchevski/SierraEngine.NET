using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public static class VulkanCore
{
    public static IntPtr glfwWindow;
    public static Window window = null!;
    public static VkPhysicalDeviceFeatures physicalDeviceFeatures;
    public static VkPhysicalDeviceProperties physicalDeviceProperties;
    public static VkPhysicalDeviceMemoryProperties physicalDeviceMemoryProperties;
    public static VkPhysicalDevice physicalDevice;
    public static VkDevice logicalDevice;
    public static VkExtent2D swapchainExtent; 
    public static VkCommandPool commandPool;
    public static VkQueue graphicsQueue;
    public static uint graphicsFamilyIndex;
}