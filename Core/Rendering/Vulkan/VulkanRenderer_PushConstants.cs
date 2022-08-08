using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Components;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkPushConstantRange vertPushConstantRange;
    private readonly uint meshModelSize = (uint) Marshal.SizeOf(typeof(VertexPushConstant));
    
    private void CreatePushConstants()
    {
        // Set up vertex shader's push constants
        vertPushConstantRange.stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT;
        vertPushConstantRange.offset = 0;
        vertPushConstantRange.size = meshModelSize;
    }
}