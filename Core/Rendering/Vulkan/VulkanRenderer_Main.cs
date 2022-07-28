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
                position = new Vector3(-0.1f, -0.4f, 0.0f),
                color = new Vector3(1.0f, 0.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(-0.1f, 0.4f, 0.0f),
                color = new Vector3(1.0f, 1.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(-0.9f, 0.4f, 0.0f),
                color = new Vector3(1.0f, 1.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(-0.9f, -0.4f, 0.0f),
                color = new Vector3(0.0f, 1.0f, 1.0f)
            }
        };

        private readonly  Vertex[] vertices2 = new Vertex[]
        {
            new Vertex()
            {
                position = new Vector3(0.9f, -0.4f, 0.0f),
                color = new Vector3(1.0f, 0.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(0.9f, 0.4f, 0.0f),
                color = new Vector3(1.0f, 1.0f, 0.0f)
            },
            new Vertex()
            {
                position = new Vector3(0.1f, 0.4f, 0.0f),
                color = new Vector3(1.0f, 1.0f, 1.0f)
            },
            new Vertex()
            {
                position = new Vector3(0.1f, -0.4f, 0.0f),
                color = new Vector3(0.0f, 1.0f, 1.0f)
            }
        };

        private readonly UInt16[] indices = new UInt16[]
        {
            0, 1, 2, 2, 3, 0
        };

    #endregion
    
    public VulkanRenderer(ref Window window)
    {
        this.window = window;
        EngineCore.glfwWindow = this.window.GetCoreWindow();
        
        Init();
    }

    private void Init()
    {
        try
        {
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
            
            Mesh mesh = new Mesh(this.commandPool, this.vertices, this.indices);
            meshes.Add(mesh);
            
            Mesh mesh2 = new Mesh(this.commandPool, this.vertices2, this.indices);
            meshes.Add(mesh2);
            
            CreateCommandBuffers();
            CreateUniformBuffers();
            
            CreateDescriptorPool();
            CreateDescriptorSets();

            CreateSynchronisation();
        }
        catch (Exception exception)
        {
            throw new Exception($"Error: {exception.Message}!");
        }
    }

    public void Update()
    {
        Draw();
    }

    public void CleanUp()
    {
        VulkanNative.vkDeviceWaitIdle(logicalDevice);

        DestroySwapchainObjects();
        
        VulkanNative.vkDestroyPipeline(this.logicalDevice, this.graphicsPipeline, null);
        VulkanNative.vkDestroyPipelineLayout(this.logicalDevice, this.graphicsPipelineLayout, null);

        for (uint i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            VulkanNative.vkDestroyBuffer(this.logicalDevice, this.uniformBuffers[i], null);
            VulkanNative.vkFreeMemory(this.logicalDevice, this.uniformBuffersMemory[i], null);
        }
        
        VulkanNative.vkDestroyDescriptorPool(this.logicalDevice, this.descriptorPool, null);
        
        VulkanNative.vkDestroyDescriptorSetLayout(this.logicalDevice, this.descriptorSetLayout, null);

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