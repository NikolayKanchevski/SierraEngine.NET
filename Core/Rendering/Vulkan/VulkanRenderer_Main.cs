using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Components;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    #region VARIABLES

        public VertexUniformData vertexUniformData;
        private readonly ulong vertexUniformDataSize = (ulong) Marshal.SizeOf(typeof(VertexUniformData));

        public FragmentUniformData fragmentUniformData;
        private readonly ulong fragmentUniformDataSize = (ulong)Marshal.SizeOf(typeof(FragmentUniformData));
        
        private readonly Window window;
        
        private readonly Vertex[] vertices = new Vertex[]
        {
            new Vertex()
            {
                position = new Vector3(-1.0f, -1.0f, -1.0f),
                // color = new Vector3(1.0f, 0.0f, 0.0f),
                textureCoordinates = new Vector2(0.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, -1.0f, -1.0f),
                // color = new Vector3(1.0f, 1.0f, 0.0f),
                textureCoordinates = new Vector2(1.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, 1.0f, -1.0f),
                // color = new Vector3(1.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(1.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-1.0f, 1.0f, -1.0f),
                // color = new Vector3(0.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(0.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-1.0f, -1.0f, 1.0f),
                // color = new Vector3(1.0f, 0.0f, 0.0f),
                textureCoordinates = new Vector2(0.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, -1.0f, 1.0f),
                // color = new Vector3(1.0f, 1.0f, 0.0f),
                textureCoordinates = new Vector2(1.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, 1.0f, 1.0f),
                // color = new Vector3(1.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(1.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-1.0f, 1.0f, 1.0f),
                // color = new Vector3(0.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(0.0f, 1.0f)
            }
        };

        private readonly UInt32[] indices = new UInt32[]
        {
            0, 1, 3, 3, 1, 2,
            1, 5, 2, 2, 5, 6,
            5, 4, 6, 6, 4, 7,
            4, 0, 7, 7, 0, 3,
            3, 2, 7, 7, 2, 6,
            4, 5, 0, 0, 5, 1
        };

    #endregion
    
    public VulkanRenderer(in Window window)
    {
        this.window = window;
        
        Init();
    }
    
    private void Init()
    {
        CreateInstance();
        // CreateDebugMessenger();
        CreateWindowSurface();
        
        GetPhysicalDevice();
        CreateLogicalDevice();
        
        CreateSwapchain();
        CreateSwapchainImageViews();
        
        CreateRenderPass();
        CreatePushConstants();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        
        CreateCommandPool();
        
        CreateColorBufferImage();
        CreateDepthBufferImage();
        
        CreateFrameBuffers();
        
        CreateTextureSampler();
        
        CreateCommandBuffers();
        CreateUniformBuffers();
        
        CreateQueryPool();
        
        CreateDescriptorPool();
        CreateUniformDescriptorSets();
        CreateImGuiContext();

        CreateSynchronisation();
        
        // Mesh mesh1 = new Mesh(this.vertices, this.indices, CreateTexture("Textures/texture1.jpg"));
        // Mesh mesh2 = new Mesh(this.vertices2, this.indices, CreateTexture("Textures/texture2.jpg"));

        MeshObject model = MeshObject.LoadFromModel("Models/Chieftain/T95_FV4201_Chieftain.fbx", this);
        // MeshObject model1 = MeshObject.LoadFromModel("Models/Kranvagn/Kranvagn_BB.obj", this);
        // MeshObject model = MeshObject.LoadFromModel("Models/Chieftain/T95_FV4201_Chieftain.obj", this);
        // MeshObject model1 = MeshObject.LoadFromModel("Models/Lamborghini/lamborghini-aventador-pbribl.obj", this);
        // MeshObject model1 = MeshObject.LoadFromModel("Models/Lambo/Lamborghini_Aventador.fbx", this);
    }

    public void Update()
    {
        Draw();
    }

    public void CleanUp()
    {
        VulkanNative.vkDeviceWaitIdle(logicalDevice);

        DestroySwapchainObjects();
        
        VulkanNative.vkDestroySampler(this.logicalDevice, this.textureSampler, null);
        
        VulkanNative.vkDestroyQueryPool(this.logicalDevice, this.drawTimeQueryPool, null);

        VulkanNative.vkDestroyDescriptorPool(this.logicalDevice, this.samplerDescriptorPool, null);
        VulkanNative.vkDestroyDescriptorSetLayout(this.logicalDevice, this.samplerDescriptorSetLayout, null);
        
        for (int i = 0; i < this.samplerDescriptorSets.Count; i++)
        {
            VulkanNative.vkDestroyImage(this.logicalDevice, this.textureImages[i], null);
            VulkanNative.vkDestroyImageView(this.logicalDevice, this.textureImageViews[i], null);
            VulkanNative.vkFreeMemory(this.logicalDevice, this.textureImageMemories[i], null);
        }
        
        VulkanNative.vkDestroyPipeline(this.logicalDevice, this.graphicsPipeline, null);
        VulkanNative.vkDestroyPipelineLayout(this.logicalDevice, this.graphicsPipelineLayout, null);

        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            VulkanNative.vkDestroyBuffer(this.logicalDevice, this.uniformBuffers[i], null);
            VulkanNative.vkFreeMemory(this.logicalDevice, this.uniformBuffersMemory[i], null);
        }
        
        VulkanNative.vkDestroyDescriptorPool(this.logicalDevice, this.uniformDescriptorPool, null);
        VulkanNative.vkDestroyDescriptorSetLayout(this.logicalDevice, this.uniformDescriptorSetLayout, null);
        
        foreach (Mesh mesh in World.meshes)
        {
            mesh.Destroy();
        }
        
        for (int i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            VulkanNative.vkDestroySemaphore(this.logicalDevice, this.imageAvailableSemaphores[i], null);
            VulkanNative.vkDestroySemaphore(this.logicalDevice, this.renderFinishedSemaphores[i], null);
            VulkanNative.vkDestroyFence(this.logicalDevice, this.frameBeingRenderedFences[i], null);
        }

        VulkanNative.vkDestroyCommandPool(this.logicalDevice, this.commandPool, null);
        
        VulkanNative.vkDestroyDevice(this.logicalDevice, null);

        DestroyDebugMessenger();
        
        VulkanNative.vkDestroySurfaceKHR(this.instance, this.surface, null);
        VulkanNative.vkDestroyInstance(this.instance, null);
    }

    public struct VertexUniformData
    {
        public Matrix4x4 view;
        public Matrix4x4 projection;
    }

    public struct FragmentUniformData
    {
        public Vector3 directionToLight;
    }
}