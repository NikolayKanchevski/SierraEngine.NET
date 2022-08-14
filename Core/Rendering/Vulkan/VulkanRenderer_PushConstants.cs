using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Components;

namespace SierraEngine.Core.Rendering.Vulkan;

public struct VertexPushConstant
{
    public Matrix4x4 modelMatrix;
}

public struct FragmentPushConstant
{
    // X - (bool) Proportional Scale
    public Vector4 data;
}

public unsafe partial class VulkanRenderer
{
    private VkPushConstantRange vertexPushConstantRange;
    private VkPushConstantRange fragmentPushConstantRange;
    
    private readonly uint vertexPushConstantSize = (uint) Marshal.SizeOf(typeof(VertexPushConstant));
    private readonly uint fragmentPushConstantSize = (uint) Marshal.SizeOf(typeof(FragmentPushConstant));
    
    private void CreatePushConstants()
    {
        // Set up vertex shader's push constants
        vertexPushConstantRange.stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT;
        vertexPushConstantRange.offset = 0;
        vertexPushConstantRange.size = vertexPushConstantSize;
        
        // Set up fragment shader's push constants
        fragmentPushConstantRange.stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;
        fragmentPushConstantRange.offset = vertexPushConstantSize;
        fragmentPushConstantRange.size = fragmentPushConstantSize;
    }
}