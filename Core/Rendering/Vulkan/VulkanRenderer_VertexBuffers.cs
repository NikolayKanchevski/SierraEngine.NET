using System.Reflection;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkBuffer vertexBuffer;
    private VkDeviceMemory vertexBufferMemory;
    
    private void CreateVertexBuffers()
    {
        // Set up vertex buffer creation info
        VkBufferCreateInfo vertexBufferCreateInfo = new VkBufferCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO,
            size = (ulong) (Marshal.SizeOf(vertices[0]) * vertices.Length),
            usage = VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT,
            sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE
        };

        // Create the vertex buffer
        fixed (VkBuffer* vertexBufferPtr = &vertexBuffer)
        {
            if (VulkanNative.vkCreateBuffer(this.logicalDevice, &vertexBufferCreateInfo, null, vertexBufferPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create vertex buffer");
            }
        }

        // Get the memory requirements for the vertex buffer
        VkMemoryRequirements vertexBufferMemoryRequirements;
        VulkanNative.vkGetBufferMemoryRequirements(this.logicalDevice, vertexBuffer, &vertexBufferMemoryRequirements);

        // Set up the vertex buffer allocation info
        VkMemoryAllocateInfo vertexBufferAllocateInfo = new VkMemoryAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
            allocationSize = vertexBufferMemoryRequirements.size,
            memoryTypeIndex = FindMemoryType(vertexBufferMemoryRequirements.memoryTypeBits, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
        };

        // Allocate the vertex buffer memory
        fixed (VkDeviceMemory* vertexBufferMemoryPtr = &vertexBufferMemory)
        {
            if (VulkanNative.vkAllocateMemory(this.logicalDevice, &vertexBufferAllocateInfo, null, vertexBufferMemoryPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to allocate memory for the vertex buffer");
            } 
        }

        // Bind the buffer memory
        VulkanNative.vkBindBufferMemory(this.logicalDevice, vertexBuffer, vertexBufferMemory, 0);

        // Create empty data pointer
        void* data;
        
        // Assign the data to the vertex buffer memory
        VulkanNative.vkMapMemory(this.logicalDevice, vertexBufferMemory, 0, vertexBufferCreateInfo.size, 0, &data);

        // Fill the data pointer with the vertices array's information
        fixed (Vertex* verticesPtr = vertices)
        {
            Buffer.MemoryCopy(verticesPtr, data, vertexBufferCreateInfo.size, vertexBufferCreateInfo.size);
        }
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(this.logicalDevice, vertexBufferMemory);
    }
}