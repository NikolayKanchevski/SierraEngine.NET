using Evergine.Bindings.Vulkan;
using Glfw;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private bool ValidationLayersSupported(in string[] givenValidationLayers)
    {
        // Get how many validation layers in total are supported
        uint layerCount = 0;
        VulkanNative.vkEnumerateInstanceLayerProperties(&layerCount, null);

        // Create an array and store the supported layers
        VkLayerProperties[] layerPropertiesArray = new VkLayerProperties[layerCount];
        fixed (VkLayerProperties* currentProperties = layerPropertiesArray)
        {
            VulkanNative.vkEnumerateInstanceLayerProperties(&layerCount, currentProperties);
        }
        
        // Check if the given layers are in the supported array
        foreach (var requiredLayer in givenValidationLayers)
        {
            bool extensionSupported = Array.Exists(layerPropertiesArray, o => Utilities.GetString(o.layerName) == requiredLayer);
            if (!extensionSupported)
            {
                // Write which layers are not supported
                VulkanDebugger.ThrowWarning($"Validation layer { requiredLayer } is not supported");
                return false;
            }
        }

        return true;
    }
    
    private bool InstanceExtensionsSupported(in string[] givenExtensions)
    {
        // Get how many extensions are supported in total
        uint extensionCount;
        VulkanNative.vkEnumerateInstanceExtensionProperties(null, &extensionCount, null);

        // Create an array to store the supported extensions
        VkExtensionProperties[] extensionPropertiesArray = new VkExtensionProperties[extensionCount];
        fixed (VkExtensionProperties* currentPropertiesPtr = extensionPropertiesArray)
        {
            VulkanNative.vkEnumerateInstanceExtensionProperties(null, &extensionCount, currentPropertiesPtr);
        }
        
        // Check if each given extension is in the supported extensions array
        foreach (var requiredExtension in givenExtensions)
        {
            bool extensionSupported = Array.Exists(extensionPropertiesArray, o => Utilities.GetString(o.extensionName) == requiredExtension);
            if (!extensionSupported)
            {
                // Write which extensions are not supported
                VulkanDebugger.ThrowWarning($"Instance extension { requiredExtension } is not supported");
                return false;
            }
        }

        return true;
    }

    private bool DeviceExtensionsSupported(in VkPhysicalDevice givenPhysicalDevice, in string[] givenExtensions)
    {
        // Get how many extensions are supported in total
        uint extensionCount;
        VulkanNative.vkEnumerateDeviceExtensionProperties(givenPhysicalDevice, null, &extensionCount, null);

        // Create an array to store the supported extensions
        VkExtensionProperties[] extensionPropertiesArray = new VkExtensionProperties[extensionCount];
        fixed (VkExtensionProperties* currentPropertiesPtr = extensionPropertiesArray)
        {
            VulkanNative.vkEnumerateDeviceExtensionProperties(givenPhysicalDevice, null, &extensionCount, currentPropertiesPtr);
        }
        
        // Check if each given extension is in the supported extensions array
        foreach (var requiredExtension in givenExtensions)
        {
            bool extensionSupported = Array.Exists(extensionPropertiesArray, o => Utilities.GetString(o.extensionName) == requiredExtension);
            if (!extensionSupported)
            {
                // Write which extensions are not supported
                VulkanDebugger.ThrowWarning($"Device extension { requiredExtension } is not supported");
                return false;
            }
        }

        return true;
    }

    private bool DeviceExtensionSupported(in VkPhysicalDevice givenPhysicalDevice, string requiredExtension)
    {
        // Get how many extensions are supported in total
        uint extensionCount;
        VulkanNative.vkEnumerateDeviceExtensionProperties(givenPhysicalDevice, null, &extensionCount, null);

        // Create an array to store the supported extensions
        VkExtensionProperties[] extensionPropertiesArray = new VkExtensionProperties[extensionCount];
        fixed (VkExtensionProperties* currentPropertiesPtr = extensionPropertiesArray)
        {
            VulkanNative.vkEnumerateDeviceExtensionProperties(givenPhysicalDevice, null, &extensionCount, currentPropertiesPtr);
        }
        
        // Check if the given extension is in the supported extensions array
        return Array.Exists(extensionPropertiesArray, o => Utilities.GetString(o.extensionName) == requiredExtension);
    }
    
    private bool PhysicalDeviceSuitable(in VkPhysicalDevice givenPhysicalDevice)
    {
        // Get the features of the given GPU
        VkPhysicalDeviceFeatures physicalDeviceFeatures = new VkPhysicalDeviceFeatures();
        VulkanNative.vkGetPhysicalDeviceFeatures(givenPhysicalDevice, &physicalDeviceFeatures);

        // Get the queue indices for it and check if they are valid
        QueueFamilyIndices familyIndices = FindQueueFamilies(in givenPhysicalDevice);
        bool indicesValid = familyIndices.IsValid();

        // Check if all required extensions are supported
        bool extensionsSupported = DeviceExtensionsSupported(in givenPhysicalDevice, requiredDeviceExtensions.ToArray());
        
        // Check for required features
        bool featuresSupported = !(this.renderingMode != RenderingMode.Fill && !physicalDeviceFeatures.fillModeNonSolid);

        // Check if the swapchain type is supported
        SwapchainSupportDetails swapchainSupportDetails = GetSwapchainSupportDetails(in givenPhysicalDevice);
        bool swapchainAdequate = !swapchainSupportDetails.formats.IsNullOrEmpty() && !swapchainSupportDetails.presentModes.IsNullOrEmpty();

        return indicesValid && extensionsSupported && swapchainAdequate && featuresSupported;
    }
    
    struct SwapchainSupportDetails {
        public VkSurfaceCapabilitiesKHR capabilities;
        public VkSurfaceFormatKHR[] formats;
        public VkPresentModeKHR[] presentModes;
    };

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

    private struct QueueFamilyIndices
    {
        public QueueFamilyIndices()
        {
        }
        
        public uint? graphicsFamily = null;
        public uint? presentFamily = null;

        public bool IsValid()
        {
            return graphicsFamily.HasValue && presentFamily.HasValue;
        }
    }

    private QueueFamilyIndices FindQueueFamilies(in VkPhysicalDevice givenPhysicalDevice)
    {
        QueueFamilyIndices indices = new QueueFamilyIndices();

        // Get how many family properties are available
        uint queueFamilyPropertiesCount = 0;
        VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(givenPhysicalDevice, &queueFamilyPropertiesCount, null);

        // Put each of them in an array
        VkQueueFamilyProperties[] vkQueueFamilyPropertiesArray = new VkQueueFamilyProperties[queueFamilyPropertiesCount];
        fixed (VkQueueFamilyProperties* currentQueueProperties = vkQueueFamilyPropertiesArray)
        {
            VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(givenPhysicalDevice, &queueFamilyPropertiesCount, currentQueueProperties);
        }

        // Iterate trough each
        for (uint i = 0; i < queueFamilyPropertiesCount; i++)
        {
            // Save the current one
            var currentQueueProperties = vkQueueFamilyPropertiesArray[i];
            
            // Check if the current queue has a graphics family
            if ((currentQueueProperties.queueFlags & VkQueueFlags.VK_QUEUE_GRAPHICS_BIT) != 0)
            {
                indices.graphicsFamily = i;
            }
            
            // Check if the current queue supports presentation
            VkBool32 presentationSupported;
            VulkanNative.vkGetPhysicalDeviceSurfaceSupportKHR(givenPhysicalDevice, i, this.surface, &presentationSupported);

            // If so set its presentation family
            if (presentationSupported)
            {
                indices.presentFamily = i;
            }

            // If the indices are already valid there's no need to continue the loop
            if (indices.IsValid())
            {
                break;
            }
        }

        return indices;
    }

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
        int width, height;
        Glfw3.GetFramebufferSize(window.GetCoreWindow(), out width, out height);
        
        // Save the sizes in a struct
        VkExtent2D createdExtent = new VkExtent2D()
        {
            width = (uint) width,
            height = (uint) height
        };
        
        // Clamp the width and height so they don't exceed the maximums
        createdExtent.width = Math.Max(givenCapabilities.minImageExtent.width, Math.Min(givenCapabilities.maxImageExtent.width, createdExtent.width));
        createdExtent.height = Math.Max(givenCapabilities.minImageExtent.height, Math.Min(givenCapabilities.maxImageExtent.height, createdExtent.height));

        // Return the manually created extent
        return createdExtent;
    }

    private VkShaderModule CreateShaderModule(string fileName)
    {
        // Read bytes from the given file
        var shaderByteCode = File.ReadAllBytes(fileName);

        // Set module creation info
        VkShaderModuleCreateInfo moduleCreateInfo = new VkShaderModuleCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO,
            codeSize = (UIntPtr) shaderByteCode.Length,
        };

        fixed (byte* shaderByteCodePtr = shaderByteCode)
        {
            moduleCreateInfo.pCode = (uint*) shaderByteCodePtr;
        }

        // Create shader module
        VkShaderModule shaderModule;
        Utilities.CheckErrors(VulkanNative.vkCreateShaderModule(this.logicalDevice, &moduleCreateInfo, null, &shaderModule));

        return shaderModule;
    }
}