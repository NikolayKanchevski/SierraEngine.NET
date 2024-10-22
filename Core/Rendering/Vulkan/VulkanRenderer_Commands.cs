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
        // Set up the command pool creation info
        VkCommandPoolCreateInfo commandPoolCreateInfo = new VkCommandPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
            flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
            queueFamilyIndex = queueFamilyIndices.graphicsFamily!.Value
        };

        // Create the command pool
        fixed (VkCommandPool* commandPoolPtr = &commandPool)
        {
            VulkanDebugger.CheckResults(
            VulkanNative.vkCreateCommandPool(this.logicalDevice, &commandPoolCreateInfo, null, commandPoolPtr),
            "Failed to create command pool"
            );
        }
        
        // Assign the EngineCore's command pool
        VulkanCore.commandPool = commandPool;
    }

    private void CreateCommandBuffers()
    {
        // Resize the command buffers array
        commandBuffers = new VkCommandBuffer[MAX_CONCURRENT_FRAMES];
        
        // Set up allocation info
        VkCommandBufferAllocateInfo commandBufferAllocateInfo = new VkCommandBufferAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
            commandPool = commandPool,
            level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY,
            commandBufferCount = 1
        };
        
        // Allocate a command buffer for each frame
        for (int i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            // Allocate the buffer
            fixed (VkCommandBuffer* commandBufferPtr = &commandBuffers[i])
            {
                VulkanDebugger.CheckResults(
                    VulkanNative.vkAllocateCommandBuffers(this.logicalDevice, &commandBufferAllocateInfo, commandBufferPtr),
                    $"Failed to allocate command buffer [{i}]"
                );
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

        // Queries must be reset after each individual use.
        VulkanNative.vkCmdResetQueryPool(givenCommandBuffer, this.drawTimeQueryPool,  imageIndex * 2, 2);
        
        // Start GPU timer
        VulkanNative.vkCmdWriteTimestamp(givenCommandBuffer, VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, drawTimeQueryPool, imageIndex * 2);

        // Begin the render pass
        renderPass.SetFramebuffer(swapchainFrameBuffers[imageIndex]);
        renderPass.Begin(givenCommandBuffer);
        
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
        VkBuffer* vertexBuffers = stackalloc VkBuffer[1]; 
        VkDescriptorSet* descriptorSetsPtr = stackalloc VkDescriptorSet[3];

        foreach (var mesh in World.meshes)
        {
            // Define a pointer to the vertex buffer
            vertexBuffers[0] = mesh.GetVertexBuffer();

            // Bind the vertex buffer
            VulkanNative.vkCmdBindVertexBuffers(givenCommandBuffer, 0, 1, vertexBuffers, offsets);

            // Bind the index buffer
            VulkanNative.vkCmdBindIndexBuffer(givenCommandBuffer, mesh.GetIndexBuffer(), 0, VkIndexType.VK_INDEX_TYPE_UINT32);

            // Get the push constant model of the current mesh and push it to shader
            PushConstant pushConstantData = mesh.GetPushConstantData();
            // FragmentPushConstant fragmentPushConstantData = mesh.GetFragmentPushConstantData();

            VulkanNative.vkCmdPushConstants(
                givenCommandBuffer, this.graphicsPipelineLayout,
                VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT, 0,
                pushConstantSize, &pushConstantData
            );
            
            descriptorSetsPtr[0] = uniformDescriptorSets[currentFrame];
            descriptorSetsPtr[1] = diffuseTextures[mesh.diffuseTextureID].descriptorSet;
            descriptorSetsPtr[2] = specularTextures[mesh.specularTextureID].descriptorSet;
            
            VulkanNative.vkCmdBindDescriptorSets(givenCommandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, this.graphicsPipelineLayout, 0, 3, descriptorSetsPtr, 0, null);

            // Draw using the index buffer to prevent vertex re-usage
            VulkanNative.vkCmdDrawIndexed(givenCommandBuffer, mesh.indexCount, 1, 0, 0, 0);
        }

        imGuiController.Render(givenCommandBuffer);

        // End the render pass
        renderPass.End(givenCommandBuffer);
        
        // End GPU timer
        VulkanNative.vkCmdWriteTimestamp(givenCommandBuffer, VkPipelineStageFlags.VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, drawTimeQueryPool, imageIndex * 2 + 1);
        
        FetchRenderTimeResults(imageIndex);

        // End the command buffer and check for errors during command execution
        if (VulkanNative.vkEndCommandBuffer(givenCommandBuffer) != VkResult.VK_SUCCESS)
        {
            VulkanDebugger.ThrowError("Failed to end command buffer");
        }
    }
    
    private void FetchRenderTimeResults(uint swapchainIndex)
    {
        UInt64* buffer = stackalloc UInt64[2];

        uint size = sizeof(UInt64) * 2;
        
        // Check if draw time query results are available
        VkResult result = VulkanNative.vkGetQueryPoolResults(this.logicalDevice, drawTimeQueryPool, swapchainIndex * 2, 2, new UIntPtr(size), buffer, sizeof(UInt64), VkQueryResultFlags.VK_QUERY_RESULT_64_BIT);
        if (result == VkResult.VK_NOT_READY)
        {
            return;
        }
        else if (result == VkResult.VK_SUCCESS)
        {
            // Calculate the difference
            drawTimeQueryResults[swapchainIndex] = (buffer[1] - buffer[0]) * timestampPeriod;
        }
        else
        {
            VulkanDebugger.ThrowError("Failed to receive query results");
        }
        
        // Calculate final GPU draw time
        VulkanRendererInfo.drawTime = AverageDrawTime(this.drawTimeQueryResults);
    }

    private float AverageDrawTime(params float[] drawTimes)
    {
        float result = 0.0f;
        foreach (float drawTime in drawTimes) result += drawTime;

        return (result / drawTimes.Length) / 1000000f;
    }
}