namespace SierraEngine.Core.Rendering.Vulkan;

public static class VulkanDebugger
{
    #if DEBUG
        private const bool CRASH_ON_ERROR = true;
    #else
        private const bool CRASH_ON_ERROR = false;
    #endif
    
    public static void DisplayInfo(string message, bool emptyLine = false)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Blue;

        Console.WriteLine($"[i] {message}.");
        if (emptyLine) Console.WriteLine();
        Console.ForegroundColor = oldConsoleColor;
    }
    
    public static void DisplaySuccess(string message, bool emptyLine = false)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine($"[+] {message}.");
        if (emptyLine) Console.WriteLine();
        Console.ForegroundColor = oldConsoleColor;
    }
    
    public static void ThrowWarning(string message, bool emptyLine = false)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.WriteLine($"[!] {message}!");
        if (emptyLine) Console.WriteLine();
        Console.ForegroundColor = oldConsoleColor;
    }
    
    public static void ThrowError(string message, bool emptyLine = false)
    {
        ConsoleColor oldConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine($"[x] {message}!");
        if (emptyLine) Console.WriteLine();
        Console.ForegroundColor = oldConsoleColor;

        // if (CRASH_ON_ERROR)
        // {
            Environment.Exit(-1);
        // }
    }
}