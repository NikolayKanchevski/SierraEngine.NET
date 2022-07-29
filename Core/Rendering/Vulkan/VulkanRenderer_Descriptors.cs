using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkDescriptorSetLayout descriptorSetLayout;
    
    private VkDescriptorPool descriptorPool;
    private VkDescriptorSet[] descriptorSets = null!;
    
    private void CreateDescriptorSetLayout()
    {
        // Set up VP binding info
        VkDescriptorSetLayoutBinding vpBinding = new VkDescriptorSetLayoutBinding()
        {
            binding = 0,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT
        };

        // Set up sampler binding info
        VkDescriptorSetLayoutBinding textureSamplerBinding = new VkDescriptorSetLayoutBinding()
        {
            binding = 1,
            descriptorCount = 1,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            pImmutableSamplers = null,
            stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT
        };

        VkDescriptorSetLayoutBinding* layoutBindings = stackalloc VkDescriptorSetLayoutBinding[] { vpBinding, textureSamplerBinding };
        
        // Set up layout creation info
        VkDescriptorSetLayoutCreateInfo descriptorSetLayoutCreateInfo = new VkDescriptorSetLayoutCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
            bindingCount = 2,
            pBindings = layoutBindings
        };

        // Create the descriptor set layout
        fixed (VkDescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {
            if (VulkanNative.vkCreateDescriptorSetLayout(this.logicalDevice, &descriptorSetLayoutCreateInfo, null, descriptorSetLayoutPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create descriptor set layout");
            }
        }
    }
    
    private void CreateDescriptorPool()
    {
        // Set up uniform buffer's pool size info
        VkDescriptorPoolSize uniformPoolSize = new VkDescriptorPoolSize()
        {
            type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
            descriptorCount = MAX_CONCURRENT_FRAMES
        };

        // Set up the texture sampler's pool size info
        VkDescriptorPoolSize textureSamplerPoolSize = new VkDescriptorPoolSize()
        {
            type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            descriptorCount = MAX_CONCURRENT_FRAMES
        };

        VkDescriptorPoolSize* descriptorPoolSizes = stackalloc VkDescriptorPoolSize[] { uniformPoolSize, textureSamplerPoolSize };
        
        // Set up pool creation info
        VkDescriptorPoolCreateInfo poolCreateInfo = new VkDescriptorPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
            maxSets = MAX_CONCURRENT_FRAMES,
            poolSizeCount = 2,
            pPoolSizes = descriptorPoolSizes
        };

        // Create the descriptor pool
        fixed (VkDescriptorPool* descriptorPoolPtr = &descriptorPool)
        {
            if (VulkanNative.vkCreateDescriptorPool(this.logicalDevice, &poolCreateInfo, null, descriptorPoolPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create descriptor pool");
            }
        }
    }

    private void CreateDescriptorSets()
    {
        // Resize the descriptorSets array
        descriptorSets = new VkDescriptorSet[MAX_CONCURRENT_FRAMES];
        
        // Allocate a descriptor set layout for each descriptor set
        VkDescriptorSetLayout* descriptorSetLayoutsPtr = stackalloc VkDescriptorSetLayout[] { this.descriptorSetLayout, this.descriptorSetLayout, this.descriptorSetLayout };

        // Define descriptor set allocation info
        VkDescriptorSetAllocateInfo setAllocateInfo = new VkDescriptorSetAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
            descriptorPool = descriptorPool,
            descriptorSetCount = MAX_CONCURRENT_FRAMES,
            pSetLayouts = descriptorSetLayoutsPtr
        };

        // Create all descriptor sets
        fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets)
        {
            if (VulkanNative.vkAllocateDescriptorSets(this.logicalDevice, &setAllocateInfo, descriptorSetsPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to allocate descriptor sets");
            }
        }

        // For each descriptor set
        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            // Set up VP buffer info
            VkDescriptorBufferInfo vpBufferInfo = new VkDescriptorBufferInfo()
            {
                buffer = uniformBuffers[i],
                offset = 0,
                range = vpSize
            };
            
            // Create the write descriptor set for VP
            VkWriteDescriptorSet vpWriteDescriptorSet = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                dstSet = descriptorSets[i],             // Descriptor set to update
                dstBinding = 0,                         // Binding to update
                dstArrayElement = 0,                    // Index in array to update
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
                descriptorCount = 1,                    // Amount to update
                pBufferInfo = &vpBufferInfo             // Information on buffer data to bind
            };

            // Set up texture sampler image info
            VkDescriptorImageInfo textureSamplerImageInfo = new VkDescriptorImageInfo()
            {
                imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                imageView = this.textureImageView,
                sampler = this.textureSampler
            };
            
            // Create the write descriptor set for texture sampler
            VkWriteDescriptorSet textureSamplerWriteDescriptorSet = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                dstSet = descriptorSets[i],
                dstBinding = 1,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                descriptorCount = 1,
                pImageInfo = &textureSamplerImageInfo
            };

            #pragma warning disable CA2014
            VkWriteDescriptorSet* writeDescriptorSetsPtr = stackalloc VkWriteDescriptorSet[] { vpWriteDescriptorSet, textureSamplerWriteDescriptorSet };

            // Update the VP write set
            VulkanNative.vkUpdateDescriptorSets(this.logicalDevice, 2, writeDescriptorSetsPtr, 0, null);
        }
    }
}