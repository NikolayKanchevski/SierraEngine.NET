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
        mvp.model = mat4.Rotate(glm.Radians(90.0f) * (float) Time.deltaTime * 100, new vec3(0.0f, 0.0f, 1.0f));
        mvp.view = mat4.LookAt(new vec3(2.0f, 2.0f, 2.0f), vec3.Zero, new vec3(0.0f, 0.0f, 1.0f));
        mvp.projection = mat4.Perspective(glm.Radians(45.0f), (float) this.swapchainExtent.width / this.swapchainExtent.height, 0.1f, 10.0f);
        mvp.projection[1, 1] *= -1;
        
        void *data;
        VulkanNative.vkMapMemory(this.logicalDevice, uniformBuffersMemory[imageIndex], 0, mvpSize, 0, &data);

        fixed (MVP* mvpPtr = &mvp)
        {
            Buffer.MemoryCopy(mvpPtr, data, mvpSize, mvpSize);   
        }
        
        VulkanNative.vkUnmapMemory(this.logicalDevice, uniformBuffersMemory[imageIndex]);
    }
}