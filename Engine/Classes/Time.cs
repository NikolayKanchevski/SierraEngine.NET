namespace SierraEngine.Engine.Classes;

/// <summary>
/// Contains all kinds of time-related values.
/// </summary>
public static class Time
{
    /// <summary>
    /// Current FPS of the application. Measured per frame.
    /// </summary>
    public static int FPS { get; private set; }
    
    /// <summary>
    /// Time since last frame. Used for <a href="https://www.construct.net/en/tutorials/delta-time-framerate-2">framerate independence</a>.
    /// </summary>
    public static float deltaTime { get; private set; }
    
    /// <summary>
    /// Time since last frame as a double. Used for <a href="https://www.construct.net/en/tutorials/delta-time-framerate-2">framerate independence</a>.
    /// </summary>
    public static double doubleDeltaTime { get; private set; }
    
    /// <summary>
    /// Time in seconds since the program has started. It is never lowered and is increased by <see cref="deltaTime"/> every frame.
    /// </summary>
    public static float upTime { get; private set; }

    private static double lastFrameTime = GLFW.Glfw.Time;

    public static void Update()
    {
        double currentFrameTime =  GLFW.Glfw.Time;
        
        doubleDeltaTime = currentFrameTime - lastFrameTime;
        deltaTime = (float) doubleDeltaTime;
        
        lastFrameTime = currentFrameTime;

        FPS = (int) Math.Round(1.0 / doubleDeltaTime);
        upTime = (float) currentFrameTime;
    }
}