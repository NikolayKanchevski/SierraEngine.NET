using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Core.Rendering.Vulkan;

#pragma warning disable CS0169
public struct PushMaterial
{
    public Vector3 diffuse;
    public float shininess;
    
    public Vector3 specular;
    private readonly float _align1_;
    
    public Vector3 ambient;
    private readonly float _align2_;
}
#pragma warning restore CS0169

public struct PushConstant
{
    /* VERTEX DATA */
    public Matrix4x4 modelMatrix;
    
    /* FRAGMENT DATA */
    public PushMaterial material;
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