using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkDescriptorSetLayout uniformDescriptorSetLayout;
    private VkDescriptorPool uniformDescriptorPool;
    
    private VkDescriptorPool samplerDescriptorPool;
    private VkDescriptorSetLayout samplerDescriptorSetLayout;

    private VkDescriptorPool imGuiDescriptorPool;
    
    private VkDescriptorSet[] uniformDescriptorSets = null!;
    private List<VkDescriptorSet> samplerDescriptorSets = new List<VkDescriptorSet>();
    
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
        
        // Set up uniform layout creation info
        VkDescriptorSetLayoutCreateInfo uniformDescriptorSetLayoutCreateInfo = new VkDescriptorSetLayoutCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
            bindingCount = 1,
            pBindings = &vpBinding
        };

        // Create the uniform descriptor set layout
        fixed (VkDescriptorSetLayout* descriptorSetLayoutPtr = &uniformDescriptorSetLayout)
        {
            if (VulkanNative.vkCreateDescriptorSetLayout(this.logicalDevice, &uniformDescriptorSetLayoutCreateInfo, null, descriptorSetLayoutPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create uniform descriptor set layout");
            }
        }

        // Set up sampler binding info
        VkDescriptorSetLayoutBinding textureSamplerBinding = new VkDescriptorSetLayoutBinding()
        {
            binding = 0,
            descriptorCount = 1,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            pImmutableSamplers = null,
            stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT
        };
        
        // Set up sampler layout creation info
        VkDescriptorSetLayoutCreateInfo textureSamplerDescriptorSetLayoutCreateInfo = new VkDescriptorSetLayoutCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
            bindingCount = 1,
            pBindings = &textureSamplerBinding
        };

        // Create the texture sampler descriptor set layout
        fixed (VkDescriptorSetLayout* samplerDescriptorSetLayoutPtr = &samplerDescriptorSetLayout)
        {
            if (VulkanNative.vkCreateDescriptorSetLayout(this.logicalDevice, &textureSamplerDescriptorSetLayoutCreateInfo, null, samplerDescriptorSetLayoutPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create sampler descriptor set layout");
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
        
        // Set up descriptor pool creation info
        VkDescriptorPoolCreateInfo uniformDescriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
            maxSets = MAX_CONCURRENT_FRAMES,
            poolSizeCount = 1,
            pPoolSizes = &uniformPoolSize
        };

        // Create the uniform descriptor pool
        fixed (VkDescriptorPool* descriptorPoolPtr = &uniformDescriptorPool)
        {
            if (VulkanNative.vkCreateDescriptorPool(this.logicalDevice, &uniformDescriptorPoolCreateInfo, null, descriptorPoolPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create uniform descriptor pool");
            }
        }

        // Set up the texture sampler's pool size info
        VkDescriptorPoolSize textureSamplerPoolSize = new VkDescriptorPoolSize()
        {
            type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            descriptorCount = MAX_TEXTURES
        };

        // Set up the texture sampler's descriptor pool creation info
        VkDescriptorPoolCreateInfo samplerDescriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
            maxSets = MAX_TEXTURES,
            poolSizeCount = 1,
            pPoolSizes = &textureSamplerPoolSize
        };

        // Create the sampler descriptor pool
        fixed (VkDescriptorPool* samplerDescriptorPoolPtr = &samplerDescriptorPool)
        {
            if (VulkanNative.vkCreateDescriptorPool(this.logicalDevice, &samplerDescriptorPoolCreateInfo, null, samplerDescriptorPoolPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create sampler descriptor pool");
            }
        }
        
        return;

        #region ImGui Pool Sizes
        VkDescriptorPoolSize* imGuiPoolSizes = stackalloc VkDescriptorPoolSize[11];
        imGuiPoolSizes[0].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLER;
        imGuiPoolSizes[0].descriptorCount = 1000;
        imGuiPoolSizes[1].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
        imGuiPoolSizes[1].descriptorCount = 1000;
        imGuiPoolSizes[2].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE;
        imGuiPoolSizes[2].descriptorCount = 1000;
        imGuiPoolSizes[3].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE;
        imGuiPoolSizes[3].descriptorCount = 1000;
        imGuiPoolSizes[4].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_TEXEL_BUFFER;
        imGuiPoolSizes[4].descriptorCount = 1000;
        imGuiPoolSizes[5].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_TEXEL_BUFFER;
        imGuiPoolSizes[5].descriptorCount = 1000;
        imGuiPoolSizes[6].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER;
        imGuiPoolSizes[6].descriptorCount = 1000;
        imGuiPoolSizes[7].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;
        imGuiPoolSizes[7].descriptorCount = 1000;
        imGuiPoolSizes[8].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
        imGuiPoolSizes[8].descriptorCount = 1000;
        imGuiPoolSizes[9].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER_DYNAMIC;
        imGuiPoolSizes[9].descriptorCount = 1000;
        imGuiPoolSizes[10].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT;
        imGuiPoolSizes[10].descriptorCount = 1000;
        #endregion

        VkDescriptorPoolCreateInfo imGuiDescriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
            flags = VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_FREE_DESCRIPTOR_SET_BIT,
            maxSets = 1000,
            poolSizeCount = 11,
            pPoolSizes = imGuiPoolSizes
        };

        fixed (VkDescriptorPool* imGuiDescriptorPoolPtr = &imGuiDescriptorPool)
        {
            if (VulkanNative.vkCreateDescriptorPool(this.logicalDevice, &imGuiDescriptorPoolCreateInfo, null, imGuiDescriptorPoolPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create ImGui descriptor pool");
            }
        }
    }

    private void CreateUniformDescriptorSets()
    {
        // Resize the uniformDescriptorSets array
        uniformDescriptorSets = new VkDescriptorSet[MAX_CONCURRENT_FRAMES];
        
        // Allocate a descriptor set layout for each uniform descriptor set
        VkDescriptorSetLayout* descriptorSetLayoutsPtr = stackalloc VkDescriptorSetLayout[] { this.uniformDescriptorSetLayout, this.uniformDescriptorSetLayout, this.uniformDescriptorSetLayout };

        // Define uniform descriptor set allocation info
        VkDescriptorSetAllocateInfo setAllocateInfo = new VkDescriptorSetAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
            descriptorPool = uniformDescriptorPool,
            descriptorSetCount = MAX_CONCURRENT_FRAMES,
            pSetLayouts = descriptorSetLayoutsPtr
        };

        // Create all uniform descriptor sets
        fixed (VkDescriptorSet* descriptorSetsPtr = uniformDescriptorSets)
        {
            if (VulkanNative.vkAllocateDescriptorSets(this.logicalDevice, &setAllocateInfo, descriptorSetsPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to allocate uniform descriptor sets");
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
                dstSet = uniformDescriptorSets[i],      // Descriptor set to update
                dstBinding = 0,                         // Binding to update
                dstArrayElement = 0,                    // Index in array to update
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
                descriptorCount = 1,                    // Amount to update
                pBufferInfo = &vpBufferInfo             // Information on buffer data to bind
            };

            // Update the VP write set
            VulkanNative.vkUpdateDescriptorSets(this.logicalDevice, 1, &vpWriteDescriptorSet, 0, null);
        }
    }

    private int CreateTextureDescriptorSet(in VkImageView textureImageView)
    {
        // Create empty descriptor set
        VkDescriptorSet textureDescriptorSet;

        // Put the sampler layout in a pointer array
        VkDescriptorSetLayout* samplerDescriptorSetLayoutsPtr = stackalloc VkDescriptorSetLayout[] { samplerDescriptorSetLayout };

        // Set up descriptor allocation info
        VkDescriptorSetAllocateInfo textureDescriptorSetAllocateInfo = new VkDescriptorSetAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
            descriptorPool = samplerDescriptorPool,
            descriptorSetCount = 1,
            pSetLayouts = samplerDescriptorSetLayoutsPtr
        };

        // Create texture descriptor sets
        if (VulkanNative.vkAllocateDescriptorSets(this.logicalDevice, &textureDescriptorSetAllocateInfo, &textureDescriptorSet) != VkResult.VK_SUCCESS)
        {
            VulkanDebugger.ThrowError("Failed to allocate texture descriptor set");
        }
        
        // Set up texture sampler image info
        VkDescriptorImageInfo textureSamplerImageInfo = new VkDescriptorImageInfo()
        {
            imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
            imageView = textureImageView,
            sampler = this.textureSampler
        };
            
        // Create the write descriptor set for texture sampler
        VkWriteDescriptorSet textureSamplerWriteDescriptorSet = new VkWriteDescriptorSet()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            dstSet = textureDescriptorSet,
            dstBinding = 0,
            dstArrayElement = 0,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            descriptorCount = 1,
            pImageInfo = &textureSamplerImageInfo
        };

        // Update the texture write set
        VulkanNative.vkUpdateDescriptorSets(this.logicalDevice, 1, &textureSamplerWriteDescriptorSet, 0, null);

        // Add the newly created descriptor set to the list
        samplerDescriptorSets.Add(textureDescriptorSet);

        // Return the ID of the newly created descriptor set
        return samplerDescriptorSets.Count - 1;
    }
}