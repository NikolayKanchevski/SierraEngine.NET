using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using Glfw;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkInstance instance;

    private readonly List<string> requiredInstanceExtensions = new List<string>()
    {
        
    };
    
#if DEBUG
    private const bool VALIDATION_ENABLED = true;
#else
    private const bool VALIDATION_ENABLED = false; 
#endif

    private readonly string[] validationLayers = new string[]
    {
        "VK_LAYER_KHRONOS_validation"
    };
    
    private void CreateInstance()
    {
        // Create application information
        VkApplicationInfo applicationInfo = new VkApplicationInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
            pApplicationName = "Sierra Engine".ToPointer(),
            applicationVersion = Utilities.Version(1, 0, 0),
            pEngineName = "No Engine".ToPointer(),
            engineVersion = Utilities.Version(1, 0, 0),
            apiVersion = Utilities.Version(1, 2, 0),
        };
        
        // Get conditional extensions
        requiredInstanceExtensions.AddRange(Glfw3.GetRequiredInstanceExtensions());
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
        if (VALIDATION_ENABLED)
        {
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
        }
        
        // Create instance
        fixed (VkInstance* instancePtr = &instance)
        {
            Utilities.CheckErrors(VulkanNative.vkCreateInstance(&instanceCreateInfo, null, instancePtr));
        }

        // Deallocate useless memory
        Marshal.FreeHGlobal((IntPtr) applicationInfo.pApplicationName);
        Marshal.FreeHGlobal((IntPtr) applicationInfo.pEngineName);
    }
}