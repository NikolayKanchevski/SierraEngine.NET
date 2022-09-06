using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkSemaphore[] imageAvailableSemaphores = null!;
    private VkSemaphore[] renderFinishedSemaphores = null!;
    private VkFence[] frameBeingRenderedFences = null!;

    private uint currentFrame = 0;
    public bool frameBufferResized;

    private const uint MAX_CONCURRENT_FRAMES = 3;
        
    private void CreateSynchronisation()
    {
        // Resize the semaphores and fences arrays
        imageAvailableSemaphores = new VkSemaphore[MAX_CONCURRENT_FRAMES];
        renderFinishedSemaphores = new VkSemaphore[MAX_CONCURRENT_FRAMES];
        frameBeingRenderedFences = new VkFence[MAX_CONCURRENT_FRAMES];
        
        // Define the semaphores creation info (universal for all semaphores)
        VkSemaphoreCreateInfo semaphoreCreateInfo = new VkSemaphoreCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO
        };

        // Define the fences creation info (universal for all fences)
        VkFenceCreateInfo fenceCreateInfo = new VkFenceCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
            flags = VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT
        };

        // Create semaphores and fences 
        for (int i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            // Create "imageAvailable" semaphore
            fixed (VkSemaphore* imageAvailableSemaphorePtr = &imageAvailableSemaphores[i])
            {
                VulkanNative.vkCreateSemaphore(this.logicalDevice, &semaphoreCreateInfo, null, imageAvailableSemaphorePtr);
            }
        
            // Create "renderFinished" semaphore
            fixed (VkSemaphore* renderFinishedSemaphorePtr = &renderFinishedSemaphores[i])
            {
                VulkanNative.vkCreateSemaphore(this.logicalDevice, &semaphoreCreateInfo, null, renderFinishedSemaphorePtr);
            }
        
            // Create "frameBeingRenderedFence" fence
            fixed (VkFence* frameBeingRenderedFencePtr = &frameBeingRenderedFences[i])
            {
                VulkanNative.vkCreateFence(this.logicalDevice, &fenceCreateInfo, null, frameBeingRenderedFencePtr);
            }
        }
    }
    
    private void Draw()
    {
        // Create a pointer to the needed fences
        VkFence* fencesPtr = stackalloc VkFence[] { frameBeingRenderedFences[currentFrame] };
        
        // Wait for the fences to be signalled
        VulkanNative.vkWaitForFences(this.logicalDevice, 1, fencesPtr, VkBool32.True, UInt64.MaxValue);

        // Get the current swapchain image
        uint imageIndex;
        VkResult imageAcquireResult = VulkanNative.vkAcquireNextImageKHR(this.logicalDevice, this.swapchain, UInt64.MaxValue, imageAvailableSemaphores[currentFrame], VkFence.Null, &imageIndex);
        
        if (imageAcquireResult == VkResult.VK_ERROR_OUT_OF_DATE_KHR)
        {
            RecreateSwapchainObjects();
            return;
        }

        // Reset the fences
        VulkanNative.vkResetFences(this.logicalDevice, 1, fencesPtr);

        UpdateUniformBuffers(imageIndex);
        
        // Reset and re-record the command buffer
        VulkanNative.vkResetCommandBuffer(this.commandBuffers[currentFrame], 0);
        this.RecordCommandBuffer(this.commandBuffers[currentFrame], imageIndex);

        // Define pointers to the needed references 
        VkSemaphore* waitSemaphores = stackalloc VkSemaphore[] { imageAvailableSemaphores[currentFrame] };
        VkPipelineStageFlags* waitStages = stackalloc VkPipelineStageFlags[] { VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT };
        VkSemaphore* signalSemaphores = stackalloc VkSemaphore[] { renderFinishedSemaphores[currentFrame] };
        VkCommandBuffer* commandBufferPtr = stackalloc VkCommandBuffer[] { commandBuffers[currentFrame] };

        // Set up the submitting info
        VkSubmitInfo submitInfo = new VkSubmitInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
            waitSemaphoreCount = 1,
            pWaitSemaphores = waitSemaphores,
            signalSemaphoreCount = 1,
            pSignalSemaphores = signalSemaphores,
            commandBufferCount = 1,
            pCommandBuffers = commandBufferPtr,
            pWaitDstStageMask = waitStages
        };

        // Submit the queue
        if (VulkanNative.vkQueueSubmit(graphicsQueue, 1, &submitInfo, frameBeingRenderedFences[currentFrame]) != VkResult.VK_SUCCESS)
        {
            VulkanDebugger.ThrowError("Failed to submit graphics queue");
        };

        // Define a pointer to the swapchain
        VkSwapchainKHR* swapchains = stackalloc VkSwapchainKHR[] { this.swapchain };
        
        // Set up presentation info
        VkPresentInfoKHR presentInfo = new VkPresentInfoKHR()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PRESENT_INFO_KHR,
            waitSemaphoreCount = 1,
            pWaitSemaphores = signalSemaphores,
            swapchainCount = 1,
            pSwapchains = swapchains,
            pImageIndices = &imageIndex,
            pResults = null
        };

        // Present
        VkResult queuePresentResult = VulkanNative.vkQueuePresentKHR(this.presentationQueue, &presentInfo);

        if (queuePresentResult == VkResult.VK_ERROR_OUT_OF_DATE_KHR || queuePresentResult == VkResult.VK_SUBOPTIMAL_KHR || frameBufferResized)
        {
            frameBufferResized = false;
            RecreateSwapchainObjects();
        }

        // Increment the current frame whilst capping it to "MAX_CONCURRENT_FRAMES"
        currentFrame = (currentFrame + 1) % MAX_CONCURRENT_FRAMES;
    }
}