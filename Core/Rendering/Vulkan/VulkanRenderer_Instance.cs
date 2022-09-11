using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using SierraEngine.Engine.Classes;
using Version = SierraEngine.Engine.Structures.Version;

namespace SierraEngine.Core.Rendering.Vulkan;

#pragma warning disable CS0162
public unsafe partial class VulkanRenderer
{
    private VkInstance instance;
    private readonly Task systemInfoTask = Task.Factory.StartNew(SystemInformation.PopulateSystemInfo);

    private readonly List<string> requiredInstanceExtensions = new List<string>();
    
#if DEBUG
    private const bool VALIDATION_ENABLED = true;
    
    private readonly string[] validationLayers = new string[]
    {
        "VK_LAYER_KHRONOS_validation"
    };
#else
    private const bool VALIDATION_ENABLED = false; 
#endif
    
    private void CreateInstance()
    {
        // Create application information
        VkApplicationInfo applicationInfo = new VkApplicationInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
            pApplicationName = "Sierra Engine".ToPointer(),
            applicationVersion = VulkanUtilities.Version(Version.MAJOR, Version.MINOR, Version.PATCH),
            pEngineName = "No Engine".ToPointer(),
            engineVersion = VulkanUtilities.Version(1, 0, 0),
            apiVersion = VulkanUtilities.Version(1, 2, 0),
        };
        
        // Get conditional extensions
        requiredInstanceExtensions.AddRange(Glfw.Glfw3.GetRequiredInstanceExtensions());
        if (VALIDATION_ENABLED) requiredInstanceExtensions.Add("VK_EXT_debug_utils");
        
        
        // Check if all extensions are supported
        bool extensionsSupported = this.InstanceExtensionsSupported(requiredInstanceExtensions.ToArray());
        if (!extensionsSupported)
        {
            VulkanDebugger.ThrowError("Cannot create instance using unsupported extensions");
        }

        // Convert extensions to pointers
        IntPtr* extensions = stackalloc IntPtr[requiredInstanceExtensions.Count];
        for (int i = 0; i < requiredInstanceExtensions.Count; i++)
        {
            extensions[i] = Marshal.StringToHGlobalAnsi(requiredInstanceExtensions[i]);
        }

        // Set up instance creation info
        VkInstanceCreateInfo instanceCreateInfo = new VkInstanceCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
            pApplicationInfo = &applicationInfo,
            enabledExtensionCount = (uint) requiredInstanceExtensions.Count,
            ppEnabledExtensionNames = (byte**) extensions
        };

        // If validation is enabled get and pass validation layers to the instance creation info
        #if DEBUG
            if (!ValidationLayersSupported(in validationLayers))
            {
                VulkanDebugger.ThrowWarning("Validation layers requested, but not available. Returning");
            }
            else
            {
                IntPtr* layers = stackalloc IntPtr[validationLayers.Length];
                for (int i = 0; i < validationLayers.Length; i++)
                {
                    layers[i] = Marshal.StringToHGlobalAnsi(validationLayers[i]);
                }

                instanceCreateInfo.enabledLayerCount = (uint) validationLayers.Length;
                instanceCreateInfo.ppEnabledLayerNames = (byte**) layers;
            }
        #endif
        
        // Create instance
        fixed (VkInstance* instancePtr = &instance)
        {
            if (VulkanNative.vkCreateInstance(&instanceCreateInfo, null, instancePtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create instance");
            }
        }

        // Deallocate useless memory
        Marshal.FreeHGlobal((IntPtr) applicationInfo.pApplicationName);
        Marshal.FreeHGlobal((IntPtr) applicationInfo.pEngineName);
    }
    
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
            bool extensionSupported = Array.Exists(layerPropertiesArray, o => VulkanUtilities.GetString(o.layerName) == requiredLayer);
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
            bool extensionSupported = Array.Exists(extensionPropertiesArray, o => VulkanUtilities.GetString(o.extensionName) == requiredExtension);
            if (!extensionSupported)
            {
                // Write which extensions are not supported
                VulkanDebugger.ThrowWarning($"Instance extension { requiredExtension } is not supported");
                return false;
            }
        }

        return true;
    }
}