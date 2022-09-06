using System.Numerics;
using Evergine.Bindings.Vulkan;
using Image = SierraEngine.Core.Rendering.Vulkan.Abstractions.Image;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkSwapchainKHR swapchain;
    private VkFormat swapchainImageFormat;
    private VkExtent2D swapchainExtent;

    private Image[] swapchainImages = null!;
    private Image swapchainImage => swapchainImages[0];
    
    private struct SwapchainSupportDetails {
        public VkSurfaceCapabilitiesKHR capabilities;
        public VkSurfaceFormatKHR[] formats;
        public VkPresentModeKHR[] presentModes;
    };

    private VkSurfaceFormatKHR ChooseSwapchainFormat(in VkSurfaceFormatKHR[] givenFormats)
    {
        // Loop trough each to check if it is one of the preferred types
        foreach (var availableFormat in givenFormats)
        {
            if (availableFormat.format == VkFormat.VK_FORMAT_B8G8R8A8_SRGB && availableFormat.colorSpace == VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR) 
            {
                return availableFormat;
            }
        }

        // Otherwise just return the very first one
        return givenFormats[0];
    }

    private VkPresentModeKHR ChooseSwapchainPresentMode(in VkPresentModeKHR[] givenPresentModes)
    {
        // Loop trough each to check if it is VK_PRESENT_MODE_MAILBOX_KHR
        foreach (var availablePresentMode in givenPresentModes)
        {
            if (availablePresentMode == VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR)
            {
                return availablePresentMode;
            }
        }

        // Otherwise return VK_PRESENT_MODE_FIFO_KHR which is guaranteed to be available
        return VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;
    }

    private VkExtent2D ChooseSwapchainExtent(in VkSurfaceCapabilitiesKHR givenCapabilities)
    {
        // Check to see if the extent is already configured
        if (givenCapabilities.currentExtent.width != uint.MaxValue)
        {
            // If so just return it
            return givenCapabilities.currentExtent;
        }

        // Otherwise get the settings for it manually by finding out the width and height for it
        
        // Save the sizes in a struct
        VkExtent2D createdExtent = new VkExtent2D()
        {
            width = (uint) window.width,
            height = (uint) window.height
        };
        
        // Clamp the width and height so they don't exceed the maximums
        createdExtent.width = Math.Max(givenCapabilities.minImageExtent.width, Math.Min(givenCapabilities.maxImageExtent.width, createdExtent.width));
        createdExtent.height = Math.Max(givenCapabilities.minImageExtent.height, Math.Min(givenCapabilities.maxImageExtent.height, createdExtent.height));

        // Return the manually created extent
        return createdExtent;
    }
    
    private SwapchainSupportDetails GetSwapchainSupportDetails(in VkPhysicalDevice givenPhysicalDevice)
    {
        // Get the details of the GPU's supported swapchain
        SwapchainSupportDetails swapchainDetails = new SwapchainSupportDetails();
        VulkanNative.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(givenPhysicalDevice, surface, &swapchainDetails.capabilities);

        // Get how many formats are available
        uint formatCount;
        VulkanNative.vkGetPhysicalDeviceSurfaceFormatsKHR(givenPhysicalDevice, this.surface, &formatCount, null);

        // Check if there are not none available
        if (formatCount != 0)
        {
            // Put each of them in an array
            swapchainDetails.formats = new VkSurfaceFormatKHR[formatCount];
            fixed (VkSurfaceFormatKHR* surfaceFormatPtr = swapchainDetails.formats)
            {
                VulkanNative.vkGetPhysicalDeviceSurfaceFormatsKHR(givenPhysicalDevice, this.surface, &formatCount, surfaceFormatPtr);
            }
        }
        
        // Get how many presentation modes are available
        uint presentModesCount;
        VulkanNative.vkGetPhysicalDeviceSurfacePresentModesKHR(givenPhysicalDevice, this.surface, &presentModesCount, null);

        // Check if there are not none available
        if (presentModesCount != 0)
        {
            // Put each of them in an array
            swapchainDetails.presentModes = new VkPresentModeKHR[presentModesCount];
            fixed (VkPresentModeKHR* surfacePresentModePtr = swapchainDetails.presentModes)
            {
                VulkanNative.vkGetPhysicalDeviceSurfacePresentModesKHR(givenPhysicalDevice, this.surface, &formatCount, surfacePresentModePtr);
            }
        }

        return swapchainDetails;
    }
    
    private void CreateSwapchain()
    {
        // Lower the sample count if it is not supported
        VkSampleCountFlags highestSupportedSampleCount = this.GetHighestSupportedSampleCount();
        if (this.msaaSampleCount > highestSupportedSampleCount)  
        {
            VulkanDebugger.ThrowWarning($"Sampling MSAA level [{ this.msaaSampleCount.ToString() }] requested but is not supported by the system. It is automatically lowered to [{ highestSupportedSampleCount }] which is the highest supported setting");
            this.msaaSampleCount = highestSupportedSampleCount;
        }
        
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
        uint* queueFamilyIndicesPtr = stackalloc uint[] { queueFamilyIndices.graphicsFamily!.Value, queueFamilyIndices.presentFamily!.Value };

        // Check whether the graphics family is the same as the present one and based on that configure the creation info
        if (queueFamilyIndices.graphicsFamily != queueFamilyIndices.presentFamily)
        {
            swapchainCreateInfo.imageSharingMode = VkSharingMode.VK_SHARING_MODE_CONCURRENT;
            swapchainCreateInfo.queueFamilyIndexCount = 2;
            swapchainCreateInfo.pQueueFamilyIndices = queueFamilyIndicesPtr;
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

        VkImage[] swapchainVkImages = new VkImage[imageCount];
        swapchainImages = new Image[imageCount];
        
        // Resize the swapchain images array and extract every swapchain image
        fixed (VkImage* currentSwapchainImagePtr = swapchainVkImages)
        {
            VulkanNative.vkGetSwapchainImagesKHR(this.logicalDevice, this.swapchain, &imageCount, currentSwapchainImagePtr);
        }
        
        for (int i = 0; i < imageCount; i++)
        {
            swapchainImages[i] = new Image(
                swapchainVkImages[i], swapchainImageFormat, msaaSampleCount,
                new Vector3(swapchainExtent.width, swapchainExtent.height, 0.0f), 1, VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR
            );
            
            swapchainImages[i].GenerateImageView(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
        }

        // Assign the EngineCore's swapchain extent
        VulkanCore.swapchainExtent = swapchainExtent;
    }

    private void RecreateSwapchainObjects()
    {
        while (window.minimized || !window.focused || window.width == 0)
        {
            GLFW.Glfw.WaitEvents();
        }
        
        VulkanNative.vkDeviceWaitIdle(this.logicalDevice);
        
        DestroySwapchainObjects();
        
        CreateSwapchain();
        CreateRenderPass();
        CreateDepthBufferImage();
        CreateColorBufferImage();
        CreateFrameBuffers();
        
        imGuiController.ResizeImGui();
    }

    private void DestroySwapchainObjects()
    {
        colorImage.CleanUp();
        
        foreach (var swapchainFramebuffer in this.swapchainFrameBuffers)
        {
            swapchainFramebuffer.CleanUp();
        }
        
        depthImage.CleanUp();

        renderPass.CleanUp();
        
        foreach (Image image in swapchainImages)
        {
            image.CleanUpImageView();
        }
        
        VulkanNative.vkDestroySwapchainKHR(this.logicalDevice, this.swapchain, null);
    }
}