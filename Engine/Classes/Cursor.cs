using System.Numerics;
using Glfw;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Classes;

public static class Cursor
{
    public static Vector2 cursorPosition { get; private set; }
    public static Vector2 cursorPositionNormalized { get; private set; }
    public static bool cursorShown { get; private set; } = true;

    public static void SetCursorPosition(in Vector2 newPosition)
    {
        Glfw3.SetCursorPosition(VulkanCore.glfwWindow, newPosition.X, newPosition.Y);
        
        CursorPositionCallback(VulkanCore.glfwWindow, newPosition.X, newPosition.Y);
    }
    
    public static void SetCursorPositionNormalized(in Vector2 newPosition)
    {
        Vector2 nonNormalizedPosition = new Vector2(newPosition.X * VulkanCore.window.width, newPosition.Y * VulkanCore.window.height);
        
        Glfw3.SetCursorPosition(VulkanCore.glfwWindow, nonNormalizedPosition.X, nonNormalizedPosition.Y);
        
        CursorPositionCallback(VulkanCore.glfwWindow, nonNormalizedPosition.X, nonNormalizedPosition.Y);
    }

    public static void CenterCursor()
    {
        SetCursorPositionNormalized(new Vector2(0.5f, 0.5f));
    }

    public static void ShowCursor()
    {
        cursorShown = true;
        Glfw3.SetInputMode(VulkanCore.glfwWindow, InputMode.Cursor, 212993);
    }

    public static void HideCursor()
    {
        cursorShown = false;
        Glfw3.SetInputMode(VulkanCore.glfwWindow, InputMode.Cursor, 212995);
    }

    public static void SetCursorVisibility(bool showCursor)
    {
        if (showCursor) ShowCursor();
        else HideCursor();
    }

    public static void Start()
    {
        Glfw3.GetCursorPosition(VulkanCore.glfwWindow, out double cursorX, out double cursorY);
        SetCursorPosition(new Vector2((float) cursorX, (float) cursorY));
    }
    
    public static void CursorPositionCallback(IntPtr glfwWindow, double xPosition, double yPosition)
    {
        
        yPosition = Math.Abs(yPosition - VulkanCore.window.height);
        
        cursorPosition = new Vector2((float) xPosition, (float) yPosition);
        cursorPositionNormalized = new Vector2((float) (xPosition / VulkanCore.window.width), (float) (yPosition / VulkanCore.window.height));
    }
}