using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkDescriptorSetLayout descriptorSetLayout;
    
    private VkDescriptorPool descriptorPool;
    private VkDescriptorSet[] descriptorSets = null!;
    
    private void CreateDescriptorSetLayout()
    {
        VkDescriptorSetLayoutBinding mvpBinding = new VkDescriptorSetLayoutBinding()
        {
            binding = 0,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT,
            pImmutableSamplers = null
        };

        VkDescriptorSetLayoutCreateInfo descriptorSetLayoutCreateInfo = new VkDescriptorSetLayoutCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
            bindingCount = 1,
            pBindings = &mvpBinding
        };

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
        VkDescriptorPoolSize poolSize = new VkDescriptorPoolSize()
        {
            type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
            descriptorCount = MAX_CONCURRENT_FRAMES
        };

        VkDescriptorPoolCreateInfo poolCreateInfo = new VkDescriptorPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
            maxSets = MAX_CONCURRENT_FRAMES,
            poolSizeCount = 1,
            pPoolSizes = &poolSize
        };

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
        descriptorSets = new VkDescriptorSet[MAX_CONCURRENT_FRAMES];
        
        VkDescriptorSetLayout* descriptorSetLayoutsPtr = stackalloc VkDescriptorSetLayout[] { this.descriptorSetLayout, this.descriptorSetLayout, this.descriptorSetLayout };

        VkDescriptorSetAllocateInfo setAllocateInfo = new VkDescriptorSetAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
            descriptorPool = descriptorPool,
            descriptorSetCount = MAX_CONCURRENT_FRAMES,
            pSetLayouts = descriptorSetLayoutsPtr
        };

        fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets)
        {
            if (VulkanNative.vkAllocateDescriptorSets(this.logicalDevice, &setAllocateInfo, descriptorSetsPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to allocate descriptor sets");
            }
        }

        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            VkDescriptorBufferInfo mvpBufferInfo = new VkDescriptorBufferInfo()
            {
                buffer = uniformBuffers[i],
                offset = 0,
                range = mvpSize
            };
            
            VkWriteDescriptorSet mvpWriteDescriptorSet = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                dstSet = descriptorSets[i],             // Descriptor set to update
                dstBinding = 0,                         // Binding to update
                dstArrayElement = 0,                    // Index in array to update
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
                descriptorCount = 1,                    // Amount to update
                pBufferInfo = &mvpBufferInfo            // Information on buffer data to bind
            };
            
            VulkanNative.vkUpdateDescriptorSets(this.logicalDevice, 1, &mvpWriteDescriptorSet, 0, null);
        }
    }
}