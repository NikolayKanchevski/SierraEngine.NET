using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan.Abstractions;

/// <summary>
/// An abstraction for the Vulkan's descriptor layout object.
/// </summary>
public unsafe class DescriptorSetLayout
{
    // ********************* Descriptor Set Layout Builder ********************* 
    public class Builder
    {
        private readonly List<Tuple<uint, VkDescriptorSetLayoutBinding>> bindings = new();
        
        public Builder AddBinding(uint binding, in VkDescriptorType descriptorType, in VkShaderStageFlags shaderStages, in uint descriptorCount = 1, VkSampler* immutableSamplers = null)
        {
            // Check if the binding already exists and therefore is in use
            if (bindings.Find(o => o.Item1 == binding) != null)
            {
                VulkanDebugger.ThrowError(
                    $"Binding [{ binding }] already in use by a " +
                    $"[{ bindings[(int) binding].Item2.descriptorType.ToString() }] descriptor");
            }

            // Set up the layout binding info
            VkDescriptorSetLayoutBinding layoutBinding = new VkDescriptorSetLayoutBinding()
            {
                binding = binding,
                descriptorType = descriptorType,
                descriptorCount = descriptorCount,
                stageFlags = shaderStages,
                pImmutableSamplers = immutableSamplers
            };
            
            // Add the binding info to the tuple list
            this.bindings.Add(new Tuple<uint, VkDescriptorSetLayoutBinding>(binding, layoutBinding));

            return this;
        }

        public void Build(out DescriptorSetLayout descriptorSetLayout)
        {
            // Create the descriptor set layout
            descriptorSetLayout = new DescriptorSetLayout(this.bindings);
        }
    }

    // ********************* Descriptor Set Layout ********************* 
    private VkDescriptorSetLayout vkDescriptorSetLayout;
    public readonly List<Tuple<uint, VkDescriptorSetLayoutBinding>> bindings;

    private DescriptorSetLayout(in List<Tuple<uint, VkDescriptorSetLayoutBinding>> givenBindings)
    {
        // Save the provided bindings
        this.bindings = givenBindings;
        
        // Create a pointer to layout binding array
        VkDescriptorSetLayoutBinding* layoutBindings = stackalloc VkDescriptorSetLayoutBinding[givenBindings.Count];
        
        // Foreach pair in the provided tuple retrieve the created set layout binding
        for (int i = 0; i < givenBindings.Count; i++)
        {
            layoutBindings[i] = givenBindings[i].Item2;
        }

        // Set up the layout creation info
        VkDescriptorSetLayoutCreateInfo layoutCreateInfo = new VkDescriptorSetLayoutCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
            bindingCount = (uint) givenBindings.Count,
            pBindings = layoutBindings
        };

        // Create the Vulkan descriptor set layout 
        fixed (VkDescriptorSetLayout* vkLayoutPtr = &vkDescriptorSetLayout)
        {
            VulkanDebugger.CheckResults(
                VulkanNative.vkCreateDescriptorSetLayout(VulkanCore.logicalDevice, &layoutCreateInfo, null, vkLayoutPtr),
                $"Failed to create descriptor layout with [{ layoutCreateInfo.bindingCount }] binging(s)"
            );   
        }
    }

    public VkDescriptorSetLayout GetVkDescriptorSetLayout()
    {
        // Get the Vulkan descriptor set layout
        return this.vkDescriptorSetLayout;
    }

    public static implicit operator VkDescriptorSetLayout(DescriptorSetLayout givenDescriptorSetLayout)
    {
        return givenDescriptorSetLayout.vkDescriptorSetLayout;
    }

    public void CleanUp()
    {
        // Destroy the Vulkan descriptor set
        VulkanNative.vkDestroyDescriptorSetLayout(VulkanCore.logicalDevice, this.vkDescriptorSetLayout, null);
    }
}

