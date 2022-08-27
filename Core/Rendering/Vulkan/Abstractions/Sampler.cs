using System.Numerics;
using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Core.Rendering.Vulkan.Abstractions;

public unsafe class Sampler
{
    public class Builder
    {
        private VkSamplerAddressMode samplerAddressMode = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT;

        private float maxAnisotropy = 1.0f;
        private float minLod;
        private float maxLod = 13.0f;
        private bool applyBilinearFiltering = true;

        public Builder SetMaxAnisotropy(float givenMaxAnisotropy)
        {
            // Check if sampler anisotropy is supported by the GPU
            if (VulkanCore.physicalDeviceFeatures.samplerAnisotropy)
            {
                // Clamp the anisotropy between 0.0 and 1.0 and multiply it by the maximum supported anisotropy
                givenMaxAnisotropy = Mathematics.Clamp(givenMaxAnisotropy, 0f, 1f);
                this.maxAnisotropy = (givenMaxAnisotropy / 1.0f) * VulkanCore.physicalDeviceProperties.limits.maxSamplerAnisotropy;
            }
            else
            {
                VulkanDebugger.ThrowWarning("Sampler anisotropy is requested but not supported by the GPU. The feature has automatically been disabled");
            }

            return this;
        }

        public Builder SetAddressMode(in VkSamplerAddressMode addressMode)
        {
            // Save the provided address mode  
            samplerAddressMode = addressMode;
            return this;
        }

        public Builder SetLod(in Vector2 givenLod)
        {
            // Save the provided LOD values
            minLod = givenLod.X;
            maxAnisotropy = givenLod.Y;
            return this;
        }

        public Builder SetBilinearFiltering(in bool apply)
        {
            // Save the bilinear filtering preference 
            applyBilinearFiltering = apply;
            return this;
        }

        public void Build(out Sampler sampler)
        {
            // Create the sampler
            sampler = new Sampler(applyBilinearFiltering, samplerAddressMode, minLod, maxLod, maxAnisotropy);
        }
    }

    private VkSampler vkSampler;

    private Sampler(in bool applyBilinearFiltering, in VkSamplerAddressMode samplerAddressMode, in float minLod, in float maxLod, in float maxAnisotropy)
    {
        // Get the sampler filter based on whether bilinear filtering is enabled 
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
            minLod = minLod,
            maxLod = maxLod,
            anisotropyEnable = maxAnisotropy > 0.0f,
            maxAnisotropy = maxAnisotropy > 0.0f ? maxAnisotropy : 0.0f 
        };

        // Create the Vulkan sampler
        fixed (VkSampler* samplerPtr = &vkSampler)
        {
            VulkanDebugger.CheckResults(
                VulkanNative.vkCreateSampler(VulkanCore.logicalDevice, &samplerCreateInfo, null, samplerPtr),
                $"Failed to create sampler with a LOD of [{ minLod }, { maxLod }] and [{ maxAnisotropy }] max anisotropy"
            );
        }
    }

    public VkSampler GetVkSampler()
    {
        // Return the Vulkan sampler
        return this.vkSampler;
    }

    public void CleanUp()
    {
        // Destroy the Vulkan sampler
        VulkanNative.vkDestroySampler(VulkanCore.logicalDevice, this.vkSampler, null);
    }
}