using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan.Abstractions;

public unsafe class Framebuffer
{
    public class Builder
    {
        private VkRenderPass vkRenderPass;
        private readonly List<VkImageView> attachments = new List<VkImageView>();
        
        public Builder SetRenderPass(in RenderPass givenRenderPass)
        {
            // Save the provided render pass
            this.vkRenderPass = givenRenderPass;
            return this;
        }
        
        public Builder AddAttachment(in VkImageView attachment)
        {
            // Add the given attachment to the local list
            this.attachments.Add(attachment);
            return this;
        }

        public Builder AddAttachments(in VkImageView[] givenAttachments)
        {
            // Add the given attachments to the local list
            this.attachments.AddRange(givenAttachments);
            return this;
        }

        public Builder AddAttachments(in List<VkImageView> givenAttachments)
        {
            // Add the given attachments to the local list
            this.attachments.AddRange(givenAttachments);
            return this;
        }

        public void Build(out Framebuffer framebuffer)
        {
            // Construct and return a framebuffer
            framebuffer = new Framebuffer(vkRenderPass, attachments.ToArray());
        }
    }

    private VkFramebuffer vkFramebuffer; 

    private Framebuffer(in VkRenderPass vkRenderPass, in VkImageView[] attachments)
    {
        // Set up the framebuffer creation info
        VkFramebufferCreateInfo framebufferCreateInfo = new VkFramebufferCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO,
            renderPass = vkRenderPass,
            attachmentCount = (uint)attachments.Length,
            width = VulkanCore.swapchainExtent.width,
            height = VulkanCore.swapchainExtent.height,
            layers = 1
        };
        
        // Assign the attachments to the framebuffer info
        fixed (VkImageView* attachmentsPtr = attachments)
        {
            framebufferCreateInfo.pAttachments = attachmentsPtr;
        }

        // Create the Vulkan framebuffer
        fixed (VkFramebuffer* framebufferPtr = &vkFramebuffer)
        {
            VulkanDebugger.CheckResults(
                VulkanNative.vkCreateFramebuffer(VulkanCore.logicalDevice, &framebufferCreateInfo, null, framebufferPtr),
                $"Failed to create a framebuffer with attachment count of [{ attachments.Length }]"
            );
        }
    }

    public VkFramebuffer GetVkFramebuffer()
    {
        // Return the Vulkan framebuffer
        return this.vkFramebuffer;
    }

    public static implicit operator VkFramebuffer(Framebuffer givenFramebuffer)
    {
        return givenFramebuffer.vkFramebuffer;
    }

    public void CleanUp()
    {
        // Destroy the Vulkan framebuffer
        VulkanNative.vkDestroyFramebuffer(VulkanCore.logicalDevice, this.vkFramebuffer, null);
    }
}