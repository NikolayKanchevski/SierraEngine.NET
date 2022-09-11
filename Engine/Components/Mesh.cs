using System.Numerics;
using Evergine.Bindings.Vulkan;
using SierraEngine.Core;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Structures;
using Buffer = SierraEngine.Core.Rendering.Vulkan.Abstractions.Buffer;

namespace SierraEngine.Engine.Components;

/// <summary>
/// Represents a mesh in the scene. The class is assignable to an object as it is a component. Holds vertex and index data read by the renderer.
/// </summary>
public class Mesh : Component
{
    /// <summary>
    /// Material to use when shading the mesh.
    /// </summary>
    public Material material;
    
    /// <summary>
    /// Total count of all vertices within the mesh.
    /// </summary>
    public uint verticesCount { get; private set; }
    
    /// <summary>
    /// Total count of all vertex indices within the mesh.
    /// </summary>
    public uint indexCount { get; private set; }
    
    /// <summary>
    /// The offset in the diffuse textures pool in the renderer.
    /// </summary>
    public int diffuseTextureID { get; private set; } = 0;
    
    /// <summary>
    /// The offset in the specular textures pool in the renderer.
    /// </summary>
    public int specularTextureID { get; private set; } = 0;

    private PushConstant pushConstantData;

    private Buffer vertexBuffer = null!;
    private Buffer indexBuffer = null!;

    /// <summary>
    /// Constructs a new mesh with from given vertices, and indices.
    /// </summary>
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

    /// <summary>
    /// Binds a given texture to the mesh.
    /// </summary>
    /// <param name="textureType">What kind the texture is of.</param>
    /// <param name="newTextureID">New texture offset in the textures pool.</param>
    /// <returns></returns>
    public void SetTexture(in TextureType textureType, in int newTextureID)
    {
        if (textureType == TextureType.Diffuse) this.diffuseTextureID = newTextureID;
        else if (textureType == TextureType.Specular) this.specularTextureID = newTextureID;
    }

    /// <summary>
    /// Removes the texture of given type from the mesh.
    /// </summary>
    /// <param name="textureType">Which texture to reset.</param>
    public void ResetTexture(in TextureType textureType)
    {
        if (textureType == TextureType.Diffuse) this.diffuseTextureID = 0;
        else if (textureType == TextureType.Specular) this.specularTextureID = 0;
    }
    
    /// <summary>
    /// Creates a sphere from given radius and resolution.
    /// </summary>
    /// <param name="radius">Radius of the sphere.</param>
    /// <param name="sectorCount">How many sectors to generate for the sphere.</param>
    /// <param name="stackCount">How many stacks to generate for the sphere.</param>
    /// <param name="renderBackFace">Whether to render the back or front face of the sphere.</param>
    /// <returns></returns>
    [Obsolete]
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

    /// <summary>
    /// Returns the data required to be passed to shaders. Should only really be used by the renderer.
    /// </summary>
    /// <returns></returns>
    public PushConstant GetPushConstantData()
    {
        // Inverse the Y coordinate to satisfy Vulkan's requirements
        Vector3 rendererPosition = new Vector3(transform.position.X, transform.position.Y * -1, transform.position.Z);

        // Update the model matrix per call
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(rendererPosition);
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationX(Mathematics.ToRadians(transform.rotation.X)) * Matrix4x4.CreateRotationY(Mathematics.ToRadians(transform.rotation.Y)) * Matrix4x4.CreateRotationZ(Mathematics.ToRadians(transform.rotation.Z));
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(transform.scale);
        
        pushConstantData.modelMatrix = translationMatrix * rotationMatrix * scaleMatrix;
        pushConstantData.material.shininess = material.shininess;
        pushConstantData.material.diffuse = material.diffuse;
        pushConstantData.material.specular = material.specular;
        pushConstantData.material.ambient = material.ambient;
        
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
        base.Destroy();
        
        VulkanRendererInfo.meshesDrawn--;
        VulkanRendererInfo.verticesDrawn -= (int) this.verticesCount;
        
        DestroyBuffers();
    }

    private void DestroyBuffers()
    {
        vertexBuffer.CleanUp();
        indexBuffer.CleanUp();
    }
    
    private void CreateVertexBuffer(in Vertex[] vertices)
    {
        VulkanUtilities.CreateVertexBuffer(vertices, out vertexBuffer);
    }

    private void CreateIndexBuffer(in UInt32[] indices)
    {
        VulkanUtilities.CreateIndexBuffer(indices, out indexBuffer);
    }
}