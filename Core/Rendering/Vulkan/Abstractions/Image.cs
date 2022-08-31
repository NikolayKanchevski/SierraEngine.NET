using System.Numerics;
using Evergine.Bindings.Vulkan;
using StbImageSharp;

namespace SierraEngine.Core.Rendering.Vulkan.Abstractions;

public unsafe class Image
{
    public class Builder
    {
        private Vector3 dimensions;
        private uint mipLevels = 1;

        private VkFormat format;
        private VkImageUsageFlags usageFlags;
        private VkMemoryPropertyFlags propertyFlags;
        private VkImageTiling imageTiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL;
        private VkSampleCountFlags samplingFlags = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
        
        public Builder SetSize(in uint givenWidth, in uint givenHeight, in uint givenDepth = 1)
        {
            // Save the given size locally
            this.dimensions = new Vector3(givenWidth, givenHeight, givenDepth);
            return this;
        }

        public Builder SetMipLevels(in uint givenMipLevels)
        {
            // Save the given mip levels locally
            this.mipLevels = givenMipLevels;
            return this;
        }

        public Builder SetFormat(in VkFormat givenFormat)
        {
            // Save the given format locally
            this.format = givenFormat;
            return this;
        }

        public Builder SetUsage(in VkImageUsageFlags givenUsageFlags)
        {
            // Save the given usage flags locally
            this.usageFlags = givenUsageFlags;
            return this;
        }

        public Builder SetMemoryFlags(in VkMemoryPropertyFlags givenMemoryFlags)
        {
            // Save the given memory flags locally
            this.propertyFlags = givenMemoryFlags;
            return this;
        }

        public Builder SetSampling(in VkSampleCountFlags givenSamplingFlags)
        {
            // Save the given sampling locally
            this.samplingFlags = givenSamplingFlags;
            return this;
        }

        public Builder SetImageTiling(in VkImageTiling givenTiling)
        {
            // Save the given image tiling locally
            this.imageTiling = givenTiling;
            return this;
        }

        public void Build(out Image image)
        {
            // Build and return the image
            image = new Image(dimensions, mipLevels, samplingFlags, format, imageTiling, usageFlags, propertyFlags);
        }
    }

    public readonly Vector3 dimensions;
    public uint width => (uint) dimensions.X;
    public uint height => (uint) dimensions.Y;
    public uint depth => (uint) dimensions.Z;
    public ulong handle => vkImage.Handle;
    
    public readonly uint mipLevels;
    public readonly VkFormat format;
    public readonly VkSampleCountFlags sampling;
    public VkImageLayout layout { get; private set; } = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED; 

    private VkImage vkImage;
    private VkImageView vkImageView;
    private VkDeviceMemory vkImageMemory;
    private bool imageViewGenerated;
    

    public Image(
        in VkImage givenVkImage, in VkFormat givenFormat, in VkSampleCountFlags givenSampling, in Vector3 givenDimensions,
        in uint givenMipLevels = 1, in VkImageLayout givenLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED)
    {
        // Save the provided values locally
        this.format = givenFormat;
        this.sampling = givenSampling;
        this.mipLevels = givenMipLevels;
        this.layout = givenLayout;
        this.dimensions = givenDimensions;
        this.vkImage = givenVkImage;
    }
    
    private Image(in Vector3 givenDimensions, in uint givenMipLevels, VkSampleCountFlags givenSampling, in VkFormat givenFormat, 
                  in VkImageTiling imageTiling, in VkImageUsageFlags usageFlags, in VkMemoryPropertyFlags propertyFlags)
    {
        // Save the provided values locally
        this.dimensions = givenDimensions;
        this.mipLevels = givenMipLevels;
        this.sampling = givenSampling;
        this.format = givenFormat;

        // Set up image creation info
        VkImageCreateInfo imageCreateInfo = new VkImageCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            imageType = VkImageType.VK_IMAGE_TYPE_2D,
            extent = new VkExtent3D()
            {
                width = (uint) givenDimensions.X,
                height = (uint) givenDimensions.Y,
                depth = (uint) givenDimensions.Z
            },
            mipLevels = givenMipLevels,
            arrayLayers = 1,
            format = givenFormat,
            tiling = imageTiling,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            usage = usageFlags,
            samples = givenSampling,
            sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE
        };
        
        // Create the Vulkan image
        fixed (VkImage* imagePtr = &vkImage)
        {
            VulkanDebugger.CheckResults(
                VulkanNative.vkCreateImage(VulkanCore.logicalDevice, &imageCreateInfo, null, imagePtr),
                $"Failed to create image with dimensions of [{ givenDimensions.ToString() }], format [{ givenFormat }], " +
                $"{ givenMipLevels } mip levels, and sampling of [{ givenSampling.ToString() }]"
            );
        }

        // Retrieve its memory requirements
        VkMemoryRequirements imageMemoryRequirements;
        VulkanNative.vkGetImageMemoryRequirements(VulkanCore.logicalDevice, vkImage, &imageMemoryRequirements);

