using System.Diagnostics;
using System.Numerics;
using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Components;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    #region VARIABLES
    
    private readonly Window window;
        
    private readonly Vertex[] vertices = new Vertex[]
    {
        new Vertex()
        {
            position = new Vector3(-1.0f, -1.0f, -1.0f),
            normal = new Vector3(0, 0, 1),
            // color = new Vector3(1.0f, 0.0f, 0.0f),
            textureCoordinates = new Vector2(0.0f, 0.0f)
        },
        new Vertex()
        {
            position = new Vector3(1.0f, -1.0f, -1.0f),
            normal = new Vector3(1, 0, 0),
            // color = new Vector3(1.0f, 1.0f, 0.0f),
            textureCoordinates = new Vector2(1.0f, 0.0f)
        },
        new Vertex()
        {
            position = new Vector3(1.0f, 1.0f, -1.0f),
            normal = new Vector3(0, 0, -1),
            // color = new Vector3(1.0f, 1.0f, 1.0f),
            textureCoordinates = new Vector2(1.0f, 1.0f)
        },
        new Vertex()
        {
            position = new Vector3(-1.0f, 1.0f, -1.0f),
            normal = new Vector3(-1, 0, 0),
            // color = new Vector3(0.0f, 1.0f, 1.0f),
            textureCoordinates = new Vector2(0.0f, 1.0f)
        },
        new Vertex()
        {
            position = new Vector3(-1.0f, -1.0f, 1.0f),
            normal = new Vector3(0, 1, 0),
            // color = new Vector3(1.0f, 0.0f, 0.0f),
            textureCoordinates = new Vector2(0.0f, 0.0f)
        },
        new Vertex()
        {
            position = new Vector3(1.0f, -1.0f, 1.0f),
            // color = new Vector3(1.0f, 1.0f, 0.0f),
            normal = new Vector3(0, -1, 0),
            textureCoordinates = new Vector2(1.0f, 0.0f)
        },
        new Vertex()
        {
            position = new Vector3(1.0f, 1.0f, 1.0f),
            normal = new Vector3(0, 0, 1),
            // color = new Vector3(1.0f, 1.0f, 1.0f),
            textureCoordinates = new Vector2(1.0f, 1.0f)
        },
        new Vertex()
        {
            position = new Vector3(-1.0f, 1.0f, 1.0f),
            normal = new Vector3(1, 0, 0),
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
        Stopwatch stopwatch = Stopwatch.StartNew();
        
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

        CreateSynchronisation();

        CreateNullTextures();
        
        CreateImGuiContext();
        
        stopwatch.Stop();
        VulkanDebugger.DisplaySuccess($"Successfully started Vulkan! Initialization took { stopwatch.ElapsedMilliseconds }ms");
        
        #if DEBUG
            VulkanRendererInfo.initializationTime += stopwatch.ElapsedMilliseconds;
        #endif
        
        Start();
    }

    private void Start()
    {
        Mesh mesh = new Mesh(vertices, indices);
        mesh.SetTexture(TextureType.Diffuse, CreateTexture("Textures/lamp.png", TextureType.Diffuse));
        
        MeshObject.LoadFromModel("Models/Chieftain/T95_FV4201_Chieftain.fbx", this);
    }

    public void Update()
    {
        Draw();
    }

    public void CleanUp()
    {
        VulkanNative.vkDeviceWaitIdle(logicalDevice);
        
        imGuiController.CleanUp();

        DestroySwapchainObjects();
        
        VulkanNative.vkDestroySampler(this.logicalDevice, this.textureSampler, null);
        
        VulkanNative.vkDestroyQueryPool(this.logicalDevice, this.drawTimeQueryPool, null);
        
        for (int i = 0; i < this.diffuseTextureImages.Count; i++)
        {
            VulkanNative.vkDestroyImage(this.logicalDevice, this.diffuseTextureImages[i], null);
            VulkanNative.vkDestroyImageView(this.logicalDevice, this.diffuseTextureImageViews[i], null);
            VulkanNative.vkFreeMemory(this.logicalDevice, this.diffuseTextureImageMemories[i], null);
        }
        
        for (int i = 0; i < this.specularTextureImages.Count; i++)
        {
            VulkanNative.vkDestroyImage(this.logicalDevice, this.specularTextureImages[i], null);
            VulkanNative.vkDestroyImageView(this.logicalDevice, this.specularTextureImageViews[i], null);
            VulkanNative.vkFreeMemory(this.logicalDevice, this.specularTextureImageMemories[i], null);
        }
        
        VulkanNative.vkDestroyPipeline(this.logicalDevice, this.graphicsPipeline, null);
        VulkanNative.vkDestroyPipelineLayout(this.logicalDevice, this.graphicsPipelineLayout, null);

        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            VulkanNative.vkDestroyBuffer(this.logicalDevice, this.uniformBuffers[i], null);
            VulkanNative.vkFreeMemory(this.logicalDevice, this.uniformBuffersMemory[i], null);
        }
        
        VulkanNative.vkDestroyDescriptorPool(this.logicalDevice, this.descriptorPool, null);
        VulkanNative.vkDestroyDescriptorSetLayout(this.logicalDevice, this.descriptorSetLayout, null);
        
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
}