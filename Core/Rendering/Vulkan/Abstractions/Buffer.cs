using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan.Abstractions;

public unsafe class Buffer
{
    public class Builder
    {
        private ulong memorySize;
        private VkMemoryPropertyFlags memoryFlags;
        private VkBufferUsageFlags usageFlags;

        public Builder SetMemorySize(in ulong givenMemorySize)
        {
            // Save the given memory size
            this.memorySize = givenMemorySize;
            return this;
        }

        public Builder SetMemorySize(in object givenStruct)
        {
            // Save calculate and save the memory size of the given struct
            this.memorySize = (ulong) Marshal.SizeOf(givenStruct);
            return this;
        }
        
        public Builder SetMemorySize<T>() where T : struct
        {
            // Save calculate and save the memory size of the given struct 
            this.memorySize = (ulong) Marshal.SizeOf(typeof(T));
            return this;
        }

        public Builder SetMemoryFlags(in VkMemoryPropertyFlags givenMemoryFlags)
        {
            // Save the given memory flags
            this.memoryFlags = givenMemoryFlags;
            return this;
        }

        public Builder SetUsageFlags(in VkBufferUsageFlags givenUsageFlags)
        {
            // Save the given usage flags
            this.usageFlags = givenUsageFlags;
            return this;
        }

        public void Build(out Buffer buffer)
        {
            // Create and return a new buffer
            buffer = new Buffer(memorySize, memoryFlags, usageFlags);
        }
    }

    public ulong handle => vkBuffer.Handle;

    private readonly VkBuffer vkBuffer;
    private readonly VkDeviceMemory vkBufferMemory;
    
    private readonly ulong memorySize;
    private readonly VkMemoryPropertyFlags memoryFlags;
    private readonly VkBufferUsageFlags usageFlags;
    
    private Buffer(ulong givenMemorySize, VkMemoryPropertyFlags givenMemoryFlags, VkBufferUsageFlags givenUsageFlags)
    {
        // Save the given data
        this.memorySize = givenMemorySize;
        this.memoryFlags = givenMemoryFlags;
        this.usageFlags = givenUsageFlags;
        
        // Set up buffer creation info
        VkBufferCreateInfo bufferCreateInfo = new VkBufferCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO,
            size = givenMemorySize,
            usage = givenUsageFlags,
            sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE
        };

        // Create the Vulkan buffer
        fixed (VkBuffer* bufferPtr = &vkBuffer)
        {
            VulkanDebugger.CheckResults(
                VulkanNative.vkCreateBuffer(VulkanCore.logicalDevice, &bufferCreateInfo, null, bufferPtr),
                $"Failed to create buffer with size of [{givenMemorySize}] for [{givenUsageFlags.ToString()}] usage"
            );
        }

        // Get the Vulkan buffer's memory requirements
        VkMemoryRequirements memoryRequirements = new VkMemoryRequirements();
        VulkanNative.vkGetBufferMemoryRequirements(VulkanCore.logicalDevice, vkBuffer, &memoryRequirements);

