using Glfw;

namespace SierraEngine.Engine;

public class Time
{
    public static int FPS { get; private set; }
    public static double deltaTime { get; private set; }
    public static double upTime { get; private set; }

    private static double lastFrameTime = Glfw3.GetTime();

    public static void Update()
    {
        double currentFrameTime = Glfw3.GetTime();
        deltaTime = (currentFrameTime - lastFrameTime);
        lastFrameTime = currentFrameTime;

        FPS = (int) Math.Round(1 / deltaTime);
        upTime = currentFrameTime;
    }
}