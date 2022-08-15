using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public struct PushConstant
{
    /* VERTEX DATA */
    public Matrix4x4 modelMatrix;
    
    /* FRAGMENT DATA */
    public float shininess;
}

public partial class VulkanRenderer
{
    private VkPushConstantRange pushConstantRange;
    private readonly uint pushConstantSize = (uint) Marshal.SizeOf(typeof(PushConstant));
    
    private void CreatePushConstants()
    {
        // Set up vertex shader's push constants
        pushConstantRange.stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;
        pushConstantRange.offset = 0;
        pushConstantRange.size = pushConstantSize;
    }
}