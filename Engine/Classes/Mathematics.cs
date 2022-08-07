namespace SierraEngine.Engine.Classes;

public static class Mathematics
{
    /// <summary>
    /// Checks if a given value is beyond the maximum limit or less than the minimum limit.
    /// </summary>
    /// <param name="value">The variable to be tested. (as float)</param>
    /// <param name="minLimit">Minimum limit. (as float)</param>
    /// <param name="maxLimit">Maximum limit. (as float)</param>
    /// <returns></returns>
    public static float Clamp(float value, float minLimit, float maxLimit)
    {
        if (value > maxLimit) return maxLimit;
        if (value < minLimit) return minLimit;

        return value;
    }
    
    /// <summary>
    /// Checks if a given value is beyond the maximum limit or less than the minimum limit.
    /// </summary>
    /// <param name="value">The variable to be tested. (as int)</param>
    /// <param name="minLimit">Minimum limit. (as int)</param>
    /// <param name="maxLimit">Maximum limit (as int).</param>
    /// <returns></returns>
    public static int Clamp(int value, int minLimit, int maxLimit)
    {
        if (value > maxLimit) return maxLimit;
        if (value < minLimit) return minLimit;

        return value;
    }
}