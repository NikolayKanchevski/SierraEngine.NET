using System.Numerics;
using Glfw;
using GLFW;
using SierraEngine.Core.Rendering.Vulkan;
using MouseButton = GLFW.MouseButton;

namespace SierraEngine.Engine.Classes;

/// <summary>
/// Contains useful methods on keyboard, mouse, and gamepad input querying.
/// </summary>
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

    private static bool atLeastOneGamepadConnected;
    private const int MAX_GAMEPADS = 8;

    private struct Gamepad
    {
        public bool connected;
        public string name;
        public float[] minimumSensitivities;
        public int[] buttons;
        public Vector2[] axes;
        public float[] triggers;
    }

    private static readonly Gamepad[] gamepads = new Gamepad[MAX_GAMEPADS]; 

    private static readonly Dictionary<string, string> shiftCharacters = new()
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

    /// <summary>
    /// Checks whether a key on the keyboard is pressed.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <returns></returns>
    public static bool GetKeyPressed(in Keys keyCode)
    {
        return keyboardKeys[(int) keyCode] == 2; // 2 = Press
    }

    /// <summary>
    /// Checks whether a key on the keyboard is held. Also returns true if pressed.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <returns></returns>
    public static bool GetKeyHeld(in Keys keyCode) 
    {
        int keyState = keyboardKeys[(int) keyCode];
        return keyState == 3 || keyState == 2; // 3 = Repeat; 2 = Press
    }

    /// <summary>
    /// Checks whether a key on the keyboard is released.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <returns></returns>
    public static bool GetKeyReleased(in Keys keyCode)
    {
        return keyboardKeys[(int) keyCode] == 1; // 1 = Release
    }

    /// <summary>
    /// Checks whether a key on the keyboard is resting - is not pressed, held, or released.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <returns></returns>
    public static bool GetKeyResting(in Keys keyCode)
    {
        return keyboardKeys[(int) keyCode] == 0; // 0 = Resting
    }

    /// <summary>
    /// Returns the name of the given key as a string.
    /// </summary>
    /// <param name="key">Which key's name to retrieve.</param>
    /// <param name="shiftPressed">Whether to use the shift variant of the key.</param>
    /// <returns></returns>
    public static string GetKeyName(in Keys key, bool shiftPressed = false)
    {
        string keyName = Glfw3.GetKeyName((Key) key, 0);

        if (key == Keys.Space) return " ";
        if (keyName == null) return "";
        
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

    /// <summary>
    /// Checks whether a button on the mouse is pressed.
    /// </summary>
    /// <param name="buttonCode">The button to check.</param>
    /// <returns></returns>
    public static bool GetMouseButtonPressed(in MouseButton buttonCode)
    {
        return mouseButtons[(int) buttonCode] == 2; // 2 = Press
    }

    /// <summary>
    /// Checks whether a button on the mouse is held. Also returns true if pressed.
    /// </summary>
    /// <param name="buttonCode">The button to check.</param>
    /// <returns></returns>
    public static bool GetMouseButtonHeld(in MouseButton buttonCode)
    {
        int buttonState = mouseButtons[(int) buttonCode];
        return buttonState == 3 || buttonState == 2; // 3 = Repeat; 2 = Press
    }
    
    /// <summary>
    /// Checks whether a button on the mouse is released.
    /// </summary>
    /// <param name="buttonCode">The button to check.</param>
    /// <returns></returns>
    public static bool GetMouseButtonReleased(in MouseButton buttonCode)
    {
        return mouseButtons[(int) buttonCode] == 1; // 1 = Release
    }

    /// <summary>
    /// Checks whether a button on the mouse is resting - is not pressed, held, or released.
    /// </summary>
    /// <param name="buttonCode">The button to check.</param>
    /// <returns></returns>
    public static bool GetMouseButtonResting(in MouseButton buttonCode)
    {
        return mouseButtons[(int) buttonCode] == 0; // 0 = Rest
    }

    /// <summary>
    /// Returns the horizontal mouse scroll delta. Horizontal scrolling is usually only supported on laptops,
    /// or special external input devices.
    /// </summary>
    /// <returns></returns>
    public static float GetHorizontalMouseScroll()
    {
        return scroll.X;
    }

    /// <summary>
    /// Returns the vertical mouse scroll delta.
    /// </summary>
    /// <returns></returns>
    public static float GetVerticalMouseScroll()
    {
        return scroll.Y;
    }

    /// <summary>
    /// Returns the name of an externally connected gamepad, if present, otherwise throws a warning and returns "GAMEPAD_NOT_CONNECTED".
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static string GetGamepadName(in int player = 0)
    {
        return CheckGamepadConnection(player) ? gamepads[player].name : "GAMEPAD_NOT_CONNECTED";
    }
    
    /// <summary>
    /// Checks whether a button on a given gamepad is pressed.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="gamepadButton">The button to check.</param>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static bool GetGamepadButtonPressed(in GamePadButton gamepadButton, in int player = 0)
    {
        return CheckGamepadConnection(player) && gamepads[player].buttons[(int) gamepadButton] == 2;
    }
    
    /// <summary>
    /// Checks whether a button on a given gamepad is held. Also returns true if pressed.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="gamepadButton">The button to check.</param>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static bool GetGamepadButtonHeld(in GamePadButton gamepadButton, in int player = 0)
    {
        return CheckGamepadConnection(player) && (gamepads[player].buttons[(int) gamepadButton] == 3 || gamepads[player].buttons[(int) gamepadButton] == 2);
    }
    
    /// <summary>
    /// Checks whether a button on a given gamepad is released.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="gamepadButton">The button to check.</param>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static bool GetGamepadButtonReleased(in GamePadButton gamepadButton, in int player = 0)
    {
        return CheckGamepadConnection(player) && gamepads[player].buttons[(int) gamepadButton] == 1;
    }

    
    /// <summary>
    /// Checks whether a button on a given gamepad is resting - is not pressed, held, or released.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="gamepadButton">The button to check.</param>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static bool GetGamepadButtonResting(in GamePadButton gamepadButton, in int player = 0)
    {
        return CheckGamepadConnection(player) && gamepads[player].buttons[(int) gamepadButton] == 0;
    }

    /// <summary>
    /// Returns the a Vector2 where X is the horizontal axis of the left stick (LSB) and Y the vertical.
    /// See <see cref="GetHorizontalGamepadLeftStickAxis"/> and/or <see cref="GetVerticalGamepadLeftStickAxis"/> 
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static Vector2 GetGamepadLeftStickAxis(in int player = 0)
    {
        return CheckGamepadConnection(player) ? gamepads[player].axes[0] * -1 : Vector2.Zero;
    }

    /// <summary>
    /// Returns the a value between -1 and 1 indicating the current input on the horizontal axis of the left (LSB) stick.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static float GetHorizontalGamepadLeftStickAxis(in int player = 0)
    {
        return CheckGamepadConnection(player) ? gamepads[player].axes[0].X * -1 : 0.0f;
    }

    /// <summary>
    /// Returns the a value between -1 and 1 indicating the current input on the vertical axis of the left (LSB) stick.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static float GetVerticalGamepadLeftStickAxis(in int player = 0)
    {
        return CheckGamepadConnection(player) ? gamepads[player].axes[0].Y * -1 : 0.0f;
    }

    /// <summary>
    /// Returns the a Vector2 where X is the horizontal axis of the left right (RSB) and Y the vertical.
    /// See <see cref="GetHorizontalGamepadRightStickAxis"/> and/or <see cref="GetVerticalGamepadRightStickAxis"/> 
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static Vector2 GetGamepadRightStickAxis(in int player = 0)
    {
        return CheckGamepadConnection(player) ? gamepads[player].axes[1] * -1 : Vector2.Zero;
    }

    /// <summary>
    /// Returns the a value between -1 and 1 indicating the current input on the horizontal axis of the right (RSB) stick.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static float GetHorizontalGamepadRightStickAxis(in int player = 0)
    {
        return CheckGamepadConnection(player) ? gamepads[player].axes[1].X * -1 : 0.0f;
    }

    /// <summary>
    /// Returns the a value between -1 and 1 indicating the current input on the vertical axis of right (RSB) the stick.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static float GetVerticalGamepadRightStickAxis(in int player = 0)
    {
        return CheckGamepadConnection(player) ? gamepads[player].axes[1].Y * -1 : 0.0f;
    }

    /// <summary>
    /// Returns a value between -1 and 1 indicating how hard the left trigger (LT button) is pressed.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static float GetGamepadLeftTriggerAxis(in int player = 0)
    {
        return CheckGamepadConnection(player) ? gamepads[player].triggers[0] : 0.0f;
    }
    
    /// <summary>
    /// Returns a value between -1 and 1 indicating how hard the right trigger (RT button) is pressed.
    /// <para>See also <a href="https://gist.github.com/palmerj/586375bcc5bc83ccdaf00c6f5f863e86">this link</a>
    /// if you are not familiar with the buttons and layout of gamepads.</para>
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static float GetGamepadRightTriggerAxis(in int player = 0)
    {
        return CheckGamepadConnection(player) ? gamepads[player].triggers[1] : 0.0f;
    }
    
    /// <summary>
    /// Checks whether a given gamepad is connected and detected.
    /// </summary>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    /// <returns></returns>
    public static bool GamepadConnected(in int player = 0)
    {
        return player < MAX_GAMEPADS && gamepads[player].connected;
    }

    /// <summary>
    /// Due to <a href="https://www.ign.com/articles/the-real-science-behind-controller-drift">controller drifting</a>,
    /// every connected gamepad has a by-default minimum sensitivity of 0.20f, which means that any stick input with a smaller
    /// value is simply reset to 0.0f. This method allows you to tweak every gamepad's sensitivity based on its drift.
    /// Assigns the new sensitivity for both gamepad sticks.
    /// </summary>
    /// <param name="minimumSensitivity">New minimum sensitivity to use.</param>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    public static void SetGamepadMinimumStickSensitivity(in float minimumSensitivity, in int player = 0)
    {
        if (CheckGamepadConnection(player))
        {
            gamepads[player].minimumSensitivities[0] = minimumSensitivity;
            gamepads[player].minimumSensitivities[1] = minimumSensitivity;
        }
    }

    /// <summary>
    /// Due to <a href="https://www.ign.com/articles/the-real-science-behind-controller-drift">controller drifting</a>,
    /// every connected gamepad has a by-default minimum sensitivity of 0.20f, which means that any stick input with a smaller
    /// value is simply reset to 0.0f. This method allows you to tweak every gamepad's sensitivity based on its drift.
    /// Assigns the new sensitivity for only the left gamepad stick.
    /// </summary>
    /// <param name="minimumSensitivity">New minimum sensitivity to use.</param>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    public static void SetLeftStickGamepadMinimumSensitivity(in float minimumSensitivity, in int player = 0)
    {
        if (CheckGamepadConnection(player)) gamepads[player].minimumSensitivities[0] = minimumSensitivity;
    }

    /// <summary>
    /// Due to <a href="https://www.ign.com/articles/the-real-science-behind-controller-drift">controller drifting</a>,
    /// every connected gamepad has a by-default minimum sensitivity of 0.20f, which means that any stick input with a smaller
    /// value is simply reset to 0.0f. This method allows you to tweak every gamepad's sensitivity based on its drift.
    /// Assigns the new sensitivity for only the right gamepad stick.
    /// </summary>
    /// <param name="minimumSensitivity">New minimum sensitivity to use.</param>
    /// <param name="player">Which player's gamepad to use (also referred to as an ID of the gamepad).</param>
    public static void SetRightStickGamepadMinimumSensitivity(in float minimumSensitivity, in int player = 0)
    {
        if (CheckGamepadConnection(player)) gamepads[player].minimumSensitivities[1] = minimumSensitivity;
    }
    
    private static bool CheckGamepadConnection(in int player = 0)
    {
        if (!atLeastOneGamepadConnected)
        {
            VulkanDebugger.ThrowWarning("No gamepads are found on the system");
            return false;
        }
        
        if (player >= MAX_GAMEPADS || !gamepads[player].connected)
        {
            VulkanDebugger.ThrowWarning($"Gamepad with an ID of [{ player }] is not connected");
            return false;
        }

        return true;
    }

    public static void Start()
    {
        for (int i = 0; i < MAX_GAMEPADS; i++)
        {
            if (GLFW.Glfw.JoystickPresent((Joystick) i))
            {
                if (GLFW.Glfw.JoystickIsGamepad(i))
                {
                    RegisterGamepad(i);

                    atLeastOneGamepadConnected = true;
                }
            }
        }
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

        if (!atLeastOneGamepadConnected) return;
        
        for (int i = 0; i < MAX_GAMEPADS; i++)
        {
            if (gamepads[i].connected)
            {
                GamePadState gamePadState;
                GLFW.Glfw.GetGamepadState(i, out gamePadState);

                Vector2 leftAxis = new Vector2(gamePadState.GetAxis(GamePadAxis.LeftX), gamePadState.GetAxis(GamePadAxis.LeftY));
                gamepads[i].axes[0] = new Vector2(Math.Abs(leftAxis.X) >= Math.Abs(gamepads[i].minimumSensitivities[0]) ? leftAxis.X : 0.0f, Math.Abs(leftAxis.Y) >= Math.Abs(gamepads[i].minimumSensitivities[0]) ? -leftAxis.Y : 0.0f);
                
                Vector2 rightAxis = new Vector2(gamePadState.GetAxis(GamePadAxis.RightX), gamePadState.GetAxis(GamePadAxis.RightY));
                gamepads[i].axes[1] = new Vector2(Math.Abs(rightAxis.X) >= Math.Abs(gamepads[i].minimumSensitivities[1]) ? rightAxis.X : 0.0f, Math.Abs(rightAxis.Y) >= Math.Abs(gamepads[i].minimumSensitivities[1]) ? -rightAxis.Y : 0.0f);

                gamepads[i].triggers[0] = (gamePadState.GetAxis(GamePadAxis.LeftTrigger) + 1) / 2;
                gamepads[i].triggers[1] = (gamePadState.GetAxis(GamePadAxis.RightTrigger) + 1) / 2;
                
                for (int j = 0; j <= 14; j++)
                {
                    int oldState = gamepads[i].buttons[j];
                    int newState = (int) gamePadState.GetButtonState((GamePadButton) j) + 1;

                    if (oldState == 3 && newState == 2 || oldState == 2 && newState == 2) newState = 3;
                    else if (oldState == 0 && newState == 1 || oldState == 1 && newState == 1) newState = 0;

                    gamepads[i].buttons[j] = newState;
                }
            }
        }
    }

    private static void RegisterGamepad(in int player)
    {
        gamepads[player].connected = true;
        gamepads[player].minimumSensitivities = new[] { 0.2f, 0.2f };
        gamepads[player].name = GLFW.Glfw.GetGamepadName(player);
        gamepads[player].buttons = new int[15];
        gamepads[player].axes = new Vector2[2];
        gamepads[player].triggers = new float[2];
    }
    
    public static void KeyboardKeyCallback(IntPtr window, Keys keyCode, int scanCode, InputState state, ModifierKeys mods)
    {
        int intCode = (int) keyCode;
        int intAction = (int) state;
        
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

    public static void MouseButtonCallback(IntPtr window, MouseButton buttonCode, InputState state, ModifierKeys modifiers)
    {
        int intCode = (int) buttonCode;
        int intAction = (int) state;
        
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

    public static void JoysticCallback(Joystick joystick, ConnectionStatus connectionStatus)
    {
        int joystickID = (int) joystick;
        if (connectionStatus == ConnectionStatus.Connected)
        {
            if (GLFW.Glfw.JoystickIsGamepad(joystickID))
            {
                RegisterGamepad(joystickID);

                atLeastOneGamepadConnected = true;
            }
        }
        else
        {
            gamepads[joystickID].connected = false;
        }
    }
}