/// <summary>
/// An abstraction for the Vulkan's descriptor pool object.
/// </summary>
public unsafe class DescriptorPool
{
    // ********************* Descriptor Pool Builder ********************* 
    public class Builder
    {
        private uint maxSets = 1000;
        private VkDescriptorPoolCreateFlags poolCreateFlags = 0;
        private readonly List<VkDescriptorPoolSize> poolSizes = new List<VkDescriptorPoolSize>();
        
        public Builder AddPoolSize(in VkDescriptorType descriptorType, in uint count)
        {
            // Add the pool size to the list of pool sizes
            this.poolSizes.Add(new VkDescriptorPoolSize() { type =  descriptorType, descriptorCount = count });
            return this;
        }
        
        public Builder SetPoolFlags(in VkDescriptorPoolCreateFlags givenPoolCreateFlags)
        {
            // Set the pool creation flags
            this.poolCreateFlags = givenPoolCreateFlags;
            return this;
        }
        
        public Builder SetMaxSets(in uint givenMaxSets)
        {
            // Set the max set value
            this.maxSets = givenMaxSets;
            return this;
        }

        public void Build(out DescriptorPool givenDescriptorPool)
        {
            // Create the descriptor pool
            givenDescriptorPool = new DescriptorPool(this.maxSets, this.poolCreateFlags, this.poolSizes.ToArray());
        }
    }

    // ********************* Descriptor Pool ********************* 
    private VkDescriptorPool vkDescriptorPool;

    private DescriptorPool(in uint maxSets, in VkDescriptorPoolCreateFlags poolCreateFlags, in VkDescriptorPoolSize[] poolSizes)
    {
        // Set up the descriptor pool creation info
        VkDescriptorPoolCreateInfo descriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
            poolSizeCount = (uint)poolSizes.Length,
            maxSets = maxSets,
            flags = poolCreateFlags
        };

        // Convert the pool sizes array to a pointer and assign it to the create info
        fixed (VkDescriptorPoolSize* poolSizesPtr = poolSizes)
        {
            descriptorPoolCreateInfo.pPoolSizes = poolSizesPtr;
        }

        // Create the Vulkan descriptor pool
        fixed (VkDescriptorPool* poolPtr = &vkDescriptorPool)
        {
            VulkanDebugger.CheckResults(
                VulkanNative.vkCreateDescriptorPool(VulkanCore.logicalDevice, &descriptorPoolCreateInfo, null, poolPtr),
                $"Failed to create descriptor pool with [{ maxSets }] max sets and [{ descriptorPoolCreateInfo.poolSizeCount }] pool sizes"
            );
        }
    }

    public bool AllocateDescriptorSet(VkDescriptorSetLayout vkDescriptorSetLayout, out VkDescriptorSet vkDescriptorSet)
    {
        // Set up the allocation info
        VkDescriptorSetAllocateInfo allocateInfo = new VkDescriptorSetAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
            descriptorPool = this.vkDescriptorPool,
            pSetLayouts = &vkDescriptorSetLayout,
            descriptorSetCount = 1
        };

        // Create the Vulkan descriptor set
        fixed (VkDescriptorSet* setPtr = &vkDescriptorSet)
        {
            return VulkanDebugger.CheckResults(
                VulkanNative.vkAllocateDescriptorSets(VulkanCore.logicalDevice, &allocateInfo, setPtr),
                "Failed to allocate descriptor set"
            );
        }
    }

    public void FreeDescriptors(in VkDescriptorSet* descriptorSets, uint descriptorCount)
    {
        VulkanNative.vkFreeDescriptorSets(VulkanCore.logicalDevice, this.vkDescriptorPool, descriptorCount, descriptorSets);
    }

    public void ResetPool()
    {
        VulkanNative.vkResetDescriptorPool(VulkanCore.logicalDevice, this.vkDescriptorPool, 0);
    }

    public void CleanUp()
    {
        // Destroy the Vulkan descriptor pool
        VulkanNative.vkDestroyDescriptorPool(VulkanCore.logicalDevice, this.vkDescriptorPool, null);
    }
}

