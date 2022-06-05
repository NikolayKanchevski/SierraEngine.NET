using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkPhysicalDevice physicalDevice;
    private readonly List<string> requiredDeviceExtensions = new List<string>()
    {
        "VK_KHR_swapchain"
    };
    
    private void GetPhysicalDevice()
    {
        // Retrieve how many GPUs are found on the system
        uint physicalDeviceCount = 0;
        Utilities.CheckErrors(VulkanNative.vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, null));

        // If none throw error
        if (physicalDeviceCount == 0)
        {
            VulkanDebugger.ThrowError("No GPUs found on the system");
        }

        // Put all found GPUs in an array
        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        Utilities.CheckErrors(VulkanNative.vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, physicalDevices));

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

                VkPhysicalDeviceProperties physicalDeviceProperties = new VkPhysicalDeviceProperties();
                VulkanNative.vkGetPhysicalDeviceProperties(physicalDevice, &physicalDeviceProperties);
                
                VulkanDebugger.DisplayInfo($"Supported GPU found: { Utilities.GetString(physicalDeviceProperties.deviceName) }");
                
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
        }
    }
}