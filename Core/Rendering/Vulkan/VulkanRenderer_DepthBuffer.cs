using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkImage depthImage;
    private VkImageView depthImageView;
    private VkDeviceMemory depthImageMemory;
    private VkFormat depthImageFormat; 

    private void CreateDepthBufferImage()
    {
        // Create the depth buffer image
        VulkanUtilities.CreateImage(
            this.swapchainExtent.width, this.swapchainExtent.height, 1, this.msaaSampleCount,
            depthImageFormat, VkImageTiling.VK_IMAGE_TILING_OPTIMAL, 
            VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT,
            out depthImage, out depthImageMemory
        );
        
        // Create the depth buffer image view
        VulkanUtilities.CreateImageView(depthImage, depthImageFormat, VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT, 1, out depthImageView);
        
        // Transition the layout of the depth buffer image so it is optimal
        VulkanUtilities.TransitionImageLayout(
            depthImage, depthImageFormat, 
            VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, 
            VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, 1
        );
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