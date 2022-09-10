using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan.Abstractions;

namespace SierraEngine.Core.Rendering.Vulkan;

public partial class VulkanRenderer
{
    private DescriptorSetLayout descriptorSetLayout = null!;
    private DescriptorPool descriptorPool = null!;
    
    private VkDescriptorSet[] uniformDescriptorSets = null!;
    
    private unsafe void CreateDescriptorSetLayout()
    {
        // Create the descriptor set layout
        new DescriptorSetLayout.Builder()
            .AddBinding(0, VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT)
            .AddBinding(1, VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT)
            .AddBinding(2, VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT)
        .Build(out descriptorSetLayout);
    }
    
    private void CreateDescriptorPool()
    {
        // Calculate the total descriptor count
        const uint DESCRIPTOR_COUNT = MAX_CONCURRENT_FRAMES + (MAX_TEXTURES * 2);
    
        // Create the descriptor pool
        new DescriptorPool.Builder()
            .SetMaxSets(DESCRIPTOR_COUNT)
            .AddPoolSize(VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, DESCRIPTOR_COUNT)
            .AddPoolSize(VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, DESCRIPTOR_COUNT)
        .Build(out descriptorPool);
    }

    private void CreateUniformDescriptorSets()
    {
        // Create the information on the buffer
        VkDescriptorBufferInfo uniformBufferInfo = new VkDescriptorBufferInfo()
        {
            offset = 0,
            range = uniformDataSize
        };
        
        // Resize the uniform buffers array and write to each descriptor
        uniformDescriptorSets = new VkDescriptorSet[MAX_CONCURRENT_FRAMES];
            
        for (int i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            uniformBufferInfo.buffer = uniformBuffers[i];
            
            new DescriptorWriter(descriptorSetLayout, descriptorPool)
                .WriteBuffer(0, uniformBufferInfo) 
            .Build(out uniformDescriptorSets[i]);
        }
    }
    
    private uint TextureTypeToBinding(in TextureType textureType)
    {
        return Convert.ToUInt32(textureType) + 1;
    }
}