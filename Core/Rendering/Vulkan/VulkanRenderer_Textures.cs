using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan.Abstractions;
using StbImageSharp;

namespace SierraEngine.Core.Rendering.Vulkan;

public enum TextureType { Diffuse, Specular, Normal, Height }

public unsafe partial class VulkanRenderer
{
    private const uint MAX_TEXTURES = World.MAX_TEXTURES;
    
    private List<Image> diffuseTextureImages = new List<Image>((int) MAX_TEXTURES);
    private List<Image> specularTextureImages = new List<Image>((int) MAX_TEXTURES);

    private VkFormat textureImageFormat;
    private Sampler textureSampler = null!;

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
                loadedImage, 
                ref diffuseTextureImages,
                ref diffuseTextureDescriptorSets, textureType, colors);
        }
        if (textureType == TextureType.Specular)
        {
            return CreateTextureImageResources(
                loadedImage, 
                ref specularTextureImages,
                ref specularTextureDescriptorSets, textureType, colors);
        }

        return -1;
    }

    private int CreateTextureImageResources(
        in ImageResult loadedImage, ref List<Image> textureImagesList, 
        ref List<VkDescriptorSet> textureDescriptorSetsList, in TextureType textureType, ColorComponents colors = ColorComponents.RedGreenBlueAlpha)
    {
        uint textureMipLevels = (uint)Math.Floor(Math.Log2(Math.Max(loadedImage.Width, loadedImage.Height)) + 1);

        ulong imageSize = (ulong) (loadedImage.Width * loadedImage.Height * GetColorChannelCount(colors));

        CreateTextureImage((uint) loadedImage.Width, (uint) loadedImage.Height, colors, imageSize, loadedImage.Data, textureMipLevels, ref textureImagesList);

        return CreateTextureDescriptorSet(textureImagesList.Last(), ref textureDescriptorSetsList, textureType);
    }

    private void CreateTextureImage(in uint imageWidth, in uint imageHeight, in ColorComponents colors, in ulong imageSize, in byte[] imageData, in uint textureMipLevels, ref List<Image> textureImagesList)
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

        Image textureImage;
        
        new Image.Builder()
            .SetSize(imageWidth, imageHeight)
            .SetMipLevels(textureMipLevels)
            .SetFormat(textureImageFormat)
            .SetUsage(VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT)
            .SetMemoryFlags(VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
        .Build(out textureImage);
        
        textureImage.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL);
        
        VulkanUtilities.CopyImageToBuffer(stagingBuffer, textureImage.GetVkImage(), textureImage.width, textureImage.height);
        
        // NOTE: Transitioning to VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL is not required as it is automatically done during the mip map generation

        textureImagesList.Add(textureImage);

        // Destroy the staging buffer and free its memory
        VulkanNative.vkDestroyBuffer(this.logicalDevice, stagingBuffer, null);
        VulkanNative.vkFreeMemory(this.logicalDevice, stagingBufferMemory, null);
        
        // Generate mip maps for the current texture
        VulkanUtilities.GenerateMipMaps(textureImage.GetVkImage(), textureImage.format, textureImage.width, textureImage.height, textureImage.mipLevels);
        
        // Create the image view using the proper image format
        textureImagesList.Last().GenerateImageView(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
    }

    private void CreateTextureSampler()
    {
        new Sampler.Builder()
            .SetMaxAnisotropy(1.0f)
            .SetBilinearFiltering(true)
        .Build(out textureSampler);
    }
    
    

    private int GetColorChannelCount(ColorComponents colors)
    {
        return (int) colors;
    }
}