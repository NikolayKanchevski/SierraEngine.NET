using Evergine.Bindings.Vulkan;
using Glfw;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkSwapchainKHR swapchain;
    private VkFormat swapchainImageFormat;
    private VkExtent2D swapchainExtent;

    private VkImage[] swapchainImages = null!;
    private VkImageView[] swapchainImageViews = null!;
    
    private void CreateSwapchain()
    {
        // Get swapchain details
        SwapchainSupportDetails swapchainSupportDetails = GetSwapchainSupportDetails(this.physicalDevice);
        
        // Get most suitable properties - format, present mode and extent
        VkSurfaceFormatKHR surfaceFormat = ChooseSwapchainFormat(swapchainSupportDetails.formats);
        VkPresentModeKHR presentMode = ChooseSwapchainPresentMode(swapchainSupportDetails.presentModes);
        VkExtent2D extent = ChooseSwapchainExtent(swapchainSupportDetails.capabilities);

        // Save the properties locally so they can be used later on
        swapchainImageFormat = surfaceFormat.format;
        swapchainExtent = extent;
        
        // Get swapchain image count and make sure it is between the min and max allowed
        uint imageCount = swapchainSupportDetails.capabilities.minImageCount + 1;
        if (swapchainSupportDetails.capabilities.maxImageCount != 0 && imageCount > swapchainSupportDetails.capabilities.maxImageCount)
        {
            imageCount = swapchainSupportDetails.capabilities.maxImageCount;
        }

        // Set up swapchain creation info
        VkSwapchainCreateInfoKHR swapchainCreateInfo = new VkSwapchainCreateInfoKHR()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
            surface = this.surface,
            minImageCount = imageCount,
            imageFormat = surfaceFormat.format,
            imageColorSpace = surfaceFormat.colorSpace,
            imageExtent = extent,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
            preTransform = swapchainSupportDetails.capabilities.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
            presentMode = presentMode,
            clipped = VkBool32.True,
            oldSwapchain = VkSwapchainKHR.Null
        };

        // Get the queue indices
        QueueFamilyIndices familyIndices = FindQueueFamilies(this.physicalDevice);
        uint* queueFamilyIndices = stackalloc uint[] { familyIndices.graphicsFamily!.Value, familyIndices.presentFamily!.Value };

        // Check whether the graphics family is the same as the present one and based on that configure the creation info
        if (familyIndices.graphicsFamily != familyIndices.presentFamily)
        {
            swapchainCreateInfo.imageSharingMode = VkSharingMode.VK_SHARING_MODE_CONCURRENT;
            swapchainCreateInfo.queueFamilyIndexCount = 2;
            swapchainCreateInfo.pQueueFamilyIndices = queueFamilyIndices;
        }
        else
        {
           swapchainCreateInfo.imageSharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE;
           swapchainCreateInfo.queueFamilyIndexCount = 0; 
           swapchainCreateInfo.pQueueFamilyIndices = null; 
        }

        // Create the swapchain
        fixed (VkSwapchainKHR* swapchainPtr = &swapchain)
        {
            if (VulkanNative.vkCreateSwapchainKHR(this.logicalDevice, &swapchainCreateInfo, null, swapchainPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create swapchain");
            }
        }

        // Get swapchain images
        VulkanNative.vkGetSwapchainImagesKHR(this.logicalDevice, this.swapchain, &imageCount, null);

        // Resize the swapchain images array and extract every swapchain image
        swapchainImages = new VkImage[imageCount];
        fixed (VkImage* currentSwapchainImagePtr = swapchainImages)
        {
            VulkanNative.vkGetSwapchainImagesKHR(this.logicalDevice, this.swapchain, &imageCount, currentSwapchainImagePtr);
        }
        
        // Assign the EngineCore's swapchain extent
        VulkanCore.swapchainExtent = swapchainExtent;
    }

    private void CreateSwapchainImageViews()
    {
        // Resize the image views array to have the same size as the swapchain images one
        swapchainImageViews = new VkImageView[swapchainImages.Length];

        // Loop trough each image view
        for (int i = 0; i < swapchainImageViews.Length; i++)
        {
            VulkanUtilities.CreateImageView(swapchainImages[i], swapchainImageFormat, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT, 1, out swapchainImageViews[i]);
        }
    }

    private void RecreateSwapchainObjects()
    {
        while (window.minimised)
        {
            Glfw3.WaitEvents();
        }
        
        VulkanNative.vkDeviceWaitIdle(this.logicalDevice);
        
        DestroySwapchainObjects();
        
        CreateSwapchain();
        CreateSwapchainImageViews();
        CreateRenderPass();
        CreateDepthBufferImage();
        CreateFrameBuffers();
    }

    private void DestroySwapchainObjects()
    {
        foreach (var swapchainFramebuffer in this.swapchainFrameBuffers)
        {
            VulkanNative.vkDestroyFramebuffer(this.logicalDevice, swapchainFramebuffer, null);
        }
        
        VulkanNative.vkDestroyImage(this.logicalDevice, this.depthImage, null);
        VulkanNative.vkDestroyImageView(this.logicalDevice, this.depthImageView, null);
        VulkanNative.vkFreeMemory(this.logicalDevice, this.depthImageMemory, null);
        
        VulkanNative.vkDestroyRenderPass(this.logicalDevice, this.renderPass, null);
        
        foreach (var imageView in this.swapchainImageViews!)
        {
            VulkanNative.vkDestroyImageView(this.logicalDevice, imageView, null);
        }
        
        VulkanNative.vkDestroySwapchainKHR(this.logicalDevice, this.swapchain, null);
    }
}