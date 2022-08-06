using System.Numerics;
using Assimp;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Components;

public class MeshObject
{
    public readonly Mesh[] meshes = null!;
    public readonly string modelLocation;

    private readonly string[] materialFilePaths = null!;
    private readonly EmbeddedTexture[] embeddedTextures = null!;
    
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
        
        for (int i = 0; i < model.MeshCount; i++)
        {
            Assimp.Mesh currentAssimpMesh = model.Meshes[i];
            
            Vertex[] vertices = new Vertex[currentAssimpMesh.VertexCount];
            
            for (int j = 0; j < currentAssimpMesh.VertexCount; j++)
            {
                Assimp.Vector3D currentAssimpVertex = currentAssimpMesh.Vertices[j];
                
                vertices[j] = new Vertex() with
                {
                    position = new Vector3(-currentAssimpVertex.X, -currentAssimpVertex.Y, currentAssimpVertex.Z)
                };

                if (currentAssimpMesh.HasTextureCoords(0))
                {
                    Assimp.Vector3D currentAssimpTextureCoordinate = currentAssimpMesh.TextureCoordinateChannels[0][j];
                    vertices[j].textureCoordinates = new Vector2(currentAssimpTextureCoordinate.X, -currentAssimpTextureCoordinate.Y);
                }
            }
            
            if (model.MaterialCount > 0)
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
                VulkanDebugger.ThrowError($"No textures/materials found in { fileName }");
            }

            int[] assimpIndices = currentAssimpMesh.GetIndices();
            UInt16[] indices = new UInt16[assimpIndices.Length];

            for (uint j = 0; j < assimpIndices.Length; j++)
            {
                indices[j] = (UInt16) assimpIndices[j];
            }

            string currentTexturePath = materialFilePaths[currentAssimpMesh.MaterialIndex];
            int idx = fileName.LastIndexOf('/');
            this.meshes[i] = new Mesh(vertices, indices, vulkanRenderer.CreateTexture(fileName[..idx] + "/" + currentTexturePath));
        }
    } 
}