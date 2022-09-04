using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan.Abstractions;

namespace SierraEngine.Core.Rendering.Vulkan;

public partial class VulkanRenderer
{
    private Framebuffer[] swapchainFrameBuffers = null!;
    
    private void CreateFrameBuffers()
    {
        // Resize the frame buffers array to be of the same size as the swapchainImages array
        swapchainFrameBuffers = new Framebuffer[swapchainImages.Length];
        
        // Assign the static attachments
        VkImageView[] attachments = new VkImageView[3];
        attachments[0] = this.colorImage;
        attachments[1] = this.depthImage;

        // Create a framebuffer for each swapchain image
        for (int i = 0; i < swapchainImages.Length; i++)
        {
            // Assign the dynamic attachments
             attachments[2] = this.swapchainImages[i];

             // Add the attachments to the framebuffer and then create it
             new Framebuffer.Builder()
                 .SetRenderPass(this.renderPass)
                 .AddAttachments(attachments)
            .Build(out swapchainFrameBuffers[i]);
        }
    }
}