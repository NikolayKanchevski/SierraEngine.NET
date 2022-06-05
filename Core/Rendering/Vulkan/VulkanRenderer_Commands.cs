using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkCommandPool commandPool;
    private VkCommandBuffer[] commandBuffers = null!;
    
    private void CreateCommandPool()
    {
        // Get family indices to later use their graphics family
        QueueFamilyIndices familyIndices = FindQueueFamilies(this.physicalDevice);

        // Set up the command pool creation info
        VkCommandPoolCreateInfo commandPoolCreateInfo = new VkCommandPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
            flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
            queueFamilyIndex = familyIndices.graphicsFamily!.Value
        };

        // Create the command pool
        fixed (VkCommandPool* commandPoolPtr = &commandPool)
        {
            Utilities.CheckErrors(VulkanNative.vkCreateCommandPool(this.logicalDevice, &commandPoolCreateInfo, null, commandPoolPtr));
        }
    }

    private void CreateCommandBuffers()
    {
        // Resize the command buffers array
        commandBuffers = new VkCommandBuffer[MAX_CONCURRENT_FRAMES];

        // Allocate a command buffer for each frame
        for (int i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            // Set up allocation info
            VkCommandBufferAllocateInfo commandBufferAllocateInfo = new VkCommandBufferAllocateInfo()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
                commandPool = commandPool,
                level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY,
                commandBufferCount = 1
            };

            // Allocate the buffer
            fixed (VkCommandBuffer* commandBufferPtr = &commandBuffers[i])
            {
                Utilities.CheckErrors(VulkanNative.vkAllocateCommandBuffers(this.logicalDevice, &commandBufferAllocateInfo, commandBufferPtr));
            }
        }
    }

    private void RecordCommandBuffer(in VkCommandBuffer givenCommandBuffer, uint imageIndex)
    {
        // Set up buffer begin info
        VkCommandBufferBeginInfo bufferBeginInfo = new VkCommandBufferBeginInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
            flags = 0,
            pInheritanceInfo = null
        };
        
        // Begin the buffer
        Utilities.CheckErrors(VulkanNative.vkBeginCommandBuffer(givenCommandBuffer, &bufferBeginInfo));

        // Set up render pass begin info
        VkRenderPassBeginInfo renderPassBeginInfo = new VkRenderPassBeginInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO,
            renderPass = this.renderPass,
            framebuffer = this.swapchainFrameBuffers[imageIndex],
        };
        
        // Adjust the render area's offset and extent
        renderPassBeginInfo.renderArea.offset = VkOffset2D.Zero;
        renderPassBeginInfo.renderArea.extent = swapchainExtent;

        // Define clear value (background color)
        VkClearValue clearColor = new VkClearValue
        {
            color = new VkClearColorValue(0.2f, 0.2f, 0.2f, 1f),
        };

        // Reference the clear value to the render pass begin info
        renderPassBeginInfo.clearValueCount = 1;
        renderPassBeginInfo.pClearValues = &clearColor;
        
        // Begin the render pass
        VulkanNative.vkCmdBeginRenderPass(givenCommandBuffer, &renderPassBeginInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);
        
        // Bind the pipeline
        VulkanNative.vkCmdBindPipeline(givenCommandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, this.graphicsPipeline);
        
        // Draw 
        VulkanNative.vkCmdDraw(givenCommandBuffer, 3, 1, 0, 0);
        
        // End the render pass
        VulkanNative.vkCmdEndRenderPass(givenCommandBuffer);
        
        // End the command buffer and check for errors during command execution
        Utilities.CheckErrors(VulkanNative.vkEndCommandBuffer(givenCommandBuffer));
    }
}