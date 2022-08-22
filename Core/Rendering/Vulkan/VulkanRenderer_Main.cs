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