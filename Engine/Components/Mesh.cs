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

    // public static Mesh CreateCube(int textureID)
    // {
    //     return null;
    //     Vertex[] vertices = new Vertex[24];
    //     UInt32[] indices = new UInt32[24];
    //
    //     for (int i = 0; i < 24; i++) indices[i] = (UInt32) i;
    //     
    //     vertices[0].position = new Vector3(1.0f, -1.0f, -1.0f);
    //     vertices[1].position = new Vector3(-1.0f, -1.0f, -1.0f);
    //     vertices[2].position = new Vector3(-1.0f, -1.0f, 1.0f);
    //     vertices[3].position = new Vector3(1.0f, -1.0f, 1.0f);
    //     vertices[4].position = new Vector3(1.0f, 1.0f, 1.0f);
    //     vertices[5].position = new Vector3(1.0f, -1.0f, 1.0f);
    //     vertices[6].position = new Vector3(-1.0f, -1.0f, 1.0f);
    //     vertices[7].position = new Vector3(-1.0f, 1.0f, 1.0f);
    //     vertices[8].position = new Vector3(-1.0f, 1.0f, 1.0f);
    //     vertices[9].position = new Vector3(-1.0f, -1.0f, 1.0f);
    //     vertices[10].position = new Vector3(-1.0f, -1.0f, -1.0f);
    //     vertices[11].position = new Vector3(-1.0f, 1.0f, -1.0f);
    //     vertices[12].position = new Vector3(-1.0f, 1.0f, -1.0f);
    //     vertices[13].position = new Vector3(1.0f, 1.0f, -1.0f);
    //     vertices[14].position = new Vector3(1.0f, 1.0f, 1.0f);
    //     vertices[15].position = new Vector3(-1.0f, 1.0f, 1.0f);
    //     vertices[16].position = new Vector3(1.0f, 1.0f, -1.0f);
    //     vertices[17].position = new Vector3(1.0f, -1.0f, -1.0f);
    //     vertices[18].position = new Vector3(1.0f, -1.0f, 1.0f);
    //     vertices[19].position = new Vector3(1.0f, 1.0f, 1.0f);
    //     vertices[20].position = new Vector3(-1.0f, 1.0f, -1.0f);
    //     vertices[21].position = new Vector3(-1.0f, -1.0f, -1.0f);
    //     vertices[22].position = new Vector3(1.0f, -1.0f, -1.0f);
    //     vertices[23].position = new Vector3(1.0f, 1.0f, -1.0f);
    //     
    //     vertices[0].normal = new Vector3(0.0f, -1.0f, 0.0f);
    //     vertices[1].normal = new Vector3(0.0f, -1.0f, 0.0f);
    //     vertices[2].normal = new Vector3(0.0f, -1.0f, 0.0f);
    //     vertices[3].normal = new Vector3(0.0f, -1.0f, 0.0f);
    //     vertices[4].normal = new Vector3(0.0f, -0.0f, 1.0f);
    //     vertices[5].normal = new Vector3(0.0f, -0.0f, 1.0f);
    //     vertices[6].normal = new Vector3(0.0f, -0.0f, 1.0f);
    //     vertices[7].normal = new Vector3(0.0f, -0.0f, 1.0f);
    //     vertices[8].normal = new Vector3(-1.0f, -0.0f, 0.0f);
    //     vertices[9].normal = new Vector3(-1.0f, -0.0f, 0.0f);
    //     vertices[10].normal = new Vector3(-1.0f, -0.0f, 0.0f);
    //     vertices[11].normal = new Vector3(-1.0f, -0.0f, 0.0f);
    //     vertices[12].normal = new Vector3(0.0f, 1.0f, 0.0f);
    //     vertices[13].normal = new Vector3(0.0f, 1.0f, 0.0f);
    //     vertices[14].normal = new Vector3(0.0f, 1.0f, 0.0f);
    //     vertices[15].normal = new Vector3(0.0f, 1.0f, 0.0f);
    //     vertices[16].normal = new Vector3(1.0f, -0.0f, 0.0f);
    //     vertices[17].normal = new Vector3(1.0f, -0.0f, 0.0f);
    //     vertices[18].normal = new Vector3(1.0f, -0.0f, 0.0f);
    //     vertices[19].normal = new Vector3(1.0f, -0.0f, 0.0f);
    //     vertices[20].normal = new Vector3(0.0f, -0.0f, -1.0f);
    //     vertices[21].normal = new Vector3(0.0f, -0.0f, -1.0f);
    //     vertices[22].normal = new Vector3(0.0f, -0.0f, -1.0f);
    //     vertices[23].normal = new Vector3(0.0f, -0.0f, -1.0f); 
    //
    //     vertices[0].textureCoordinates = new Vector2(0.625f, -0.5f);
    //     vertices[1].textureCoordinates = new Vector2(0.875f, -0.5f);
    //     vertices[2].textureCoordinates = new Vector2(0.875f, -0.75f);
    //     vertices[3].textureCoordinates = new Vector2(0.625f, -0.75f);
    //     vertices[4].textureCoordinates = new Vector2(0.375f, -0.75f);
    //     vertices[5].textureCoordinates = new Vector2(0.625f, -0.75f);
    //     vertices[6].textureCoordinates = new Vector2(0.625f, -1f);
    //     vertices[7].textureCoordinates = new Vector2(0.375f, -1f);
    //     vertices[8].textureCoordinates = new Vector2(0.375f, -0f);
    //     vertices[9].textureCoordinates = new Vector2(0.625f, -0f);
    //     vertices[10].textureCoordinates = new Vector2(0.625f, -0.25f);
    //     vertices[11].textureCoordinates = new Vector2(0.375f, -0.25f);
    //     vertices[12].textureCoordinates = new Vector2(0.125f, -0.5f);
    //     vertices[13].textureCoordinates = new Vector2(0.375f, -0.5f);
    //     vertices[14].textureCoordinates = new Vector2(0.375f, -0.75f);
    //     vertices[15].textureCoordinates = new Vector2(0.125f, -0.75f);
    //     vertices[16].textureCoordinates = new Vector2(0.375f, -0.5f);
    //     vertices[17].textureCoordinates = new Vector2(0.625f, -0.5f);
    //     vertices[18].textureCoordinates = new Vector2(0.625f, -0.75f);
    //     vertices[19].textureCoordinates = new Vector2(0.375f, -0.75f);
    //     vertices[20].textureCoordinates = new Vector2(0.375f, -0.25f);
    //     vertices[21].textureCoordinates = new Vector2(0.625f, -0.25f);
    //     vertices[22].textureCoordinates = new Vector2(0.625f, -0.5f);
    //     vertices[23].textureCoordinates = new Vector2(0.375f, -0.5f);
    //
    //     return new Mesh(vertices, indices, textureID);
    // }
    
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
        VulkanRendererInfo.verticesDrawn += (int) verticesCount;
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

public struct VertexPushConstant
{
    public Matrix4x4 modelMatrix;
}