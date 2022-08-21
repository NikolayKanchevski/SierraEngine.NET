using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using Glfw;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using Cursor = Glfw.Cursor;

namespace SierraEngine.Core.Rendering;

/// <summary>A class to create windows on the screen. Wraps around a "core" GLFW window and extends its abilities.</summary>
public class Window
{
    /// <summary>
    /// Returns the width of the window.
    /// </summary>
    public int width { get; private set; }
    /// <summary>
    /// Returns the height of the window.
    /// </summary>
    public int height { get; private set; }
    /// <summary>
    /// Returns the title displayed at the top of the window.
    /// </summary>
    public string title { get; private set; }
    /// <summary>
    /// Checks whether the window is closed.
    /// </summary>
    public bool closed => Glfw3.WindowShouldClose(glfwWindow);

    /// <summary>
    /// Checks whether the window is minimised and is not shown.
    /// </summary>
    public bool minimized { get; private set; }

    /// <summary>
    /// Checks whether the window is maximised (uses the whole screen).
    /// </summary>
    public bool maximized { get; private set; }
    
    /// <summary>
    /// Returns the current opacity of the window.
    /// </summary>
    public float opacity { get; private set;  }

    /// <summary>
    /// Checks whether the window is focused (is the one handling input currently).
    /// </summary>
    public bool focused { get; private set; } = true;
    /// <summary>
    /// Checks whether the window is hidden from the user.
    /// </summary>
    public bool hidden { get; private set; } = true;

    /// <summary>
    /// Returns the name of the current monitor used by this window.
    /// </summary>
    public string monitorName { get; private set; } = "Null";
    
    public VulkanRenderer? vulkanRenderer { get; private set; }
    private readonly Vector2 position;

    private IntPtr glfwWindow;
    private Vector4 monitorWorkArea;
    private MonitorHandle monitor;
    
    private readonly bool resizable;
    private readonly bool requireFocus;
    
    private GCHandle selfPointerHandle;

    private readonly ErrorDelegate glfwErrorDelegate = GlfwErrorCallback;
    private readonly WindowSizeDelegate resizeCallbackDelegate = WindowResizeCallback;
    private readonly WindowBooleanDelegate focusCallback = WindowFocusCallback;
    private readonly WindowBooleanDelegate minimizeCallback = WindowMinimizeCallback;
    private readonly WindowBooleanDelegate maximizeCallback = WindowMaximizeCallback;
    private readonly KeyDelegate keyCallbackDelegate = Input.KeyboardKeyCallback;
    private readonly CursorPosDelegate cursorCallbackDelegate = Engine.Classes.Cursor.CursorPositionCallback;
    private readonly MouseButtonDelegate buttonCallbackDelegate = Input.MouseButtonCallback;
    private readonly ScrollDelegate scrollCallbackDelegate = Input.MouseScrollCallback;
    
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
    /// Shows the window after startup, or manually hiding it. See <see cref="Hide"/>
    /// </summary>
    public void Show()
    {
        this.hidden = false;
        Glfw3.ShowWindow(glfwWindow);
    }

    /// <summary>
    /// Hides the window completely from the user. Removes it from the task bar and is not visible.
    /// </summary>
    public void Hide()
    {
        this.hidden = true;
        Glfw3.HideWindow(glfwWindow);
    }

