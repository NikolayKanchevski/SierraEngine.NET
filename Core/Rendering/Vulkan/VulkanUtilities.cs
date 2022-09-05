using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Classes;
using Buffer = SierraEngine.Core.Rendering.Vulkan.Abstractions.Buffer;

namespace SierraEngine.Core.Rendering.Vulkan;

public struct Vertex
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 textureCoordinates;
}

public static unsafe class VulkanUtilities
{
    public static byte* ToPointer(this string text)
    {
        return (byte*) Marshal.StringToHGlobalAnsi(text);
    }
    
    public static uint Version(uint major, uint minor, uint patch)
    {
        return (major << 22) | (minor << 12) | patch;
    }

    public static Vector2 ToVector3(this Assimp.Vector2D givenVector)
    {
        return new Vector2(givenVector.X, givenVector.Y);
    }

    public static Vector2 ToVector2(this Assimp.Vector3D givenVector)
    {
        return new Vector2(givenVector.X, givenVector.Y);
    }

    public static Vector3 ToVector3(this Assimp.Vector3D givenVector)
    {
        return new Vector3(givenVector.X, givenVector.Y, givenVector.Z);
    }
    
    public static Vector3 ToVector3(this Assimp.Color4D givenVector)
    {
        return new Vector3(givenVector.R, givenVector.G, givenVector.B);
    }

    public static string GetString(byte* stringStart)
    {
        int characters = 0;
        while (stringStart[characters] != 0)
        {
            characters++;
        }

        return System.Text.Encoding.UTF8.GetString(stringStart, characters);
    }
    
    public static bool IsNullOrEmpty(this Array array)
    {
        return (array == null || array.Length == 0);
    }
    
    public static VkShaderModule CreateShaderModule(string fileName)
    {
        // Read bytes from the given file
        var shaderByteCode = Files.GetBytes(fileName);

        // Set module creation info
        VkShaderModuleCreateInfo moduleCreateInfo = new VkShaderModuleCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO,
            codeSize = (UIntPtr) shaderByteCode.Length,
        };

        fixed (byte* shaderByteCodePtr = shaderByteCode)
        {
            moduleCreateInfo.pCode = (uint*) shaderByteCodePtr;
        }

        // Create shader module
        VkShaderModule shaderModule;
        if (VulkanNative.vkCreateShaderModule(VulkanCore.logicalDevice, &moduleCreateInfo, null, &shaderModule) != VkResult.VK_SUCCESS)
        {
            VulkanDebugger.ThrowError($"Failed to create shader module for [{ fileName }]");
        }

        return shaderModule;
    }

    public static void CreateVertexBuffer(in Vertex[] vertices, out Buffer vertexBuffer)
    {
        // Calculate the buffer size
        ulong bufferSize = (ulong) (Marshal.SizeOf(vertices[0]) * vertices.Length);

        Buffer stagingBuffer;
        
        new Buffer.Builder()
            .SetMemorySize(bufferSize)
            .SetMemoryFlags(VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags. VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
            .SetUsageFlags(VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT)
        .Build(out stagingBuffer);

        // Fill the data pointer with the vertices array's information
        fixed (Vertex* verticesPtr = vertices)
        {
            stagingBuffer.CopyFromPointer(verticesPtr);
        }

        new Buffer.Builder()
            .SetMemorySize(bufferSize)
            .SetMemoryFlags(VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
            .SetUsageFlags(VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT)
        .Build(out vertexBuffer);
        
        stagingBuffer.CopyToBuffer(vertexBuffer);
     
        stagingBuffer.CleanUp();
    }

    public static void CreateIndexBuffer(in UInt32[] indices, out Buffer indexBuffer)
    {
        // Calculate the buffer size
        ulong bufferSize = (ulong) (Marshal.SizeOf(indices[0]) * indices.Length);
    
        Buffer stagingBuffer;
        
        new Buffer.Builder()
            .SetMemorySize(bufferSize)
            .SetMemoryFlags(VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags. VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
            .SetUsageFlags(VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT)
        .Build(out stagingBuffer);

        // Fill the data pointer with the vertices array's information
        fixed (UInt32* verticesPtr = indices)
        {
            stagingBuffer.CopyFromPointer(verticesPtr);
        }

        new Buffer.Builder()
            .SetMemorySize(bufferSize)
            .SetMemoryFlags(VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
            .SetUsageFlags(VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT)
        .Build(out indexBuffer);
        
        stagingBuffer.CopyToBuffer(indexBuffer);
     
        stagingBuffer.CleanUp();
    }
    
    public static VkCommandBuffer BeginSingleTimeCommands()
    {
        VkCommandBufferAllocateInfo commandBufferAllocateInfo = new VkCommandBufferAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
            level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY,
            commandPool = VulkanCore.commandPool,
            commandBufferCount = 1
        };

        VkCommandBuffer commandBuffer;
        if (VulkanNative.vkAllocateCommandBuffers(VulkanCore.logicalDevice, &commandBufferAllocateInfo, &commandBuffer) != VkResult.VK_SUCCESS)
        {
            VulkanDebugger.ThrowError("Failed to allocate command buffer");
        }

        VkCommandBufferBeginInfo commandBufferBeginInfo = new VkCommandBufferBeginInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
            flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT
        };

        VulkanNative.vkBeginCommandBuffer(commandBuffer, &commandBufferBeginInfo);

        return commandBuffer;
    }

    public static void EndSingleTimeCommands(in VkCommandBuffer commandBuffer)
    {
        VulkanNative.vkEndCommandBuffer(commandBuffer);

        VkCommandBuffer* commandBuffersPtr = stackalloc VkCommandBuffer[] { commandBuffer };
        
        VkSubmitInfo submitInfo = new VkSubmitInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
            commandBufferCount = 1,
            pCommandBuffers = commandBuffersPtr
        };

        VulkanNative.vkQueueSubmit(VulkanCore.graphicsQueue, 1, &submitInfo, VkFence.Null);
        VulkanNative.vkQueueWaitIdle(VulkanCore.graphicsQueue);
        
        VulkanNative.vkFreeCommandBuffers(VulkanCore.logicalDevice, VulkanCore.commandPool, 1, commandBuffersPtr);
    }

    public static uint FindMemoryTypeIndex(uint typeFilter, in VkMemoryPropertyFlags givenMemoryPropertyFlags)
    {
        VkPhysicalDeviceMemoryProperties memoryProperties;
        VulkanNative.vkGetPhysicalDeviceMemoryProperties(VulkanCore.physicalDevice, &memoryProperties);
        
        for (uint i = 0; i < memoryProperties.memoryTypeCount; i++) {
            if ((typeFilter & (1 << (int) i)) != 0 &&
                (memoryProperties.GetMemoryType(i).propertyFlags & givenMemoryPropertyFlags) == givenMemoryPropertyFlags) 
            {
                return i;
            }
        }

        return 0;
    }

    private static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index)
    {
        return (&memoryProperties.memoryTypes_0)[index];
    }
}