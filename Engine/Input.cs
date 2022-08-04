using Glfw;

namespace SierraEngine.Engine;

public static class Input
{
    private static readonly int[] keyboardKeys = new int[348];
    private static int[] mouseButtons = new int[7];
    
    public static bool GetKeyPressed(Key keyCode) {
        return keyboardKeys[(int) keyCode] == (int) InputAction.Press + 1;
    }
    
    public static bool GetKeyHeld(Key keyCode) 
    {
        int keyStatus = keyboardKeys[(int) keyCode];
        return keyStatus == (int) InputAction.Press + 1 || keyStatus == (int) InputAction.Repeat + 1;
    }
    
    public static bool GetKeyReleased(Key keyCode)
    {
        return keyboardKeys[(int) keyCode] == (int) InputAction.Release + 1;
    }
    
    public static void KeyboardKeyCallback(IntPtr glfwWindow, Key keyCode, int scancode, InputAction action, Modifier mods)
    {
        int intCode = (int) keyCode;
        int intAction = (int) action;
        
        if (intCode >= keyboardKeys.Length || intCode < 0) return;
        
        keyboardKeys[intCode] = intAction + 1;
        if (action == InputAction.Release)
        {
            Wait(0.1f, () => 
                keyboardKeys[intCode] = 0
            );
        }
    }

    private static void Wait(float seconds, Action action)
    {
        var timer = new System.Timers.Timer(seconds * 100);
        timer.Elapsed += delegate { action.Invoke(); };
        timer.AutoReset = false;
        timer.Start();
    }
}