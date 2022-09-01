using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Components;

namespace SierraEngine.Core;

public static class World
{
    // TODO: Set a texture system so that there is not a limit to their amount and is performant
    public const uint MAX_TEXTURES = 128; // Changed as @kael wouldn't stop bitching about it
    public const int MAX_POINT_LIGHTS = 64;
    public const int MAX_DIRECTIONAL_LIGHTS = 16;
    
    public static List<Mesh> meshes { get; private set; } = new List<Mesh>(); 
    public static List<GameObject> hierarchy { get; private set; } = new List<GameObject>();
    
    public static GameObject? selectedGameObject;
    
    public static void PrintHierarchy()
    {
        foreach (GameObject gameObject in hierarchy)
        {
            if (!gameObject.hasParent)
            {
                PrintDeeper(gameObject, 0);
            }
        }
    }

    private static void PrintDeeper(in GameObject gameObject, in int iteration)
    {
        if (iteration > 0)
        {
            for (uint i = 0; i < iteration; i++)
            {
                Console.Write(" ");
            }
            Console.Write("'-- ");
        }
        
        Console.WriteLine(gameObject.name);
        
        foreach (var child in gameObject.children)
        {
            PrintDeeper(child, iteration + 1);
        }
    }
}