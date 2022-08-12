using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkBuffer[] vertexUniformBuffers = null!;
    private VkDeviceMemory[] vertexUniformBuffersMemory = null!;
    
    private VkBuffer[] fragmentUniformBuffers = null!;
    private VkDeviceMemory[] fragmentUniformBuffersMemory = null!;
    
    private void CreateUniformBuffers()
    {
        // Resize the uniformBuffers and its memories arrays
        vertexUniformBuffers = new VkBuffer[MAX_CONCURRENT_FRAMES];
        vertexUniformBuffersMemory = new VkDeviceMemory[MAX_CONCURRENT_FRAMES];

        fragmentUniformBuffers = new VkBuffer[MAX_CONCURRENT_FRAMES];
        fragmentUniformBuffersMemory = new VkDeviceMemory[MAX_CONCURRENT_FRAMES];

        // For each concurrent frame
        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            // Create a uniform buffer
            VulkanUtilities.CreateBuffer(
                vertexUniformDataSize, VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT, 
                VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
                out vertexUniformBuffers[i], out vertexUniformBuffersMemory[i]
            );
            
            // Create a uniform buffer
            VulkanUtilities.CreateBuffer(
                vertexUniformDataSize, VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT, 
                VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
                out fragmentUniformBuffers[i], out fragmentUniformBuffersMemory[i]
            );
        }
    }

    private void UpdateUniformBuffer(uint imageIndex)
    {
        UpdateVertexUniformBuffer(imageIndex);
        UpdateFragmentUniformBuffer(imageIndex);
    }

    private void UpdateVertexUniformBuffer(uint imageIndex)
    {
        // Create an empty pointer
        void *data;
        
        // Map memory to current vertex uniform buffer's memory to the empty pointer
        VulkanNative.vkMapMemory(this.logicalDevice, vertexUniformBuffersMemory[imageIndex], 0, vertexUniformDataSize, 0, &data);

        // Copy memory data
        fixed (VertexUniformData* vertexUniformDataPtr = &vertexUniformData)
        {
            Buffer.MemoryCopy(vertexUniformDataPtr, data, vertexUniformDataSize, vertexUniformDataSize);
        }
        
        // Unmap the memory for current vertex uniform buffer's memory
        VulkanNative.vkUnmapMemory(this.logicalDevice, vertexUniformBuffersMemory[imageIndex]);
    }
    
    private void UpdateFragmentUniformBuffer(uint imageIndex)
    {
        // Create an empty pointer
        void *data;
        
        // Map memory to current fragment uniform buffer's memory to the empty pointer
        VulkanNative.vkMapMemory(this.logicalDevice, fragmentUniformBuffersMemory[imageIndex], 0, fragmentUniformDataSize, 0, &data);

        // Copy memory data
        fixed (FragmentUniformData* fragmentUniformDataPtr = &fragmentUniformData)
        {
            Buffer.MemoryCopy(fragmentUniformDataPtr, data, fragmentUniformDataSize, fragmentUniformDataSize);
        }
        
        // Unmap the memory for current fragment uniform buffer's memory
        VulkanNative.vkUnmapMemory(this.logicalDevice, fragmentUniformBuffersMemory[imageIndex]);
    }
}