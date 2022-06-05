namespace SierraEngine.Core.Rendering.Vulkan;

public static class VulkanDebugger
{
    #if DEBUG
        private const bool CRASH_ON_ERROR = true;
    #else
        private const bool CRASH_ON_ERROR = false;
    #endif
    
    public static void DisplayInfo(string message)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Blue;

        Console.WriteLine($"[i] {message}.");
        Console.ForegroundColor = oldConsoleColor;
    }
    
    public static void ThrowWarning(string message)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.WriteLine($"[!] {message}!");
        Console.ForegroundColor = oldConsoleColor;
    }
    
    public static void ThrowError(string message)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine($"[-] {message}!");
        Console.ForegroundColor = oldConsoleColor;

        if (CRASH_ON_ERROR)
        {
            Environment.Exit(-1);
        }
    }
}