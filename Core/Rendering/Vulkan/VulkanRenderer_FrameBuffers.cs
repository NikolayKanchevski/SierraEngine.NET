using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkFramebuffer[] swapchainFrameBuffers = null!;
    
    private void CreateFrameBuffers()
    {
        // Resize the framebuffers array to be of the same size as the swapchainImages array
        swapchainFrameBuffers = new VkFramebuffer[swapchainImages.Length];

        // Create a framebuffer for each swapchain image
        for (int i = 0; i < swapchainImageViews.Length; i++)
        {
            // Define the attachments for the framebuffer
            #pragma warning disable CA2014
            VkImageView* attachments = stackalloc VkImageView[] { swapchainImageViews[i], depthImageView };

            // Set up the framebuffer creation info
            VkFramebufferCreateInfo framebufferCreateInfo = new VkFramebufferCreateInfo()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO,
                renderPass = this.renderPass,
                attachmentCount = 2,
                pAttachments = attachments,
                width = swapchainExtent.width,
                height = swapchainExtent.height,
                layers = 1
            };

            // Create the framebuffer
            fixed (VkFramebuffer* framebufferPtr = &swapchainFrameBuffers[i])
            {
                if (VulkanNative.vkCreateFramebuffer(this.logicalDevice, &framebufferCreateInfo, null, framebufferPtr) != VkResult.VK_SUCCESS)
                {
                    VulkanDebugger.ThrowError("Failed to create the framebuffer");
                }
            }
        }
    }
}