    /// <summary>
    /// Sets the transparency (opacity) of the window
    /// </summary>
    /// <param name="opacity">How transparent the window should become (from 0.0f to 1.0f).</param>
    public void SetOpacity(in float opacity)
    {
        this.opacity = opacity;
        Glfw3.SetWindowOpacity(glfwWindow, opacity);
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

    /* -- EXTERNAL METHODS -- */

    /// <summary>
    /// Does drawing and required GLFW updates. Only gets executed if the window is not minimised and is focused if required to be.
    /// </summary>
    public void Update()
    {
        Glfw3.PollEvents();
        
        if (requireFocus && !focused) return;
        
        if (minimized || hidden) return;
        
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
    public Window(string title, int width, int height, bool resizable = false, bool requireFocus = false)
    {
        Glfw3.Init();

        #if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
        #endif
        
        RetrieveMonitorData();
        
        this.width = width;
        this.height = height;
        this.title = title;
        this.resizable = resizable;
        this.requireFocus = requireFocus;

        this.position = new Vector2((monitorWorkArea.Z - width) / 2, (monitorWorkArea.W - height) / 2);

        InitWindow();
        
        #if DEBUG
            stopwatch.Stop();
            VulkanDebugger.DisplaySuccess($"Window [\"{ title }\"] successfully created! Initialization took: { stopwatch.ElapsedMilliseconds }ms");
            VulkanRendererInfo.initializationTime += stopwatch.ElapsedMilliseconds;
        #endif
    }
    
    /// <summary>
    /// Creates a new window without the need of setting its size. It will automatically be 800x600 or,
    /// if maximized, as big as it can be on your display.
    /// </summary>
    /// <param name="title">What the title / name of the window should be.</param>
    /// <param name="maximized">A bool indicating whether the window should use all the space on your screen and start maximized.</param>
    /// <param name="resizable">Whether the window is going to be resizable or not.</param>
    /// <param name="requireFocus">Whether the window requires to be focused in order to draw and handle events.</param>
    public Window(string title, bool maximized = false, bool resizable = false, bool requireFocus = false)
    {
        Glfw3.Init();
        
        #if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
        #endif
        
        RetrieveMonitorData();
        
        this.title = title;
        this.resizable = resizable;
        this.requireFocus = requireFocus;
        
        this.width = 800;
        this.height = 600;

        this.position = new Vector2((monitorWorkArea.Z - width) / 2, (monitorWorkArea.W - height) / 2);

        if (maximized)
        {
            this.position = Vector2.Zero;
            
            this.width = (int) monitorWorkArea.Z;
            this.height = (int) monitorWorkArea.W;
        }

        InitWindow();

        if (maximized)
        {
            Glfw3.MaximizeWindow(glfwWindow);
        }
        
        #if DEBUG
            stopwatch.Stop();
            VulkanDebugger.DisplaySuccess($"Window [\"{ title }\"] successfully created! Initialization took: { stopwatch.ElapsedMilliseconds }ms");
            VulkanRendererInfo.initializationTime += stopwatch.ElapsedMilliseconds;
        #endif
    }

    private void RetrieveMonitorData()
    {
        this.monitor = Glfw3.GetPrimaryMonitor();
        Glfw3.GetMonitorWorkarea(monitor.RawHandle, out var x, out var y, out var monitorWidth, out var monitorHeight);
        
        this.monitorWorkArea = new Vector4(x, y, monitorWidth, monitorHeight);
        this.monitorName = Glfw3.GetMonitorName(monitor).ToString();
    }
    
    private void InitWindow()
    {
        Glfw3.WindowHint(WindowAttribute.Resizable, Convert.ToInt32(resizable));
        Glfw3.WindowHint(WindowAttribute.ClientApi, 0);
        Glfw3.WindowHint(WindowAttribute.Visible, 0);

        glfwWindow = Glfw3.CreateWindow(width, height, title, MonitorHandle.Zero, IntPtr.Zero);
        Glfw3.SetWindowPos(glfwWindow, (int) position.X, (int) position.Y);
        
        VulkanCore.glfwWindow = glfwWindow;
        VulkanCore.window = this;

        selfPointerHandle = GCHandle.Alloc(this);
        IntPtr selfPointer = (IntPtr) selfPointerHandle;
        Glfw3.SetWindowUserPointer(glfwWindow, selfPointer);

        Glfw3.GetFramebufferSize(glfwWindow, out var actualSizeX, out var actualSizeY);
        this.height = actualSizeY / 2;

        SetCallbacks();
    }

    /* -- CALLBACKS -- */

    private static void GlfwErrorCallback(ErrorCode errorCode, string errorMessage)
    {
        VulkanDebugger.ThrowError($"GLFW Error: { errorMessage } ({ errorCode.ToString() })");
    }
    
    private static void WindowResizeCallback(IntPtr resizedGlfwWindow, int newWidth, int newHeight)
    {
        GetGlfwWindowParentClass(resizedGlfwWindow, out var windowObject);
        
        windowObject.width = newWidth;
        windowObject.height = newHeight;

        if (windowObject.vulkanRenderer == null)
        {
            return;
        }
        
        windowObject.vulkanRenderer.frameBufferResized = true;
        windowObject.vulkanRenderer.Update();
        windowObject.vulkanRenderer.Update();

        SierraEngine.Engine.Classes.Cursor.ResetCursorOffset();
    }

    private static void WindowFocusCallback(IntPtr focusedWindow, bool focused)
    {
        GetGlfwWindowParentClass(focusedWindow, out var windowObject);

        windowObject.focused = focused;
        windowObject.minimized = false;

        SierraEngine.Engine.Classes.Cursor.ResetCursorOffset();
    }

    private static void WindowMinimizeCallback(IntPtr minimizedWindow, bool minimized)
    {
        GetGlfwWindowParentClass(minimizedWindow, out var windowObject);

        windowObject.minimized = minimized;

        SierraEngine.Engine.Classes.Cursor.ResetCursorOffset();
    }

    private static void WindowMaximizeCallback(IntPtr maximizedWindow, bool maximized)
    {
        GetGlfwWindowParentClass(maximizedWindow, out var windowObject);

        windowObject.minimized = !maximized;
        windowObject.maximized = maximized;

        SierraEngine.Engine.Classes.Cursor.ResetCursorOffset();
    }

    /* -- INTERNAL METHODS -- */

    private void SetCallbacks()
    {
        Glfw3.SetErrorCallback(glfwErrorDelegate);
        
        Glfw3.SetWindowSizeCallback(glfwWindow, resizeCallbackDelegate);
        
        Glfw3.SetWindowFocusCallback(glfwWindow, focusCallback);

        Glfw3.SetWindowRefreshCallback(glfwWindow, window => WindowFocusCallback(window, true));
        
        Glfw3.SetWindowIconifyCallback(glfwWindow, minimizeCallback);

        Glfw3.SetWindowMaximizeCallback(glfwWindow, maximizeCallback);

        Glfw3.SetKeyCallback(glfwWindow, keyCallbackDelegate);

        Glfw3.SetCursorPosCallback(glfwWindow, cursorCallbackDelegate);

        Glfw3.SetMouseButtonPosCallback(glfwWindow, buttonCallbackDelegate);

        Glfw3.SetScrollCallback(glfwWindow, scrollCallbackDelegate);
    }
    
    /* -- INTERNAL HELPER METHODS -- */

    private static void GetGlfwWindowParentClass(IntPtr glfwWindowPtr, out Window outWindow)
    {
        IntPtr windowObjectPtr = Glfw3.GetWindowUserPointer(glfwWindowPtr);
        GCHandle gcHandle = (GCHandle) windowObjectPtr;
        outWindow = (gcHandle.Target as Window)!;
    }
}