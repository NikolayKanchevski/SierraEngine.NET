using Evergine.Bindings.Vulkan;
using StbImageSharp;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkImage textureImage;
    private VkImageView textureImageView;
    private VkDeviceMemory textureImageMemory;
    
    private VkFormat textureImageFormat;
    private VkSampler textureSampler;
    
    private void CreateTexture(string fileName, ColorComponents colors = ColorComponents.RedGreenBlueAlpha)
    {
        byte[] fileData = File.ReadAllBytes($"Textures/{ fileName }");
        ImageResult loadedImage = ImageResult.FromMemory(fileData, colors);

        ulong imageSize = (ulong) (loadedImage.Width * loadedImage.Height * GetColorChannelCount(colors));
        
        CreateTextureImage(loadedImage.Width, loadedImage.Height, colors, imageSize, loadedImage.Data);
        CreateTextureImageView();
    }

    private void CreateTextureImage(in int imageWidth, in int imageHeight, in ColorComponents colors, in ulong imageSize, in byte[] imageData)
    {
        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        
        VulkanUtilities.CreateBuffer(
            imageSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
            out stagingBuffer, out stagingBufferMemory
        );

        void* data;
        VulkanNative.vkMapMemory(this.logicalDevice, stagingBufferMemory, 0, imageSize, 0, &data);

        fixed (byte* imageDataPtr = imageData)
        {
            Buffer.MemoryCopy(imageDataPtr, data, imageSize, imageSize);
        }
        
        VulkanNative.vkUnmapMemory(this.logicalDevice, stagingBufferMemory);

        textureImageFormat = colors switch
        {
            ColorComponents.Grey => VkFormat.VK_FORMAT_R8_SRGB,
            ColorComponents.GreyAlpha => VkFormat.VK_FORMAT_R8G8_SRGB,
            ColorComponents.RedGreenBlue => VkFormat.VK_FORMAT_R8G8B8_SRGB,
            _ => VkFormat.VK_FORMAT_R8G8B8A8_SRGB
        };

        VulkanUtilities.CreateImage(
            imageWidth, imageHeight,
            textureImageFormat, VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
            VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT,
            out textureImage, out textureImageMemory
        );
        
        VulkanUtilities.TransitionImageLayout(
            textureImage, textureImageFormat, 
            VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL
        );
        
        VulkanUtilities.CopyBufferToImage(
            stagingBuffer, textureImage, (uint) imageWidth, (uint) imageHeight
        );
        
        VulkanUtilities.TransitionImageLayout(
            textureImage, textureImageFormat,
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
        );

        VulkanNative.vkDestroyBuffer(this.logicalDevice, stagingBuffer, null);
        VulkanNative.vkFreeMemory(this.logicalDevice, stagingBufferMemory, null);
    }

    private void CreateTextureImageView()
    {
        VulkanUtilities.CreateImageView(textureImage, textureImageFormat, out textureImageView);
    }

    private void CreateTextureSampler(VkSamplerAddressMode samplerAddressMode = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT, bool applyBilinearFiltering = true)
    {
        VkFilter samplerFilter = applyBilinearFiltering ? VkFilter.VK_FILTER_LINEAR : VkFilter.VK_FILTER_NEAREST;
        
        
        
        VkSamplerCreateInfo samplerCreateInfo = new VkSamplerCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO,
            minFilter = samplerFilter,
            magFilter = samplerFilter,
            addressModeU = samplerAddressMode,
            addressModeV = samplerAddressMode,
            addressModeW = samplerAddressMode,
            borderColor = VkBorderColor.VK_BORDER_COLOR_INT_OPAQUE_BLACK,
            unnormalizedCoordinates = VkBool32.False,
            compareEnable = VkBool32.False,
            compareOp = VkCompareOp.VK_COMPARE_OP_ALWAYS,
            mipmapMode = VkSamplerMipmapMode.VK_SAMPLER_MIPMAP_MODE_LINEAR,
            mipLodBias = 0.0f,
            minLod = 0.0f,
            maxLod = 0.0f
        };

        if (physicalDeviceFeatures.samplerAnisotropy)
        {
            samplerCreateInfo.anisotropyEnable = VkBool32.True;
            // TODO: Create an option to set the max anisotropy by % - (% * MAX_SUPPORTED_ANISOTROPY)
            samplerCreateInfo.maxAnisotropy = this.physicalDeviceProperties.limits.maxSamplerAnisotropy;
        }

        fixed (VkSampler* textureSamplerPtr = &textureSampler)
        {
            if (VulkanNative.vkCreateSampler(this.logicalDevice, &samplerCreateInfo, null, textureSamplerPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create texture sampler");
            }
        }
    }

    private int GetColorChannelCount(ColorComponents colors)
    {
        switch (colors)
        {
            case ColorComponents.RedGreenBlueAlpha:
                return 4;
            case ColorComponents.RedGreenBlue:
                return 3;
            case ColorComponents.GreyAlpha:
                return 2;
            default:
                return 1;
        }
    }
}