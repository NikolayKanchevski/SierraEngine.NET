using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Components;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkCommandPool commandPool;
    private VkCommandBuffer[] commandBuffers = null!;

    private readonly VkClearColorValue backgroundColor = new VkClearColorValue(0.0f, 0.0f, 0.0f, 1.0f);
    
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
            if (VulkanNative.vkCreateCommandPool(this.logicalDevice, &commandPoolCreateInfo, null, commandPoolPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create the command pool");
            }
        }
        
        // Assign the EngineCore's command pool
        VulkanCore.commandPool = commandPool;
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
                if (VulkanNative.vkAllocateCommandBuffers(this.logicalDevice, &commandBufferAllocateInfo, commandBufferPtr) != VkResult.VK_SUCCESS)
                {
                    VulkanDebugger.ThrowError($"Failed to allocate command buffer [{ i }]");
                }
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
        if (VulkanNative.vkBeginCommandBuffer(givenCommandBuffer, &bufferBeginInfo) != VkResult.VK_SUCCESS)
        {
            VulkanDebugger.ThrowError($"Failed to begin command buffer [{ imageIndex }]");
        }

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

        VkClearValue* clearValues = stackalloc VkClearValue[2];
        clearValues[0].color = backgroundColor;
        clearValues[1].depthStencil = new VkClearDepthStencilValue()
        {
            depth = 1.0f,
            stencil = 0
        };
        
        // Reference the clear value to the render pass begin info
        renderPassBeginInfo.clearValueCount = 2;
        renderPassBeginInfo.pClearValues = clearValues;
        
        // Begin the render pass
        VulkanNative.vkCmdBeginRenderPass(givenCommandBuffer, &renderPassBeginInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);
        
        // Bind the pipeline
        VulkanNative.vkCmdBindPipeline(givenCommandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, this.graphicsPipeline);
        
        // Set up the viewport
        VkViewport viewport = new VkViewport()
        {
            x = 0,
            y = 0,
            width = this.swapchainExtent.width,
            height = this.swapchainExtent.height,
            minDepth = 0.0f,
            maxDepth = 1.0f
        };
        
        // Set up the scissor
        VkRect2D scissor = new VkRect2D()
        {
            offset = VkOffset2D.Zero,
            extent = this.swapchainExtent
        };
        
        VulkanNative.vkCmdSetViewport(givenCommandBuffer, 0, 1, &viewport);
        VulkanNative.vkCmdSetScissor(givenCommandBuffer, 0, 1, &scissor);
        
        ulong* offsets = stackalloc ulong[] { 0 };
        
        #pragma warning disable CA2014
        foreach (Mesh mesh in World.meshes)
        {
            // Define a pointer to the vertex buffer
            VkBuffer* vertexBuffers = stackalloc VkBuffer[] { mesh.GetVertexBuffer() };
            
            // Bind the vertex buffer
            VulkanNative.vkCmdBindVertexBuffers(givenCommandBuffer, 0, 1, vertexBuffers, offsets);
            
            // Bind the index buffer
            VulkanNative.vkCmdBindIndexBuffer(givenCommandBuffer, mesh.GetIndexBuffer(), 0, VkIndexType.VK_INDEX_TYPE_UINT16);

            // Get the push constant model of the current mesh and push it to shader
            Model curentModel = mesh.GetModelStructure();
            VulkanNative.vkCmdPushConstants(
                givenCommandBuffer, this.graphicsPipelineLayout,
                VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT, 0, 
                meshModelSize, &curentModel);
            
            VkDescriptorSet* descriptorSetsPtr = stackalloc VkDescriptorSet[] { uniformDescriptorSets[currentFrame], samplerDescriptorSets[mesh.textureID] };
            VulkanNative.vkCmdBindDescriptorSets(givenCommandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, this.graphicsPipelineLayout, 0, 2, descriptorSetsPtr, 0, null);
            
            // Draw using the index buffer to prevent vertex re-usage
            VulkanNative.vkCmdDrawIndexed(givenCommandBuffer, mesh.indexCount, 1, 0, 0, 0);
        }
        
        // End the render pass
        VulkanNative.vkCmdEndRenderPass(givenCommandBuffer);
        
        // End the command buffer and check for errors during command execution
        if (VulkanNative.vkEndCommandBuffer(givenCommandBuffer) != VkResult.VK_SUCCESS)
        {
            VulkanDebugger.ThrowError("Failed to end command buffer");
        }
    }
}