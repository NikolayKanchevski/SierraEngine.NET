using System.Runtime.InteropServices;
using Assimp;
using Evergine.Bindings.Vulkan;
using SierraEngine.Engine;
using SierraEngine.Engine.Classes;
using StbImageSharp;

namespace SierraEngine.Core.Rendering.Vulkan;

public enum TextureType { Diffuse, Specular, Normal, Height }

public unsafe partial class VulkanRenderer
{
    private List<uint> diffuseTextureMipLevels = new List<uint>((int) MAX_TEXTURES);
    private List<VkImage> diffuseTextureImages = new List<VkImage>((int) MAX_TEXTURES);
    private List<VkImageView> diffuseTextureImageViews = new List<VkImageView>((int) MAX_TEXTURES);
    private List<VkDeviceMemory> diffuseTextureImageMemories = new List<VkDeviceMemory>((int) MAX_TEXTURES);
    
    private List<uint> specularTextureMipLevels = new List<uint>((int) MAX_TEXTURES);
    private List<VkImage> specularTextureImages = new List<VkImage>((int) MAX_TEXTURES);
    private List<VkImageView> specularTextureImageViews = new List<VkImageView>((int) MAX_TEXTURES);
    private List<VkDeviceMemory> specularTextureImageMemories = new List<VkDeviceMemory>((int) MAX_TEXTURES);

    private VkFormat textureImageFormat;
    private VkSampler textureSampler;

    private void CreateNullTextures()
    {
        CreateTexture("Textures/Null/DiffuseNull.jpg", TextureType.Diffuse);
        CreateTexture("Textures/Null/SpecularNull.jpg", TextureType.Specular);
    }
    
    public int CreateTexture(string fileName, TextureType textureType, ColorComponents colors = ColorComponents.RedGreenBlueAlpha)
    {
        // Load image data in bytes
        byte[] fileData = File.ReadAllBytes($"{ fileName }");
        ImageResult loadedImage = ImageResult.FromMemory(fileData, colors);

        if (textureType == TextureType.Diffuse)
        {
            return CreateTextureImageResources(
                loadedImage, ref diffuseTextureMipLevels, 
                ref diffuseTextureImages, ref diffuseTextureImageViews, ref diffuseTextureImageMemories,
                ref diffuseTextureDescriptorSets, textureType, colors);
        }
        if (textureType == TextureType.Specular)
        {
            return CreateTextureImageResources(
                loadedImage, ref specularTextureMipLevels, 
                ref specularTextureImages, ref specularTextureImageViews, ref specularTextureImageMemories,
                ref specularTextureDescriptorSets, textureType, colors);
        }

        return -1;
    }

    private int CreateTextureImageResources(
        in ImageResult loadedImage, ref List<uint> textureMipLevelsList, 
        ref List<VkImage> textureImagesList, ref List<VkImageView> textureImageViewsList, ref List<VkDeviceMemory> textureImageMemoriesList, 
        ref List<VkDescriptorSet> textureDescriptorSetsList, in TextureType textureType, ColorComponents colors = ColorComponents.RedGreenBlueAlpha)
    {
        uint textureMipLevels = (uint)Math.Floor(Math.Log2(Math.Max(loadedImage.Width, loadedImage.Height)) + 1);
        textureMipLevelsList.Add(textureMipLevels);
        
        ulong imageSize = (ulong) (loadedImage.Width * loadedImage.Height * GetColorChannelCount(colors));

        CreateTextureImage((uint) loadedImage.Width, (uint) loadedImage.Height, colors, imageSize, loadedImage.Data, ref textureMipLevelsList, ref textureImagesList, ref textureImageMemoriesList);
        CreateTextureImageView(ref textureMipLevelsList, textureImagesList, ref textureImageViewsList);

        return CreateTextureDescriptorSet(textureImageViewsList.Last(), ref textureDescriptorSetsList, textureType);
    }

    private void CreateTextureImage(in uint imageWidth, in uint imageHeight, in ColorComponents colors, in ulong imageSize, in byte[] imageData, ref List<uint> textureMipLevelsList, ref List<VkImage> textureImagesList, ref List<VkDeviceMemory> textureImageMemories)
    {
        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        
        // Create the staging buffer
        VulkanUtilities.CreateBuffer(
            imageSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
            out stagingBuffer, out stagingBufferMemory
        );

        void* data;
        VulkanNative.vkMapMemory(this.logicalDevice, stagingBufferMemory, 0, imageSize, 0, &data);

        // Copy image data to the buffer
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

        VkImage textureImage;
        VkDeviceMemory textureImageMemory;

        // Create the vulkan image
        VulkanUtilities.CreateImage(
            imageWidth, imageHeight, textureMipLevelsList.Last(), VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            textureImageFormat, VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
            VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT,
            out textureImage, out textureImageMemory
        );
        
        // Transition its layout so that it can be used for copying
        VulkanUtilities.TransitionImageLayout(
            textureImage, textureImageFormat, 
            VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, textureMipLevelsList.Last()
        );
        
        // Copy the transitioned image to the staging buffer
        VulkanUtilities.CopyImageToBuffer(
            stagingBuffer, textureImage, imageWidth, imageHeight
        );
        
        // NOTE: Transitioning to VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL is not required as it is automatically done during the mip map generation
        
        // Add the texture image and its memory to the lists
        textureImagesList.Add(textureImage);
        textureImageMemories.Add(textureImageMemory);

        // Destroy the staging buffer and free its memory
        VulkanNative.vkDestroyBuffer(this.logicalDevice, stagingBuffer, null);
        VulkanNative.vkFreeMemory(this.logicalDevice, stagingBufferMemory, null);
        
        // Generate mip maps for the current texture
        VulkanUtilities.GenerateMipMaps(textureImage, textureImageFormat, imageWidth, imageHeight, textureMipLevelsList.Last());
    }

    private void CreateTextureImageView(ref List<uint> textureMipLevelsList, in List<VkImage> textureImagesList, ref List<VkImageView> textureImageViewsList)
    {
        // Create the image view using the proper image format
        VkImageView textureImageView;
        VulkanUtilities.CreateImageView(textureImagesList.Last(), textureImageFormat, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT, textureMipLevelsList.Last(), out textureImageView);
        
        // Add the image view to the list
        textureImageViewsList.Add(textureImageView);
    }

    private void CreateTextureSampler(VkSamplerAddressMode samplerAddressMode = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT, float maxAnisotropy = 100.0f, bool applyBilinearFiltering = true)
    {
        // Check if bilinear filtering is requested and 
        VkFilter samplerFilter = applyBilinearFiltering ? VkFilter.VK_FILTER_LINEAR : VkFilter.VK_FILTER_NEAREST;

        // Set up the sampler creation info
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
            maxLod = 13.0f
        };

        // Check if sampler anisotropy is requested and supported
        if (physicalDeviceFeatures.samplerAnisotropy && maxAnisotropy > 0.0f)
        {
            samplerCreateInfo.anisotropyEnable = VkBool32.True;
            maxAnisotropy = Mathematics.Clamp(maxAnisotropy, 0f, 100f);
            samplerCreateInfo.maxAnisotropy = (maxAnisotropy / 100.0f) * this.physicalDeviceProperties.limits.maxSamplerAnisotropy;
        }
        else
        {
            VulkanDebugger.ThrowWarning("Sampler anisotropy is requested but not supported by the GPU. The feature has automatically been disabled");
        }

        // Create the texture sampler
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
        return (int) colors;
    }
}