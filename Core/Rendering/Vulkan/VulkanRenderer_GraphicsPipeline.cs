using System.Diagnostics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

internal enum RenderingMode { Fill, Wireframe, Point }

public unsafe partial class VulkanRenderer
{
    private RenderingMode renderingMode = RenderingMode.Fill;
    private VkPipelineLayout graphicsPipelineLayout;
    private VkPipeline graphicsPipeline;
    
    private void CreateGraphicsPipeline()
    {
        // Create shader modules out of the read shader
        VkShaderModule vertShaderModule = CreateShaderModule("Shaders/shader.vert.spv");
        VkShaderModule fragShaderModule = CreateShaderModule("Shaders/shader.frag.spv");

        // Set vertex shader properties
        VkPipelineShaderStageCreateInfo vertShaderStageInfo = new VkPipelineShaderStageCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
            stage = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT,
            module = vertShaderModule,
            pName = "main".ToPointer()
        };

        // Set fragment shader properties
        VkPipelineShaderStageCreateInfo fragShaderStageInfo = new VkPipelineShaderStageCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
            stage = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT,
            module = fragShaderModule,
            pName = "main".ToPointer()
        };
        
        // Put each stage info in an array
        VkPipelineShaderStageCreateInfo* shaderStages = stackalloc VkPipelineShaderStageCreateInfo[] { vertShaderStageInfo, fragShaderStageInfo };

        // Set up binding description
        VkVertexInputBindingDescription bindingDescription = new VkVertexInputBindingDescription()
        {
            binding = 0,
            stride = (uint) sizeof(Vertex),
            inputRate = VkVertexInputRate.VK_VERTEX_INPUT_RATE_VERTEX
        };

        // Define the attributes to be sent to the shader
        VkVertexInputAttributeDescription* attributeDescriptions = stackalloc VkVertexInputAttributeDescription[2];

        // Set up for the "position" property
        attributeDescriptions[0].binding = 0;
        attributeDescriptions[0].location = 0;
        attributeDescriptions[0].format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT;
        attributeDescriptions[0].offset = (uint) Marshal.OffsetOf(typeof(Vertex), "position");
        
        // Set up for the "color" property
        attributeDescriptions[1].binding = 0;
        attributeDescriptions[1].location = 1;
        attributeDescriptions[1].format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT;
        attributeDescriptions[1].offset = (uint) Marshal.OffsetOf(typeof(Vertex), "color");
        
        // Set up how vertex data sent
        VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new VkPipelineVertexInputStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO,
            vertexBindingDescriptionCount = 1,
            vertexAttributeDescriptionCount = 2,
            pVertexBindingDescriptions = &bindingDescription,
            pVertexAttributeDescriptions = attributeDescriptions
        };

        // Set up assembly info
        VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new VkPipelineInputAssemblyStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO,
            topology = VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST,
            primitiveRestartEnable = VkBool32.False
        };

        VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new VkPipelineViewportStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO,
            viewportCount = 1,
            pViewports = null,
            scissorCount = 1,
            pScissors = null
        };

        // Set up rasterization
        VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new VkPipelineRasterizationStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO,
            depthClampEnable = VkBool32.False,
            rasterizerDiscardEnable = VkBool32.False,
            lineWidth = 1.0f,
            cullMode = VkCullModeFlags.VK_CULL_MODE_FRONT_BIT,
            frontFace = VkFrontFace.VK_FRONT_FACE_COUNTER_CLOCKWISE,
            depthBiasEnable = VkBool32.False
        };

        // Determine the polygon mode depending on "renderingMode"
        rasterizationStateCreateInfo.polygonMode = renderingMode switch
        {
            RenderingMode.Fill => VkPolygonMode.VK_POLYGON_MODE_FILL,
            RenderingMode.Wireframe => VkPolygonMode.VK_POLYGON_MODE_LINE,
            RenderingMode.Point => VkPolygonMode.VK_POLYGON_MODE_POINT,
            _ => rasterizationStateCreateInfo.polygonMode
        };

        // Set up multi sampling
        VkPipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new VkPipelineMultisampleStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO,
            sampleShadingEnable = VkBool32.False,
            rasterizationSamples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            minSampleShading = 1.0f,
            pSampleMask = null,
            alphaToCoverageEnable = VkBool32.False,
            alphaToOneEnable = VkBool32.False,
        };

        // Set up color blending
        VkPipelineColorBlendAttachmentState blendingAttachmentState = new VkPipelineColorBlendAttachmentState()
        {
            colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT,
            blendEnable = VkBool32.False,
            srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA,
            dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA,
            colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
            srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
            dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO,
            alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
        };

        // Set up bindings
        VkPipelineColorBlendStateCreateInfo blendingStateCreateInfo = new VkPipelineColorBlendStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO,
            logicOpEnable = VkBool32.False,
            logicOp = VkLogicOp.VK_LOGIC_OP_COPY,
            attachmentCount = 1,
            pAttachments = &blendingAttachmentState
        };

        // Define dynamic states to use
        VkDynamicState* dynamicStates = stackalloc VkDynamicState[]
        {
            VkDynamicState.VK_DYNAMIC_STATE_VIEWPORT,
            VkDynamicState.VK_DYNAMIC_STATE_SCISSOR
        };

        // Set up rasterization
        VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new VkPipelineDynamicStateCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO,
            dynamicStateCount = 2,
            pDynamicStates = dynamicStates
        };

        VkDescriptorSetLayout* descriptorSetLayoutsPtr = stackalloc VkDescriptorSetLayout[] { this.descriptorSetLayout };

        // Set pipeline layout creation info
        VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new VkPipelineLayoutCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
            setLayoutCount = 1,
            pSetLayouts = descriptorSetLayoutsPtr,
            pushConstantRangeCount = 0,
            pPushConstantRanges = null
        };

        // Create the pipeline layout
        fixed (VkPipelineLayout* pipelineLayoutPtr = &graphicsPipelineLayout)
        {
            if (VulkanNative.vkCreatePipelineLayout(this.logicalDevice, &pipelineLayoutCreateInfo, null, pipelineLayoutPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create pipeline layout");
            }
        }

        // Set up graphics pipeline creation info using all the modules created before
        VkGraphicsPipelineCreateInfo graphicsPipelineCreateInfo = new VkGraphicsPipelineCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO,
            stageCount = 2,
            pStages = shaderStages,
            pVertexInputState = &vertexInputStateCreateInfo,
            pInputAssemblyState = &inputAssemblyStateCreateInfo,
            pViewportState = &viewportStateCreateInfo,
            pRasterizationState = &rasterizationStateCreateInfo,
            pMultisampleState = &multisampleStateCreateInfo,
            pDepthStencilState = null,
            pColorBlendState = &blendingStateCreateInfo,
            pDynamicState = &dynamicStateCreateInfo,
            layout = graphicsPipelineLayout,
            renderPass = this.renderPass,
            subpass = 0,
            basePipelineHandle = VkPipeline.Null,
            basePipelineIndex = -1
        };

        // Create the graphics pipeline
        fixed (VkPipeline* graphicsPipelinePtr = &graphicsPipeline)
        {
            if (VulkanNative.vkCreateGraphicsPipelines(this.logicalDevice, VkPipelineCache.Null, 1, &graphicsPipelineCreateInfo, null, graphicsPipelinePtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create graphics pipeline");
            }
        }

        // Destroy the made shader modules
        VulkanNative.vkDestroyShaderModule(this.logicalDevice, vertShaderModule, null);
        VulkanNative.vkDestroyShaderModule(this.logicalDevice, fragShaderModule, null);
    }

    // private void CompileShaders()
    // {
    //     string shaderCompilerPath = Program.ROOT_FOLDER_PATH + "Core/Rendering/Shading/Compilers/ShaderCompiler";
    //     if (OperatingSystem.IsWindows())
    //     {
    //         shaderCompilerPath += "Windows.bat";
    //     }
    //     else if (OperatingSystem.IsMacOS())
    //     {
    //         shaderCompilerPath += "MacOS.sh";
    //
    //         ProcessStartInfo processInfo = new ProcessStartInfo()
    //         {
    //             FileName = "/bin/bash",
    //             WorkingDirectory = Program.ROOT_FOLDER_PATH + "Core/Rendering/Shading/Compilers/",
    //             Arguments = "ShaderCompilerMacOS.sh",
    //             WindowStyle = ProcessWindowStyle.Minimized,
    //             CreateNoWindow = true,
    //             UseShellExecute = false,
    //             RedirectStandardInput = true,
    //             RedirectStandardOutput = true
    //         };
    //         Process process = Process.Start(processInfo)!;
    //         process.WaitForExit();
    //     }
    //     else
    //     {
    //         throw new Exception("[-] No shader compiler found for this OS!");
    //     }
    // }
}