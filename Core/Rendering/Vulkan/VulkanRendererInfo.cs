namespace SierraEngine.Core.Rendering.Vulkan;

public static class VulkanRendererInfo
{
    public static float drawTime;
    public static int verticesDrawn = 0;
    public static int meshesDrawn = 0;
    public static int objectsInScene = 0;
    
    #if DEBUG
        public static float initializationTime = 0;
    #endif
}