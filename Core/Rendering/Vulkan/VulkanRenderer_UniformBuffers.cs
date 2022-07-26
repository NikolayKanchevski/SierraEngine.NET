using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using GlmSharp;
using SierraEngine.Engine;

namespace SierraEngine.Core.Rendering.Vulkan;

// TODO: Use push constants instead

public unsafe partial class VulkanRenderer
{
    private VkBuffer[] uniformBuffers = null!;
    private VkDeviceMemory[] uniformBuffersMemory = null!;
    
    private void CreateUniformBuffers()
    {
        uniformBuffers = new VkBuffer[MAX_CONCURRENT_FRAMES];
        uniformBuffersMemory = new VkDeviceMemory[MAX_CONCURRENT_FRAMES];

        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            CreateBuffer(
                mvpSize, VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT, 
                VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
                out uniformBuffers[i], out uniformBuffersMemory[i]
            );
        }
    }

    private void UpdateUniformBuffer(uint imageIndex)
    {
        void *data;
        VulkanNative.vkMapMemory(this.logicalDevice, uniformBuffersMemory[imageIndex], 0, mvpSize, 0, &data);

        fixed (MVP* mvpPtr = &mvp)
        {
            Buffer.MemoryCopy(mvpPtr, data, mvpSize, mvpSize);
        }
        
        VulkanNative.vkUnmapMemory(this.logicalDevice, uniformBuffersMemory[imageIndex]);
    }
}