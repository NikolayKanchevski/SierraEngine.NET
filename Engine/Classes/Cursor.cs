using System.Numerics;
using Glfw;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Classes;

public static class Cursor
{
    public static Vector2 cursorPosition { get; private set; }
    public static Vector2 lastCursorPosition { get; private set; }
    public static Vector2 cursorPositionNormalized { get; private set; }
    public static bool cursorShown { get; private set; } = true;

    private static Vector2 cursorOffset = Vector2.Zero;
    private static bool cursorPositionSet;

    public static float GetHorizontalCursorOffset()
    {
        return cursorOffset.X;
    }

    public static float GetVerticalCursorOffset()
    {
        return cursorOffset.Y;
    }

    public static void SetCursorPosition(in Vector2 newPosition)
    {
        Glfw3.SetCursorPosition(VulkanCore.glfwWindow, newPosition.X, newPosition.Y);
        
        CursorPositionCallback(VulkanCore.glfwWindow, newPosition.X, newPosition.Y);
        
        ResetCursorOffset();
    }
    
    public static void SetCursorPositionNormalized(in Vector2 newPosition)
    {
        Vector2 nonNormalizedPosition = new Vector2(newPosition.X * VulkanCore.window.width, newPosition.Y * VulkanCore.window.height);
        
        SetCursorPosition(nonNormalizedPosition);
    }

    public static void CenterCursor()
    {
        SetCursorPositionNormalized(new Vector2(0.5f, 0.5f));
    }

    public static void ShowCursor(bool centerCursor = true)
    {
        cursorShown = true;
        Glfw3.SetInputMode(VulkanCore.glfwWindow, InputMode.Cursor, 212993);
        
        if (centerCursor) CenterCursor();
        
        ResetCursorOffset();
    }

    public static void HideCursor()
    {
        cursorShown = false;
        Glfw3.SetInputMode(VulkanCore.glfwWindow, InputMode.Cursor, 212995);
        
        ResetCursorOffset();
    }

    public static void SetCursorVisibility(bool showCursor)
    {
        lastCursorPosition = cursorPosition;
        
        if (showCursor) ShowCursor();
        else HideCursor();
    }

    public static void ResetCursorOffset()
    {
        cursorOffset = Vector2.Zero;
    }

    public static Vector2 GetGlfwCursorPosition()
    {
        Glfw3.GetCursorPosition(VulkanCore.glfwWindow, out double x, out double y);
        return new Vector2((float)x, (float)y);
    }

    public static void Start()
    {
        Glfw3.GetCursorPosition(VulkanCore.glfwWindow, out double cursorX, out double cursorY);
        SetCursorPosition(new Vector2((float) cursorX, (float) cursorY));
    }

    public static void Update()
    {
        if (cursorPositionSet)
        {
            cursorPositionSet = false;
            return;
        }

        ResetCursorOffset();
    }

    public static void CursorPositionCallback(IntPtr glfwWindow, double xPosition, double yPosition)
    {
        lastCursorPosition = cursorPosition;
        
        yPosition = Math.Abs(yPosition - VulkanCore.window.height);
        
        cursorPosition = new Vector2((float) xPosition, (float) yPosition);
        cursorPositionNormalized = new Vector2((float) (xPosition / VulkanCore.window.width), (float) (yPosition / VulkanCore.window.height));

        cursorOffset = new Vector2(lastCursorPosition.X - cursorPosition.X, lastCursorPosition.Y - cursorPosition.Y);
        cursorPositionSet = true;
    }
}