using System.Numerics;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Components;
using SierraEngine.Engine.Components.Lighting;

namespace SierraEngine.Core;

/// <summary>
/// The core of the engine. Maintains the renderer, the window, and every object in the scene.
/// </summary>
public static class World
{
    // TODO: Set a texture system so that there is not a limit to their amount and is performant
    public const uint MAX_TEXTURES = 128; // Changed as @kael wouldn't stop bitching about it
    public const int MAX_POINT_LIGHTS = 64; // Remember to change the limit in the fragment shader too!
    public const int MAX_DIRECTIONAL_LIGHTS = 16; // Remember to change the limit in the fragment shader too!
    public const int MAX_SPOTLIGHT_LIGHTS = 16; // Remember to change the limit in the fragment shader too!
    
    public static List<Mesh> meshes { get; } = new List<Mesh>(); 
    public static List<GameObject> hierarchy { get; } = new List<GameObject>();

    public static UniformDirectionalLight[] directionalLights { get; private set; } = new UniformDirectionalLight[MAX_DIRECTIONAL_LIGHTS];
    private static readonly List<int> freeDirectionalLightSlots = new List<int>(MAX_DIRECTIONAL_LIGHTS);
    private static int directionalLightsCount;
    
    public static UniformPointLight[] pointLights { get; private set; } = new UniformPointLight[MAX_POINT_LIGHTS];
    private static readonly List<int> freePointLightSlots = new List<int>(MAX_DIRECTIONAL_LIGHTS);
    private static int pointLightsCount;

    public static UniformSpotLight[] spotLights { get; private set; } = new UniformSpotLight[MAX_SPOTLIGHT_LIGHTS];
    private static readonly List<int> freeSpotLightSlots = new List<int>(MAX_DIRECTIONAL_LIGHTS);
    private static int spotLightsCount;

    public static Camera camera = null!;
    public static GameObject? selectedGameObject;

    /// <summary>
    /// Starts the built-in classes. This means that each of them acquires its needed per-initialization data.
    /// Must be called once at the very beginning of the game's code.
    /// </summary>
    public static void Init()
    {
        Input.Start();
    }

    /// <summary>
    /// Updates everything connected to the world - the window surface, the renderer, and the built-in classes,
    /// but not the built-in classes which must be updated once the world has been updated from here and from the user.
    /// See <see cref="UpdateClasses"/>.
    /// </summary>
    public static void Update()
    {
        UpdateObjects();
        
        UpdateRenderer();

        UpdateWindow();
    }

    /// <summary>
    /// Updates the required built-in classes. For example, important variables such as <see cref="Time.deltaTime"/>
    /// would not work if this method is not called once and every frame and would also break if called more than once
    /// per frame.
    /// </summary>
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

        // Inverse Y coordinate to satisfy Vulkan's requirements
        Vector3 rendererCameraPosition = new Vector3(camera.transform.position.X, camera.transform.position.Y * -1, camera.transform.position.Z);
        Vector3 rendererCameraFrontDirection = new Vector3(camera.frontDirection.X, camera.frontDirection.Y * -1, camera.frontDirection.Z);
        Vector3 rendererCameraUpDirection = new Vector3(camera.upDirection.X, camera.upDirection.Y, camera.upDirection.Z);
        
        // Assign renderer data
        VulkanCore.vulkanRenderer.uniformData.view = Matrix4x4.CreateLookAt(rendererCameraPosition, rendererCameraPosition + rendererCameraFrontDirection, rendererCameraUpDirection);
        VulkanCore.vulkanRenderer.uniformData.projection = Matrix4x4.CreatePerspectiveFieldOfView(Mathematics.ToRadians(camera.fov), VulkanCore.swapchainAspectRatio, camera.nearClip, camera.farClip);
        VulkanCore.vulkanRenderer.uniformData.projection.M11 *= -1;

        VulkanCore.vulkanRenderer.uniformData.directionalLights = World.directionalLights;
        VulkanCore.vulkanRenderer.uniformData.directionalLightsCount = World.directionalLightsCount;
        
        VulkanCore.vulkanRenderer.uniformData.pointLights = World.pointLights;
        VulkanCore.vulkanRenderer.uniformData.pointLightsCount = World.pointLightsCount;
        
        VulkanCore.vulkanRenderer.uniformData.spotLights = World.spotLights;
        VulkanCore.vulkanRenderer.uniformData.spotLightsCount = World.spotLightsCount;
    }

    private static void UpdateWindow()
    {
        VulkanCore.window.Update();
    }
    
    public static int RegisterDirectionalLight(DirectionalLight directionalLight)
    {
        if (freeDirectionalLightSlots.Count > 0)
        {
            directionalLights[freeDirectionalLightSlots[0]] = directionalLight;
            
            freeDirectionalLightSlots.RemoveAt(0);
            return freeDirectionalLightSlots[0];
        }
        
        if (directionalLightsCount == MAX_DIRECTIONAL_LIGHTS)
        {
            VulkanDebugger.ThrowWarning("Limit of directional lights reached. Light is automatically discarded");
            return -1;
        }
        
        directionalLights[directionalLightsCount] = directionalLight;
        directionalLightsCount++;

        return directionalLightsCount - 1;
    }

    public static void RemoveDirectionalLight(DirectionalLight directionalLight)
    {
        directionalLights[directionalLight.ID] = default;
        freeDirectionalLightSlots.Add(directionalLight.ID);
    }

    public static int RegisterPointLight(PointLight pointLight)
    {
        if (freePointLightSlots.Count > 0)
        {
            pointLights[freePointLightSlots[0]] = pointLight;
            
            freePointLightSlots.RemoveAt(0);
            return freePointLightSlots[0];
        }
        
        if (pointLightsCount == MAX_POINT_LIGHTS)
        {
            VulkanDebugger.ThrowWarning("Limit of point lights reached. Light is automatically discarded");
            return -1;
        }
        
        pointLights[pointLightsCount] = pointLight;
        pointLightsCount++;
            
        return pointLightsCount - 1;
    }

    public static void RemovePointLight(PointLight pointLight)
    {
        pointLights[pointLight.ID] = default;
        freePointLightSlots.Add(pointLight.ID);
    }

    public static int RegisterSpotLight(Spotlight spotlight)
    {
        if (freeSpotLightSlots.Count > 0)
        {
            spotLights[freeSpotLightSlots[0]] = spotlight;
            
            freeSpotLightSlots.RemoveAt(0);
            return freeSpotLightSlots[0];
        }
        
        if (spotLightsCount == MAX_SPOTLIGHT_LIGHTS)
        {
            VulkanDebugger.ThrowWarning("Limit of spot lights reached. Light is automatically discarded");
            return -1;
        }

        spotLights[spotLightsCount] = spotlight;
        spotLightsCount++;

        return spotLightsCount - 1;
    }

    public static void RemoveSpotLight(Spotlight spotlight)
    {
        spotLights[spotlight.ID] = default;
        freeSpotLightSlots.Add(spotlight.ID);
    }
}