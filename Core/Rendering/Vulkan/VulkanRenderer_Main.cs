using Evergine.Bindings.Vulkan;
using Exception = System.Exception;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private readonly Window window;
    
    public VulkanRenderer(ref Window window)
    {
        this.window = window;
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
            CreateGraphicsPipeline();
            CreateFrameBuffers();
            CreateCommandPool();
            CreateCommandBuffers();
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

        for (int i = 0; i < MAX_CONCURRENT_FRAMES; i++)
        {
            VulkanNative.vkDestroySemaphore(this.logicalDevice, this.imageAvailableSemaphores[i], null);
            VulkanNative.vkDestroySemaphore(this.logicalDevice, this.renderFinishedSemaphores[i], null);
            VulkanNative.vkDestroyFence(this.logicalDevice, this.frameBeingRenderedFences[i], null);
        }

        VulkanNative.vkDestroyCommandPool(this.logicalDevice, this.commandPool, null);
        
        foreach (var swapchainFramebuffer in this.swapchainFrameBuffers)
        {
            VulkanNative.vkDestroyFramebuffer(this.logicalDevice, swapchainFramebuffer, null);
        }
        
        VulkanNative.vkDestroyPipeline(this.logicalDevice, this.graphicsPipeline, null);
        VulkanNative.vkDestroyPipelineLayout(this.logicalDevice, this.graphicsPipelineLayout, null);
        
        VulkanNative.vkDestroyRenderPass(this.logicalDevice, this.renderPass, null);
        
        foreach (var imageView in this.swapchainImageViews!)
        {
            VulkanNative.vkDestroyImageView(this.logicalDevice, imageView, null);
        }
        
        VulkanNative.vkDestroySwapchainKHR(this.logicalDevice, this.swapchain, null);
        VulkanNative.vkDestroyDevice(this.logicalDevice, null);

        DestroyDebugMessenger();
        
        VulkanNative.vkDestroySurfaceKHR(this.instance, this.surface, null);
        VulkanNative.vkDestroyInstance(this.instance, null);
    }
}