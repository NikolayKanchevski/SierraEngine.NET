using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Components;

namespace SierraEngine.Core;

public static class World
{
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
        
        for (int i = 0; i < gameObject.children.Count; i++)
        {
            PrintDeeper(gameObject.children[i], iteration + 1);
        }
    }
}