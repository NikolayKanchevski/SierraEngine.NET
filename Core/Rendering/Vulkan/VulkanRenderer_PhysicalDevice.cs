using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkPhysicalDevice physicalDevice;
    private VkPhysicalDeviceProperties physicalDeviceProperties;
    private VkPhysicalDeviceFeatures physicalDeviceFeatures;
    
    private readonly List<string> requiredDeviceExtensions = new List<string>()
    {
        "VK_KHR_swapchain"
    };
    
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
                this.physicalDeviceProperties = deviceProperties;

                // Retrieve the GPU's features
                VkPhysicalDeviceFeatures deviceFeatures;
                VulkanNative.vkGetPhysicalDeviceFeatures(physicalDevice, &deviceFeatures);
                this.physicalDeviceFeatures = deviceFeatures;
                
                VulkanDebugger.DisplayInfo($"Supported GPU found: { VulkanUtilities.GetString(deviceProperties.deviceName) }");
                
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
            if (DeviceExtensionSupported(in this.physicalDevice, "VK_KHR_portability_subset")) requiredDeviceExtensions.Add("VK_KHR_portability_subset");

            // Assign the EngineCore's physical device
            VulkanCore.physicalDevice = this.physicalDevice;
        }
    }
}