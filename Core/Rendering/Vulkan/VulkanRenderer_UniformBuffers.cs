using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan.Abstractions;
using SierraEngine.Engine.Components;
using Buffer = SierraEngine.Core.Rendering.Vulkan.Abstractions.Buffer;

namespace SierraEngine.Core.Rendering.Vulkan;

#pragma warning disable CS0169
public struct UniformDirectionalLight
{
    public Vector3 direction;
    public float intensity;
    
    public Vector3 color;
    private readonly float _align2_;
    
    public Vector3 ambient;
    private readonly float _align3_;
    
    public Vector3 diffuse;
    private readonly float _align4_;
    
    public Vector3 specular;
    private readonly float _align5_;
}

public struct UniformPointLight
{
    public Vector3 position;
    private readonly float _align1_;

    public Vector3 color;   
    public float intensity;

    public Vector3 ambient;
    private readonly float _align2_;    
    
    public Vector3 diffuse;
    public float linear;
        
    public Vector3 specular;
    public float quadratic;
}
#pragma warning restore CS0169

[StructLayout(LayoutKind.Sequential)]
public struct UniformData
{
    /* Vertex Uniform Data */
    public Matrix4x4 view;
    public Matrix4x4 projection;
        
    /* Fragment Uniform Data */
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = World.MAX_DIRECTIONAL_LIGHTS)]
    public UniformDirectionalLight[] directionalLights;
    
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = World.MAX_POINT_LIGHTS)]
    public UniformPointLight[] pointLights;

    public int pointLightsCount;
    public int directionalLightsCount;
}

public unsafe partial class VulkanRenderer
{
    public UniformData uniformData;
    private readonly ulong uniformDataSize = (ulong) Marshal.SizeOf(typeof(UniformData));
    
    private Buffer[] uniformBuffers = null!;
    
    private void CreateUniformBuffers()
    {
        // Resize the uniformBuffers and its memories arrays
        uniformBuffers = new Buffer[MAX_CONCURRENT_FRAMES];

        // Create uniform arrays
        uniformData.pointLights = new UniformPointLight[World.MAX_POINT_LIGHTS];
        uniformData.directionalLights = new UniformDirectionalLight[World.MAX_DIRECTIONAL_LIGHTS];

        // For each concurrent frame
        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            new Buffer.Builder()
                .SetMemorySize<UniformData>()
                .SetMemoryFlags(VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
                .SetUsageFlags(VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT)
            .Build(out uniformBuffers[i]);
        }
    }

    private void UpdateUniformBuffers(in uint imageIndex)
    {
        uniformBuffers[imageIndex].CopyStruct(uniformData);
    }
}