using System.Numerics;
using Evergine.Bindings.Vulkan;
using SierraEngine.Core;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

public unsafe class Mesh : Component
{
    public Material material;
    
    public uint verticesCount { get; private set; }
    public uint indexCount { get; private set; }
    
    public int diffuseTextureID { get; private set; } = 0;
    public int specularTextureID { get; private set; } = 0;

    private PushConstant pushConstantData;
    
    private VkBuffer vertexBuffer;
    private VkDeviceMemory vertexBufferMemory;

    private VkBuffer indexBuffer;
    private VkDeviceMemory indexBufferMemory;

    public Mesh(in Vertex[] givenVertices, in UInt32[] givenIndices, int newDiffuseTextureId)
    {
        // Retrieve the public values
        this.verticesCount = (uint) givenVertices.Length;
        this.indexCount = (uint) givenIndices.Length;
        this.diffuseTextureID = newDiffuseTextureId;

        // Create buffers
        CreateVertexBuffer(in givenVertices);
        CreateIndexBuffer(in givenIndices);
        
        // Add the mesh to the world
        World.meshes.Add(this);
        VulkanRendererInfo.meshesDrawn++;
        VulkanRendererInfo.verticesDrawn += (int) verticesCount;
    }

    public Mesh(in Vertex[] givenVertices, in UInt32[] givenIndices)
    {
        // Retrieve the public values
        this.verticesCount = (uint) givenVertices.Length;
        this.indexCount = (uint) givenIndices.Length;

        // Create buffers
        CreateVertexBuffer(in givenVertices);
        CreateIndexBuffer(in givenIndices);
        
        // Add the mesh to the world
        World.meshes.Add(this);
        VulkanRendererInfo.meshesDrawn++;
        VulkanRendererInfo.verticesDrawn += (int) verticesCount;
    }

    public void SetTexture(in TextureType textureType, in int newTextureID)
    {
        if (textureType == TextureType.Diffuse) this.diffuseTextureID = newTextureID;
        else if (textureType == TextureType.Specular) this.specularTextureID = newTextureID;
    }

    public void ResetTexture(in TextureType textureType)
    {
        if (textureType == TextureType.Diffuse) this.diffuseTextureID = 0;
        else if (textureType == TextureType.Specular) this.specularTextureID = 0;
    }
    
    public static Mesh CreateSphere(in float radius, in int sectorCount, in int stackCount, bool renderBackFace = false)
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

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    public PushConstant GetPushConstantData()
    {
        // Update the model matrix per call
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(transform.position);
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationX(Mathematics.ToRadians(transform.rotation.X)) * Matrix4x4.CreateRotationY(Mathematics.ToRadians(transform.rotation.Y)) * Matrix4x4.CreateRotationZ(Mathematics.ToRadians(transform.rotation.Z));
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(transform.scale);
        
        pushConstantData.modelMatrix = translationMatrix * rotationMatrix * scaleMatrix;
        pushConstantData.shininess = material.shininess;
        
        // Return it
        return this.pushConstantData;
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
        VulkanRendererInfo.meshesDrawn--;
        VulkanRendererInfo.verticesDrawn -= (int) this.verticesCount;
        
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
        VulkanUtilities.CreateVertexBuffer(vertices, out vertexBuffer, out vertexBufferMemory);
    }

    private void CreateIndexBuffer(in UInt32[] indices)
    {
        VulkanUtilities.CreateIndexBuffer(indices, out indexBuffer, out indexBufferMemory);
    }
}