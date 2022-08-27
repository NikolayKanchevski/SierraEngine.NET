using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan.Abstractions;

namespace SierraEngine.Core.Rendering.Vulkan;

public partial class VulkanRenderer
{
    private Subpass subpass = null!;
    private RenderPass renderPass = null!;
    
    private void CreateRenderPass()
    {
        new Subpass.Builder()
            .SetPipelineBindPoint(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS)
            .AddColorAttachment(0, swapchainImage, VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR, VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE)
            .SetDepthAttachment(1, depthImage, VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR, VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE)
            .AddResolveAttachment(
                2, swapchainImage, VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR, 
                VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
                VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE, VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT, 
                VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE, VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_NONE)
        .Build(out subpass);

        new RenderPass.Builder()
            .SetSubpass(subpass)
        .Build(out renderPass);
    }
}