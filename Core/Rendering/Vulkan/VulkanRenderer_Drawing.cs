using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkSemaphore imageAvailableSemaphore;
    private VkSemaphore renderFinishedSemaphore;
    private VkFence frameBeingRenderedFence;

    private void CreateSynchronisation()
    {
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

        // Create "imageAvailable" semaphore
        fixed (VkSemaphore* imageAvailableSemaphorePtr = &imageAvailableSemaphore)
        {
            Utilities.CheckErrors(VulkanNative.vkCreateSemaphore(this.logicalDevice, &semaphoreCreateInfo, null, imageAvailableSemaphorePtr));
        }
        
        // Create "renderFinished" semaphore
        fixed (VkSemaphore* renderFinishedSemaphorePtr = &renderFinishedSemaphore)
        {
            Utilities.CheckErrors(VulkanNative.vkCreateSemaphore(this.logicalDevice, &semaphoreCreateInfo, null, renderFinishedSemaphorePtr));
        }
        
        // Create "frameBeingRenderedFence" fence
        fixed (VkFence* frameBeingRenderedFencePtr = &frameBeingRenderedFence)
        {
            Utilities.CheckErrors(VulkanNative.vkCreateFence(this.logicalDevice, &fenceCreateInfo, null, frameBeingRenderedFencePtr));
        }
    }
    
    private void Draw()
    {
        // Create a pointer to the needed fences
        VkFence* fencesPtr = stackalloc VkFence[] { frameBeingRenderedFence };
        
        // Wait for the fences to be signalled and reset them
        VulkanNative.vkWaitForFences(this.logicalDevice, 1, fencesPtr, VkBool32.True, UInt64.MaxValue);
        VulkanNative.vkResetFences(this.logicalDevice, 1, fencesPtr);

        // Get the current swapchain image
        uint imageIndex;
        VulkanNative.vkAcquireNextImageKHR(this.logicalDevice, this.swapchain, UInt64.MaxValue, imageAvailableSemaphore, VkFence.Null, &imageIndex);

        // Reset and re-record the command buffer
        VulkanNative.vkResetCommandBuffer(this.commandBuffer, 0);
        this.RecordCommandBuffer(this.commandBuffer, imageIndex);

        // Define pointers to the needed references 
        VkSemaphore* waitSemaphores = stackalloc VkSemaphore[] { imageAvailableSemaphore };
        VkPipelineStageFlags* waitStages = stackalloc VkPipelineStageFlags[] { VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT };
        VkSemaphore* signalSemaphores = stackalloc VkSemaphore[] { renderFinishedSemaphore };
        VkCommandBuffer* commandBufferPtr = stackalloc VkCommandBuffer[] { commandBuffer };

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
        Utilities.CheckErrors(VulkanNative.vkQueueSubmit(graphicsQueue, 1, &submitInfo, frameBeingRenderedFence));

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
        Utilities.CheckErrors(VulkanNative.vkQueuePresentKHR(this.presentationQueue, &presentInfo));
    }
}