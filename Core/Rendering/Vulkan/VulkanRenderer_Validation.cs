using System.Diagnostics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
#pragma warning disable CS0162
#pragma warning disable CS8618

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private delegate VkBool32 DebugCallbackDelegate(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity, VkDebugUtilsMessageTypeFlagsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT pCallbackData, void* pUserData);

    private static readonly DebugCallbackDelegate CallbackDelegate = new DebugCallbackDelegate(DebugCallback);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate VkResult VkCreateDebugUtilsMessengerExtDelegate(VkInstance instance, VkDebugUtilsMessengerCreateInfoEXT* pCreateInfo, VkAllocationCallbacks* pAllocator, VkDebugUtilsMessengerEXT* pMessenger);
    private static VkCreateDebugUtilsMessengerExtDelegate vkCreateDebugUtilsMessengerExtPtr;

    private static VkResult VkCreateDebugUtilsMessengerExt(VkInstance instance, VkDebugUtilsMessengerCreateInfoEXT* pCreateInfo, VkAllocationCallbacks* pAllocator, VkDebugUtilsMessengerEXT* pMessenger)
        => vkCreateDebugUtilsMessengerExtPtr!(instance, pCreateInfo, pAllocator, pMessenger);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void VkDestroyDebugUtilsMessengerExtDelegate(VkInstance instance, VkDebugUtilsMessengerEXT messenger, VkAllocationCallbacks* pAllocator);
    private static VkDestroyDebugUtilsMessengerExtDelegate vkDestroyDebugUtilsMessengerExtPtr;

    private static void VkDestroyDebugUtilsMessengerExt(VkInstance instance, VkDebugUtilsMessengerEXT messenger, VkAllocationCallbacks* pAllocator)
        => vkDestroyDebugUtilsMessengerExtPtr(instance, messenger, pAllocator);

    private VkDebugUtilsMessengerEXT debugMessenger;

    private static VkBool32 DebugCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity, VkDebugUtilsMessageTypeFlagsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT pCallbackData, void* pUserData)
    {
        if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.None || messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_VERBOSE_BIT_EXT) return VkBool32.True;

        if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_INFO_BIT_EXT)
        {
            VulkanDebugger.ThrowError($"Validation Info: {VulkanUtilities.GetString(pCallbackData.pMessage)}");
            return VkBool32.True;
        }
        if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_WARNING_BIT_EXT)
        {
            VulkanDebugger.ThrowError($"Validation Warning: {VulkanUtilities.GetString(pCallbackData.pMessage)}");
            return VkBool32.True;
        }
        else
        {
            VulkanDebugger.ThrowError($"Validation ERROR: {VulkanUtilities.GetString(pCallbackData.pMessage)}");
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
                vkCreateDebugUtilsMessengerExtPtr = Marshal.GetDelegateForFunctionPointer<VkCreateDebugUtilsMessengerExtDelegate>(funcPtr);

                VkDebugUtilsMessengerCreateInfoEXT createInfo;
                this.PopulateDebugMessengerCreateInfo(out createInfo);
                if (VkCreateDebugUtilsMessengerExt(instance, &createInfo, null, debugMessengerPtr) != VkResult.VK_SUCCESS)
                {
                    VulkanDebugger.ThrowError("Failed to create debug messenger");
                }
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
            vkDestroyDebugUtilsMessengerExtPtr = Marshal.GetDelegateForFunctionPointer<VkDestroyDebugUtilsMessengerExtDelegate>(funcPtr);
            VkDestroyDebugUtilsMessengerExt(instance, debugMessenger, null);
        }
    }
}