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
            samples = this.msaaSampleCount,
            loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
            stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, 
            finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
        };

        // Set up the color attachment reference
        VkAttachmentReference colorAttachmentReference = new VkAttachmentReference()
        {
            attachment = 0,
            layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
        };

        // Retrieve the best depth buffer image format
        this.depthImageFormat = this.GetBestDepthBufferFormat(
            new VkFormat[] { VkFormat.VK_FORMAT_D32_SFLOAT_S8_UINT, VkFormat.VK_FORMAT_D32_SFLOAT, VkFormat.VK_FORMAT_D24_UNORM_S8_UINT },
            VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
            VkFormatFeatureFlags.VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT
        );

        // Set up depth attachment
        VkAttachmentDescription depthAttachment = new VkAttachmentDescription()
        {
            format = depthImageFormat,
            samples = this.msaaSampleCount,
            loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
            stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL
        };

        // Set up depth attachment reference
        VkAttachmentReference depthAttachmentReference = new VkAttachmentReference()
        {
            attachment = 1,
            layout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL
        };

        VkAttachmentDescription colorAttachmentResolve = new VkAttachmentDescription()
        {
            format = this.swapchainImageFormat,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
            stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR
        };

        VkAttachmentReference colorAttachmentResolveReference = new VkAttachmentReference()
        {
            attachment = 2,
            layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
        };

        VkAttachmentReference* resolveAttachmentReferencesPtr = stackalloc VkAttachmentReference[] { colorAttachmentResolveReference };
        
        // Set up the subpass properties
        VkSubpassDescription subpassDescription = new VkSubpassDescription()
        {
            pipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentReference,
            pDepthStencilAttachment = &depthAttachmentReference,
            pResolveAttachments = resolveAttachmentReferencesPtr
        };
        
        // Create a subpass dependency
        VkSubpassDependency subpassDependency = new VkSubpassDependency()
        {
            srcSubpass = ~0U,
            dstSubpass = 0,
            srcStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT | VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT,
            srcAccessMask = 0,
            dstStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT | VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT,
            dstAccessMask = VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT | VkAccessFlags.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT,
        };

        VkAttachmentDescription* attachmentReferencesPtr = stackalloc VkAttachmentDescription[] { colorAttachment, depthAttachment, colorAttachmentResolve };
        
        // Set up the render pass creation info 
        VkRenderPassCreateInfo renderPassCreateInfo = new VkRenderPassCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO,
            attachmentCount = 3,
            pAttachments = attachmentReferencesPtr,
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