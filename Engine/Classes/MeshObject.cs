using System.Diagnostics;
using System.Numerics;
using Assimp;
using SierraEngine.Core.Rendering.Vulkan;
using Mesh = SierraEngine.Engine.Components.Mesh;
using TextureType = SierraEngine.Core.Rendering.Vulkan.TextureType;

namespace SierraEngine.Engine.Classes;

public class MeshObject
{
    public readonly Mesh[] meshes;
    public readonly uint verticesCount;
    public readonly string modelLocation;

    private readonly string[] diffuseTextureFileNames = null!;
    private readonly string[] specularTextureFileNames = null!;
    
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
                
                vertices[j].normal = currentAssimpMesh.HasNormals ? currentAssimpMesh.Normals[j].ToVector3() : Vector3.Zero;
                vertices[j].normal.Y *= -1;
                
                vertices[j].textureCoordinates = currentAssimpMesh.HasTextureCoords(0) ? currentAssimpMesh.TextureCoordinateChannels[0][j].ToVector2() : Vector2.Zero;
                vertices[j].textureCoordinates.Y *= -1;
            }
            
            if (model.HasMaterials)
            {
                diffuseTextureFileNames = new string[model.MaterialCount];
                specularTextureFileNames = new string[model.MaterialCount];
        
                for (int j = 0; j < model.MaterialCount; j++)
                {
                    if (model.Materials[j].GetMaterialTexture(Assimp.TextureType.Diffuse, 0, out TextureSlot diffuseTextureSlot))
                    {
                        int diffusePathIdx = diffuseTextureSlot.FilePath.LastIndexOf("/", StringComparison.Ordinal) + 1;
                        diffuseTextureFileNames[j] = diffuseTextureSlot.FilePath[diffusePathIdx..];
                    }
                    
                    if (model.Materials[j].GetMaterialTexture(Assimp.TextureType.Specular, 0, out TextureSlot specularTextureSlot))
                    {
                        int specularPathIdx = specularTextureSlot.FilePath.LastIndexOf("/", StringComparison.Ordinal) + 1;
                        specularTextureFileNames[j] = specularTextureSlot.FilePath[specularPathIdx..]; 
                    }
                }
            }
            else
            {
                VulkanDebugger.ThrowError($"No textures/materials found in {fileName}");
            }

            string currentDiffuseTexturePath = Files.FindInSubdirectories(fileName[..idx] + "/", diffuseTextureFileNames[currentAssimpMesh.MaterialIndex]);

            this.meshes[i] = new Mesh(vertices, indices);

            if (currentDiffuseTexturePath != null && currentDiffuseTexturePath.Trim() != "")
            {
                this.meshes[i].SetTexture(TextureType.Diffuse, vulkanRenderer.CreateTexture(currentDiffuseTexturePath, TextureType.Diffuse));
            }

            string currentSpecularTexturePath = Files.FindInSubdirectories(fileName[..idx] + "/", specularTextureFileNames[currentAssimpMesh.MaterialIndex]);

            if (currentSpecularTexturePath != null && currentSpecularTexturePath.Trim() != "")
            {
                this.meshes[i].SetTexture(TextureType.Specular, vulkanRenderer.CreateTexture(currentSpecularTexturePath, TextureType.Specular));
            }

            this.meshes[i].material.shininess = model.Materials[currentAssimpMesh.MaterialIndex].Shininess / 512f;

            this.meshes[i].meshName = currentAssimpMesh.Name;
        }
        
        VulkanDebugger.DisplayInfo($"Total vertices count for the model [{fileName[(idx + 1)..]}] containing [{meshes.Length}] mesh(es): {verticesCount}. Time elapsed during model loading: {stopwatch.ElapsedMilliseconds}ms");
        model.Clear();
    } 
}