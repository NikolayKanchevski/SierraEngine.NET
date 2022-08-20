using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using Glfw;
using ImGuiNET;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using Silk.NET.Core.Native;
using Cursor = SierraEngine.Engine.Classes.Cursor;

namespace SierraEngine.Core.Rendering.ImGui;

public unsafe class ImGuiController
{
    private int windowWidth;
    private int windowHeight;
    private uint swapchainImageCount;
    private bool frameBegun;
    private const ulong BUFFER_MEMORY_ALIGNMENT = 256;
    private GlobalMemory frameRenderBuffers = null!;

    private VkDescriptorPool descriptorPool;
    private readonly VkSampleCountFlags msaaSampleCount;
    private VkSampler fontSampler;
    private VkDescriptorSetLayout descriptorSetLayout;
    private VkDescriptorSet descriptorSet;
    private VkPipelineLayout graphicsPipelineLayout;
    private VkShaderModule vertexShaderModule;
    private VkShaderModule fragmentShaderModule;
    private VkImage fontImage;
    private VkImageView fontImageView;
    private VkDeviceMemory fontImageMemory;
    private VkPipeline graphicsPipeline;
    
    private WindowRenderBuffers mainWindowRenderBuffers;
    
    public ImGuiController(in Window window, ref VkRenderPass renderPass, in uint swapchainImageCount, in VkSampleCountFlags sampleCountFlags)
    {
        this.msaaSampleCount = sampleCountFlags;
        
        var context = ImGuiNET.ImGui.CreateContext();
        ImGuiNET.ImGui.SetCurrentContext(context);

        // Use the default font
        var io = ImGuiNET.ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        // io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        //
        // io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        // io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        
        Init(in window, ref renderPass, swapchainImageCount);

        SetKeyMappings();

        SetPerFrameImGuiData();

        BeginFrame();
    }

    private void Init(in Window window, ref VkRenderPass renderPass, in uint swapChainImageCount)
    {
        windowWidth = window.width;
        windowHeight = window.height;
        swapchainImageCount = swapChainImageCount;

        if (swapchainImageCount < 2)
        {
            VulkanDebugger.ThrowError("Swapchain image count must be >= 2");
        }

        // Set default style
        ImGuiNET.ImGui.StyleColorsDark();

        // Create the descriptor pool for ImGui
        VkDescriptorPoolSize descriptorPoolSize = new VkDescriptorPoolSize() with
        {
            type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            descriptorCount = 1
        };
        
        VkDescriptorPoolCreateInfo descriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
            poolSizeCount = 1,
            pPoolSizes = &descriptorPoolSize,
            maxSets = 1
        };

        fixed (VkDescriptorPool* descriptorPoolPtr = &descriptorPool)
        {
            if (VulkanNative.vkCreateDescriptorPool(VulkanCore.logicalDevice, &descriptorPoolCreateInfo, null, descriptorPoolPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create ImGui descriptor pool");
            }
        }

        VkSamplerCreateInfo samplerCreateInfo = new VkSamplerCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO,
            magFilter = VkFilter.VK_FILTER_LINEAR,
            minFilter = VkFilter.VK_FILTER_LINEAR,
            mipmapMode = VkSamplerMipmapMode.VK_SAMPLER_MIPMAP_MODE_LINEAR,
            addressModeU = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT,
            addressModeV = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT,
            addressModeW = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT,
            minLod = -1000.0f,
            maxLod = 1000.0f,
            maxAnisotropy = 1.0f
        };

        fixed (VkSampler* fontSamplerPtr = &fontSampler)
        {
            if (VulkanNative.vkCreateSampler(VulkanCore.logicalDevice, &samplerCreateInfo, null, fontSamplerPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create ImGui font sampler");
            }
        }

        VkSampler* immutableSamplersPtr = stackalloc VkSampler[] { fontSampler };
        
        VkDescriptorSetLayoutBinding descriptorSetLayoutBinding = new VkDescriptorSetLayoutBinding()
        {
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT,
            pImmutableSamplers = immutableSamplersPtr
        };

        VkDescriptorSetLayoutCreateInfo descriptorSetLayoutCreateInfo = new VkDescriptorSetLayoutCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
            bindingCount = 1,
            pBindings = &descriptorSetLayoutBinding
        };