        // Set up the buffer's memory allocation info
        VkMemoryAllocateInfo memoryAllocationInfo = new VkMemoryAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
            allocationSize = memoryRequirements.size,
            memoryTypeIndex = VulkanUtilities.FindMemoryTypeIndex(memoryRequirements.memoryTypeBits, memoryFlags)
        };

        // Allocate buffer's memory
        fixed (VkDeviceMemory* memoryPtr = &vkBufferMemory)
        {
            if (VulkanNative.vkAllocateMemory(VulkanCore.logicalDevice, &memoryAllocationInfo, null, memoryPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to allocate memory");
            }
        }

        // Bind the allocated memory to the buffer
        VulkanNative.vkBindBufferMemory(VulkanCore.logicalDevice, vkBuffer, vkBufferMemory, 0);
    }

    public void CopyFromPointer(in void* pointer, in ulong offset = 0)
    {
        // Create an empty pointer
        void *data;
        
        // Map memory
        VulkanNative.vkMapMemory(VulkanCore.logicalDevice, vkBufferMemory, 0, memorySize, 0, &data);
        
        // Copy memory data to Vulkan buffer
        System.Buffer.MemoryCopy(pointer, data, memorySize, memorySize);
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, vkBufferMemory);
    }
    
    public void CopyBytes(in byte[] givenData, in ulong offset = 0)
    {
        // Create an empty pointer
        void *data;
        
        // Map memory
        VulkanNative.vkMapMemory(VulkanCore.logicalDevice, vkBufferMemory, 0, memorySize, 0, &data);
        
        // Copy memory data to Vulkan buffer
        fixed (byte* imageDataPtr = givenData)
        {
            System.Buffer.MemoryCopy(imageDataPtr, data, memorySize, memorySize);
        }
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, vkBufferMemory);
    }

    public void CopyStruct<T>(in T givenData, in ulong offset = 0)
    {
        // Create an empty pointer
        void *data;
        
        // Map memory
        VulkanNative.vkMapMemory(VulkanCore.logicalDevice, vkBufferMemory, offset, memorySize, 0, &data);

        // Copy memory data to Vulkan buffer
        IntPtr uniformDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(givenData));
        Marshal.StructureToPtr(givenData!, uniformDataPtr, true);
        
        System.Buffer.MemoryCopy(uniformDataPtr.ToPointer(), data, memorySize, memorySize);
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, vkBufferMemory);
        Marshal.FreeHGlobal(uniformDataPtr);
    }

    public void CopyImage(in Image givenImage, in Vector3 imageOffset = default, in ulong offset = 0)
    {
        // Create a temporary command buffer
        VkCommandBuffer commandBuffer = VulkanUtilities.BeginSingleTimeCommands();

        // Set up image copy region
        VkBufferImageCopy copyRegion = new VkBufferImageCopy()
        {
            bufferOffset = offset,
            bufferRowLength = 0,
            bufferImageHeight = 0,
            imageSubresource = new VkImageSubresourceLayers()
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1
            },
            imageOffset = new VkOffset3D()
            {
                x = (int) imageOffset.X,
                y = (int) imageOffset.Y,
                z = (int) imageOffset.Z
            },
            imageExtent = new VkExtent3D()
            {
                width = givenImage.width,
                height = givenImage.height,
                depth = givenImage.depth
            }
        };
        
        // Copy the image to the buffer
        VulkanNative.vkCmdCopyBufferToImage(commandBuffer, this, givenImage, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, 1, &copyRegion);

        // Destroy the temporary command buffer
        VulkanUtilities.EndSingleTimeCommands(commandBuffer);
    }

    public void CopyToBuffer(in Buffer anotherBuffer)
    {
        // Check if the two buffers are compatible
        if (this.memorySize != anotherBuffer.memorySize)
        {
            VulkanDebugger.ThrowError("Cannot copy data from one buffer to another with a different memory size!");
        }
        
        // Create a temporary command buffer
        VkCommandBuffer commandBuffer = VulkanUtilities.BeginSingleTimeCommands();

        // Set up the buffer's copy region
        VkBufferCopy copyRegion = new VkBufferCopy()
        {
            size = this.memorySize
        };
        
        // Copy the buffer
        VulkanNative.vkCmdCopyBuffer(commandBuffer, this, anotherBuffer, 1, &copyRegion);
        
        // Destroy the temporary command buffer
        VulkanUtilities.EndSingleTimeCommands(commandBuffer);
    }

    public void DestroyBuffer()
    {
        VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, vkBuffer, null);
    }

    public void FreeMemory()
    {
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, vkBufferMemory, null);
    }
    
    public void CleanUp()
    {
        DestroyBuffer();
        FreeMemory();
    }

    public VkBuffer GetVkBuffer()
    {
        return this.vkBuffer;
    }

    public VkDeviceMemory GetVkMemory()
    {
        return this.vkBufferMemory;
    }

    public static implicit operator VkBuffer(in Buffer givenBuffer)
    {
        return givenBuffer.vkBuffer;
    }

    public static implicit operator VkDeviceMemory(in Buffer givenBuffer)
    {
        return givenBuffer.vkBufferMemory;
    }
}