using System.Numerics;
using SierraEngine.Engine.Components;

namespace SierraEngine.Engine.Classes;

public class Light : Component
{
    public Vector3 color;
    public Vector3 ambient;
    public Vector3 diffuse;
    public Vector3 specular;
}