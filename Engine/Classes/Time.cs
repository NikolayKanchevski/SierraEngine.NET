namespace SierraEngine.Engine.Classes;

public static class Time
{
    public static int FPS { get; private set; }
    public static float deltaTime { get; private set; }
    public static double doubleDeltaTime { get; private set; }
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