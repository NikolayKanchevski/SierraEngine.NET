using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkDescriptorSetLayout descriptorSetLayout;
    private VkDescriptorPool descriptorPool;

    // private VkDescriptorPool imGuiDescriptorPool;
    
    private VkDescriptorSet[] uniformDescriptorSets = null!;
    private readonly List<VkDescriptorSet> samplerDescriptorSets = new List<VkDescriptorSet>();
    
    private void CreateDescriptorSetLayout()
    {
        // Set up uniform buffer binding info
        VkDescriptorSetLayoutBinding uniformBinding = new VkDescriptorSetLayoutBinding()
        {
            binding = 0,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT
        };

        // Set up fragment texture sampler binding info
        VkDescriptorSetLayoutBinding fragmentTextureSamplerBinding = new VkDescriptorSetLayoutBinding()
        {
            binding = 1,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT
        };

        // Collect all layout bindings in a single pointer array
        VkDescriptorSetLayoutBinding* descriptorSetLayoutBindingsPtr = stackalloc VkDescriptorSetLayoutBinding[] { uniformBinding, fragmentTextureSamplerBinding };
        
        // Set up descriptor layout creation info
        VkDescriptorSetLayoutCreateInfo uniformDescriptorSetLayoutCreateInfo = new VkDescriptorSetLayoutCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
            bindingCount = 2,
            pBindings = descriptorSetLayoutBindingsPtr
        };

        // Create the uniform descriptor set layout
        fixed (VkDescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {
            if (VulkanNative.vkCreateDescriptorSetLayout(this.logicalDevice, &uniformDescriptorSetLayoutCreateInfo, null, descriptorSetLayoutPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create uniform descriptor set layout");
            }
        }
    }
    
    private void CreateDescriptorPool()
    {
        uint descriptorCount = MAX_CONCURRENT_FRAMES + MAX_TEXTURES;
        
        // Set up uniform buffer's pool size info
        VkDescriptorPoolSize uniformPoolSize = new VkDescriptorPoolSize()
        {
            type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
            descriptorCount = descriptorCount
        };
        
        // Set up texture sampler's pool size info
        VkDescriptorPoolSize textureSamplerPoolSize = new VkDescriptorPoolSize()
        {
            type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            descriptorCount = descriptorCount
        };

        // Collect all descriptor pool sizes in a single pointer array
        VkDescriptorPoolSize* descriptorPoolSizesPtr = stackalloc VkDescriptorPoolSize[] { uniformPoolSize, textureSamplerPoolSize };
        
        // Set up descriptor pool creation info
        VkDescriptorPoolCreateInfo descriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
            maxSets = MAX_CONCURRENT_FRAMES + MAX_TEXTURES,
            poolSizeCount = 2,
            pPoolSizes = descriptorPoolSizesPtr
        };

        // Create the uniform descriptors pool
        fixed (VkDescriptorPool* descriptorPoolPtr = &descriptorPool)
        {
            if (VulkanNative.vkCreateDescriptorPool(this.logicalDevice, &descriptorPoolCreateInfo, null, descriptorPoolPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create uniform descriptor pool");
            }
        }
        
        #region ImGui Pool
        // VkDescriptorPoolSize* imGuiPoolSizes = stackalloc VkDescriptorPoolSize[11];
        // imGuiPoolSizes[0].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLER;
        // imGuiPoolSizes[0].descriptorCount = 1000;
        // imGuiPoolSizes[1].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
        // imGuiPoolSizes[1].descriptorCount = 1000;
        // imGuiPoolSizes[2].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE;
        // imGuiPoolSizes[2].descriptorCount = 1000;
        // imGuiPoolSizes[3].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE;
        // imGuiPoolSizes[3].descriptorCount = 1000;
        // imGuiPoolSizes[4].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_TEXEL_BUFFER;
        // imGuiPoolSizes[4].descriptorCount = 1000;
        // imGuiPoolSizes[5].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_TEXEL_BUFFER;
        // imGuiPoolSizes[5].descriptorCount = 1000;
        // imGuiPoolSizes[6].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER;
        // imGuiPoolSizes[6].descriptorCount = 1000;
        // imGuiPoolSizes[7].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;
        // imGuiPoolSizes[7].descriptorCount = 1000;
        // imGuiPoolSizes[8].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
        // imGuiPoolSizes[8].descriptorCount = 1000;
        // imGuiPoolSizes[9].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER_DYNAMIC;
        // imGuiPoolSizes[9].descriptorCount = 1000;
        // imGuiPoolSizes[10].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT;
        // imGuiPoolSizes[10].descriptorCount = 1000;
        //
        // VkDescriptorPoolCreateInfo imGuiDescriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo()
        // {
        //     sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
        //     flags = VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_FREE_DESCRIPTOR_SET_BIT,
        //     maxSets = 1000,
        //     poolSizeCount = 11,
        //     pPoolSizes = imGuiPoolSizes
        // };
        //
        // fixed (VkDescriptorPool* imGuiDescriptorPoolPtr = &imGuiDescriptorPool)
        // {
        //     if (VulkanNative.vkCreateDescriptorPool(this.logicalDevice, &imGuiDescriptorPoolCreateInfo, null, imGuiDescriptorPoolPtr) != VkResult.VK_SUCCESS)
        //     {
        //         VulkanDebugger.ThrowError("Failed to create ImGui descriptor pool");
        //     }
        // }
        #endregion
    }

    private void CreateUniformDescriptorSets()
    {
        // Resize the uniformDescriptorSets arrays
        uniformDescriptorSets = new VkDescriptorSet[MAX_CONCURRENT_FRAMES];
        
        // Allocate a descriptor set layout for each uniform descriptor set group
        VkDescriptorSetLayout* descriptorSetLayoutsPtr = stackalloc VkDescriptorSetLayout[] { this.descriptorSetLayout, this.descriptorSetLayout, this.descriptorSetLayout };

        // Define vertex uniform descriptor set allocation info
        VkDescriptorSetAllocateInfo setAllocateInfo = new VkDescriptorSetAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
            descriptorPool = descriptorPool,
            descriptorSetCount = MAX_CONCURRENT_FRAMES,
            pSetLayouts = descriptorSetLayoutsPtr
        };

        // Allocate the vertex uniform sets
        fixed (VkDescriptorSet* descriptorSetsPtr = uniformDescriptorSets)
        {
            if (VulkanNative.vkAllocateDescriptorSets(this.logicalDevice, &setAllocateInfo, descriptorSetsPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to allocate uniform descriptor sets");
            }
        }

        // For each descriptor set group
        VkWriteDescriptorSet* writeDescriptorSetsPtr = stackalloc VkWriteDescriptorSet[1];
        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            // Set up vertex uniform descriptor buffer info
            VkDescriptorBufferInfo uniformBufferInfo = new VkDescriptorBufferInfo()
            {
                buffer = uniformBuffers[i],
                offset = 0,
                range = uniformDataSize
            };
            
            // Create the write descriptor set for vertex uniform buffer
            VkWriteDescriptorSet uniformWriteDescriptorSet = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                dstSet = uniformDescriptorSets[i],      // Descriptor set to update
                dstBinding = 0,                         // Binding to update
                dstArrayElement = 0,                    // Index in array to update
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
                descriptorCount = 1,                    // Amount to update
                pBufferInfo = &uniformBufferInfo             // Information on buffer data to bind
            };

            // Put all uniform write descriptors in a single pointer array  
            writeDescriptorSetsPtr[0] = uniformWriteDescriptorSet;
            
            // Update uniform descriptor sets
            VulkanNative.vkUpdateDescriptorSets(this.logicalDevice, 1, writeDescriptorSetsPtr, 0, null);
        }
    }

    private int CreateTextureDescriptorSet(in VkImageView textureImageView)
    {
        // Create empty descriptor set
        VkDescriptorSet textureDescriptorSet;

        // Put the sampler layout in a pointer array
        VkDescriptorSetLayout* samplerDescriptorSetLayoutsPtr = stackalloc VkDescriptorSetLayout[] { descriptorSetLayout };

        // Set up descriptor allocation info
        VkDescriptorSetAllocateInfo textureDescriptorSetAllocateInfo = new VkDescriptorSetAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
            descriptorPool = descriptorPool,
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
            dstBinding = 1,
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