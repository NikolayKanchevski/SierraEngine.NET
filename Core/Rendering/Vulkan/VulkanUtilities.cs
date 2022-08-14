using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;

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
        return (byte*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(text);
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

    public static void CreateVertexBuffer(in Vertex[] vertices, out VkBuffer vertexBuffer, out VkDeviceMemory vertexBufferMemory)
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
        VulkanNative.vkMapMemory(VulkanCore.logicalDevice, stagingBufferMemory, 0, bufferSize, 0, &data);

        // Fill the data pointer with the vertices array's information
        fixed (Vertex* verticesPtr = vertices)
        {
            Buffer.MemoryCopy(verticesPtr, data, bufferSize, bufferSize);
        }
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, stagingBufferMemory);
        
        // Create the vertex buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
            out vertexBuffer, out vertexBufferMemory);
        
        // Copy the staging buffer to the vertex buffer
        VulkanUtilities.CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);
        
        // Destroy the staging buffer and free its memory
        VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, stagingBuffer, null);
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, stagingBufferMemory, null);
    }

    public static void CreateIndexBuffer(in UInt32[] indices, out VkBuffer indexBuffer, out VkDeviceMemory indexBufferMemory)
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
        VulkanNative.vkMapMemory(VulkanCore.logicalDevice, stagingBufferMemory, 0, bufferSize, 0, &data);

        // Fill the data pointer with the indices array's information
        fixed (UInt32* indicesPtr = indices)
        {
            Buffer.MemoryCopy(indicesPtr, data, bufferSize, bufferSize);
        }
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, stagingBufferMemory);
        
        // Create the index buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
            out indexBuffer, out indexBufferMemory);
        
        // Copy the staging buffer to the index buffer
        VulkanUtilities.CopyBuffer(stagingBuffer, indexBuffer, bufferSize);
        
        // Destroy the staging buffer and free its memory
        VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, stagingBuffer, null);
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, stagingBufferMemory, null);
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

    public static void CreateImage(in uint width, in uint height, in uint mipLevels, VkSampleCountFlags sampleCountFlags, in VkFormat format, in VkImageTiling imageTiling, in VkImageUsageFlags usageFlags, in VkMemoryPropertyFlags propertyFlags, out VkImage image, out VkDeviceMemory imageMemory)
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
            mipLevels = mipLevels,
            arrayLayers = 1,
            format = format,
            tiling = imageTiling,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            usage = usageFlags,
            samples = sampleCountFlags,
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

    public static void CreateImageView(in VkImage image, VkFormat imageFormat, VkImageAspectFlags aspectFlags, in uint mipLevels, out VkImageView imageView)
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
                levelCount = mipLevels,
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

    public static void TransitionImageLayout(in VkImage image, in VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout, in uint mipLevels)
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
                levelCount = mipLevels,							                    // Number of mip levels to alter starting from baseMipLevel
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

    public static void GenerateMipMaps(in VkImage image, VkFormat imageFormat, uint textureWidth, uint textureHeight, uint mipLevels)
    {
        VkFormatProperties formatProperties;
        VulkanNative.vkGetPhysicalDeviceFormatProperties(VulkanCore.physicalDevice, imageFormat, &formatProperties);

        // Check if optimal tiling is supported by the GPU
        if ((formatProperties.optimalTilingFeatures & VkFormatFeatureFlags.VK_FORMAT_FEATURE_SAMPLED_IMAGE_FILTER_LINEAR_BIT) == 0)
        {
            VulkanDebugger.ThrowError($"Texture image format [{ imageFormat.ToString() }] does not support linear blitting");
        }
        
        // Begin a command buffer
        VkCommandBuffer commandBuffer = BeginSingleTimeCommands();

        // Create an image memory barrier
        VkImageMemoryBarrier memoryBarrier = new VkImageMemoryBarrier()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
            image = image,
            srcQueueFamilyIndex = ~0U,
            dstQueueFamilyIndex = ~0U,
            subresourceRange = new VkImageSubresourceRange()
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                baseArrayLayer = 0,
                layerCount = 1,
                levelCount = 1
            }
        };
        
        uint mipWidth = textureWidth;
        uint mipHeight = textureHeight;

        // For each mip level resize the image
        for (uint i = 1; i < mipLevels; i++) {
            memoryBarrier.subresourceRange.baseMipLevel = i - 1;
            memoryBarrier.oldLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
            memoryBarrier.newLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
            memoryBarrier.srcAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT;
            memoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT;

            VulkanNative.vkCmdPipelineBarrier(commandBuffer,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT, VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT, 0,
                0, null,
                0, null,
                1, &memoryBarrier);

            VkImageBlit blit = new VkImageBlit()
            {
                srcOffsets_0 = new VkOffset3D()
                {
                    x = 0,
                    y = 0,
                    z = 0
                },
                srcOffsets_1 = new VkOffset3D()
                {
                    x = (int)mipWidth,
                    y = (int)mipHeight,
                    z = 1
                },
                srcSubresource = new VkImageSubresourceLayers()
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    mipLevel = i - 1,
                    baseArrayLayer = 0,
                    layerCount = 1,
                },
                dstOffsets_0 = new VkOffset3D()
                {
                    x = 0,
                    y = 0,
                    z = 0
                },
                dstOffsets_1 = new VkOffset3D()
                {
                    x = (int)(mipWidth > 1 ? mipWidth / 2 : 1),
                    y = (int)(mipHeight > 1 ? mipHeight / 2 : 1),
                    z = 1
                },
                dstSubresource = new VkImageSubresourceLayers()
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    mipLevel = i,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };

            VulkanNative.vkCmdBlitImage(commandBuffer,
                image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                1, &blit,
                VkFilter.VK_FILTER_LINEAR);

            memoryBarrier.oldLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
            memoryBarrier.newLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
            memoryBarrier.srcAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT;
            memoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_SHADER_READ_BIT;
            
            VulkanNative.vkCmdPipelineBarrier(commandBuffer,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT, VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT, 0,
                0, null,
                0, null,
                1, &memoryBarrier);

            if (mipWidth > 1) mipWidth /= 2;
            if (mipHeight > 1) mipHeight /= 2;
        }

        // Set base mip level and transition the layout of the texture
        memoryBarrier.subresourceRange.baseMipLevel = mipLevels - 1;
        memoryBarrier.oldLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
        memoryBarrier.newLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
        memoryBarrier.srcAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT;
        memoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_SHADER_READ_BIT;

        // Bind the image barrier and apply the changes
        VulkanNative.vkCmdPipelineBarrier(commandBuffer,
            VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT, VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT, 0,
            0, null,
            0, null,
            1, &memoryBarrier);
        
        // End the current command buffer
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