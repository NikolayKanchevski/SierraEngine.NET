using System.Runtime.InteropServices;
using Glfw;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Core;
using SierraEngine.Engine;

namespace SierraEngine.Core.Rendering;

/// <summary>A class to create windows on the screen. Wraps around a "core" GLFW window and extends its abilities.</summary>
public unsafe class Window
{
    public int width { get; private set; }
    public int height { get; private set; }
    public string title { get; private set; }
    
    private IntPtr glfwWindow;
    private VulkanRenderer? vulkanRenderer = null;
    
    private readonly bool resizable;
    private readonly bool requireFocus;
    
    private GCHandle selfPointerHandle;

    private readonly WindowSizeDelegate resizeCallbackDelegate = WindowResizeCallback;
    private readonly KeyDelegate keyCallbackDelegate = Input.KeyboardKeyCallback;
    
    /* -- REFERENCES TO PRIVATE FIELDS -- */
    
    /// <returns>Pointer (IntPtr) of the core GLFW window.</returns>
    public IntPtr GetCoreWindow()
    {
        return glfwWindow;
    }

    /* -- SETTER METHODS -- */

    /// <summary>
    /// Sets the title / name of the window
    /// </summary>
    /// <param name="newTitle">What the new title should be.</param>
    public void SetTitle(string newTitle)
    {
        this.title = newTitle;
        Glfw3.SetWindowTitle(glfwWindow, newTitle);
    }

    /// <summary>
    /// Sets the icon of the window. Works only on Windows.
    /// </summary>
    /// <param name="iconWidth">What the width of the new icon image is.</param>
    /// <param name="iconHeight">What the height of the new icon image is.</param>
    /// <param name="iconImageData">The icon image data converted to a byte array.</param>
    public void SetIcon(int iconWidth, int iconHeight, in byte[] iconImageData)
    {
        Image icon = new Image((uint) iconWidth, (uint) iconHeight, iconImageData);
        Glfw3.SetWindowIcon(glfwWindow, 1, ref icon);
    }
    
    /// <summary>
    /// Sets the renderer to use when drawing to the window. Not required for the window to draw.
    /// </summary>
    /// <param name="renderer">Reference to the already created renderer to use.</param>
    public void SetRenderer(ref VulkanRenderer renderer)
    {
        this.vulkanRenderer = renderer;
    }
    
    /* -- GETTER PROPERTIES / METHODS -- */

    /// <summary>
    /// Checks whether the window is closed.
    /// </summary>
    public bool closed => Glfw3.WindowShouldClose(glfwWindow);

    /// <summary>
    /// Checks whether the window is minimised and is not shown.
    /// </summary>
    public bool minimised => Convert.ToBoolean(Glfw3.GetWindowAttrib(glfwWindow, WindowAttribute.Iconified));

    /// <summary>
    /// Checks whether the window is maximised (uses the whole screen).
    /// </summary>
    public bool maximised => Convert.ToBoolean(Glfw3.GetWindowAttrib(glfwWindow, WindowAttribute.Maximized));

    /// <summary>
    /// Checks whether the window is focused (is the one handling input currently).
    /// </summary>
    public bool focused => Convert.ToBoolean(Glfw3.GetWindowAttrib(glfwWindow, WindowAttribute.Focused));

    /* -- EXTERNAL METHODS -- */

    /// <summary>
    /// Does drawing and required GLFW updates. Only gets executed if the window is not minimised and is focused if required to be.
    /// </summary>
    public void Update()
    {
        if (requireFocus && !focused) return;
        
        Glfw3.PollEvents();
        
        if (minimised) return;
        
        vulkanRenderer?.Update();
    }

    /// <summary>
    /// Destroys the window and cleans all allocated memory for it. Also destroys the renderer if present.
    /// </summary>
    public void Destroy()
    {
        Glfw3.DestroyWindow(glfwWindow);
        vulkanRenderer?.CleanUp();
        
        selfPointerHandle.Free();
    }
    
    /* -- CONSTRUCTOR -- */
    
    /// <summary>
    /// Creates a new window on the system.
    /// </summary>
    /// <param name="width">How T H I C C the window should be.</param>
    /// <param name="height">How high the window should be.</param>
    /// <param name="title">What the title / name of the window should be.</param>
    /// <param name="resizable">Whether the window is going to be resizable or not.</param>
    /// <param name="requireFocus">Whether the window requires to be focused in order to draw and handle events.</param>
    public Window(int width, int height, string title, bool resizable = false, bool requireFocus = false)
    {
        this.width = width;
        this.height = height;
        this.title = title;
        this.resizable = resizable;
        this.requireFocus = requireFocus;

        InitWindow();
    }
    
    private void InitWindow()
    {
        Glfw3.Init();
        
        Glfw3.WindowHint(WindowAttribute.Resizable, Convert.ToInt32(resizable));
        Glfw3.WindowHint(WindowAttribute.ClientApi, 0);

        glfwWindow = Glfw3.CreateWindow(width, height, title, MonitorHandle.Zero, IntPtr.Zero);
        
        selfPointerHandle = GCHandle.Alloc(this);
        IntPtr selfPointer = (IntPtr) selfPointerHandle;
        Glfw3.SetWindowUserPointer(glfwWindow, selfPointer);

        SetCallbacks();
    }

    /* -- CALLBACKS -- */
    
    private static void WindowResizeCallback(IntPtr resizedGlfwWindow, int newWidth, int newHeight)
    {
        Window windowObject;
        GetGlfwWindowParentClass(resizedGlfwWindow, out windowObject);
        
        windowObject.width = newWidth;
        windowObject.height = newHeight;
        
        windowObject.vulkanRenderer?.Update();
    }

    /* -- INTERNAL METHODS -- */

    private void SetCallbacks()
    {
        Glfw3.SetWindowSizeCallback(glfwWindow, resizeCallbackDelegate);
        Glfw3.SetKeyCallback(glfwWindow, keyCallbackDelegate);
    }
    
    /* -- INTERNAL HELPER METHODS -- */

    private static void GetGlfwWindowParentClass(IntPtr glfwWindowPtr, out Window outWindow)
    {
        IntPtr windowObjectPtr = Glfw3.GetWindowUserPointer(glfwWindowPtr);
        GCHandle gcHandle = (GCHandle) windowObjectPtr;
        outWindow = (gcHandle.Target as Window)!;
    }
}