using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public static class VulkanDebugger
{
    #if DEBUG
        private const bool DEBUG_MODE = true;
    #else
        private const bool DEBUG_MODE = false;
    #endif
    
    enum MessageType { Info, Success, Warning, Error }
    
    private static MessageType lastMessageType = MessageType.Success;
    
    public static void DisplayInfo(string message)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Blue;
        
        if (lastMessageType != MessageType.Info) Console.WriteLine();

        Console.WriteLine($"[i] {message}.");
        Console.ForegroundColor = oldConsoleColor;

        lastMessageType = MessageType.Info;
    }
    
    public static void DisplaySuccess(string message)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        
        if (lastMessageType != MessageType.Success) Console.WriteLine();

        Console.WriteLine($"[+] {message}.");
        Console.ForegroundColor = oldConsoleColor;

        lastMessageType = MessageType.Success;
    }
    
    public static void ThrowWarning(string message)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        
        if (lastMessageType != MessageType.Warning) Console.WriteLine();

        Console.WriteLine($"[!] {message}!");
        Console.ForegroundColor = oldConsoleColor;

        lastMessageType = MessageType.Warning;
    }
    
    public static void ThrowError(string message)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        
        if (lastMessageType != MessageType.Error) Console.WriteLine();

        Console.WriteLine($"[x] {message}!");
        Console.ForegroundColor = oldConsoleColor;

        lastMessageType = MessageType.Error;

        if (DEBUG_MODE)
        {
            Environment.Exit(-1);
        }
    }

    public static void CheckResults(in VkResult result, in string errorMessage)
    {
        if (result != VkResult.VK_SUCCESS && result != VkResult.VK_SUBOPTIMAL_KHR)
        {
            ThrowError(errorMessage + $". Error code: { result.ToString() }");
        }
    }
}