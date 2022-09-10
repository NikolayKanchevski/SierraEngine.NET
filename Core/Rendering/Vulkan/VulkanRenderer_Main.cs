using System.Diagnostics;
using Evergine.Bindings.Vulkan;
using SierraEngine.Core.Rendering.Vulkan.Abstractions;
using SierraEngine.Engine.Components;
using Buffer = SierraEngine.Core.Rendering.Vulkan.Abstractions.Buffer;

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
        CreateCommandPool();
        CreateDepthBufferImage();

        CreateRenderPass();
        CreatePushConstants();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();

        CreateColorBufferImage();
        
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
        
        textureSampler.CleanUp();
        
        VulkanNative.vkDestroyQueryPool(this.logicalDevice, this.drawTimeQueryPool, null);

        Parallel.ForEach(diffuseTextures, texture =>
        {
            texture.CleanUp();
        });
        
        Parallel.ForEach(specularTextures, texture =>
        {
            texture.CleanUp();
        });

        VulkanNative.vkDestroyPipeline(this.logicalDevice, this.graphicsPipeline, null);
        VulkanNative.vkDestroyPipelineLayout(this.logicalDevice, this.graphicsPipelineLayout, null);

        Parallel.ForEach(uniformBuffers, uniformBuffer =>
        {
            uniformBuffer.CleanUp();
        });
        
        descriptorPool.CleanUp();
        descriptorSetLayout.CleanUp();

        foreach (Mesh mesh in World.meshes)
        {
            mesh.Destroy();
        }

        Parallel.For(0, MAX_CONCURRENT_FRAMES, i =>
        {
            VulkanNative.vkDestroySemaphore(this.logicalDevice, this.imageAvailableSemaphores[i], null);
            VulkanNative.vkDestroySemaphore(this.logicalDevice, this.renderFinishedSemaphores[i], null);
            VulkanNative.vkDestroyFence(this.logicalDevice, this.frameBeingRenderedFences[i], null);
        });

        VulkanNative.vkDestroyCommandPool(this.logicalDevice, this.commandPool, null);
        
        VulkanNative.vkDestroyDevice(this.logicalDevice, null);

        DestroyDebugMessenger();
        
        VulkanNative.vkDestroySurfaceKHR(this.instance, this.surface, null);
        VulkanNative.vkDestroyInstance(this.instance, null);
    }
}