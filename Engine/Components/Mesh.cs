using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using SierraEngine.Core;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Components;

public unsafe class Mesh : Component
{
    public new Transform transform = new Transform();
    public string meshName = "None";
    public uint verticesCount { get; private set; }
    public uint indexCount { get; private set; }
    public int textureID { get; private set; }

    private VertexPushConstant vertexPushConstantData = new VertexPushConstant();
    
    private VkBuffer vertexBuffer;
    private VkDeviceMemory vertexBufferMemory;

    private VkBuffer indexBuffer;
    private VkDeviceMemory indexBufferMemory;

    public static Mesh CreateSphere(in float radius, in int sectorCount, in int stackCount, in int newTextureID, bool renderBackFace = false)
    {
       List<Vertex> vertices = new ();

        float x, y, z, xy;                              // vertex position
        float nx, ny, nz, lengthInv = 1.0f / radius;    // vertex normal
        float s, t;                                     // vertex texCoord

        float sectorStep = 2 * 3.141592653589793f / sectorCount;
        float stackStep = 3.141592653589793f / stackCount;
        float sectorAngle, stackAngle;

        for (int i = 0; i <= stackCount; ++i)
        {
            stackAngle = 3.141592653589793f / 2 - i * stackStep; // starting from pi/2 to -pi/2
            xy = radius * (float)Math.Cos(stackAngle); // r * cos(u)
            z = radius * (float)Math.Sin(stackAngle); // r * sin(u)

            // add (sectorCount+1) vertexPositions per stack
            // the first and last vertexPositions have same position and normal, but different tex coords
            for (int j = 0; j <= sectorCount; ++j)
            {
                sectorAngle = j * sectorStep; // starting from 0 to 2pi

                // vertex position (x, y, z)
                x = xy * (float)Math.Cos(sectorAngle); // r * cos(u) * cos(v)
                y = xy * (float)Math.Sin(sectorAngle); // r * cos(u) * sin(v)
                if (!renderBackFace) x *= -1;
                var position = new Vector3(x, y, z);

                // normalized vertex normal (nx, ny, nz)
                nx = x * lengthInv;
                ny = y * lengthInv;
                nz = z * lengthInv;
                var normal = new Vector3(nx, ny, nz);

                // vertex tex coord (s, t) range between [0, 1]
                s = (float)j / sectorCount;
                t = (float)i / stackCount;
                var textureCoordinate = new Vector2(s, t);
                
                vertices.Add(new Vertex() with
                {
                    position = position,
                    normal = normal,
                    textureCoordinates = textureCoordinate
                });
            }
        }

        List<UInt32> indices = new List<UInt32>();
        
        int k1, k2;
        for (int i = 0; i < stackCount; ++i)
        {
            k1 = i * (sectorCount + 1);     // beginning of current stack
            k2 = k1 + sectorCount + 1;      // beginning of next stack

            for (int j = 0; j < sectorCount; ++j, ++k1, ++k2)
            {
                // 2 triangles per sector excluding first and last stacks
                // k1 => k2 => k1+1
                if (i != 0)
                {
                    indices.Add((UInt32) k1);
                    indices.Add((UInt32) k2);
                    indices.Add((UInt32) k1 + 1);
                }

                // k1+1 => k2 => k2+1
                if (i != (stackCount-1))
                {
                    indices.Add((UInt32) k1 + 1);
                    indices.Add((UInt32) k2);
                    indices.Add((UInt32) k2 + 1);
                }
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), newTextureID);
    }

    public Mesh(in Vertex[] givenVertices, in UInt32[] givenIndices, int newTextureID)
    {
        // Retrieve the public values
        this.verticesCount = (uint) givenVertices.Length;
        this.indexCount = (uint) givenIndices.Length;
        this.textureID = newTextureID;

        // Create buffers
        CreateVertexBuffer(in givenVertices);
        CreateIndexBuffer(in givenIndices);
        
        // Add the mesh to the world
        World.meshes.Add(this);
    }

