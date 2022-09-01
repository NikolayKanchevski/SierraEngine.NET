using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Components;

namespace SierraEngine.Core;

public static class World
{
    // TODO: Set a texture system so that there is not a limit to their amount and is performant
    public const uint MAX_TEXTURES = 128; // Changed as @kael wouldn't stop bitching about it
    public const int MAX_POINT_LIGHTS = 64; // Remember to change the limit in the fragment shader too!
    public const int MAX_DIRECTIONAL_LIGHTS = 16; // Remember to change the limit in the fragment shader too!
    
    public static List<Mesh> meshes { get; private set; } = new List<Mesh>(); 
    public static List<GameObject> hierarchy { get; private set; } = new List<GameObject>();

    public static int directionalLightsCount { get; private set; } = 0;
    public static UniformDirectionalLight[] directionalLights { get; }= new UniformDirectionalLight[MAX_DIRECTIONAL_LIGHTS];
    
    public static int pointLightsCount { get; private set; } = 0;
    public static UniformPointLight[] pointLights { get; } = new UniformPointLight[MAX_POINT_LIGHTS];

    
    public static GameObject? selectedGameObject;

    public static void Update()
    {
        foreach (GameObject gameObject in hierarchy)
        {
            gameObject.Update();
        }
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