// ********************* Descriptor Set Writer ********************* 
public unsafe class DescriptorWriter
{
    private readonly DescriptorPool descriptorPool;
    private readonly DescriptorSetLayout descriptorSetLayout;
    private readonly List<VkWriteDescriptorSet> writeDescriptorSets = new List<VkWriteDescriptorSet>();
    
    public DescriptorWriter(in DescriptorSetLayout descriptorSetLayout, in DescriptorPool descriptorPool)
    {
        // Save the provided values
        this.descriptorSetLayout = descriptorSetLayout;
        this.descriptorPool = descriptorPool;
    }

    public DescriptorWriter WriteBuffer(uint binding, VkDescriptorBufferInfo bufferInfo)
    {
        // Check if the current binding is not available
        if (descriptorSetLayout.bindings.Find(o => o.Item1 == binding) == null)
        {
            VulkanDebugger.ThrowError(
                $"Descriptor set layout does not contain the specified binding: [{ binding }]");
        }

        // Get the binding description and check if it expects more than 1 descriptors 
        VkDescriptorSetLayoutBinding bindingDescription = descriptorSetLayout.bindings[(int) binding].Item2;
        if (bindingDescription.descriptorCount != 1)
        {
            VulkanDebugger.ThrowError(
                $"Trying to bind [{ bindingDescription.descriptorCount }] descriptors while only 1 at a time is supported"
            );
        }

        // Create the write descriptor
        VkWriteDescriptorSet writeDescriptor = new VkWriteDescriptorSet()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            descriptorType = bindingDescription.descriptorType,
            dstBinding = binding,
            pBufferInfo = &bufferInfo,
            descriptorCount = 1
        };
        
        // Add the write descriptor to the list
        writeDescriptorSets.Add(writeDescriptor);

        return this;
    }

    public DescriptorWriter WriteImage(uint binding, VkDescriptorImageInfo imageInfo)
    {
        // Check if the current binding is not available
        if (descriptorSetLayout.bindings.First(o => o.Item1 == binding) == null)
        {
            VulkanDebugger.ThrowError(
                $"Descriptor set layout does not contain the specified binding: [{ binding }]");
        }
        
        // Get the binding description and check if it expects more than 1 descriptors
        VkDescriptorSetLayoutBinding bindingDescription = descriptorSetLayout.bindings[(int) binding].Item2;
        if (bindingDescription.descriptorCount != 1)
        {
            VulkanDebugger.ThrowError(
                $"Trying to bind [{ bindingDescription.descriptorCount }] descriptors while only 1 at a time is supported"
            );
        }

        // Create the write descriptor
        VkWriteDescriptorSet writeDescriptor = new VkWriteDescriptorSet()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            descriptorType = bindingDescription.descriptorType,
            dstBinding = binding,
            pImageInfo = &imageInfo,
            descriptorCount = 1
        };

        // Add the write descriptor to the list
        writeDescriptorSets.Add(writeDescriptor);

        return this;
    }

    public void Build(out VkDescriptorSet descriptorSet)
    {
        // Check if the allocation failed
        bool success = descriptorPool.AllocateDescriptorSet(descriptorSetLayout, out descriptorSet);
        if (!success)
        {
            VulkanDebugger.ThrowError("Could not allocate descriptor set");
        }

        // Update the descriptor sets
        Overwrite(descriptorSet);
    }

    private void Overwrite(in VkDescriptorSet descriptorSet)
    {
        for (int i = 0; i < writeDescriptorSets.Count; i++)
        {
            writeDescriptorSets[i] = writeDescriptorSets[i] with { dstSet = descriptorSet };
        }


        fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets.ToArray())
        {
            VulkanNative.vkUpdateDescriptorSets(VulkanCore.logicalDevice, (uint) writeDescriptorSets.Count, writeDescriptorSetsPtr, 0, null);
        }
    }
}