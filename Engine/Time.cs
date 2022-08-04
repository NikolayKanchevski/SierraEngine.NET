using Glfw;

namespace SierraEngine.Engine;

public static class Time
{
    public static uint FPS { get; private set; }
    public static float deltaTime { get; private set; }
    public static double doubleDeltaTime { get; private set; }
    public static float upTime { get; private set; }

    private static double lastFrameTime = Glfw3.GetTime();

    public static void Update()
    {
        double currentFrameTime = Glfw3.GetTime();
        
        doubleDeltaTime = currentFrameTime - lastFrameTime;
        deltaTime = (float) doubleDeltaTime;
        
        lastFrameTime = currentFrameTime;

        FPS = (uint) Math.Round(1.0 / doubleDeltaTime);
        upTime = (float) currentFrameTime;
    }
}