using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkSampleCountFlags msaaSampleCount = VkSampleCountFlags.VK_SAMPLE_COUNT_64_BIT;
    private bool sampleShadingEnabled = true;

    private VkImage colorImage;
    private VkImageView colorImageView;
    private VkDeviceMemory colorImageMemory;
    
    private void CreateColorBufferImage()
    {
        // Create the sampled color image
        VulkanUtilities.CreateImage(
            this.swapchainExtent.width, this.swapchainExtent.height, 1, msaaSampleCount,
            swapchainImageFormat, VkImageTiling.VK_IMAGE_TILING_OPTIMAL, VkImageUsageFlags.VK_IMAGE_USAGE_TRANSIENT_ATTACHMENT_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, out colorImage, out colorImageMemory
        );

        // Create an image view off the sampled color image
        VulkanUtilities.CreateImageView(colorImage, this.swapchainImageFormat, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT, 1, out colorImageView);
    }

    private VkSampleCountFlags GetHighestSupportedSampleCount()
    {
        VkSampleCountFlags countFlags = physicalDeviceProperties.limits.framebufferColorSampleCounts & physicalDeviceProperties.limits.framebufferDepthSampleCounts;
        
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_64_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_64_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_32_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_32_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_16_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_16_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_8_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_8_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_4_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_4_BIT;
        if ((countFlags & VkSampleCountFlags.VK_SAMPLE_COUNT_2_BIT) != 0) return VkSampleCountFlags.VK_SAMPLE_COUNT_2_BIT;

        return VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
    }
}