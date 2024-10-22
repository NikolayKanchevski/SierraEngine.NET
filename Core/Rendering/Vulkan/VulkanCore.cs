using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public static class VulkanCore
{
    public static GLFW.Window glfwWindow;
    public static Window window = null!;
    /// <summary>
    /// Only call this if you are SURE the window has a Vulkan renderer attached
    /// </summary>
    public static VulkanRenderer vulkanRenderer => window.vulkanRenderer!; 
    public static VkPhysicalDeviceFeatures physicalDeviceFeatures;
    public static VkPhysicalDeviceProperties physicalDeviceProperties;
    public static VkPhysicalDeviceMemoryProperties physicalDeviceMemoryProperties;
    public static VkPhysicalDevice physicalDevice;
    public static VkDevice logicalDevice;
    public static VkExtent2D swapchainExtent;
    public static float swapchainAspectRatio => (float) swapchainExtent.width / swapchainExtent.height;
    public static VkCommandPool commandPool;
    public static VkQueue graphicsQueue;
    public static uint graphicsFamilyIndex;
}