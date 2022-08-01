namespace SierraEngine.Engine;

public static class Mathematics
{
    /// <summary>
    /// Checks if a given value is beyond the maximum limit or less than the minimum limit.
    /// </summary>
    /// <param name="value">The variable to be tested.</param>
    /// <param name="maxLimit">Maximum limit.</param>
    /// <param name="minLimit">Minimum limit.</param>
    /// <returns></returns>
    public static float Clamp(float value, float maxLimit, float minLimit)
    {
        if (value > maxLimit) return maxLimit;
        if (value < minLimit) return minLimit;

        return value;
    }
}