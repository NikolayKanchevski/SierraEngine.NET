using System.Numerics;

namespace SierraEngine.Engine.Structures;

/// <summary>
/// A struct for storing per-texture's material data.
/// </summary>
public struct Material
{
    /// <summary>
    /// How shiny the mesh should be.
    /// </summary>
    public float shininess;
    
    /// <summary>
    /// How opaque the main (diffuse) color of the mesh should be.
    /// </summary>
    public Vector3 diffuse;
    
    /// <summary>
    /// How strong the specular should be.
    /// </summary>
    public Vector3 specular;
    
    /// <summary>
    /// How lit the ambient light color should be.
    /// </summary>
    public Vector3 ambient;
}