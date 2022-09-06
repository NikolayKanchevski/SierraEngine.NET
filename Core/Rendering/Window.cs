using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;
using Glfw;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using GLFW;
using ErrorCode = GLFW.ErrorCode;

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
    public bool closed => GLFW.Glfw.WindowShouldClose(glfwWindow);

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

    private GLFW.Window glfwWindow;
    private Vector4 monitorWorkArea;
    private GLFW.Monitor monitor;
    
    private readonly bool resizable;
    private readonly bool requireFocus;
    
    private GCHandle selfPointerHandle;

    private readonly ErrorCallback glfwErrorDelegate = GlfwErrorCallback;
    private readonly SizeCallback resizeCallbackDelegate = WindowResizeCallback;
    private readonly FocusCallback focusCallback = WindowFocusCallback;
    private readonly IconifyCallback minimizeCallback = WindowMinimizeCallback;
    private readonly WindowMaximizedCallback maximizeCallback = WindowMaximizeCallback;
    private readonly KeyCallback keyCallbackDelegate = Input.KeyboardKeyCallback;
    private readonly MouseCallback cursorCallbackDelegate = Engine.Classes.Cursor.CursorPositionCallback;
    private readonly MouseButtonCallback buttonCallbackDelegate = Input.MouseButtonCallback;
    private readonly MouseCallback scrollCallbackDelegate = Input.MouseScrollCallback;
    
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
        GLFW.Glfw.SetWindowTitle(glfwWindow, newTitle);
    }
    
    /// <summary>
    /// Shows the window after startup, or manually hiding it. See <see cref="Hide"/>
    /// </summary>
    public void Show()
    {
        this.hidden = false;
        GLFW.Glfw.ShowWindow(glfwWindow);
    }

    /// <summary>
    /// Hides the window completely from the user. Removes it from the task bar and is not visible.
    /// </summary>
    public void Hide()
    {
        this.hidden = true;
        GLFW.Glfw.HideWindow(glfwWindow);
    }

    /// <summary>
    /// Sets the transparency (opacity) of the window
    /// </summary>
    /// <param name="opacity">How transparent the window should become (from 0.0f to 1.0f).</param>
    public void SetOpacity(in float opacity)
    {
        this.opacity = opacity;
        GLFW.Glfw.SetWindowOpacity(glfwWindow, opacity);
    }

    /// <summary>
    /// Sets the icon of the window. Works only on Windows.
    /// </summary>
    /// <param name="iconWidth">What the width of the new icon image is.</param>
    /// <param name="iconHeight">What the height of the new icon image is.</param>
    /// <param name="iconImageData">The icon image data converted to a byte array.</param>
    public void SetIcon(int iconWidth, int iconHeight, in byte[] iconImageData)
    {
        // TODO: FIX
        // Image icon = new Image(iconWidth, iconHeight, );
        // GLFW.Glfw.SetWindowIcon(glfwWindow, 1, new [] { icon });
    }
    
    /// <summary>
    /// Checks whether a Vulkan rendering backend is attached to the window.
    /// </summary>
    /// <returns>A boolean indicating whether the Vulkan renderer is null.</returns>
    public bool HasRenderer()
    {
        return vulkanRenderer != null;
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
        GLFW.Glfw.PollEvents();
        
        if (requireFocus && !focused) return;
        
        if (minimized || hidden) return;
        
        vulkanRenderer?.Update();
    }

    /// <summary>
    /// Destroys the window and cleans all allocated memory for it. Also destroys the renderer if present.
    /// </summary>
    public void Destroy()
    {
        GLFW.Glfw.DestroyWindow(glfwWindow);
        
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
        GLFW.Glfw.Init();

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
        GLFW.Glfw.Init();
        
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
            GLFW.Glfw.MaximizeWindow(glfwWindow);
        }
        
        #if DEBUG
            stopwatch.Stop();
            VulkanDebugger.DisplaySuccess($"Window [\"{ title }\"] successfully created! Initialization took: { stopwatch.ElapsedMilliseconds }ms");
            VulkanRendererInfo.initializationTime += stopwatch.ElapsedMilliseconds;
        #endif
    }

    private void RetrieveMonitorData()
    {
        this.monitor = GLFW.Glfw.PrimaryMonitor;
        
        this.monitorWorkArea = new Vector4(monitor.WorkArea.X, monitor.WorkArea.Y, monitor.WorkArea.Width, monitor.WorkArea.Height);
        this.monitorName = GLFW.Glfw.GetMonitorName(monitor).ToString();
    }
    
    private void InitWindow()
    {
        GLFW.Glfw.WindowHint(Hint.Resizable, resizable);
        GLFW.Glfw.WindowHint(Hint.ClientApi, 0);
        GLFW.Glfw.WindowHint(Hint.Visible, 0);

        glfwWindow = GLFW.Glfw.CreateWindow(width, height, title, GLFW.Monitor.None, GLFW.Window.None);
        GLFW.Glfw.SetWindowPosition(glfwWindow, (int) position.X, (int) position.Y);
        
        VulkanCore.glfwWindow = glfwWindow;
        VulkanCore.window = this;

        selfPointerHandle = GCHandle.Alloc(this);
        IntPtr selfPointer = (IntPtr) selfPointerHandle;
        GLFW.Glfw.SetWindowUserPointer(glfwWindow, selfPointer);

        GLFW.Glfw.GetFramebufferSize(glfwWindow, out var actualSizeX, out var actualSizeY);
        this.width = actualSizeX / 2;
        this.height = actualSizeY / 2;

        SetCallbacks();
    }

    /* -- CALLBACKS -- */

    private static void GlfwErrorCallback(ErrorCode errorCode, IntPtr message)
    {
        VulkanDebugger.ThrowError($"GLFW Error: { Marshal.PtrToStringUTF8(message) } ({ errorCode.ToString() })");
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
        GLFW.Glfw.SetErrorCallback(glfwErrorDelegate);
        
        GLFW.Glfw.SetWindowSizeCallback(glfwWindow, resizeCallbackDelegate);
        
        GLFW.Glfw.SetWindowFocusCallback(glfwWindow, focusCallback);

        GLFW.Glfw.SetWindowRefreshCallback(glfwWindow, window => WindowFocusCallback(window, true));
        
        GLFW.Glfw.SetWindowIconifyCallback(glfwWindow, minimizeCallback);

        GLFW.Glfw.SetWindowMaximizeCallback(glfwWindow, maximizeCallback);

        GLFW.Glfw.SetKeyCallback(glfwWindow, keyCallbackDelegate);

        GLFW.Glfw.SetCursorPositionCallback(glfwWindow, cursorCallbackDelegate);

        GLFW.Glfw.SetMouseButtonCallback(glfwWindow, buttonCallbackDelegate);

        GLFW.Glfw.SetScrollCallback(glfwWindow, scrollCallbackDelegate);
    }
    
    /* -- INTERNAL HELPER METHODS -- */

    private static void GetGlfwWindowParentClass(IntPtr glfwWindowPtr, out Window outWindow)
    {
        IntPtr windowObject = Glfw3.GetWindowUserPointer(glfwWindowPtr);
        
        GCHandle gcHandle = (GCHandle) windowObject;
        outWindow = (gcHandle.Target as Window)!;
    }
}