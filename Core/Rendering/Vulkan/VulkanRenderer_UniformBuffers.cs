using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using Buffer = SierraEngine.Core.Rendering.Vulkan.Abstractions.Buffer;

namespace SierraEngine.Core.Rendering.Vulkan;

#pragma warning disable CS0169
public struct UniformDirectionalLight
{
    public Vector3 direction;
    public float intensity;
    
    public Vector3 color;
    private readonly float _align1_;
}

public struct UniformPointLight
{
    public Vector3 position;
    public float linear;

    public Vector3 color;   
    public float intensity;
        
    private readonly Vector3 _align_1;
    public float quadratic;
}

public struct UniformSpotLight
{
    public Vector3 position;
    public float radius;

    public Vector3 direction;
    public float intensity;

    public Vector3 color;
    public float linear;    
    
    private readonly Vector2 _align1_;
    public float quadratic;
    public float spreadRadius;
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
    
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = World.MAX_SPOTLIGHT_LIGHTS)]
    public UniformSpotLight[] spotLights;

    public int directionalLightsCount;
    public int pointLightsCount;
    public int spotLightsCount;
}

public partial class VulkanRenderer
{
    public UniformData uniformData;
    private readonly ulong uniformDataSize = (ulong) Marshal.SizeOf(typeof(UniformData));
    
    private Buffer[] uniformBuffers = null!;
    
    private void CreateUniformBuffers()
    {
        // Resize the uniformBuffers and its memories arrays
        uniformBuffers = new Buffer[MAX_CONCURRENT_FRAMES];

        // Create uniform arrays
        uniformData.directionalLights = new UniformDirectionalLight[World.MAX_DIRECTIONAL_LIGHTS];
        uniformData.pointLights = new UniformPointLight[World.MAX_POINT_LIGHTS];
        uniformData.spotLights = new UniformSpotLight[World.MAX_SPOTLIGHT_LIGHTS];

        // For each concurrent frame
        Parallel.For(0, MAX_CONCURRENT_FRAMES, i =>
        {
            new Buffer.Builder()
                .SetMemorySize(uniformDataSize)
                .SetMemoryFlags(VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
                .SetUsageFlags(VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT)
                .Build(out uniformBuffers[i]);
        });
    }

    private void UpdateUniformBuffers(in uint imageIndex)
    {
        uniformBuffers[imageIndex].CopyStruct(uniformData);
    }
}