using System;
using System.Diagnostics;
using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public static unsafe class Utilities
{
    public static byte* ToPointer(this string text)
    {
        return (byte*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(text);
    }

    public static uint Version(uint major, uint minor, uint patch)
    {
        return (major << 22) | (minor << 12) | patch;
    }

    public static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index)
    {
        return (&memoryProperties.memoryTypes_0)[index];
    }

    public static string GetString(byte* stringStart)
    {
        int characters = 0;
        while (stringStart[characters] != 0)
        {
            characters++;
        }

        return System.Text.Encoding.UTF8.GetString(stringStart, characters);
    }

    [Conditional("DEBUG")]
    public static void CheckErrors(VkResult result)
    {
        if (result != VkResult.VK_SUCCESS)
        {
            throw new InvalidOperationException(result.ToString());
        }
    }
    
    /// <summary>Indicates whether the specified array is null or has a length of zero.</summary>
    /// <param name="array">The array to test.</param>
    /// <returns>true if the array parameter is null or has a length of zero; otherwise, false.</returns>
    public static bool IsNullOrEmpty(this Array array)
    {
        return (array == null || array.Length == 0);
    }
}