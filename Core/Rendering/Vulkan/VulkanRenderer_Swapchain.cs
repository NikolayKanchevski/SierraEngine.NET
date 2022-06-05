using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkSwapchainKHR swapchain;
    private VkFormat swapchainImageFormat;
    private VkExtent2D swapchainExtent;

    private VkImage[] swapchainImages;
    private VkImageView[] swapchainImageViews;
    
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
            Utilities.CheckErrors(VulkanNative.vkCreateSwapchainKHR(this.logicalDevice, &swapchainCreateInfo, null, swapchainPtr));
        }

        // Get swapchain images
        VulkanNative.vkGetSwapchainImagesKHR(this.logicalDevice, this.swapchain, &imageCount, null);

        // Resize the swapchain images array and extract every swapchain image
        swapchainImages = new VkImage[imageCount];
        fixed (VkImage* currentSwapchainImagePtr = swapchainImages)
        {
            VulkanNative.vkGetSwapchainImagesKHR(this.logicalDevice, this.swapchain, &imageCount, currentSwapchainImagePtr);
        }
    }

    private void CreateSwapchainImageViews()
    {
        // Resize the image views array to have the same size as the swapchain images one
        swapchainImageViews = new VkImageView[swapchainImages.Length];

        // Loop trough each image view
        for (int i = 0; i < swapchainImageViews.Length; i++)
        {
            // Assign its creation info
            VkImageViewCreateInfo imageViewCreateInfo = new VkImageViewCreateInfo()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
                image = swapchainImages[i],
                viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
                format = swapchainImageFormat
            };

            // Set colors to be automatically assigned and not swapped
            imageViewCreateInfo.components.r = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY;
            imageViewCreateInfo.components.g = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY;
            imageViewCreateInfo.components.b = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY;
            imageViewCreateInfo.components.a = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY;

            // Set how the image view can be read / used
            imageViewCreateInfo.subresourceRange.aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT;
            imageViewCreateInfo.subresourceRange.baseMipLevel = 0;
            imageViewCreateInfo.subresourceRange.levelCount = 1;
            imageViewCreateInfo.subresourceRange.baseArrayLayer = 0;
            imageViewCreateInfo.subresourceRange.layerCount = 1;

            // Put the image view in the array
            fixed (VkImageView* imageViewPtr = &swapchainImageViews[i])
            {
                Utilities.CheckErrors(VulkanNative.vkCreateImageView(this.logicalDevice, &imageViewCreateInfo, null, imageViewPtr));
            }
        }
    }
}