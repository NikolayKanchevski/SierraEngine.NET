using System.Numerics;
using Glfw;

namespace SierraEngine.Engine.Classes;

public static class Input
{
    private static readonly int[] keyboardKeys = new int[349];
    private static int lastKeySet;
    private static bool keySet;

    public static readonly List<int> pressedKeys = new List<int>(12);
    
    private static readonly int[] mouseButtons = new int[8];
    private static int lastButtonSet;
    private static bool buttonSet;

    private static Vector2 scroll;
    private static bool scrollSet;

    private static readonly Dictionary<string, string> shiftCharacters = new Dictionary<string, string>()
    {
        { "1", "!" },
        { "2", "@" },
        { "3", "#" },
        { "4", "$" },
        { "5", "%" },
        { "6", "^" },
        { "7", "&" },
        { "8", "*" },
        { "9", "(" },
        { "0", ")" },
        { "-", "_" },
        { "=", "+" },
        { "[", "{" },
        { "]", "}" },
        { ";", ":" },
        { "'", "\"" },
        { "\\", "|" },
        { ",", "<" },
        { ".", ">" },
        { "/", "?" },
        { "`", "~" },
    };

    public static bool GetKeyPressed(in Key keyCode)
    {
        int intCode = (int) keyCode;
        if (intCode >= keyboardKeys.Length) return false;
        
        return keyboardKeys[intCode] == 2; // 2 = Press
    }
    
    public static bool GetKeyHeld(in Key keyCode) 
    {
        int intCode = (int) keyCode;
        if (intCode >= keyboardKeys.Length) return false;
        
        int keyState = keyboardKeys[intCode];
        return keyState == 3 || keyState == 2; // 3 = Repeat; 2 = Press
    }
    
    public static bool GetKeyReleased(in Key keyCode)
    {
        int intCode = (int) keyCode;
        if (intCode >= keyboardKeys.Length) return false;
        
        return keyboardKeys[intCode] == 1; // 1 = Release
    }

    public static string GetKeyName(in Key key, bool shiftPressed = false)
    {
        string keyName = Glfw3.GetKeyName(key, 0);

        if (!shiftPressed)
        {
            return keyName;
        }
        else
        {
            if (keyName.ToUpper() != keyName)
            {
                return keyName.ToUpper();
            }
            else
            {
                if (shiftCharacters.TryGetValue(keyName, out var shiftedKey))
                {
                    return shiftedKey;
                }
                
                else return keyName;
            }
        }
    }

    public static bool GetMouseButtonPressed(in MouseButton buttonCode)
    {
        int intCode = (int) buttonCode;
        if (intCode >= mouseButtons.Length) return false;
        
        return mouseButtons[intCode] == 2; // 2 = Press
    }

    public static bool GetMouseButtonHeld(in MouseButton buttonCode)
    {
        int intCode = (int) buttonCode;
        if (intCode >= mouseButtons.Length) return false;
        
        int buttonState = mouseButtons[intCode];
        return buttonState == 3 || buttonState == 2; // 3 = Repeat; 2 = Press
    }
    
    public static bool GetMouseButtonReleased(in MouseButton buttonCode)
    {
        int intCode = (int) buttonCode;
        if (intCode >= mouseButtons.Length) return false;
        
        return mouseButtons[intCode] == 1; // 1 = Release
    }

    public static float GetHorizontalMouseScroll()
    {
        return scroll.X;
    }

    public static float GetVerticalMouseScroll()
    {
        return scroll.Y;
    }

    public static void Update()
    {
        if (scrollSet) scrollSet = false;
        else scroll = Vector2.Zero;

        if (keySet) keySet = false;
        else
        {
            if (keyboardKeys[lastKeySet] == 2)
            {
                pressedKeys.Remove(lastKeySet);
                keyboardKeys[lastKeySet] = 3;
            }
            else if (keyboardKeys[lastKeySet] == 1) keyboardKeys[lastKeySet] = 0;
        }
        
        if (buttonSet) buttonSet = false;
        else
        {
            if (mouseButtons[lastButtonSet] == 2) mouseButtons[lastButtonSet] = 3;
            else if (mouseButtons[lastButtonSet] == 1) mouseButtons[lastButtonSet] = 0;
        }
    }
    
    public static void KeyboardKeyCallback(IntPtr glfwWindow, Key keyCode, int scancode, InputAction action, Modifier mods)
    {
        int intCode = (int) keyCode;
        int intAction = (int) action;
        
        if (intCode >= keyboardKeys.Length || intCode < 0) return;
        
        keyboardKeys[intCode] = intAction + 1;
        lastKeySet = intCode;
        keySet = true;

        if (keyboardKeys[intCode] == 2 || keyboardKeys[intCode] == 3 && pressedKeys.Count < pressedKeys.Capacity)
        {
            if (intCode is >= 32 and <= 96)
            {
                pressedKeys.Add(intCode);
            }
        }
    }

    public static void MouseButtonCallback(IntPtr glfwWindow, MouseButton buttonCode, InputAction action, Modifier mods)
    {
        int intCode = (int) buttonCode;
        int intAction = (int) action;
        
        if (intCode >= mouseButtons.Length || intCode < 0) return;

        mouseButtons[intCode] = intAction + 1;
        lastButtonSet = intCode;
        buttonSet = true;
    }

    public static void MouseScrollCallback(IntPtr glfwWindow, double xScroll, double yScroll)
    {
        if (Math.Abs(xScroll) >= Math.Abs(yScroll)) yScroll = 0;
        else if (Math.Abs(xScroll) < Math.Abs(yScroll)) xScroll = 0;
        
        scroll = new Vector2((float) xScroll, (float) yScroll);
        scrollSet = true;
    }
}