    public VertexPushConstant GetVertexPushConstantData()
    {
        // Update the model matrix per call
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(transform.position);
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationX(transform.rotation.X) * Matrix4x4.CreateRotationY(transform.rotation.Y) * Matrix4x4.CreateRotationZ(transform.rotation.Z);
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(transform.scale);
        vertexPushConstantData.modelMatrix = translationMatrix * rotationMatrix * scaleMatrix;

        // Set proportional scale bool
        // vertexPushConstantData.proportionalScale = transform.scale.X == transform.scale.Y && transform.scale.Y == transform.scale.Z && transform.scale.Z == transform.scale.X;
        
        // Return it
        return this.vertexPushConstantData;
    }

    public VkBuffer GetVertexBuffer()
    {
        return this.vertexBuffer;
    }

    public VkBuffer GetIndexBuffer()
    {
        return this.indexBuffer;
    }

    public override void Destroy()
    {
        DestroyBuffers();
    }

    private void DestroyBuffers()
    {
        VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, vertexBuffer, null);
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, vertexBufferMemory, null);
        VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, indexBuffer, null);
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, indexBufferMemory, null);
    }
    private void CreateVertexBuffer(in Vertex[] vertices)
    {
        // Calculate the buffer size
        ulong bufferSize = (ulong) (Marshal.SizeOf(vertices[0]) * vertices.Length);
    
        // Define the staging buffer and its memory
        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        
        // Create the staging buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags. VK_MEMORY_PROPERTY_HOST_COHERENT_BIT, 
            out stagingBuffer, out stagingBufferMemory);

        // Create empty data pointer
        void* data;
        
        // Assign the data to the vertex buffer memory
        VulkanNative.vkMapMemory(VulkanCore.logicalDevice, stagingBufferMemory, 0, bufferSize, 0, &data);

        // Fill the data pointer with the vertices array's information
        fixed (Vertex* verticesPtr = vertices)
        {
            Buffer.MemoryCopy(verticesPtr, data, bufferSize, bufferSize);
        }
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, stagingBufferMemory);
        
        // Create the vertex buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
            out vertexBuffer, out vertexBufferMemory);
        
        // Copy the staging buffer to the vertex buffer
        VulkanUtilities.CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);
        
        // Destroy the staging buffer and free its memory
        VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, stagingBuffer, null);
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, stagingBufferMemory, null);
    }

    private void CreateIndexBuffer(in UInt32[] indices)
    {
        // Calculate the buffer size
        ulong bufferSize = (ulong) (Marshal.SizeOf(indices[0]) * indices.Length);
    
        // Define the staging buffer and its memory
        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        
        // Create the staging buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags. VK_MEMORY_PROPERTY_HOST_COHERENT_BIT, 
            out stagingBuffer, out stagingBufferMemory);

        // Create empty data pointer
        void* data;
        
        // Assign the data to the index buffer memory
        VulkanNative.vkMapMemory(VulkanCore.logicalDevice, stagingBufferMemory, 0, bufferSize, 0, &data);

        // Fill the data pointer with the indices array's information
        fixed (UInt32* indicesPtr = indices)
        {
            Buffer.MemoryCopy(indicesPtr, data, bufferSize, bufferSize);
        }
        
        // Unmap the memory
        VulkanNative.vkUnmapMemory(VulkanCore.logicalDevice, stagingBufferMemory);
        
        // Create the index buffer
        VulkanUtilities.CreateBuffer(
            bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT, 
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
            out indexBuffer, out indexBufferMemory);
        
        // Copy the staging buffer to the index buffer
        VulkanUtilities.CopyBuffer(stagingBuffer, indexBuffer, bufferSize);
        
        // Destroy the staging buffer and free its memory
        VulkanNative.vkDestroyBuffer(VulkanCore.logicalDevice, stagingBuffer, null);
        VulkanNative.vkFreeMemory(VulkanCore.logicalDevice, stagingBufferMemory, null);
    }
}

public struct VertexPushConstant
{
    public Matrix4x4 modelMatrix;
}