using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan.Abstractions;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private Image depthImage = null!;
    private VkFormat depthImageFormat; 

    private void CreateDepthBufferImage()
    {
        // Retrieve the best depth buffer image format
        this.depthImageFormat = this.GetBestDepthBufferFormat(
            new VkFormat[] { VkFormat.VK_FORMAT_D32_SFLOAT_S8_UINT, VkFormat.VK_FORMAT_D32_SFLOAT, VkFormat.VK_FORMAT_D24_UNORM_S8_UINT },
            VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
            VkFormatFeatureFlags.VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT
        );
        
        new Image.Builder()
            .SetSize(swapchainExtent.width, swapchainExtent.height)
            .SetSampling(this.msaaSampleCount)
            .SetUsage(VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT)
            .SetMemoryFlags(VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
            .SetFormat(depthImageFormat)
        .Build(out depthImage);
        
        depthImage.GenerateImageView(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT);
        
        depthImage.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL);
    }

    private VkFormat GetBestDepthBufferFormat(in VkFormat[] formatCandidates, in VkImageTiling imageTiling, VkFormatFeatureFlags formatFeatureFlags)
    {
        // Check every format
        foreach (VkFormat format in formatCandidates)
        {
            // Get the properties for the current format
            VkFormatProperties formatProperties;
            VulkanNative.vkGetPhysicalDeviceFormatProperties(this.physicalDevice, format, &formatProperties);

            // Check if the required format properties are supported
            if (imageTiling == VkImageTiling.VK_IMAGE_TILING_LINEAR && (formatProperties.linearTilingFeatures & formatFeatureFlags) == formatFeatureFlags)
            {
                return format;
            }
            else if (imageTiling == VkImageTiling.VK_IMAGE_TILING_OPTIMAL && (formatProperties.optimalTilingFeatures & formatFeatureFlags) == formatFeatureFlags)
            {
                return format;
            }
        }
        
        // Otherwise throw an error
        VulkanDebugger.ThrowError("No depth buffer formats supported");

        return VkFormat.VK_FORMAT_UNDEFINED;
    }
}