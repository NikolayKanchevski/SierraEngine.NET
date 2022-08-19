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
    private ulong _bufferMemoryAlignment = 256;
    private GlobalMemory frameRenderBuffers = null!;

    private VkDescriptorPool descriptorPool;
    private VkSampleCountFlags msaaSampleCount;
    private VkRenderPass renderPass;
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
    
    public ImGuiController(in Window window, in uint swapchainImageCount, in VkFormat swapchainImageFormat, in VkFormat depthBufferFormat, in VkSampleCountFlags sampleCountFlags)
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
        
        Init(in window, swapchainImageCount, swapchainImageFormat, depthBufferFormat);

        SetKeyMappings();

        SetPerFrameImGuiData();

        BeginFrame();
    }

    private void Init(in Window window, in uint swapChainImageCount, in VkFormat swapchainImageFormat, in VkFormat depthBufferFormat)
    {
        // this.view = view;
        // _input = input;
        windowWidth = window.width;
        windowHeight = window.height;
        swapchainImageCount = swapChainImageCount;

        if (swapchainImageCount < 2) throw new Exception("Swapchain image count must be >= 2");

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
        
        VkAttachmentDescription colorAttachment = new VkAttachmentDescription()
        {
            format = swapchainImageFormat,
            samples = msaaSampleCount,
            loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_LOAD,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
            stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, 
            finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
        };

        VkAttachmentReference colorAttachmentReference = new VkAttachmentReference()
        {
            attachment = 0,
            layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
        };

        VkAttachmentDescription depthAttachment = new VkAttachmentDescription()
        {
            format = depthBufferFormat,
            samples = msaaSampleCount,
            loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_LOAD,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
            stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL
        };
        
        VkAttachmentReference depthAttachmentReference = new VkAttachmentReference()
        {
            attachment = 1,
            layout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL
        };
        
        VkAttachmentDescription colorAttachmentResolve = new VkAttachmentDescription()
        {
            format = swapchainImageFormat,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
            stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR
        };

        VkAttachmentReference colorAttachmentResolveReference = new VkAttachmentReference()
        {
            attachment = 2,
            layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
        };

        VkAttachmentReference* resolveAttachmentReferencesPtr = stackalloc VkAttachmentReference[] { colorAttachmentResolveReference };

        VkAttachmentReference* attachmentReferences = stackalloc VkAttachmentReference[] { colorAttachmentReference };

        VkSubpassDescription subpassDescription = new VkSubpassDescription()
        {
            pipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
            colorAttachmentCount = 1,
            pColorAttachments = attachmentReferences,
            pDepthStencilAttachment = &depthAttachmentReference,
            pResolveAttachments = resolveAttachmentReferencesPtr
        };
        
        VkAttachmentDescription* attachmentDescriptionsPtr = stackalloc VkAttachmentDescription[] { colorAttachment, depthAttachment, colorAttachmentResolve };

        VkSubpassDependency subpassDependency = new VkSubpassDependency()
        {
            srcSubpass = ~0U,
            dstSubpass = 0,
            srcStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
            srcAccessMask = 0,
            dstStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
            dstAccessMask = VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_READ_BIT | VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT
        };

        VkRenderPassCreateInfo renderPassCreateInfo = new VkRenderPassCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO,
            attachmentCount = 3,
            pAttachments = attachmentDescriptionsPtr,
            subpassCount = 1,
            pSubpasses = &subpassDescription,
            dependencyCount = 1,
            pDependencies = &subpassDependency
        };

        fixed (VkRenderPass* renderPassPtr = &renderPass)
        {
            if (VulkanNative.vkCreateRenderPass(VulkanCore.logicalDevice, &renderPassCreateInfo, null, renderPassPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create ImGui render pass");
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
            codeSize = (nuint) VERTEX_SHADER.Length * sizeof(uint)
        };

        fixed (uint* vertexShaderPtr = VERTEX_SHADER)
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
            codeSize = (nuint) FRAGMENT_SHADER.Length * sizeof(uint),
        };

        fixed (uint* fragmentShaderPtr = FRAGMENT_SHADER)
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
        
        VkBuffer uploadBuffer;
        VkDeviceMemory uploadBufferMemory;
        
        VulkanUtilities.CreateBuffer(
            uploadSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, 
            out uploadBuffer, out uploadBufferMemory);

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
        io.KeyMap[(int) ImGuiKey.A] = (int) Key.A;
        io.KeyMap[(int) ImGuiKey.C] = (int) Key.C;
        io.KeyMap[(int) ImGuiKey.V] = (int) Key.V;
        io.KeyMap[(int) ImGuiKey.X] = (int) Key.X;
        io.KeyMap[(int) ImGuiKey.Y] = (int) Key.Y;
        io.KeyMap[(int) ImGuiKey.Z] = (int) Key.Z;
    }
    
    private void SetPerFrameImGuiData()
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.DisplaySize = new Vector2(windowWidth, windowHeight);

        if (windowWidth > 0 && windowHeight > 0)
            // io.DisplayFramebufferScale = new Vector2(_view.FramebufferSize.X / windowWidth,
            //     _view.FramebufferSize.Y / windowHeight);
        
            io.DisplayFramebufferScale = new Vector2(VulkanCore.swapchainExtent.width / (float) windowWidth,
                VulkanCore.swapchainExtent.height / (float) windowHeight);
            

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
            RenderImDrawData(ImGuiNET.ImGui.GetDrawData(), commandBuffer, framebuffer, swapChainExtent);
        }
    }

    public void ResizeImGui()
    {
        windowWidth = VulkanCore.window.width;
        windowHeight = VulkanCore.window.height;
    }
    
    private void UpdateImGuiInput()
    {
        var io = ImGuiNET.ImGui.GetIO();

        // var mouseState = _input.Mice[0].CaptureState();
        // var keyboardState = _input.Keyboards[0];
        //
        // io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
        // io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
        // io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);
        //
        // var point = new Point((int) mouseState.Position.X, (int) mouseState.Position.Y);
        // io.MousePos = new Vector2(point.X, point.Y);
        //
        // var wheel = mouseState.GetScrollWheels()[0];
        // io.MouseWheel = wheel.Y;
        // io.MouseWheelH = wheel.X;

        // foreach (Key key in Enum.GetValues(typeof(Key)))
        // {
        //     if (key == Key.Unknown) continue;
        //     io.KeysDown[(int) key] = keyboardState.IsKeyPressed(key);
        // }
        //
        // foreach (var c in _pressedChars) io.AddInputCharacter(c);
        //
        // _pressedChars.Clear();

        io.MouseDown[0] = Input.GetKeyHeld(Key.J);
        io.MouseDown[1] = Input.GetKeyPressed(Key.K);
        io.MouseDown[2] = Input.GetKeyPressed(Key.L);

        io.MousePos = Cursor.GetGlfwCursorPosition();
        
        io.KeyCtrl = Input.GetKeyPressed(Key.LeftControl) || Input.GetKeyPressed(Key.RightControl);
        io.KeyAlt = Input.GetKeyPressed(Key.LeftAlt) || Input.GetKeyPressed(Key.RightAlt);
        io.KeyShift = Input.GetKeyPressed(Key.LeftShift) || Input.GetKeyPressed(Key.RightShift);
        io.KeySuper = Input.GetKeyPressed(Key.LeftSuper) || Input.GetKeyPressed(Key.RightSuper);
    }
    
    public void CleanUp()
    {
        // _view.Resize -= WindowResized;
        // _keyboard.KeyChar -= OnKeyChar;
        
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
        VulkanNative.vkDestroyRenderPass(VulkanCore.logicalDevice, renderPass, default);

        ImGuiNET.ImGui.DestroyContext();
    }
    
    private void BeginFrame()
    {
        ImGuiNET.ImGui.NewFrame();
        frameBegun = true;
        // _keyboard = _input.Keyboards[0];
        // _view.Resize += WindowResized;
        // _keyboard.KeyChar += OnKeyChar;
    }
    
    private void RenderImDrawData(in ImDrawDataPtr drawDataPtr, in VkCommandBuffer commandBuffer,
        in VkFramebuffer framebuffer, in VkExtent2D swapchainExtent)
    {
        var framebufferWidth = (int) (drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
        var framebufferHeight = (int) (drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
        if (framebufferWidth <= 0 || framebufferHeight <= 0) return;

        VkRenderPassBeginInfo renderPassBeginInfo = new VkRenderPassBeginInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO,
            renderPass = renderPass,
            framebuffer = framebuffer,
            renderArea = new VkRect2D()
            {
                extent = swapchainExtent
            },
            clearValueCount = 0
        };
        
        VulkanNative.vkCmdBeginRenderPass(commandBuffer, &renderPassBeginInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);
        
        var drawData = *drawDataPtr.NativePtr;

        // Avoid rendering when minimized, scale coordinates for retina displays (screen coordinates != framebuffer coordinates)
        var fb_width = (int) (drawData.DisplaySize.X * drawData.FramebufferScale.X);
        var fb_height = (int) (drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (fb_width <= 0 || fb_height <= 0) return;
        
        // Allocate array to store enough vertex/index buffers
        if (mainWindowRenderBuffers.frameRenderBuffers == null)
        {
            mainWindowRenderBuffers.index = 0;
            mainWindowRenderBuffers.count = (uint) swapchainImageCount;
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
            var vertex_size = (ulong) drawData.TotalVtxCount * (ulong) sizeof(ImDrawVert);
            var index_size = (ulong) drawData.TotalIdxCount * sizeof(ushort);
            if (frameRenderBuffer.vertexBuffer.Handle == default || frameRenderBuffer.vertexBufferSize < vertex_size)
                CreateOrResizeBuffer(ref frameRenderBuffer.vertexBuffer, ref frameRenderBuffer.vertexBufferMemory,
                    ref frameRenderBuffer.vertexBufferSize, vertex_size, VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT);
            if (frameRenderBuffer.indexBuffer.Handle == default || frameRenderBuffer.indexBufferSize < index_size)
                CreateOrResizeBuffer(ref frameRenderBuffer.indexBuffer, ref frameRenderBuffer.indexBufferMemory,
                    ref frameRenderBuffer.indexBufferSize, index_size, VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT);

            // Upload vertex/index data into a single contiguous GPU buffer
            ImDrawVert* vtx_dst = null;
            ushort* idx_dst = null;
            if (VulkanNative.vkMapMemory(VulkanCore.logicalDevice, frameRenderBuffer.vertexBufferMemory, 0, frameRenderBuffer.vertexBufferSize, 0,
                    (void**) &vtx_dst) != VkResult.VK_SUCCESS) throw new Exception("Unable to map device memory");
            if (VulkanNative.vkMapMemory(VulkanCore.logicalDevice, frameRenderBuffer.indexBufferMemory, 0, frameRenderBuffer.indexBufferSize, 0,
                    (void**) &idx_dst) != VkResult.VK_SUCCESS) throw new Exception("Unable to map device memory");
            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmd_list = drawData.CmdLists[n];
                Unsafe.CopyBlock(vtx_dst, cmd_list->VtxBuffer.Data.ToPointer(),
                    (uint) cmd_list->VtxBuffer.Size * (uint) sizeof(ImDrawVert));
                Unsafe.CopyBlock(idx_dst, cmd_list->IdxBuffer.Data.ToPointer(),
                    (uint) cmd_list->IdxBuffer.Size * sizeof(ushort));
                vtx_dst += cmd_list->VtxBuffer.Size;
                idx_dst += cmd_list->IdxBuffer.Size;
            }
            
            VkMappedMemoryRange* mappedMemoryRange = stackalloc VkMappedMemoryRange[2];

            mappedMemoryRange[0].sType = VkStructureType.VK_STRUCTURE_TYPE_MAPPED_MEMORY_RANGE;
            mappedMemoryRange[0].memory = frameRenderBuffer.vertexBufferMemory;
            mappedMemoryRange[0].size = UInt64.MaxValue;
            mappedMemoryRange[1].sType = VkStructureType.VK_STRUCTURE_TYPE_MAPPED_MEMORY_RANGE;
            mappedMemoryRange[1].memory = frameRenderBuffer.indexBufferMemory;
            mappedMemoryRange[1].size = UInt64.MaxValue;;
            
            if (VulkanNative.vkFlushMappedMemoryRanges(VulkanCore.logicalDevice, 2, mappedMemoryRange) != VkResult.VK_SUCCESS)
                throw new Exception("Unable to flush memory to device");
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
            VkBuffer* vertex_buffers = stackalloc VkBuffer[] { frameRenderBuffer.vertexBuffer };
            ulong vertex_offset = 0;
            VulkanNative.vkCmdBindVertexBuffers(commandBuffer, 0, 1, vertex_buffers, (ulong*) Unsafe.AsPointer(ref vertex_offset));
            VulkanNative.vkCmdBindIndexBuffer(commandBuffer, frameRenderBuffer.indexBuffer, 0,
                sizeof(ushort) == 2 ? VkIndexType.VK_INDEX_TYPE_UINT16 : VkIndexType.VK_INDEX_TYPE_UINT32);
        }

        // Setup viewport:
        VkViewport viewport;
        viewport.x = 0;
        viewport.y = 0;
        viewport.width = fb_width;
        viewport.height = fb_height;
        viewport.minDepth = 0.0f;
        viewport.maxDepth = 1.0f;
        VulkanNative.vkCmdSetViewport(commandBuffer, 0, 1, &viewport);

        // Setup scale and translation:
        // Our visible imgui space lies from draw_data.DisplayPps (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.
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
            ref var cmd_list = ref drawData.CmdLists[n];
            for (var cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
            {
                ref var pcmd = ref cmd_list->CmdBuffer.Ref<ImDrawCmd>(cmd_i);

                // Project scissor/clipping rectangles into framebuffer space
                Vector4 clipRect;
                clipRect.X = (pcmd.ClipRect.X - clipOff.X) * clipScale.X;
                clipRect.Y = (pcmd.ClipRect.Y - clipOff.Y) * clipScale.Y;
                clipRect.Z = (pcmd.ClipRect.Z - clipOff.X) * clipScale.X;
                clipRect.W = (pcmd.ClipRect.W - clipOff.Y) * clipScale.Y;

                if (clipRect.X < fb_width && clipRect.Y < fb_height && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
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
                    VulkanNative.vkCmdDrawIndexed(commandBuffer, pcmd.ElemCount, 1, pcmd.IdxOffset + (uint) indexOffset,
                        (int) pcmd.VtxOffset + vertexOffset, 0);
                }
            }

            indexOffset += cmd_list->IdxBuffer.Size;
            vertexOffset += cmd_list->VtxBuffer.Size;
        }

        VulkanNative.vkCmdEndRenderPass(commandBuffer);
    }
    
    private void CreateOrResizeBuffer(ref VkBuffer deviceBuffer, ref VkDeviceMemory deviceBufferMemory, ref ulong bufferSize,
        ulong newSize, VkBufferUsageFlags usage)
    {
        if (deviceBuffer.Handle != default) VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, deviceBuffer, default);
        if (deviceBufferMemory.Handle != default) VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, deviceBufferMemory, default);

        ulong sizeAlignedVertexBuffer = ((newSize - 1) / _bufferMemoryAlignment + 1) * _bufferMemoryAlignment;

        VkBufferCreateInfo bufferCreateInfo = new VkBufferCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO,
            size = sizeAlignedVertexBuffer,
            usage = usage,
            sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE
        };

        VulkanUtilities.CreateBuffer(
            sizeAlignedVertexBuffer, usage, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT,
            out deviceBuffer, out deviceBufferMemory
        );

        VkMemoryRequirements memoryRequirements;
        VulkanNative.vkGetBufferMemoryRequirements(VulkanCore.logicalDevice, deviceBuffer, &memoryRequirements);

        bufferSize = memoryRequirements.size;
    }
    
    private readonly uint[] VERTEX_SHADER =
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

    private readonly uint[] FRAGMENT_SHADER =
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