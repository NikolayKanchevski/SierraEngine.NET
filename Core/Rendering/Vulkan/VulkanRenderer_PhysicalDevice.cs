using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Components;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkPhysicalDevice physicalDevice;
    private QueueFamilyIndices queueFamilyIndices;
    
    private readonly List<string> requiredDeviceExtensions = new List<string>()
    {
        "VK_KHR_swapchain"
    };
    
    private struct QueueFamilyIndices
    {
        public QueueFamilyIndices() { }
        
        public uint? graphicsFamily = null;
        public uint? presentFamily = null;

        public bool IsValid()
        {
            return graphicsFamily.HasValue && presentFamily.HasValue;
        }
    }
    
    private void GetPhysicalDevice()
    {
        // Retrieve how many GPUs are found on the system
        uint physicalDeviceCount = 0;
        if (VulkanNative.vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, null) != VkResult.VK_SUCCESS)
        {
            VulkanDebugger.ThrowError("Failed to retrieve available GPUs (physical devices)");
        }

        // If none throw error
        if (physicalDeviceCount == 0)
        {
            VulkanDebugger.ThrowError("No GPUs found on the system");
        }

        // Put all found GPUs in an array
        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        VulkanNative.vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, physicalDevices);

        // Loop trough each to see if it supports the program
        bool suitablePhysicalDeviceFound = false;
        for (int i = 0; i < physicalDeviceCount; i++)
        {
            VkPhysicalDevice currentPhysicalDevice = physicalDevices[i];
            if (PhysicalDeviceSuitable(in currentPhysicalDevice))
            {
                // TODO: Pick the MOST SUITABLE device, not the first one that is supported!
                this.physicalDevice = currentPhysicalDevice;
                suitablePhysicalDeviceFound = true;

                // Retrieve the GPU's properties
                VkPhysicalDeviceProperties deviceProperties;
                VulkanNative.vkGetPhysicalDeviceProperties(physicalDevice, &deviceProperties);
                VulkanCore.physicalDeviceProperties = deviceProperties;
                
                // Retrieve the GPU's memory properties
                VkPhysicalDeviceMemoryProperties deviceMemoryProperties;
                VulkanNative.vkGetPhysicalDeviceMemoryProperties(physicalDevice, &deviceMemoryProperties);
                VulkanCore.physicalDeviceMemoryProperties = deviceMemoryProperties;
                
                // Retrieve the GPU's features
                VkPhysicalDeviceFeatures deviceFeatures;
                VulkanNative.vkGetPhysicalDeviceFeatures(physicalDevice, &deviceFeatures);
                VulkanCore.physicalDeviceFeatures = deviceFeatures;

                // Get queue family indices
                this.queueFamilyIndices = FindQueueFamilies(currentPhysicalDevice);
                
                // Detect system properties now that we are sure the system is capable of running the Vulkan program
                Task.WaitAll(systemInfoTask);
                SystemInformation.SetUsedGPUModel(VulkanUtilities.GetString(deviceProperties.deviceName));
                
                // Show support message
                VulkanDebugger.DisplaySuccess($"Vulkan is supported by your { SystemInformation.deviceModelName } running { SystemInformation.operatingSystemVersion } [Validation: { VALIDATION_ENABLED } | CPU: { SystemInformation.cpuModelName } | GPU: { SystemInformation.gpuModelName }]");

                break;
            }
        }

        // If no GPUs support the program throw error
        if (!suitablePhysicalDeviceFound)
        {
            VulkanDebugger.ThrowError("Couldn't find a GPU that supports the program");
        }
        else
        {
            // Add mandatory conditional device extensions
            if (DeviceExtensionSupported(in this.physicalDevice, "VK_KHR_portability_subset"))
            {
                requiredDeviceExtensions.Add("VK_KHR_portability_subset");
            }

            // Assign the EngineCore's physical device
            VulkanCore.physicalDevice = this.physicalDevice;
        }
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
            bool extensionSupported = Array.Exists(extensionPropertiesArray, o => VulkanUtilities.GetString(o.extensionName) == requiredExtension);
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
        return Array.Exists(extensionPropertiesArray, o => VulkanUtilities.GetString(o.extensionName) == requiredExtension);
    }

    private bool PhysicalDeviceSuitable(in VkPhysicalDevice givenPhysicalDevice)
    {
        // Get the features of the given GPU
        VkPhysicalDeviceFeatures deviceFeatures = new VkPhysicalDeviceFeatures();
        VulkanNative.vkGetPhysicalDeviceFeatures(givenPhysicalDevice, &deviceFeatures);
        
        // Get the queue indices for it and check if they are valid
        QueueFamilyIndices familyIndices = FindQueueFamilies(in givenPhysicalDevice);
        bool indicesValid = familyIndices.IsValid();

        // Check if all required extensions are supported
        bool extensionsSupported = DeviceExtensionsSupported(in givenPhysicalDevice, requiredDeviceExtensions.ToArray());
        
        // Check for required features
        bool featuresSupported = !(this.renderingMode != RenderingMode.Fill && !deviceFeatures.fillModeNonSolid);

        // Check if the swapchain type is supported
        SwapchainSupportDetails swapchainSupportDetails = GetSwapchainSupportDetails(in givenPhysicalDevice);
        bool swapchainAdequate = !swapchainSupportDetails.formats.IsNullOrEmpty() && !swapchainSupportDetails.presentModes.IsNullOrEmpty();

        return indicesValid && extensionsSupported && swapchainAdequate && featuresSupported;
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
}