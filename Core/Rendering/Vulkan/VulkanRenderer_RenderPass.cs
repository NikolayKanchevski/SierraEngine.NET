using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkRenderPass renderPass;
    
    private void CreateRenderPass()
    {
        // Set up color attachment properties - operations, format, and samples
        VkAttachmentDescription colorAttachment = new VkAttachmentDescription()
        {
            format = swapchainImageFormat,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
            stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, 
            finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR
        };

        // Set up the color attachment reference
        VkAttachmentReference colorAttachmentReference = new VkAttachmentReference()
        {
            attachment = 0,
            layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
        };

        // Set up the subpass properties
        VkSubpassDescription subpassDescription = new VkSubpassDescription()
        {
            pipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentReference
        };
        
        // Create a subpass dependency
        VkSubpassDependency subpassDependency = new VkSubpassDependency()
        {
            srcSubpass = ~0U,
            dstSubpass = 0,
            srcStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
            srcAccessMask = 0,
            dstStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
            dstAccessMask = VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
        };
        
        // Set up the render pass creation info 
        VkRenderPassCreateInfo renderPassCreateInfo = new VkRenderPassCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO,
            attachmentCount = 1,
            pAttachments = &colorAttachment,
            subpassCount = 1,
            pSubpasses = &subpassDescription,
            dependencyCount = 1,
            pDependencies = &subpassDependency
        };


        // Create the render pass
        fixed (VkRenderPass* renderPassPtr = &renderPass)
        {
            if (VulkanNative.vkCreateRenderPass(this.logicalDevice, &renderPassCreateInfo, null, renderPassPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create render pass");
            }
        }
    } 
}