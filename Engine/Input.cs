using Glfw;

namespace SierraEngine.Engine;

public static class Input
{
    private static readonly int[] keyboardKeys = new int[348];
    private static int[] mouseButtons = new int[7];
    
    public static void KeyboardKeyCallback(IntPtr glfwWindow, Key keyCode, int scancode, InputAction action, Modifier mods)
    {
        int intCode = (int) keyCode;
        int intAction = (int) action;
        
        if (intCode >= keyboardKeys.Length || intCode < 0) return;
        
        keyboardKeys[intCode] = intAction + 1;
        if (action == InputAction.Release)
        {
            Wait(4, () => 
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

    public static void Update()
    {
        // for (int i = 0; i < keyboardKeys.Length; i++)
        // {
        //     if (keyboardKeys[i] == 1)
        //     {
        //         keyboardKeys[i] = 0;
        //     }
        // }
    }

    // void Input::RegisterKeyboardKey(GLFWwindow *window, int key, int scancode, int action, int mods) {
    //     if (key > 348) return;
    //
    //     keyboardKeys[key] = action;
    // }
    //
    // void Input::RegisterMouseButton(GLFWwindow *window, int button, int action, int mods) {
    //     if (button > 7) return;
    //
    //     mouseButtons[button] = action;
    // }
    //
    // bool Input::getKeyPressed(Key keyCode) {
    //     return keyboardKeys[keyCode] == GLFW_PRESS;
    // }
    //
    // bool Input::getKeyHeld(Key keyCode) {
    //     const uint16_t keyStatus = keyboardKeys[keyCode];
    //     return keyStatus == GLFW_PRESS || keyStatus == GLFW_REPEAT;
    // }
    //
    // bool Input::getKeyReleased(Key keyCode) {
    //     return keyboardKeys[keyCode] == GLFW_RELEASE;
    // }
    //
    // bool Input::getMouseButtonPressed(Key keyCode) {
    //     return mouseButtons[keyCode] == GLFW_PRESS;
    // }
    //
    // bool Input::getMouseButtonHeld(Key keyCode) {
    //     const uint16_t keyStatus = mouseButtons[keyCode];
    //     return keyStatus == GLFW_PRESS || keyStatus == GLFW_REPEAT;
    // }
    //
    // bool Input::getMouseButtonReleased(Key keyCode) {
    //     return mouseButtons[keyCode] == GLFW_RELEASE;
    // }
    //
    // void Input::update() {
    //     for (auto &key : keyboardKeys) {
    //         if (key == GLFW_KEY_UP) {
    //             key = 3;
    //         }
    //         else if (key == GLFW_PRESS) {
    //             key = 2;
    //         }
    //     }
    //
    //     for (auto &button : mouseButtons) {
    //         if (button == GLFW_KEY_UP) {
    //             button = 3;
    //         }
    //         else if (button == GLFW_PRESS) {
    //             button = 2;
    //         }
    //     }
    // }
}