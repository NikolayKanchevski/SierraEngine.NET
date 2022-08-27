using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan.Abstractions;

namespace SierraEngine.Core.Rendering.Vulkan;

public partial class VulkanRenderer
{
    private VkSampleCountFlags msaaSampleCount = VkSampleCountFlags.VK_SAMPLE_COUNT_64_BIT;
    private bool sampleShadingEnabled = true;

    private Image colorImage = null!;
    
    private void CreateColorBufferImage()
    {
        // Create the sampled color image
        new Image.Builder()
            .SetSize(swapchainExtent.width, swapchainExtent.height)
            .SetSampling(msaaSampleCount)
            .SetFormat(swapchainImageFormat)
            .SetUsage(VkImageUsageFlags.VK_IMAGE_USAGE_TRANSIENT_ATTACHMENT_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT)
            .SetMemoryFlags(VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
        .Build(out colorImage);
        
        // Create an image view off the sampled color image
        colorImage.GenerateImageView(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
    }

    private VkSampleCountFlags GetHighestSupportedSampleCount()
    {
        VkSampleCountFlags countFlags = VulkanCore.physicalDeviceProperties.limits.framebufferColorSampleCounts & VulkanCore.physicalDeviceProperties.limits.framebufferDepthSampleCounts;
        
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_64_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_64_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_32_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_32_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_16_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_16_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_8_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_8_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_4_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_4_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_2_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_2_BIT;

        return VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
    }
}