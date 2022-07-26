using System;
using System.Diagnostics;
using System.Numerics;
using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public struct Vertex
{
    public Vector3 position;
    public Vector3 color;
}

public static unsafe class VulkanUtilities
{
    public static byte* ToPointer(this string text)
    {
        return (byte*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(text);
    }

    public static uint Version(uint major, uint minor, uint patch)
    {
        return (major << 22) | (minor << 12) | patch;
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
    
    public static void CreateBuffer(ulong size, VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags propertyFlags, out VkBuffer buffer, out VkDeviceMemory memory)
    {
        VkBufferCreateInfo bufferCreateInfo = new VkBufferCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO,
            size = size,
            usage = usageFlags,
            sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE
        };

        fixed (VkBuffer* bufferPtr = &buffer)
        {
            if (VulkanNative.vkCreateBuffer(EngineCore.logicalDevice, &bufferCreateInfo, null, bufferPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError($"Failed to create buffer");
            }
        }

        VkMemoryRequirements memoryRequirements = new VkMemoryRequirements();
        VulkanNative.vkGetBufferMemoryRequirements(EngineCore.logicalDevice, buffer, &memoryRequirements);

        VkMemoryAllocateInfo memoryAllocationInfo = new VkMemoryAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
            allocationSize = memoryRequirements.size,
            memoryTypeIndex = FindMemoryType(memoryRequirements.memoryTypeBits, propertyFlags)
        };

        fixed (VkDeviceMemory* memoryPtr = &memory)
        {
            if (VulkanNative.vkAllocateMemory(EngineCore.logicalDevice, &memoryAllocationInfo, null, memoryPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to allocate memory");
            }
        }

        VulkanNative.vkBindBufferMemory(EngineCore.logicalDevice, buffer, memory, 0);
    }

    public static void CopyBuffer(in VkQueue transferQueue, in VkBuffer sourceBuffer, in VkBuffer destinationBuffer, ulong size)
    {
        // Set up allocation info
        VkCommandBufferAllocateInfo bufferAllocationInfo = new VkCommandBufferAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
            level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY,
            commandPool = EngineCore.commandPool,
            commandBufferCount = 1
        };

        // Define and allocate a command buffer
        VkCommandBuffer commandBuffer;
        VulkanNative.vkAllocateCommandBuffers(EngineCore.logicalDevice, &bufferAllocationInfo, &commandBuffer);

        // Set up the buffer begin info
        VkCommandBufferBeginInfo bufferBeginInfo = new VkCommandBufferBeginInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
            flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT
        };

        // Set the offsets of the copy (from where to where to copy)
        VkBufferCopy copyRegion = new VkBufferCopy()
        {
            size = size
        };
        
        // Begin the command buffer
        VulkanNative.vkBeginCommandBuffer(commandBuffer, &bufferBeginInfo);
        
        // Copy the source buffer to the destination buffer
        VulkanNative.vkCmdCopyBuffer(commandBuffer, sourceBuffer, destinationBuffer, 1, &copyRegion);

        // End the command buffer
        VulkanNative.vkEndCommandBuffer(commandBuffer);

        // Set up submit info
        VkSubmitInfo submitInfo = new VkSubmitInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
            commandBufferCount = 1,
            pCommandBuffers = &commandBuffer
        };

        // Submit the queue and wait for it to execute
        VulkanNative.vkQueueSubmit(transferQueue, 1, &submitInfo, VkFence.Null);
        VulkanNative.vkQueueWaitIdle(transferQueue);
        
        // Free the command buffer
        VulkanNative.vkFreeCommandBuffers(EngineCore.logicalDevice, EngineCore.commandPool, 1, &commandBuffer);
    }

    private static uint FindMemoryType(uint typeFilter, in VkMemoryPropertyFlags givenMemoryPropertyFlags)
    {
        VkPhysicalDeviceMemoryProperties memoryProperties;
        VulkanNative.vkGetPhysicalDeviceMemoryProperties(EngineCore.physicalDevice, &memoryProperties);
        
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