using System.Diagnostics;
using System.Numerics;
using Assimp;
using SierraEngine.Core.Rendering.Vulkan;
using Mesh = SierraEngine.Engine.Components.Mesh;
using TextureType = SierraEngine.Core.Rendering.Vulkan.TextureType;

namespace SierraEngine.Engine.Classes;

public class MeshObject
{
    public GameObject rootGameObject { get; private set; }= null!;
    
    public int vertexCount { get; private set; }
    public readonly string modelLocation;
    
    private readonly Mesh[] meshes;
    private readonly string modelName;

    private readonly Scene model;

    private string[] diffuseTextureFileNames = null!;
    private string[] specularTextureFileNames = null!;
    
    public static MeshObject LoadFromModel(string fileName)
    {
        return new MeshObject(fileName);
    }
    
    private MeshObject(string filePath)
    {
        this.model = new AssimpContext().ImportFile(filePath);
        this.meshes = new Mesh[model.MeshCount];
        
        #if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
        #endif
        
        int startIdx = filePath.LastIndexOf('/');
        modelName = filePath[(startIdx + 1)..];
        
        this.modelLocation = filePath[..startIdx];
        
        int endIdx = modelName.LastIndexOf('.');
        modelName = modelName[..(endIdx)];
        
        ListDeeperNode(model.RootNode);
        
        
        #if DEBUG
            stopwatch.Stop();
            VulkanDebugger.DisplayInfo($"Total vertices count for the model [{ modelName }] containing [{ meshes.Length }] mesh(es): { vertexCount }. Time elapsed during model loading: { stopwatch.ElapsedMilliseconds }ms");
        #endif

        model.Clear();
    }

    private void ListDeeperNode(in Node node, in GameObject parentObject = null!, in bool firstTime = true)
    {
        GameObject nodeGameObject = new GameObject(firstTime ? modelName : node.Name);
        if (firstTime) rootGameObject = nodeGameObject;
        
        if (parentObject != null) nodeGameObject.SetParent(parentObject);
        
        if (model.HasMaterials) LoadAssimpMeshTextures();
        
        for (int i = 0; i < node.MeshCount; i++)
        {
            Assimp.Mesh currentAssimpMesh = model.Meshes[node.MeshIndices[i]];

            Mesh mesh = LoadAssimpMesh(currentAssimpMesh);
            mesh.material.shininess = model.Materials[currentAssimpMesh.MaterialIndex].Shininess / 512f;

            Assimp.Material currentAssimpMaterial = model.Materials[currentAssimpMesh.MaterialIndex];
            if (currentAssimpMaterial.HasTextureDiffuse)
            {
                string diffuseTexturePath = Files.FindInSubdirectories(modelLocation, diffuseTextureFileNames[currentAssimpMesh.MaterialIndex]);
                mesh.SetTexture(TextureType.Diffuse, VulkanCore.vulkanRenderer.CreateTexture(diffuseTexturePath, TextureType.Diffuse));
            }
            if (currentAssimpMaterial.HasTextureSpecular)
            {
                string specularTexturePath = Files.FindInSubdirectories(modelLocation, specularTextureFileNames[currentAssimpMesh.MaterialIndex]);
                mesh.SetTexture(TextureType.Specular, VulkanCore.vulkanRenderer.CreateTexture(specularTexturePath, TextureType.Specular));
            }
            
            nodeGameObject.AddComponent<Mesh>(mesh);
        }
         
        for (int i = 0; i < node.ChildCount; i++)
        {
            ListDeeperNode(node.Children[i], nodeGameObject, false);
        }
    }

    private Mesh LoadAssimpMesh(in Assimp.Mesh assimpMesh)
    {
        Vertex[] meshVertices = new Vertex[assimpMesh.VertexCount];
        for (int j = 0; j < meshVertices.Length; j++)
        {
            meshVertices[j].position = assimpMesh.Vertices[j].ToVector3();
            meshVertices[j].position.Y *= -1;
                
            meshVertices[j].normal = assimpMesh.HasNormals ? assimpMesh.Normals[j].ToVector3() : Vector3.Zero;
            meshVertices[j].normal.Y *= -1;
                
            meshVertices[j].textureCoordinates = assimpMesh.HasTextureCoords(0) ? assimpMesh.TextureCoordinateChannels[0][j].ToVector2() : Vector2.Zero;
            meshVertices[j].textureCoordinates.Y *= -1;
        }

        this.vertexCount = meshVertices.Length;
        
        return new Mesh(meshVertices, assimpMesh.GetUnsignedIndices());   
    }

    private void LoadAssimpMeshTextures()
    {
        if (!model.HasMaterials) return;

        diffuseTextureFileNames = new string[model.MaterialCount];
        specularTextureFileNames = new string[model.MaterialCount];
        
        for (int i = 0; i < model.MaterialCount; i++)
        {
            Assimp.Material assimpMaterial = model.Materials[i];
            
            if (assimpMaterial.GetMaterialTexture(Assimp.TextureType.Diffuse, 0, out var diffuseTextureSlot))
                diffuseTextureFileNames[i] = Files.TrimPath(diffuseTextureSlot.FilePath);
            
            if (assimpMaterial.GetMaterialTexture(Assimp.TextureType.Specular, 0, out var specularTextureSlot))
                specularTextureFileNames[i] = Files.TrimPath(specularTextureSlot.FilePath);
        }
    }
}