        fixed (VkDescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {
            if (VulkanNative.vkCreateDescriptorSetLayout(VulkanCore.logicalDevice, &descriptorSetLayoutCreateInfo, null, descriptorSetLayoutPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create ImGui descriptor set layout");
            }
        }

        VkDescriptorSetLayout* descriptorSetLayoutsPtr = stackalloc VkDescriptorSetLayout[] { descriptorSetLayout };

        VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new VkDescriptorSetAllocateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
            descriptorPool = descriptorPool,
            descriptorSetCount = 1,
            pSetLayouts = descriptorSetLayoutsPtr
        };

        fixed (VkDescriptorSet* descriptorSetPtr = &descriptorSet)
        {
            if (VulkanNative.vkAllocateDescriptorSets(VulkanCore.logicalDevice, &descriptorSetAllocateInfo, descriptorSetPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to allocate ImGui descriptor set");   
            }
        }

        VkPushConstantRange vertexPushConstant = new VkPushConstantRange()
        {
            stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT,
            offset = sizeof(float) * 0,
            size = sizeof(float) * 4
        };

        VkPushConstantRange* pushConstantsPtr = stackalloc VkPushConstantRange[] { vertexPushConstant };

        VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new VkPipelineLayoutCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
            setLayoutCount = 1,
            pSetLayouts = descriptorSetLayoutsPtr,
            pushConstantRangeCount = 1,
            pPushConstantRanges = pushConstantsPtr
        };

        fixed (VkPipelineLayout* pipelineLayoutPtr = &graphicsPipelineLayout)
        {
            if (VulkanNative.vkCreatePipelineLayout(VulkanCore.logicalDevice, &pipelineLayoutCreateInfo, null, pipelineLayoutPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create ImGui pipeline layout");
            }
        }

        VkShaderModuleCreateInfo vertexShaderModuleCreateInfo = new VkShaderModuleCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO,
            codeSize = (nuint) vertexShader.Length * sizeof(uint)
        };

        fixed (uint* vertexShaderPtr = vertexShader)
        {
            vertexShaderModuleCreateInfo.pCode = vertexShaderPtr;
        } 
        
        fixed (VkShaderModule* vertexShaderPtr = &vertexShaderModule)
        {
            if (VulkanNative.vkCreateShaderModule(VulkanCore.logicalDevice, &vertexShaderModuleCreateInfo, null, vertexShaderPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create ImGui vertex shader module");
            }
        }
        
        VkShaderModuleCreateInfo fragmentShaderModuleCreateInfo = new VkShaderModuleCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO,
            codeSize = (nuint) fragmentShader.Length * sizeof(uint),
        };

        fixed (uint* fragmentShaderPtr = fragmentShader)
        {
            fragmentShaderModuleCreateInfo.pCode = fragmentShaderPtr;
        }
        
        fixed (VkShaderModule* fragmentShaderPtr = &fragmentShaderModule)
        {
            if (VulkanNative.vkCreateShaderModule(VulkanCore.logicalDevice, &fragmentShaderModuleCreateInfo, null, fragmentShaderPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create ImGui fragment shader module");
            }
        }

        VkPipelineShaderStageCreateInfo* pipelineShaderStageCreateInfosPtr = stackalloc VkPipelineShaderStageCreateInfo[2];
        
        pipelineShaderStageCreateInfosPtr[0].sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
        pipelineShaderStageCreateInfosPtr[0].stage = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT;
        pipelineShaderStageCreateInfosPtr[0].module = vertexShaderModule;
        pipelineShaderStageCreateInfosPtr[0].pName = "main".ToPointer();
        
        pipelineShaderStageCreateInfosPtr[1].sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
        pipelineShaderStageCreateInfosPtr[1].stage = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;
        pipelineShaderStageCreateInfosPtr[1].module = fragmentShaderModule;
        pipelineShaderStageCreateInfosPtr[1].pName = "main".ToPointer();

        VkVertexInputBindingDescription vertexInputBindingDescription = new VkVertexInputBindingDescription()
        {
            stride = (uint)Marshal.SizeOf(typeof(ImDrawVert)),
            inputRate = VkVertexInputRate.VK_VERTEX_INPUT_RATE_VERTEX
        };

        VkVertexInputAttributeDescription* attributeDescriptionsPtr = stackalloc VkVertexInputAttributeDescription[3];

        attributeDescriptionsPtr[0].location = 0;
        attributeDescriptionsPtr[0].binding = vertexInputBindingDescription.binding;
        attributeDescriptionsPtr[0].format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT;
        attributeDescriptionsPtr[0].offset = (uint) Marshal.OffsetOf(typeof(ImDrawVert), nameof(ImDrawVert.pos));

        attributeDescriptionsPtr[1].location = 1;
        attributeDescriptionsPtr[1].binding = vertexInputBindingDescription.binding;
        attributeDescriptionsPtr[1].format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT;
        attributeDescriptionsPtr[1].offset = (uint) Marshal.OffsetOf(typeof(ImDrawVert), nameof(ImDrawVert.uv));

        attributeDescriptionsPtr[2].location = 2;
        attributeDescriptionsPtr[2].binding = vertexInputBindingDescription.binding;
        attributeDescriptionsPtr[2].format = VkFormat.VK_FORMAT_R8G8B8_UNORM;
        attributeDescriptionsPtr[2].offset = (uint) Marshal.OffsetOf(typeof(ImDrawVert), nameof(ImDrawVert.col));

        VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new VkPipelineVertexInputStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO,
            vertexAttributeDescriptionCount = 3,
            pVertexBindingDescriptions = &vertexInputBindingDescription,
            vertexBindingDescriptionCount = 1,
            pVertexAttributeDescriptions = attributeDescriptionsPtr
        };

        VkPipelineInputAssemblyStateCreateInfo assemblyStateCreateInfo = new VkPipelineInputAssemblyStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO,
            topology = VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST
        };

        VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new VkPipelineViewportStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO,
            viewportCount = 1,
            scissorCount = 1
        };

        VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new VkPipelineRasterizationStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO,
            polygonMode = VkPolygonMode.VK_POLYGON_MODE_FILL,
            cullMode = VkCullModeFlags.VK_CULL_MODE_NONE,
            frontFace = VkFrontFace.VK_FRONT_FACE_COUNTER_CLOCKWISE,
            lineWidth = 1.0f
        };

        VkPipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new VkPipelineMultisampleStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO,
            rasterizationSamples = msaaSampleCount
        };

        VkPipelineColorBlendAttachmentState colorBlendAttachmentState = new VkPipelineColorBlendAttachmentState()
        {
            blendEnable = VkBool32.True,
            srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA,
            dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA,
            colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
            srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
            dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA,
            alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
            colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                             VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT
        };

        VkPipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new VkPipelineDepthStencilStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO
        };

        VkPipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new VkPipelineColorBlendStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO,
            attachmentCount = 1,
            pAttachments = &colorBlendAttachmentState
        };

        VkDynamicState* dynamicStatesPtr = stackalloc VkDynamicState[] { VkDynamicState.VK_DYNAMIC_STATE_VIEWPORT, VkDynamicState.VK_DYNAMIC_STATE_SCISSOR };

        VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new VkPipelineDynamicStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO,
            dynamicStateCount = 2,
            pDynamicStates = dynamicStatesPtr
        };

        VkGraphicsPipelineCreateInfo graphicsPipelineCreateInfo = new VkGraphicsPipelineCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO,
            stageCount = 2,
            pStages = pipelineShaderStageCreateInfosPtr,
            pVertexInputState = &vertexInputStateCreateInfo,
            pInputAssemblyState = &assemblyStateCreateInfo,
            pViewportState = &viewportStateCreateInfo,
            pRasterizationState = &rasterizationStateCreateInfo,
            pMultisampleState = &multisampleStateCreateInfo,
            pDepthStencilState = &depthStencilStateCreateInfo,
            pColorBlendState = &colorBlendStateCreateInfo,
            pDynamicState = &dynamicStateCreateInfo,
            layout = graphicsPipelineLayout,
            renderPass = renderPass,
            subpass = 0
        };

        fixed (VkPipeline* graphicsPipelinePtr = &graphicsPipeline)
        {
            if (VulkanNative.vkCreateGraphicsPipelines(VulkanCore.logicalDevice, VkPipelineCache.Null, 1, &graphicsPipelineCreateInfo, null, graphicsPipelinePtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create ImGui graphics pipeline");
            }
        }
        
        Marshal.FreeHGlobal((IntPtr) pipelineShaderStageCreateInfosPtr[0].pName);
        Marshal.FreeHGlobal((IntPtr) pipelineShaderStageCreateInfosPtr[1].pName);

        // Initialise ImGui Vulkan adapter
        var io = ImGuiNET.ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height);
        var uploadSize = (ulong) (width * height * 4 * sizeof(byte));
        VkCommandBuffer commandBuffer = VulkanUtilities.BeginSingleTimeCommands();
        
        VulkanUtilities.CreateImage(
            (uint) width, (uint) height, 1,
            VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT, VkFormat.VK_FORMAT_R8G8B8A8_UNORM, 
            VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
            VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT,
            out fontImage, out fontImageMemory
        );

        VulkanUtilities.CreateImageView(
            fontImage, VkFormat.VK_FORMAT_R8G8B8A8_UNORM, 
            VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT, 1,
            out fontImageView
        );

        VkDescriptorImageInfo descriptorImageInfo = new VkDescriptorImageInfo()
        {
            sampler = fontSampler,
            imageView = fontImageView,
            imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
        };

        VkWriteDescriptorSet writeDescriptorSet = new VkWriteDescriptorSet()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            descriptorCount = 1,
            dstSet = descriptorSet,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            pImageInfo = &descriptorImageInfo
        };

        VulkanNative.vkUpdateDescriptorSets(VulkanCore.logicalDevice, 1, &writeDescriptorSet, 0, null);

        VulkanUtilities.CreateBuffer(
            uploadSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, 
            out var uploadBuffer, out var uploadBufferMemory);

        void* data;
        VulkanNative.vkMapMemory(VulkanCore.logicalDevice, uploadBufferMemory, 0, uploadSize, 0, &data);
        Unsafe.CopyBlock(data, pixels.ToPointer(), (uint) uploadSize);

        VkMappedMemoryRange mappedMemoryRange = new VkMappedMemoryRange()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_MAPPED_MEMORY_RANGE,
            memory = uploadBufferMemory,
            size = uploadSize
        };

        if (VulkanNative.vkFlushMappedMemoryRanges(VulkanCore.logicalDevice, 1, &mappedMemoryRange) != VkResult.VK_SUCCESS)
        {
            VulkanDebugger.ThrowError("Failed to flush ImGui mapped memory range");
        }
        
        VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, uploadBufferMemory);

        const uint VK_QUEUE_FAMILY_IGNORED = ~0U;

        VkImageMemoryBarrier imageCopyBarrier = new VkImageMemoryBarrier()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
            dstAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT,
            oldLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            newLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
            srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            image = fontImage,
            subresourceRange = new VkImageSubresourceRange()
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                layerCount = 1,
                levelCount = 1
            }
        };
        
        VulkanNative.vkCmdPipelineBarrier(
            commandBuffer, VkPipelineStageFlags.VK_PIPELINE_STAGE_HOST_BIT, 
            VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT, 
            0, 0, null, 0, 
            null, 1, &imageCopyBarrier
        );

        VkBufferImageCopy imageCopyRegion = new VkBufferImageCopy()
        {
            imageSubresource = new VkImageSubresourceLayers()
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                layerCount = 1
            },
            imageExtent = new VkExtent3D()
            {
                width = (uint)width,
                height = (uint)height,
                depth = 1
            }
        };
        
        VulkanNative.vkCmdCopyBufferToImage(
            commandBuffer, uploadBuffer, fontImage,
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, 1, &imageCopyRegion
        );

        VkImageMemoryBarrier imageBarrier = new VkImageMemoryBarrier()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
            srcAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT,
            dstAccessMask = VkAccessFlags.VK_ACCESS_SHADER_READ_BIT,
            oldLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
            newLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
            srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            image = fontImage,
            subresourceRange = new VkImageSubresourceRange()
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                layerCount = 1,
                levelCount = 1
            }
        };
        
        
        VulkanNative.vkCmdPipelineBarrier(
            commandBuffer, VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT, 
            VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT, 0, 
            0, null, 0, null,
            1, &imageBarrier
        );
        
        // Store our identifier
        io.Fonts.SetTexID((IntPtr) fontImage.Handle);
        
        VulkanUtilities.EndSingleTimeCommands(commandBuffer);

        VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, uploadBuffer, default);
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, uploadBufferMemory, default);
    }

    private void SetKeyMappings()
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.KeyMap[(int) ImGuiKey.Tab] = (int) Key.Tab;
        io.KeyMap[(int) ImGuiKey.LeftArrow] = (int) Key.Left;
        io.KeyMap[(int) ImGuiKey.RightArrow] = (int) Key.Right;
        io.KeyMap[(int) ImGuiKey.UpArrow] = (int) Key.Up;
        io.KeyMap[(int) ImGuiKey.DownArrow] = (int) Key.Down;
        io.KeyMap[(int) ImGuiKey.PageUp] = (int) Key.PageUp;
        io.KeyMap[(int) ImGuiKey.PageDown] = (int) Key.PageDown;
        io.KeyMap[(int) ImGuiKey.Home] = (int) Key.Home;
        io.KeyMap[(int) ImGuiKey.End] = (int) Key.End;
        io.KeyMap[(int) ImGuiKey.Delete] = (int) Key.Delete;
        io.KeyMap[(int) ImGuiKey.Backspace] = (int) Key.Backspace;
        io.KeyMap[(int) ImGuiKey.Enter] = (int) Key.Enter;
        io.KeyMap[(int) ImGuiKey.Escape] = (int) Key.Escape;
        
        io.KeyMap[(int) ImGuiKey.LeftShift] = (int) Key.LeftShift;
        io.KeyMap[(int) ImGuiKey.RightShift] = (int) Key.RightShift;
        io.KeyMap[(int) ImGuiKey.LeftAlt] = (int) Key.LeftAlt;
        io.KeyMap[(int) ImGuiKey.RightAlt] = (int) Key.RightAlt;
        io.KeyMap[(int) ImGuiKey.CapsLock] = (int) Key.CapsLock;
        io.KeyMap[(int) ImGuiKey.LeftSuper] = (int) Key.LeftSuper;
        
        io.KeyMap[(int) ImGuiKey.Equal] = (int) Key.Equal;
        io.KeyMap[(int) ImGuiKey.Minus] = (int) Key.Minus;
        
        io.KeyMap[(int) ImGuiKey.LeftBracket] = (int) Key.LeftBracket;
        io.KeyMap[(int) ImGuiKey.RightBracket] = (int) Key.RightBracket;
        io.KeyMap[(int) ImGuiKey.Semicolon] = (int) Key.RightBracket;
        
        io.KeyMap[(int) ImGuiKey.Apostrophe] = (int) Key.Apostrophe;
        io.KeyMap[(int) ImGuiKey.Slash] = (int) Key.Slash;
        io.KeyMap[(int) ImGuiKey.Backslash] = (int) Key.Backslash;
        io.KeyMap[(int) ImGuiKey.Comma] = (int) Key.Comma;
        io.KeyMap[(int) ImGuiKey.Period] = (int) Key.Period;
        
        io.KeyMap[(int) ImGuiKey.A] = (int) Key.A;
        io.KeyMap[(int) ImGuiKey.B] = (int) Key.B;
        io.KeyMap[(int) ImGuiKey.C] = (int) Key.C;
        io.KeyMap[(int) ImGuiKey.D] = (int) Key.D;
        io.KeyMap[(int) ImGuiKey.E] = (int) Key.E;
        io.KeyMap[(int) ImGuiKey.F] = (int) Key.F;
        io.KeyMap[(int) ImGuiKey.G] = (int) Key.G;
        io.KeyMap[(int) ImGuiKey.H] = (int) Key.H;
        io.KeyMap[(int) ImGuiKey.I] = (int) Key.I;
        io.KeyMap[(int) ImGuiKey.J] = (int) Key.J;
        io.KeyMap[(int) ImGuiKey.K] = (int) Key.K;
        io.KeyMap[(int) ImGuiKey.L] = (int) Key.L;
        io.KeyMap[(int) ImGuiKey.M] = (int) Key.M;
        io.KeyMap[(int) ImGuiKey.N] = (int) Key.N;
        io.KeyMap[(int) ImGuiKey.O] = (int) Key.O;
        io.KeyMap[(int) ImGuiKey.P] = (int) Key.P;
        io.KeyMap[(int) ImGuiKey.Q] = (int) Key.Q;
        io.KeyMap[(int) ImGuiKey.R] = (int) Key.R;
        io.KeyMap[(int) ImGuiKey.S] = (int) Key.S;
        io.KeyMap[(int) ImGuiKey.T] = (int) Key.T;
        io.KeyMap[(int) ImGuiKey.U] = (int) Key.U;
        io.KeyMap[(int) ImGuiKey.V] = (int) Key.V;
        io.KeyMap[(int) ImGuiKey.W] = (int) Key.W;
        io.KeyMap[(int) ImGuiKey.X] = (int) Key.X;
        io.KeyMap[(int) ImGuiKey.Y] = (int) Key.Y;
        io.KeyMap[(int) ImGuiKey.Z] = (int) Key.Z;
        
        io.KeyMap[(int) ImGuiKey.Keypad0] = (int) Key.Keypad0;
        io.KeyMap[(int) ImGuiKey.Keypad1] = (int) Key.Keypad1;
        io.KeyMap[(int) ImGuiKey.Keypad2] = (int) Key.Keypad2;
        io.KeyMap[(int) ImGuiKey.Keypad3] = (int) Key.Keypad3;
        io.KeyMap[(int) ImGuiKey.Keypad4] = (int) Key.Keypad4;
        io.KeyMap[(int) ImGuiKey.Keypad5] = (int) Key.Keypad5;
        io.KeyMap[(int) ImGuiKey.Keypad6] = (int) Key.Keypad6;
        io.KeyMap[(int) ImGuiKey.Keypad7] = (int) Key.Keypad7;
        io.KeyMap[(int) ImGuiKey.Keypad8] = (int) Key.Keypad8;
        io.KeyMap[(int) ImGuiKey.Keypad9] = (int) Key.Keypad9;
        
        io.KeyMap[(int) ImGuiKey.F1] = (int) Key.F1;
        io.KeyMap[(int) ImGuiKey.F2] = (int) Key.F2;
        io.KeyMap[(int) ImGuiKey.F3] = (int) Key.F3;
        io.KeyMap[(int) ImGuiKey.F4] = (int) Key.F4;
        io.KeyMap[(int) ImGuiKey.F5] = (int) Key.F5;
        io.KeyMap[(int) ImGuiKey.F6] = (int) Key.F6;
        io.KeyMap[(int) ImGuiKey.F7] = (int) Key.F7;
        io.KeyMap[(int) ImGuiKey.F8] = (int) Key.F8;
        io.KeyMap[(int) ImGuiKey.F9] = (int) Key.F9;
        io.KeyMap[(int) ImGuiKey.F10] = (int) Key.F10;
        io.KeyMap[(int) ImGuiKey.F11] = (int) Key.F11;
        io.KeyMap[(int) ImGuiKey.F12] = (int) Key.F12;
    }
    
    private void SetPerFrameImGuiData()
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.DisplaySize = new Vector2(windowWidth, windowHeight);

        if (windowWidth > 0 && windowHeight > 0)
        {
            io.DisplayFramebufferScale = new Vector2(VulkanCore.swapchainExtent.width / (float) windowWidth,
                VulkanCore.swapchainExtent.height / (float) windowHeight);
        }
        
        io.DeltaTime = Time.deltaTime; // DeltaTime is in seconds.
    }
    
    
    public void Update()
    {
        if (frameBegun) ImGuiNET.ImGui.Render();

        SetPerFrameImGuiData();
        UpdateImGuiInput();

        frameBegun = true;
        ImGuiNET.ImGui.NewFrame();
    }
    
    public void Render(in VkCommandBuffer commandBuffer, in VkFramebuffer framebuffer, in VkExtent2D swapChainExtent)
    {
        if (frameBegun)
        {
            frameBegun = false;
            ImGuiNET.ImGui.Render();
            RenderImDrawData(ImGuiNET.ImGui.GetDrawData(), commandBuffer);
        }
    }

    public void ResizeImGui()
    {
        windowWidth = VulkanCore.window.width;
        windowHeight = VulkanCore.window.height;
    }
    
    private void UpdateImGuiInput()
    {
        ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
        
        for (int i = 256; i <= 314; i++)
        {
            io.KeysDown[i] = Input.GetKeyHeld((Key) i);
        }
        
        for (int i = 340; i <= 348; i++)
        {
            io.KeysDown[i] = Input.GetKeyHeld((Key) i);
        }
        
        io.KeysDown[65] = Input.GetKeyHeld((Key) 65);
        io.KeysDown[67] = Input.GetKeyHeld((Key) 67);
        io.KeysDown[86] = Input.GetKeyHeld((Key) 86);
        io.KeysDown[88] = Input.GetKeyHeld((Key) 88);
        io.KeysDown[89] = Input.GetKeyHeld((Key) 89);
        io.KeysDown[90] = Input.GetKeyHeld((Key) 90);
        
        io.MouseDown[0] = Input.GetMouseButtonHeld(MouseButton.Left);
        io.MouseDown[1] = Input.GetMouseButtonHeld(MouseButton.Right);
        io.MouseDown[2] = Input.GetMouseButtonHeld(MouseButton.Middle);
        io.MouseDown[3] = Input.GetMouseButtonHeld(MouseButton.Button4);
        io.MouseDown[4] = Input.GetMouseButtonHeld(MouseButton.Button5);
        
        io.MouseWheel = Input.GetVerticalMouseScroll();
        io.MouseWheelH = Input.GetHorizontalMouseScroll();

        io.MousePos = Cursor.GetGlfwCursorPosition();
        
        io.KeyCtrl = Input.GetKeyHeld(Key.LeftControl) || Input.GetKeyHeld(Key.RightControl);
        io.KeyAlt = Input.GetKeyHeld(Key.LeftAlt) || Input.GetKeyHeld(Key.RightAlt);
        io.KeyShift = Input.GetKeyHeld(Key.LeftShift) || Input.GetKeyHeld(Key.RightShift);
        io.KeySuper = Input.GetKeyHeld(Key.LeftSuper) || Input.GetKeyPressed(Key.RightSuper);
        
        foreach (int key in Input.pressedKeys)
        {
            io.AddInputCharactersUTF8(Input.GetKeyName((Key) key, io.KeyShift));
        }
        
        Input.pressedKeys.Clear();
    }
    
    public void CleanUp()
    {
        for (uint n = 0; n < mainWindowRenderBuffers.count; n++)
        {
            VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, mainWindowRenderBuffers.frameRenderBuffers[n].vertexBuffer, null);
            VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, mainWindowRenderBuffers.frameRenderBuffers[n].vertexBufferMemory, null);
            VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, mainWindowRenderBuffers.frameRenderBuffers[n].indexBuffer, null);
            VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, mainWindowRenderBuffers.frameRenderBuffers[n].indexBufferMemory, null);
        }

        VulkanNative.vkDestroyShaderModule(VulkanCore.logicalDevice, vertexShaderModule, default);
        VulkanNative.vkDestroyShaderModule(VulkanCore.logicalDevice, fragmentShaderModule, default);
        VulkanNative.vkDestroyImageView(VulkanCore.logicalDevice, fontImageView, default);
        VulkanNative.vkDestroyImage(VulkanCore.logicalDevice, fontImage, default);
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, fontImageMemory, default);
        VulkanNative.vkDestroySampler(VulkanCore.logicalDevice, fontSampler, default);
        VulkanNative.vkDestroyDescriptorSetLayout(VulkanCore.logicalDevice, descriptorSetLayout, default);
        VulkanNative.vkDestroyPipelineLayout(VulkanCore.logicalDevice, graphicsPipelineLayout, default);
        VulkanNative.vkDestroyPipeline(VulkanCore.logicalDevice, graphicsPipeline, default);
        VulkanNative.vkDestroyDescriptorPool(VulkanCore.logicalDevice, descriptorPool, default);

        ImGuiNET.ImGui.DestroyContext();
    }
    
    private void BeginFrame()
    {
        ImGuiNET.ImGui.NewFrame();
        frameBegun = true;
    }
    
    private void RenderImDrawData(in ImDrawDataPtr drawDataPtr, in VkCommandBuffer commandBuffer)
    {
        var drawData = *drawDataPtr.NativePtr;

        // Avoid rendering when minimized, scale coordinates for retina displays (screen coordinates != framebuffer coordinates)
        var fbWidth = (int) (drawData.DisplaySize.X * drawData.FramebufferScale.X);
        var fbHeight = (int) (drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (fbWidth <= 0 || fbHeight <= 0) return;
        
        // Allocate array to store enough vertex/index buffers
        if (mainWindowRenderBuffers.frameRenderBuffers == null)
        {
            mainWindowRenderBuffers.index = 0;
            mainWindowRenderBuffers.count = swapchainImageCount;
            frameRenderBuffers =
                GlobalMemory.Allocate(sizeof(FrameRenderBuffer) * (int) mainWindowRenderBuffers.count);
            mainWindowRenderBuffers.frameRenderBuffers = frameRenderBuffers.AsPtr<FrameRenderBuffer>();
            for (var i = 0; i < (int) mainWindowRenderBuffers.count; i++)
            {
                mainWindowRenderBuffers.frameRenderBuffers[i].indexBuffer = VkBuffer.Null;
                mainWindowRenderBuffers.frameRenderBuffers[i].indexBufferSize = 0;
                mainWindowRenderBuffers.frameRenderBuffers[i].indexBufferMemory = VkDeviceMemory.Null;
                mainWindowRenderBuffers.frameRenderBuffers[i].vertexBuffer = VkBuffer.Null;
                mainWindowRenderBuffers.frameRenderBuffers[i].vertexBufferSize = 0;
                mainWindowRenderBuffers.frameRenderBuffers[i].vertexBufferMemory = VkDeviceMemory.Null;
            }
        }
        
        mainWindowRenderBuffers.index = (mainWindowRenderBuffers.index + 1) % mainWindowRenderBuffers.count;

        ref var frameRenderBuffer = ref mainWindowRenderBuffers.frameRenderBuffers[mainWindowRenderBuffers.index];

        if (drawData.TotalVtxCount > 0)
        {
            // Create or resize the vertex/index buffers
            var vertexSize = (ulong) drawData.TotalVtxCount * (ulong) sizeof(ImDrawVert);
            var indexSize = (ulong) drawData.TotalIdxCount * sizeof(ushort);
            if (frameRenderBuffer.vertexBuffer.Handle == default || frameRenderBuffer.vertexBufferSize < vertexSize)
                CreateOrResizeBuffer(ref frameRenderBuffer.vertexBuffer, ref frameRenderBuffer.vertexBufferMemory,
                    ref frameRenderBuffer.vertexBufferSize, vertexSize, VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT);
            if (frameRenderBuffer.indexBuffer.Handle == default || frameRenderBuffer.indexBufferSize < indexSize)
                CreateOrResizeBuffer(ref frameRenderBuffer.indexBuffer, ref frameRenderBuffer.indexBufferMemory,
                    ref frameRenderBuffer.indexBufferSize, indexSize, VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT);

            // Upload vertex/index data into a single contiguous GPU buffer
            ImDrawVert* vtxDst = null;
            ushort* idxDst = null;

            VulkanNative.vkMapMemory(VulkanCore.logicalDevice, frameRenderBuffer.vertexBufferMemory, 0, frameRenderBuffer.vertexBufferSize, 0, (void**) &vtxDst);
            VulkanNative.vkMapMemory(VulkanCore.logicalDevice, frameRenderBuffer.indexBufferMemory, 0, frameRenderBuffer.indexBufferSize, 0, (void**) &idxDst);
            
            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];
                Unsafe.CopyBlock(vtxDst, cmdList->VtxBuffer.Data.ToPointer(),
                    (uint) cmdList->VtxBuffer.Size * (uint) sizeof(ImDrawVert));
                Unsafe.CopyBlock(idxDst, cmdList->IdxBuffer.Data.ToPointer(),
                    (uint) cmdList->IdxBuffer.Size * sizeof(ushort));
                vtxDst += cmdList->VtxBuffer.Size;
                idxDst += cmdList->IdxBuffer.Size;
            }
            
            VkMappedMemoryRange* mappedMemoryRange = stackalloc VkMappedMemoryRange[2];

            mappedMemoryRange[0].sType = VkStructureType.VK_STRUCTURE_TYPE_MAPPED_MEMORY_RANGE;
            mappedMemoryRange[0].memory = frameRenderBuffer.vertexBufferMemory;
            mappedMemoryRange[0].size = UInt64.MaxValue;
            mappedMemoryRange[1].sType = VkStructureType.VK_STRUCTURE_TYPE_MAPPED_MEMORY_RANGE;
            mappedMemoryRange[1].memory = frameRenderBuffer.indexBufferMemory;
            mappedMemoryRange[1].size = UInt64.MaxValue;

            if (VulkanNative.vkFlushMappedMemoryRanges(VulkanCore.logicalDevice, 2, mappedMemoryRange) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Unable to flush memory to device");
            }
            
            VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, frameRenderBuffer.vertexBufferMemory);
            VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, frameRenderBuffer.indexBufferMemory);
        }

        // Setup desired Vulkan state
        VkDescriptorSet* descriptorSetsPtr = stackalloc VkDescriptorSet[] { descriptorSet };
        
        VulkanNative.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, graphicsPipeline);
        VulkanNative.vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, graphicsPipelineLayout, 0, 1, descriptorSetsPtr, 0,
            null);

        // Bind Vertex And Index Buffer:
        if (drawData.TotalVtxCount > 0)
        {
            VkBuffer* vertexBuffers = stackalloc VkBuffer[] { frameRenderBuffer.vertexBuffer };
            ulong currentVertexOffset = 0;
            VulkanNative.vkCmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, (ulong*) Unsafe.AsPointer(ref currentVertexOffset));
            VulkanNative.vkCmdBindIndexBuffer(commandBuffer, frameRenderBuffer.indexBuffer, 0,
                VkIndexType.VK_INDEX_TYPE_UINT16);
        }

        // Setup viewport:
        VkViewport viewport;
        viewport.x = 0;
        viewport.y = 0;
        viewport.width = fbWidth;
        viewport.height = fbHeight;
        viewport.minDepth = 0.0f;
        viewport.maxDepth = 1.0f;
        VulkanNative.vkCmdSetViewport(commandBuffer, 0, 1, &viewport);

        // Setup scale and translation:
        // Our visible ImGui space lies from draw_data.DisplayPps (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.
        float* scale = stackalloc float[2];
        scale[0] = 2.0f / drawData.DisplaySize.X;
        scale[1] = 2.0f / drawData.DisplaySize.Y;
        
        float* translate = stackalloc float[2];
        translate[0] = -1.0f - drawData.DisplayPos.X * scale[0];
        translate[1] = -1.0f - drawData.DisplayPos.Y * scale[1];
        
        VulkanNative.vkCmdPushConstants(commandBuffer, graphicsPipelineLayout, VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT, sizeof(float) * 0,
            sizeof(float) * 2, scale);
        VulkanNative.vkCmdPushConstants(commandBuffer, graphicsPipelineLayout, VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT, sizeof(float) * 2,
            sizeof(float) * 2, translate);

        // Will project scissor/clipping rectangles into framebuffer space
        var clipOff = drawData.DisplayPos; // (0,0) unless using multi-viewports
        var clipScale = drawData.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

        // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        var vertexOffset = 0;
        var indexOffset = 0;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            ref var cmdList = ref drawData.CmdLists[n];
            for (var cmdI = 0; cmdI < cmdList->CmdBuffer.Size; cmdI++)
            {
                ref var imGuiDrawCommand = ref cmdList->CmdBuffer.Ref<ImDrawCmd>(cmdI);

                // Project scissor/clipping rectangles into framebuffer space
                Vector4 clipRect;
                clipRect.X = (imGuiDrawCommand.ClipRect.X - clipOff.X) * clipScale.X;
                clipRect.Y = (imGuiDrawCommand.ClipRect.Y - clipOff.Y) * clipScale.Y;
                clipRect.Z = (imGuiDrawCommand.ClipRect.Z - clipOff.X) * clipScale.X;
                clipRect.W = (imGuiDrawCommand.ClipRect.W - clipOff.Y) * clipScale.Y;

                if (clipRect.X < fbWidth && clipRect.Y < fbHeight && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
                {
                    // Negative offsets are illegal for vkCmdSetScissor
                    if (clipRect.X < 0.0f)
                        clipRect.X = 0.0f;
                    if (clipRect.Y < 0.0f)
                        clipRect.Y = 0.0f;

                    // Apply scissor/clipping rectangle
                    var scissor = new VkRect2D();
                    scissor.offset.x = (int) clipRect.X;
                    scissor.offset.y = (int) clipRect.Y;
                    scissor.extent.width = (uint) (clipRect.Z - clipRect.X);
                    scissor.extent.height = (uint) (clipRect.W - clipRect.Y);
                    VulkanNative.vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

                    // Draw
                    VulkanNative.vkCmdDrawIndexed(commandBuffer, imGuiDrawCommand.ElemCount, 1, imGuiDrawCommand.IdxOffset + (uint) indexOffset,
                        (int) imGuiDrawCommand.VtxOffset + vertexOffset, 0);
                }
            }

            indexOffset += cmdList->IdxBuffer.Size;
            vertexOffset += cmdList->VtxBuffer.Size;
        }
    }
    
    // ReSharper disable once RedundantAssignment
    private void CreateOrResizeBuffer(ref VkBuffer deviceBuffer, ref VkDeviceMemory deviceBufferMemory, ref ulong bufferSize,
        ulong newSize, VkBufferUsageFlags usage)
    {
        if (deviceBuffer.Handle != default) VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, deviceBuffer, default);
        if (deviceBufferMemory.Handle != default) VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, deviceBufferMemory, default);

        ulong sizeAlignedVertexBuffer = ((newSize - 1) / BUFFER_MEMORY_ALIGNMENT + 1) * BUFFER_MEMORY_ALIGNMENT;

        VulkanUtilities.CreateBuffer(
            sizeAlignedVertexBuffer, usage, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT,
            out deviceBuffer, out deviceBufferMemory
        );

        VkMemoryRequirements memoryRequirements;
        VulkanNative.vkGetBufferMemoryRequirements(VulkanCore.logicalDevice, deviceBuffer, &memoryRequirements);

        bufferSize = memoryRequirements.size;
    }
    
    private readonly uint[] vertexShader =
    {
        0x07230203, 0x00010000, 0x00080001, 0x0000002e, 0x00000000, 0x00020011, 0x00000001, 0x0006000b,
        0x00000001, 0x4c534c47, 0x6474732e, 0x3035342e, 0x00000000, 0x0003000e, 0x00000000, 0x00000001,
        0x000a000f, 0x00000000, 0x00000004, 0x6e69616d, 0x00000000, 0x0000000b, 0x0000000f, 0x00000015,
        0x0000001b, 0x0000001c, 0x00030003, 0x00000002, 0x000001c2, 0x00040005, 0x00000004, 0x6e69616d,
        0x00000000, 0x00030005, 0x00000009, 0x00000000, 0x00050006, 0x00000009, 0x00000000, 0x6f6c6f43,
        0x00000072, 0x00040006, 0x00000009, 0x00000001, 0x00005655, 0x00030005, 0x0000000b, 0x0074754f,
        0x00040005, 0x0000000f, 0x6c6f4361, 0x0000726f, 0x00030005, 0x00000015, 0x00565561, 0x00060005,
        0x00000019, 0x505f6c67, 0x65567265, 0x78657472, 0x00000000, 0x00060006, 0x00000019, 0x00000000,
        0x505f6c67, 0x7469736f, 0x006e6f69, 0x00030005, 0x0000001b, 0x00000000, 0x00040005, 0x0000001c,
        0x736f5061, 0x00000000, 0x00060005, 0x0000001e, 0x73755075, 0x6e6f4368, 0x6e617473, 0x00000074,
        0x00050006, 0x0000001e, 0x00000000, 0x61635375, 0x0000656c, 0x00060006, 0x0000001e, 0x00000001,
        0x61725475, 0x616c736e, 0x00006574, 0x00030005, 0x00000020, 0x00006370, 0x00040047, 0x0000000b,
        0x0000001e, 0x00000000, 0x00040047, 0x0000000f, 0x0000001e, 0x00000002, 0x00040047, 0x00000015,
        0x0000001e, 0x00000001, 0x00050048, 0x00000019, 0x00000000, 0x0000000b, 0x00000000, 0x00030047,
        0x00000019, 0x00000002, 0x00040047, 0x0000001c, 0x0000001e, 0x00000000, 0x00050048, 0x0000001e,
        0x00000000, 0x00000023, 0x00000000, 0x00050048, 0x0000001e, 0x00000001, 0x00000023, 0x00000008,
        0x00030047, 0x0000001e, 0x00000002, 0x00020013, 0x00000002, 0x00030021, 0x00000003, 0x00000002,
        0x00030016, 0x00000006, 0x00000020, 0x00040017, 0x00000007, 0x00000006, 0x00000004, 0x00040017,
        0x00000008, 0x00000006, 0x00000002, 0x0004001e, 0x00000009, 0x00000007, 0x00000008, 0x00040020,
        0x0000000a, 0x00000003, 0x00000009, 0x0004003b, 0x0000000a, 0x0000000b, 0x00000003, 0x00040015,
        0x0000000c, 0x00000020, 0x00000001, 0x0004002b, 0x0000000c, 0x0000000d, 0x00000000, 0x00040020,
        0x0000000e, 0x00000001, 0x00000007, 0x0004003b, 0x0000000e, 0x0000000f, 0x00000001, 0x00040020,
        0x00000011, 0x00000003, 0x00000007, 0x0004002b, 0x0000000c, 0x00000013, 0x00000001, 0x00040020,
        0x00000014, 0x00000001, 0x00000008, 0x0004003b, 0x00000014, 0x00000015, 0x00000001, 0x00040020,
        0x00000017, 0x00000003, 0x00000008, 0x0003001e, 0x00000019, 0x00000007, 0x00040020, 0x0000001a,
        0x00000003, 0x00000019, 0x0004003b, 0x0000001a, 0x0000001b, 0x00000003, 0x0004003b, 0x00000014,
        0x0000001c, 0x00000001, 0x0004001e, 0x0000001e, 0x00000008, 0x00000008, 0x00040020, 0x0000001f,
        0x00000009, 0x0000001e, 0x0004003b, 0x0000001f, 0x00000020, 0x00000009, 0x00040020, 0x00000021,
        0x00000009, 0x00000008, 0x0004002b, 0x00000006, 0x00000028, 0x00000000, 0x0004002b, 0x00000006,
        0x00000029, 0x3f800000, 0x00050036, 0x00000002, 0x00000004, 0x00000000, 0x00000003, 0x000200f8,
        0x00000005, 0x0004003d, 0x00000007, 0x00000010, 0x0000000f, 0x00050041, 0x00000011, 0x00000012,
        0x0000000b, 0x0000000d, 0x0003003e, 0x00000012, 0x00000010, 0x0004003d, 0x00000008, 0x00000016,
        0x00000015, 0x00050041, 0x00000017, 0x00000018, 0x0000000b, 0x00000013, 0x0003003e, 0x00000018,
        0x00000016, 0x0004003d, 0x00000008, 0x0000001d, 0x0000001c, 0x00050041, 0x00000021, 0x00000022,
        0x00000020, 0x0000000d, 0x0004003d, 0x00000008, 0x00000023, 0x00000022, 0x00050085, 0x00000008,
        0x00000024, 0x0000001d, 0x00000023, 0x00050041, 0x00000021, 0x00000025, 0x00000020, 0x00000013,
        0x0004003d, 0x00000008, 0x00000026, 0x00000025, 0x00050081, 0x00000008, 0x00000027, 0x00000024,
        0x00000026, 0x00050051, 0x00000006, 0x0000002a, 0x00000027, 0x00000000, 0x00050051, 0x00000006,
        0x0000002b, 0x00000027, 0x00000001, 0x00070050, 0x00000007, 0x0000002c, 0x0000002a, 0x0000002b,
        0x00000028, 0x00000029, 0x00050041, 0x00000011, 0x0000002d, 0x0000001b, 0x0000000d, 0x0003003e,
        0x0000002d, 0x0000002c, 0x000100fd, 0x00010038
    };

    private readonly uint[] fragmentShader =
    {
        0x07230203, 0x00010000, 0x00080001, 0x0000001e, 0x00000000, 0x00020011, 0x00000001, 0x0006000b,
        0x00000001, 0x4c534c47, 0x6474732e, 0x3035342e, 0x00000000, 0x0003000e, 0x00000000, 0x00000001,
        0x0007000f, 0x00000004, 0x00000004, 0x6e69616d, 0x00000000, 0x00000009, 0x0000000d, 0x00030010,
        0x00000004, 0x00000007, 0x00030003, 0x00000002, 0x000001c2, 0x00040005, 0x00000004, 0x6e69616d,
        0x00000000, 0x00040005, 0x00000009, 0x6c6f4366, 0x0000726f, 0x00030005, 0x0000000b, 0x00000000,
        0x00050006, 0x0000000b, 0x00000000, 0x6f6c6f43, 0x00000072, 0x00040006, 0x0000000b, 0x00000001,
        0x00005655, 0x00030005, 0x0000000d, 0x00006e49, 0x00050005, 0x00000016, 0x78655473, 0x65727574,
        0x00000000, 0x00040047, 0x00000009, 0x0000001e, 0x00000000, 0x00040047, 0x0000000d, 0x0000001e,
        0x00000000, 0x00040047, 0x00000016, 0x00000022, 0x00000000, 0x00040047, 0x00000016, 0x00000021,
        0x00000000, 0x00020013, 0x00000002, 0x00030021, 0x00000003, 0x00000002, 0x00030016, 0x00000006,
        0x00000020, 0x00040017, 0x00000007, 0x00000006, 0x00000004, 0x00040020, 0x00000008, 0x00000003,
        0x00000007, 0x0004003b, 0x00000008, 0x00000009, 0x00000003, 0x00040017, 0x0000000a, 0x00000006,
        0x00000002, 0x0004001e, 0x0000000b, 0x00000007, 0x0000000a, 0x00040020, 0x0000000c, 0x00000001,
        0x0000000b, 0x0004003b, 0x0000000c, 0x0000000d, 0x00000001, 0x00040015, 0x0000000e, 0x00000020,
        0x00000001, 0x0004002b, 0x0000000e, 0x0000000f, 0x00000000, 0x00040020, 0x00000010, 0x00000001,
        0x00000007, 0x00090019, 0x00000013, 0x00000006, 0x00000001, 0x00000000, 0x00000000, 0x00000000,
        0x00000001, 0x00000000, 0x0003001b, 0x00000014, 0x00000013, 0x00040020, 0x00000015, 0x00000000,
        0x00000014, 0x0004003b, 0x00000015, 0x00000016, 0x00000000, 0x0004002b, 0x0000000e, 0x00000018,
        0x00000001, 0x00040020, 0x00000019, 0x00000001, 0x0000000a, 0x00050036, 0x00000002, 0x00000004,
        0x00000000, 0x00000003, 0x000200f8, 0x00000005, 0x00050041, 0x00000010, 0x00000011, 0x0000000d,
        0x0000000f, 0x0004003d, 0x00000007, 0x00000012, 0x00000011, 0x0004003d, 0x00000014, 0x00000017,
        0x00000016, 0x00050041, 0x00000019, 0x0000001a, 0x0000000d, 0x00000018, 0x0004003d, 0x0000000a,
        0x0000001b, 0x0000001a, 0x00050057, 0x00000007, 0x0000001c, 0x00000017, 0x0000001b, 0x00050085,
        0x00000007, 0x0000001d, 0x00000012, 0x0000001c, 0x0003003e, 0x00000009, 0x0000001d, 0x000100fd,
        0x00010038
    };

    private struct FrameRenderBuffer
    {
        public VkDeviceMemory vertexBufferMemory;
        public VkDeviceMemory indexBufferMemory;
        public ulong vertexBufferSize;
        public ulong indexBufferSize;
        public VkBuffer vertexBuffer;
        public VkBuffer indexBuffer;
    }

    private struct WindowRenderBuffers
    {
        public uint index;
        public uint count;
        public FrameRenderBuffer* frameRenderBuffers;
    }
}