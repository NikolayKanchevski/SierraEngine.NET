using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan.Abstractions;
using StbImageSharp;

namespace SierraEngine.Core.Rendering.Vulkan;

public enum TextureType { None = 0, Diffuse, Specular, Normal, Height }

public partial class VulkanRenderer
{
    private const uint MAX_TEXTURES = World.MAX_TEXTURES;
    
    private readonly List<Texture> diffuseTextures = new List<Texture>((int) MAX_TEXTURES);
    private readonly List<Texture> specularTextures = new List<Texture>((int) MAX_TEXTURES);

    private Sampler textureSampler = null!;

    private void CreateNullTextures()
    {
        CreateTexture("Textures/Null/DiffuseNull.jpg", TextureType.Diffuse);
        CreateTexture("Textures/Null/SpecularNull.jpg", TextureType.Specular);
    }

    public int CreateTexture(string fileName, TextureType textureType, ColorComponents colors = ColorComponents.RedGreenBlueAlpha)
    {
        // Load image data in bytes
        new Texture.Builder()
            .SetSampler(textureSampler)
            .SetDescriptorSetLayout(this.descriptorSetLayout)
            .SetDescriptorPool(descriptorPool)
            .SetTextureType(textureType)
            .SetColors(colors)
        .Build(fileName, out var texture);

        if (textureType == TextureType.Diffuse)
        {
            diffuseTextures.Add(texture);
            return diffuseTextures.Count - 1;
        }
        else if (textureType == TextureType.Specular)
        {
            specularTextures.Add(texture);
            return specularTextures.Count - 1;
        }
        
        return -1;
    }

    private void CreateTextureSampler()
    {
        new Sampler.Builder()
            .SetMaxAnisotropy(1.0f)
            .SetBilinearFiltering(true)
        .Build(out textureSampler);
    }
}