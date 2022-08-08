using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Assimp;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Components;

public class MeshObject
{
    public readonly Mesh[] meshes;
    public readonly uint verticesCount;
    public readonly string modelLocation;

    private readonly string[] materialFilePaths = null!;
    private readonly EmbeddedTexture[] embeddedTextures = null!;
    
    public static MeshObject LoadFromModel(string fileName, VulkanRenderer vulkanRenderer)
    {
        return new MeshObject(fileName, vulkanRenderer);
    }
    
    private MeshObject(string fileName, VulkanRenderer vulkanRenderer)
    {
        this.modelLocation = Directory.GetCurrentDirectory() + "/" + fileName;

        Scene model = new AssimpContext().ImportFile(fileName);
        
        this.meshes = new Mesh[model.MeshCount];
        
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        int idx = fileName.LastIndexOf('/');
        for (int i = 0; i < model.MeshCount; i++)
        {
            Assimp.Mesh currentAssimpMesh = model.Meshes[i];

            Vertex[] vertices = new Vertex[currentAssimpMesh.VertexCount];
            UInt32[] indices = currentAssimpMesh.GetUnsignedIndices();
            
            verticesCount += (uint) currentAssimpMesh.VertexCount;
        
            for (int j = 0; j < currentAssimpMesh.VertexCount; j++)
            {
                vertices[j].position = currentAssimpMesh.Vertices[j].ToVector3();
                vertices[j].position.Y *= -1;
                
                vertices[j].normal = currentAssimpMesh.Normals[j].ToVector3();
                vertices[j].normal.Y *= -1;
                
                vertices[j].textureCoordinates = currentAssimpMesh.HasTextureCoords(0) ? currentAssimpMesh.TextureCoordinateChannels[0][j].ToVector2() : Vector2.Zero;
                vertices[j].textureCoordinates.Y *= -1;
            }
        
            if (model.HasMaterials)
            {
                materialFilePaths = new string[model.MaterialCount];
        
                for (int j = 0; j < model.MaterialCount; j++)
                {
                    if (model.Materials[j].GetMaterialTexture(TextureType.Diffuse, 0, out TextureSlot textureSlot))
                    {
                        materialFilePaths[j] = textureSlot.FilePath;
                    }
                }
            }
            else
            {
                VulkanDebugger.ThrowError($"No textures/materials found in {fileName}");
            }
            
            string currentTexturePath = materialFilePaths[currentAssimpMesh.MaterialIndex];

            if (fileName.EndsWith(".fbx"))
            {
                int texturePathIdx = currentTexturePath.IndexOf("/", StringComparison.Ordinal);
                currentTexturePath = "textures" + currentTexturePath[texturePathIdx..];
            }
            
            if (currentTexturePath == null || currentTexturePath.Trim() == "") continue;
        
            this.meshes[i] = new Mesh(vertices, indices, vulkanRenderer.CreateTexture(fileName[..idx] + "/" + currentTexturePath));
            this.meshes[i].meshName = currentAssimpMesh.Name;
        }
        
        VulkanDebugger.DisplayInfo($"Total vertices count for the model [{fileName[(idx + 1)..]}] containing [{meshes.Length}] mesh(es): {verticesCount}. Time elapsed during model loading: {stopwatch.ElapsedMilliseconds}ms");
        model.Clear();
    } 
}