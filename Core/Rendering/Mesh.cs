using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Core.Rendering;

public unsafe class Mesh
{
    public int verticesCount;
    public int indexCount;

    private VkPhysicalDevice physicalDevice;
    private VkDevice logicalDevice;

    private VkBuffer vertexBuffer;
    private VkDeviceMemory vertexBufferMemory;

    private VkBuffer indexBuffer;
    private VkDeviceMemory indexBufferMemory;

    public Mesh(VkPhysicalDevice newPhysicalDevice, VkDevice newLogicalDevice, VkQueue transferQueue, VkCommandPool transferCommandPool, 
                in Vertex[] vertices, in UInt16[] indices)
    {
        this.verticesCount = vertices.Length;
        this.indexCount = indices.Length;
        this.physicalDevice = newPhysicalDevice;
        this.logicalDevice = newLogicalDevice;
        
        CreateVertexBuffer(transferQueue, transferCommandPool, in vertices);
        CreateIndexBuffer(transferQueue, transferCommandPool, in indices);
    }

    public VkBuffer GetVertexBuffer()
    {
        return this.vertexBuffer;
    }

    public VkBuffer GetIndexBuffer()
    {
        return this.indexBuffer;
    }

    public void DestroyBuffers()
    {
        VulkanNative.vkDestroyBuffer(logicalDevice, vertexBuffer, null);
        VulkanNative.vkFreeMemory(logicalDevice, vertexBufferMemory, null);
        VulkanNative.vkDestroyBuffer(logicalDevice, indexBuffer, null);
        VulkanNative.vkFreeMemory(logicalDevice, indexBufferMemory, null);
    }

    private void CreateVertexBuffer(VkQueue transferQueue, VkCommandPool transferCommandPool, in Vertex[] vertices)
    {
        // Calculate the buffer size
        ulong bufferSize = (ulong) (Marshal.SizeOf(vertices[0]) * vertices.Length);
    
        // Define the staging buffer and its memory
        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        
        // Create the staging buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags. VK_MEMORY_PROPERTY_HOST_COHERENT_BIT, 
            out stagingBuffer, out stagingBufferMemory);

        // Create empty data pointer
        void* data;
        
        // Assign the data to the vertex buffer memory
        VulkanNative.vkMapMemory(this.logicalDevice, stagingBufferMemory, 0, bufferSize, 0, &data);

        // Fill the data pointer with the vertices array's information
        fixed (Vertex* verticesPtr = vertices)
        {
            Buffer.MemoryCopy(verticesPtr, data, bufferSize, bufferSize);
        }
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(this.logicalDevice, stagingBufferMemory);
        
        // Create the vertex buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
            out vertexBuffer, out vertexBufferMemory);
        
        // Copy the staging buffer to the vertex buffer
        VulkanUtilities.CopyBuffer(transferQueue, stagingBuffer, vertexBuffer, bufferSize);
        
        // Destroy the staging buffer and free its memory
        VulkanNative.vkDestroyBuffer(this.logicalDevice, stagingBuffer, null);
        VulkanNative.vkFreeMemory(this.logicalDevice, stagingBufferMemory, null);
    }

    private void CreateIndexBuffer(VkQueue transferQueue, VkCommandPool transferCommandPool, in UInt16[] indices)
    {
        // Calculate the buffer size
        ulong bufferSize = (ulong) (Marshal.SizeOf(indices[0]) * indices.Length);
    
        // Define the staging buffer and its memory
        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        
        // Create the staging buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags. VK_MEMORY_PROPERTY_HOST_COHERENT_BIT, 
            out stagingBuffer, out stagingBufferMemory);

        // Create empty data pointer
        void* data;
        
        // Assign the data to the index buffer memory
        VulkanNative.vkMapMemory(this.logicalDevice, stagingBufferMemory, 0, bufferSize, 0, &data);

        // Fill the data pointer with the indices array's information
        fixed (UInt16* indicesPtr = indices)
        {
            Buffer.MemoryCopy(indicesPtr, data, bufferSize, bufferSize);
        }
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(this.logicalDevice, stagingBufferMemory);
        
        // Create the index buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
            out indexBuffer, out indexBufferMemory);
        
        // Copy the staging buffer to the index buffer
        VulkanUtilities.CopyBuffer(transferQueue, stagingBuffer, indexBuffer, bufferSize);
        
        // Destroy the staging buffer and free its memory
        VulkanNative.vkDestroyBuffer(this.logicalDevice, stagingBuffer, null);
        VulkanNative.vkFreeMemory(this.logicalDevice, stagingBufferMemory, null);
    }
}