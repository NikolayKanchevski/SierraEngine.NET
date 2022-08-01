using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using Glfw;
using GlmSharp;
using SierraEngine.Engine;
using Exception = System.Exception;
using System.Threading;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    #region VARIABLES

        public VP vp;

        private readonly ulong vpSize = (ulong) Marshal.SizeOf(typeof(VP));

        private readonly List<Mesh> meshes = new List<Mesh>();

        private readonly Window window;
        
        private readonly Vertex[] vertices = new Vertex[]
        {
            new Vertex()
            {
                position = new Vector3(-1.0f, -1.0f, -1.0f),
                color = new Vector3(1.0f, 0.0f, 0.0f),
                textureCoordinates = new Vector2(0.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, -1.0f, -1.0f),
                color = new Vector3(1.0f, 1.0f, 0.0f),
                textureCoordinates = new Vector2(1.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, 1.0f, -1.0f),
                color = new Vector3(1.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(1.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-1.0f, 1.0f, -1.0f),
                color = new Vector3(0.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(0.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-1.0f, -1.0f, 1.0f),
                color = new Vector3(1.0f, 0.0f, 0.0f),
                textureCoordinates = new Vector2(0.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, -1.0f, 1.0f),
                color = new Vector3(1.0f, 1.0f, 0.0f),
                textureCoordinates = new Vector2(1.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, 1.0f, 1.0f),
                color = new Vector3(1.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(1.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-1.0f, 1.0f, 1.0f),
                color = new Vector3(0.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(0.0f, 1.0f)
            }
        };

        private readonly  Vertex[] vertices2 = new Vertex[]
        {
            new Vertex()
            {
                position = new Vector3(-1.0f, -1.0f, -1.0f),
                color = new Vector3(1.0f, 0.0f, 0.0f),
                textureCoordinates = new Vector2(0.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, -1.0f, -1.0f),
                color = new Vector3(1.0f, 1.0f, 0.0f),
                textureCoordinates = new Vector2(1.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, 1.0f, -1.0f),
                color = new Vector3(1.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(1.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-1.0f, 1.0f, -1.0f),
                color = new Vector3(0.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(0.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-1.0f, -1.0f, 1.0f),
                color = new Vector3(1.0f, 0.0f, 0.0f),
                textureCoordinates = new Vector2(0.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, -1.0f, 1.0f),
                color = new Vector3(1.0f, 1.0f, 0.0f),
                textureCoordinates = new Vector2(1.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(1.0f, 1.0f, 1.0f),
                color = new Vector3(1.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(1.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-1.0f, 1.0f, 1.0f),
                color = new Vector3(0.0f, 1.0f, 1.0f),
                textureCoordinates = new Vector2(0.0f, 1.0f)
            }
        };

        private readonly UInt16[] indices = new UInt16[]
        {
            0, 1, 3, 3, 1, 2,
            1, 5, 2, 2, 5, 6,
            5, 4, 6, 6, 4, 7,
            4, 0, 7, 7, 0, 3,
            3, 2, 7, 7, 2, 6,
            4, 5, 0, 0, 5, 1
        };

    #endregion
    
    public VulkanRenderer(ref Window window)
    {
        this.window = window;
        VulkanCore.glfwWindow = this.window.GetCoreWindow();
        
        Init();
    }

    private void Init()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices2[i].position.X -= 5;
        }
    
        CreateInstance();
        CreateDebugMessenger();
        CreateWindowSurface();
        
        GetPhysicalDevice();
        CreateLogicalDevice();
        
        CreateSwapchain();
        CreateSwapchainImageViews();
        
        CreateRenderPass();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        
        CreateFrameBuffers();
        CreateCommandPool();
        
        CreateTextureSampler();
        
        CreateCommandBuffers();
        CreateUniformBuffers();
        
        CreateDescriptorPool();
        CreateUniformDescriptorSets();

        CreateSynchronisation();
        
        Mesh mesh1 = new Mesh(this.vertices, this.indices, CreateTexture("texture1.jpg"));
        meshes.Add(mesh1);
        
        Mesh mesh2 = new Mesh(this.vertices2, this.indices, CreateTexture("texture2.jpg"));
        meshes.Add(mesh2);
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

        foreach (Mesh mesh in meshes)
        {
            mesh.DestroyBuffers();
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

    public struct VP
    {
        public mat4 model;
        public mat4 view;
        public mat4 projection;
    }
}