using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

// TODO: Use push constants instead

public unsafe partial class VulkanRenderer
{
    private VkBuffer[] uniformBuffers = null!;
    private VkDeviceMemory[] uniformBuffersMemory = null!;
    
    private void CreateUniformBuffers()
    {
        // Resize the uniformBuffers and its memories arrays
        uniformBuffers = new VkBuffer[MAX_CONCURRENT_FRAMES];
        uniformBuffersMemory = new VkDeviceMemory[MAX_CONCURRENT_FRAMES];

        // For each concurrent frame
        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            // Create a uniform buffer
            VulkanUtilities.CreateBuffer(
                vpSize, VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT, 
                VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
                out uniformBuffers[i], out uniformBuffersMemory[i]
            );
        }
    }

    private void UpdateUniformBuffer(uint imageIndex)
    {
        // Create an empty pointer
        void *data;
        
        // Map memory to current uniform buffer's memory to the empty pointer
        VulkanNative.vkMapMemory(this.logicalDevice, uniformBuffersMemory[imageIndex], 0, vpSize, 0, &data);

        // Copy memory data
        fixed (VP* mvpPtr = &vp)
        {
            Buffer.MemoryCopy(mvpPtr, data, vpSize, vpSize);
        }
        
        // Unmap the memory for current uniform buffer's memory
        VulkanNative.vkUnmapMemory(this.logicalDevice, uniformBuffersMemory[imageIndex]);
    }
}