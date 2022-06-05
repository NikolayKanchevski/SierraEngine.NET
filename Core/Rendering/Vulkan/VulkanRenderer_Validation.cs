using System.Diagnostics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
#pragma warning disable CS0162
#pragma warning disable CS8618

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{ 
    public delegate VkBool32 DebugCallbackDelegate(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity, VkDebugUtilsMessageTypeFlagsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT pCallbackData, void* pUserData);
    public static DebugCallbackDelegate CallbackDelegate = new DebugCallbackDelegate(DebugCallback);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate VkResult vkCreateDebugUtilsMessengerEXTDelegate(VkInstance instance, VkDebugUtilsMessengerCreateInfoEXT* pCreateInfo, VkAllocationCallbacks* pAllocator, VkDebugUtilsMessengerEXT* pMessenger);
    private static vkCreateDebugUtilsMessengerEXTDelegate vkCreateDebugUtilsMessengerEXT_ptr;
    public static VkResult vkCreateDebugUtilsMessengerEXT(VkInstance instance, VkDebugUtilsMessengerCreateInfoEXT* pCreateInfo, VkAllocationCallbacks* pAllocator, VkDebugUtilsMessengerEXT* pMessenger)
        => vkCreateDebugUtilsMessengerEXT_ptr(instance, pCreateInfo, pAllocator, pMessenger);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void vkDestroyDebugUtilsMessengerEXTDelegate(VkInstance instance, VkDebugUtilsMessengerEXT messenger, VkAllocationCallbacks* pAllocator);
    private static vkDestroyDebugUtilsMessengerEXTDelegate vkDestroyDebugUtilsMessengerEXT_ptr;
    public static void vkDestroyDebugUtilsMessengerEXT(VkInstance instance, VkDebugUtilsMessengerEXT messenger, VkAllocationCallbacks* pAllocator)
        => vkDestroyDebugUtilsMessengerEXT_ptr(instance, messenger, pAllocator);

    private VkDebugUtilsMessengerEXT debugMessenger;

    public static VkBool32 DebugCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity, VkDebugUtilsMessageTypeFlagsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT pCallbackData, void* pUserData)
    {
        if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.None || messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_VERBOSE_BIT_EXT) return VkBool32.True;

        if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_INFO_BIT_EXT)
        {
            VulkanDebugger.ThrowError($"Validation Info: {Utilities.GetString(pCallbackData.pMessage)}");
            return VkBool32.True;
        }
        else if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_WARNING_BIT_EXT)
        {
            VulkanDebugger.ThrowError($"Validation Warning: {Utilities.GetString(pCallbackData.pMessage)}");
            return VkBool32.True;
        }
        else
        {
            VulkanDebugger.ThrowError($"Validation ERROR: {Utilities.GetString(pCallbackData.pMessage)}");
            return VkBool32.False;
        }
    }
    
    private void CreateDebugMessenger()
    {
        if (!VALIDATION_ENABLED) return;
        
        fixed (VkDebugUtilsMessengerEXT* debugMessengerPtr = &debugMessenger)
        {
            var funcPtr = VulkanNative.vkGetInstanceProcAddr(instance, "vkCreateDebugUtilsMessengerEXT".ToPointer());
            if (funcPtr != IntPtr.Zero)
            {
                vkCreateDebugUtilsMessengerEXT_ptr = Marshal.GetDelegateForFunctionPointer<vkCreateDebugUtilsMessengerEXTDelegate>(funcPtr);

                VkDebugUtilsMessengerCreateInfoEXT createInfo;
                this.PopulateDebugMessengerCreateInfo(out createInfo);
                Utilities.CheckErrors(vkCreateDebugUtilsMessengerEXT(instance, &createInfo, null, debugMessengerPtr));
            }
        }
    }

    private void PopulateDebugMessengerCreateInfo(out VkDebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo = default;
        createInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT;
        createInfo.messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_VERBOSE_BIT_EXT | VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_WARNING_BIT_EXT | VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT;
        createInfo.messageType = VkDebugUtilsMessageTypeFlagsEXT.VK_DEBUG_UTILS_MESSAGE_TYPE_GENERAL_BIT_EXT | VkDebugUtilsMessageTypeFlagsEXT.VK_DEBUG_UTILS_MESSAGE_TYPE_PERFORMANCE_BIT_EXT | VkDebugUtilsMessageTypeFlagsEXT.VK_DEBUG_UTILS_MESSAGE_TYPE_VALIDATION_BIT_EXT;
        createInfo.pfnUserCallback = Marshal.GetFunctionPointerForDelegate(CallbackDelegate);
        createInfo.pUserData = null;
    }       

    private void DestroyDebugMessenger()
    {
        if (!VALIDATION_ENABLED) return;
        
        var funcPtr = VulkanNative.vkGetInstanceProcAddr(instance, "vkDestroyDebugUtilsMessengerEXT".ToPointer());
        if (funcPtr != IntPtr.Zero)
        {
            vkDestroyDebugUtilsMessengerEXT_ptr = Marshal.GetDelegateForFunctionPointer<vkDestroyDebugUtilsMessengerEXTDelegate>(funcPtr);
            vkDestroyDebugUtilsMessengerEXT(instance, debugMessenger, null);
        }
    }
}