using Glfw;
using GlmSharp;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine;

public static class Cursor
{
    public static vec2 cursorPosition { get; private set; }
    public static vec2 cursorPositionNormalized { get; private set; }
    public static bool cursorShown { get; private set; } = true;

    public static void SetCursorPosition(in vec2 newPosition)
    {
        Glfw3.SetCursorPosition(VulkanCore.glfwWindow, newPosition.x, newPosition.y);
        
        CursorPositionCallback(VulkanCore.glfwWindow, newPosition.x, newPosition.y);
    }
    
    public static void SetCursorPositionNormalized(in vec2 newPosition)
    {
        vec2 nonNormalizedPosition = new vec2(newPosition.x * VulkanCore.window.width, newPosition.y * VulkanCore.window.height);
        
        Glfw3.SetCursorPosition(VulkanCore.glfwWindow, nonNormalizedPosition.x, nonNormalizedPosition.y);
        
        CursorPositionCallback(VulkanCore.glfwWindow, nonNormalizedPosition.x, nonNormalizedPosition.y);
    }

    public static void CenterCursor()
    {
        SetCursorPositionNormalized(new vec2(0.5f, 0.5f));
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
    
    public static void CursorPositionCallback(IntPtr glfwWindow, double xPosition, double yPosition)
    {
        yPosition = glm.Abs(yPosition - VulkanCore.window.height);
        
        cursorPosition = new vec2((float) xPosition, (float) yPosition);
        cursorPositionNormalized = new vec2((float) (xPosition / VulkanCore.window.width), (float) (yPosition / VulkanCore.window.height));
    }
}