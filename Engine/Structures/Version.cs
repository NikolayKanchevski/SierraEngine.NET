namespace SierraEngine.Engine.Structures;

/// <summary>
/// A version structure, represented as 3 sub-version uints - major, minor, and patch.
/// </summary>
public struct Version
{
    public const uint MAJOR = 1;
    public const uint MINOR = 0;
    public const uint PATCH = 0;

    public new static string ToString()
    {
        return $"v{ MAJOR }.{ MINOR }.{ PATCH }";
    }
}