        // Set up image memory allocation info
        VkMemoryAllocateInfo imageMemoryAllocateInfo = new VkMemoryAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
            allocationSize = imageMemoryRequirements.size,
            memoryTypeIndex = VulkanUtilities.FindMemoryTypeIndex(imageMemoryRequirements.memoryTypeBits, propertyFlags)
        };

        // Allocate the image to memory
        fixed (VkDeviceMemory* imageMemoryPtr = &vkImageMemory)
        {
            VulkanDebugger.CheckResults(
                VulkanNative.vkAllocateMemory(VulkanCore.logicalDevice, &imageMemoryAllocateInfo, null, imageMemoryPtr),
                $"Could not allocate memory for image with dimensions of [{ givenDimensions.ToString() }], format [{ givenFormat }], " +
                $"{ givenMipLevels } mip levels, and sampling of [{ givenSampling.ToString() }]"
            );
        }

        // Bind the image to its corresponding memory
        VulkanNative.vkBindImageMemory(VulkanCore.logicalDevice, vkImage, vkImageMemory, 0);
    }

    public void GenerateImageView(in VkImageAspectFlags givenAspectFlags)
    {
        // Check if an image view has already been generated
        if (imageViewGenerated)
        {
            VulkanDebugger.ThrowWarning("Trying to create an image view for an image with an already existing view. " +
                                        "The process is automatically suspended");
            return;
        }
        
        imageViewGenerated = true;

        // Set up image view creation info
        VkImageViewCreateInfo imageViewCreateInfo = new VkImageViewCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
            image = vkImage,
            viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
            format = format,
            subresourceRange = new VkImageSubresourceRange()
            {
                aspectMask = givenAspectFlags,
                baseMipLevel = 0,
                levelCount = mipLevels,
                baseArrayLayer = 0,
                layerCount = 1
            }
        };
        
        // Create the image view
        fixed (VkImageView* textureImageViewPtr = &vkImageView)
        {
            VulkanDebugger.CheckResults(
                VulkanNative.vkCreateImageView(VulkanCore.logicalDevice, &imageViewCreateInfo, null, textureImageViewPtr),
                $"Could not create image view for an image with dimensions of [{ dimensions.ToString() }], format [{ format }], " +
                $"and { mipLevels } mip levels"
            );
        }
    }

    public void TransitionLayout(in VkImageLayout newLayout)
    {
        // Create a temporary command buffer
        VkCommandBuffer commandBuffer = VulkanUtilities.BeginSingleTimeCommands();
        
        // Create image memory barrier
        VkImageMemoryBarrier imageMemoryBarrier = new VkImageMemoryBarrier()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
            oldLayout = layout,									// Layout to transition from
            newLayout = newLayout,								// Layout to transition to
            srcQueueFamilyIndex = ~0U,			                // Queue family to transition from
            dstQueueFamilyIndex = ~0U,			                // Queue family to transition to
            image = vkImage,									// Image being accessed and modified as part of barrier
            subresourceRange = new VkImageSubresourceRange()
            {
                baseMipLevel = 0,						                    // First mip level to start alterations on
                levelCount = mipLevels,							            // Number of mip levels to alter starting from baseMipLevel
                baseArrayLayer = 0,						                    // First layer to start alterations on
                layerCount = 1,							                    // Number of layers to alter starting from baseArrayLayer
            }
        };
        
        // If transitioning from a depth image...
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

        // If transitioning from a new or undefined image to an image that is ready to receive data...
        if (layout == VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED && newLayout == VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL)
        {
            imageMemoryBarrier.srcAccessMask = 0;
            imageMemoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT;

            srcStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;  // The stage the transition must occur after
            dstStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT;     // The stage the transition must occur before
        }
        // If transitioning from transfer destination to shader readable...
        else if (layout == VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL && newLayout == VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL)
        {
            imageMemoryBarrier.srcAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT;
            imageMemoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_SHADER_READ_BIT;

            srcStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT;
            dstStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
        }
        else if (layout == VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR && newLayout == VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL)
        {
            imageMemoryBarrier.srcAccessMask = 0;
            imageMemoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_SHADER_READ_BIT;

            srcStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
            dstStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
        }
        
        // If transitioning from an undefined layout to one optimal for depth stencil...
        else if (layout == VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED && newLayout == VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL) 
        {
            imageMemoryBarrier.srcAccessMask = 0;
            imageMemoryBarrier.dstAccessMask = VkAccessFlags.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT | VkAccessFlags.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;

            srcStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
            dstStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT;
        }
        else
        {
            VulkanDebugger.ThrowError($"Transitioning images from [{ layout.ToString() }] to [{ newLayout.ToString() }] is not supported");
        }

        // Bind the pipeline barrier
        VulkanNative.vkCmdPipelineBarrier(
                commandBuffer,
                srcStage, dstStage,		                                // Pipeline stages (match to src and dst AccessMasks)
                0,						                    // Dependency flags
                0, null,				    // Memory Barrier count + data
                0, null,			// Buffer Memory Barrier count + data
                1, &imageMemoryBarrier	            // Image Memory Barrier count + data
        );
        
        // End command buffer
        VulkanUtilities.EndSingleTimeCommands(commandBuffer);

        // Change the current layout
        this.layout = newLayout;
    }
    
    public VkImage GetVkImage()
    {
        return this.vkImage;
    }

    public VkImageView GetVkImageView()
    {
        return this.vkImageView;
    }

    public VkDeviceMemory GetVkImageMemory()
    {
        return this.vkImageMemory;
    }

    public void CleanUpImageView()
    {
        if (imageViewGenerated) VulkanNative.vkDestroyImageView(VulkanCore.logicalDevice, this.vkImageView, null);
    }

    public void CleanUpImage()
    {
        VulkanNative.vkDestroyImage(VulkanCore.logicalDevice, this.vkImage, null);
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, this.vkImageMemory, null);   
    }

    public void CleanUp()
    {
        CleanUpImage();
        CleanUpImageView();
    }
}