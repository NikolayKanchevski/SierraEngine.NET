using System.Numerics;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Components;

namespace SierraEngine.Core;

public static class Version
{
    public const uint MAJOR = 1;
    public const uint MINOR = 0;
    public const uint PATCH = 0;

    public new static string ToString()
    {
        return $"v{ MAJOR }.{ MINOR }.{ PATCH }";
    }
}

public static class World
{
    // TODO: Set a texture system so that there is not a limit to their amount and is performant
    public const uint MAX_TEXTURES = 128; // Changed as @kael wouldn't stop bitching about it
    public const int MAX_POINT_LIGHTS = 64; // Remember to change the limit in the fragment shader too!
    public const int MAX_DIRECTIONAL_LIGHTS = 16; // Remember to change the limit in the fragment shader too!
    
    public static List<Mesh> meshes { get; } = new List<Mesh>(); 
    public static List<GameObject> hierarchy { get; } = new List<GameObject>();

    private static int directionalLightsCount;
    public static UniformDirectionalLight[] directionalLights { get; } = new UniformDirectionalLight[MAX_DIRECTIONAL_LIGHTS];
    
    private static int pointLightsCount;
    public static UniformPointLight[] pointLights { get; } = new UniformPointLight[MAX_POINT_LIGHTS];

    public static Camera camera = null!;

    
    public static GameObject? selectedGameObject;

    public static void Update()
    {
        UpdateObjects();
        
        UpdateRenderer();

        UpdateWindow();
    }

    public static void UpdateClasses()
    { 
        Time.Update();
        Input.Update();
        Cursor.Update();
        
        VulkanCore.vulkanRenderer.imGuiController.Update();
    }
    
    private static void UpdateObjects()
    {
        foreach (GameObject gameObject in hierarchy)
        {
            gameObject.Update();
        }
    }

    private static void UpdateRenderer()
    {
        if (!VulkanCore.window.HasRenderer()) return;
        
        VulkanCore.vulkanRenderer.uniformData.view = Matrix4x4.CreateLookAt(camera.position, camera.position + camera.frontDirection, camera.upDirection);
        VulkanCore.vulkanRenderer.uniformData.projection = Matrix4x4.CreatePerspectiveFieldOfView(Mathematics.ToRadians(camera.fov), VulkanCore.swapchainAspectRatio, camera.nearClip, camera.farClip);
        VulkanCore.vulkanRenderer.uniformData.projection.M11 *= -1;

        VulkanCore.vulkanRenderer.uniformData.directionalLights = World.directionalLights;
        VulkanCore.vulkanRenderer.uniformData.directionalLightsCount = World.directionalLightsCount;
        
        VulkanCore.vulkanRenderer.uniformData.pointLights = World.pointLights;
        VulkanCore.vulkanRenderer.uniformData.pointLightsCount = World.pointLightsCount;
    }

    private static void UpdateWindow()
    {
        VulkanCore.window.Update();
    }
    
    public static int RegisterDirectionalLight(DirectionalLight directionalLight)
    {
        if (directionalLightsCount == MAX_DIRECTIONAL_LIGHTS)
        {
            VulkanDebugger.ThrowWarning("Limit of directional lights reached. Light is automatically discarded");
            return -1;
        }
        
        directionalLights[directionalLightsCount] = directionalLight;
        directionalLightsCount++;

        return directionalLightsCount - 1;
    }

    public static int  RegisterPointLight(PointLight pointLight)
    {
        if (pointLightsCount == MAX_POINT_LIGHTS)
        {
            VulkanDebugger.ThrowWarning("Limit of point lights reached. Light is automatically discarded");
            return -1;
        }
        
        pointLights[pointLightsCount] = pointLight;
        pointLightsCount++;

        return pointLightsCount - 1;
    }
}