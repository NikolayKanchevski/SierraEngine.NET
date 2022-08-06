using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Assimp;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Components;

public class MeshObject
{
    public readonly Mesh[] meshes = null!;
    public readonly uint verticesCount;
    public readonly string modelLocation;

    private readonly string[] materialFilePaths = null!;
    
    public MeshObject() { }
    
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
            int[] assimpIndices = currentAssimpMesh.GetIndices();

            Vertex[] vertices = new Vertex[currentAssimpMesh.VertexCount];
            UInt16[] indices = new UInt16[assimpIndices.Length];
            
            verticesCount += (uint) currentAssimpMesh.VertexCount;

            for (int j = 0; j < currentAssimpMesh.VertexCount; j++)
            {
                Assimp.Vector3D currentAssimpVertex = currentAssimpMesh.Vertices[j];

                Vertex currentVertex = new Vertex() with
                {
                    position = new Vector3(currentAssimpVertex.X, -currentAssimpVertex.Y, currentAssimpVertex.Z)
                };
                
                if (currentAssimpMesh.HasTextureCoords(0))
                {
                    Assimp.Vector3D currentAssimpTextureCoordinate = currentAssimpMesh.TextureCoordinateChannels[0][j];
                    currentVertex.textureCoordinates = new Vector2(currentAssimpTextureCoordinate.X, -currentAssimpTextureCoordinate.Y);
                }
                
                vertices[j] = currentVertex;
                indices[j] = (UInt16) assimpIndices[j];
            }

            if (model.HasMaterials)
            {
                materialFilePaths = new string[model.MaterialCount];

                for (int j = 0; j < model.MaterialCount; j++)
                {
                    Material currentAssimpMaterial = model.Materials[j];

                    if (currentAssimpMaterial.GetMaterialTexture(TextureType.Diffuse, 0, out TextureSlot textureSlot))
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

            this.meshes[i] = new Mesh(vertices.ToArray(), indices, vulkanRenderer.CreateTexture(fileName[..idx] + "/" + currentTexturePath));
        }
        
        VulkanDebugger.DisplayInfo($"Total vertices count for the model [{fileName[(idx + 1)..]}] which contains [{meshes.Length}] mesh(es): {verticesCount}.\n    Time elapsed during model loading: {stopwatch.ElapsedMilliseconds}ms");
        model.Clear();
    } 
}