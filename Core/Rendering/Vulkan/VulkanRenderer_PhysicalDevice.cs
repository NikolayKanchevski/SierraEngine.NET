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
                SystemInformation.PopulateSystemInfo();
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
}