using System.Numerics;
using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public struct Vertex
{
    public Vector3 position;
    public Vector3 color;
    public Vector2 textureCoordinates;

    public static bool operator==(Vertex left, Vertex right)
    {
        return left.position == right.position;
    }
    
    public static bool operator!=(Vertex left, Vertex right)
    {
        return !(left == right);
    }
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
            if (VulkanNative.vkCreateBuffer(VulkanCore.logicalDevice, &bufferCreateInfo, null, bufferPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError($"Failed to create buffer");
            }
        }

        VkMemoryRequirements memoryRequirements = new VkMemoryRequirements();
        VulkanNative.vkGetBufferMemoryRequirements(VulkanCore.logicalDevice, buffer, &memoryRequirements);

        VkMemoryAllocateInfo memoryAllocationInfo = new VkMemoryAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
            allocationSize = memoryRequirements.size,
            memoryTypeIndex = FindMemoryTypeIndex(memoryRequirements.memoryTypeBits, propertyFlags)
        };

        fixed (VkDeviceMemory* memoryPtr = &memory)
        {
            if (VulkanNative.vkAllocateMemory(VulkanCore.logicalDevice, &memoryAllocationInfo, null, memoryPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to allocate memory");
            }
        }

        VulkanNative.vkBindBufferMemory(VulkanCore.logicalDevice, buffer, memory, 0);
    }

    public static void CopyBuffer(in VkBuffer sourceBuffer, in VkBuffer destinationBuffer, ulong size)
    {
        VkCommandBuffer commandBuffer = BeginSingleTimeCommands();

        VkBufferCopy copyRegion = new VkBufferCopy()
        {
            size = size
        };
        
        VulkanNative.vkCmdCopyBuffer(commandBuffer, sourceBuffer, destinationBuffer, 1, &copyRegion);
        
        EndSingleTimeCommands(commandBuffer);
    }

    public static void CreateImage(in uint width, in uint height, in VkFormat format, in VkImageTiling imageTiling, in VkImageUsageFlags usageFlags, in VkMemoryPropertyFlags propertyFlags, out VkImage image, out VkDeviceMemory imageMemory)
    {
        VkImageCreateInfo imageCreateInfo = new VkImageCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            imageType = VkImageType.VK_IMAGE_TYPE_2D,
            extent = new VkExtent3D()
            {
                width = width,
                height = height,
                depth = 1
            },
            mipLevels = 1,
            arrayLayers = 1,
            format = format,
            tiling = imageTiling,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            usage = usageFlags,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE
        };
        
        fixed (VkImage* imagePtr = &image)
        {
            if (VulkanNative.vkCreateImage(VulkanCore.logicalDevice, &imageCreateInfo, null, imagePtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError($"Failed to create VkImage");
            }    
        }

        VkMemoryRequirements imageMemoryRequirements;
        VulkanNative.vkGetImageMemoryRequirements(VulkanCore.logicalDevice, image, &imageMemoryRequirements);

        VkMemoryAllocateInfo imageMemoryAllocateInfo = new VkMemoryAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
            allocationSize = imageMemoryRequirements.size,
            memoryTypeIndex = VulkanUtilities.FindMemoryTypeIndex(imageMemoryRequirements.memoryTypeBits, propertyFlags)
        };

        fixed (VkDeviceMemory* imageMemoryPtr = &imageMemory)
        {
            if (VulkanNative.vkAllocateMemory(VulkanCore.logicalDevice, &imageMemoryAllocateInfo, null, imageMemoryPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError($"Failed to allocate memory for image" );
            }
        }

        VulkanNative.vkBindImageMemory(VulkanCore.logicalDevice, image, imageMemory, 0);
    }

    public static void CreateImageView(in VkImage image, VkFormat imageFormat, VkImageAspectFlags aspectFlags, out VkImageView imageView)
    {
        VkImageViewCreateInfo imageViewCreateInfo = new VkImageViewCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
            image = image,
            viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
            format = imageFormat,
            subresourceRange = new VkImageSubresourceRange()
            {
                aspectMask = aspectFlags,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1
            }
        };
        
        fixed (VkImageView* textureImageViewPtr = &imageView)
        {
            if (VulkanNative.vkCreateImageView(VulkanCore.logicalDevice, &imageViewCreateInfo, null, textureImageViewPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create image view for texture");
            }
        }
    }

    public static void CopyImageToBuffer(in VkBuffer sourceBuffer, in VkImage image, in uint width, in uint height)
    {
        VkCommandBuffer commandBuffer = BeginSingleTimeCommands();

        VkBufferImageCopy copyRegion = new VkBufferImageCopy()
        {
            bufferOffset = 0,
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
                x = 0,
                y = 0,
                z = 0
            },
            imageExtent = new VkExtent3D()
            {
                width = width,
                height = height,
                depth = 1
            }
        };
        
        VulkanNative.vkCmdCopyBufferToImage(commandBuffer, sourceBuffer, image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, 1, &copyRegion);

        EndSingleTimeCommands(commandBuffer);
    }

    public static void TransitionImageLayout(in VkImage image, in VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout)
    {
        VkCommandBuffer commandBuffer = BeginSingleTimeCommands();
        
        VkImageMemoryBarrier imageMemoryBarrier = new VkImageMemoryBarrier()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
            oldLayout = oldLayout,									// Layout to transition from
            newLayout = newLayout,									// Layout to transition to
            srcQueueFamilyIndex = ~0U,			                    // Queue family to transition from
            dstQueueFamilyIndex = ~0U,			                    // Queue family to transition to
            image = image,											// Image being accessed and modified as part of barrier
            subresourceRange = new VkImageSubresourceRange()
            {
                baseMipLevel = 0,						                    // First mip level to start alterations on
                levelCount = 1,							                    // Number of mip levels to alter starting from baseMipLevel
                baseArrayLayer = 0,						                    // First layer to start alterations on
                layerCount = 1,							                    // Number of layers to alter starting from baseArrayLayer
            }
        };
        
        if (newLayout == VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL) 
        {
            imageMemoryBarrier.subresourceRange.aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT;

            if (format == VkFormat.VK_FORMAT_D32_SFLOAT_S8_UINT || format == VkFormat.VK_FORMAT_D24_UNORM_S8_UINT) 
            {
                imageMemoryBarrier.subresourceRange.aspectMask |= VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT;
            }
        } 
        else 
        {
            imageMemoryBarrier.subresourceRange.aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT;
        }
        
        VkPipelineStageFlags srcStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_NONE;
        VkPipelineStageFlags dstStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_NONE;

        // If transitioning from new image to image ready to receive data...
        if (oldLayout == VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED && newLayout == VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL)
        {
            imageMemoryBarrier.srcAccessMask = 0;
            imageMemoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT;

            srcStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;  // The stage the transition must occur after
            dstStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT;     // The stage the transition must occur before
        }
        
        // If transitioning from transfer destination to shader readable...
        else if (oldLayout == VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL && newLayout == VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL)
        {
            imageMemoryBarrier.srcAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT;
            imageMemoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_SHADER_READ_BIT;

            srcStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT;
            dstStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
        }
        
        // If transitioning from an undefined layout to one optimal for depth stencil...
        else if (oldLayout == VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED && newLayout == VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL) 
        {
            imageMemoryBarrier.srcAccessMask = 0;
            imageMemoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT | VkAccessFlags.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;

            srcStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
            dstStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT;
        }
        else
        {
            VulkanDebugger.ThrowError($"Transitioning images from [{ oldLayout.ToString() }] to [{ newLayout.ToString() }] is not supported");
        }

        VulkanNative.vkCmdPipelineBarrier(
                commandBuffer,
                srcStage, dstStage,		                                // Pipeline stages (match to src and dst AccessMasks)
                0,						                // Dependency flags
                0, null,				    // Memory Barrier count + data
                0, null,			// Buffer Memory Barrier count + data
                1, &imageMemoryBarrier	            // Image Memory Barrier count + data
        );
        
        EndSingleTimeCommands(commandBuffer);
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

    private static uint FindMemoryTypeIndex(uint typeFilter, in VkMemoryPropertyFlags givenMemoryPropertyFlags)
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