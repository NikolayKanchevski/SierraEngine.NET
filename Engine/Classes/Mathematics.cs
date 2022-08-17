namespace SierraEngine.Engine.Classes;

public static class Mathematics
{
    /// <summary>
    /// Checks if a given value is beyond the maximum limit or less than the minimum limit.
    /// </summary>
    /// <param name="value">The variable to be tested. (double)</param>
    /// <param name="minLimit">Minimum limit. (double)</param>
    /// <param name="maxLimit">Maximum limit. (double)</param>
    /// <returns></returns>
    public static double Clamp(double value, double minLimit, double maxLimit)
    {
        if (value > maxLimit) return maxLimit;
        if (value < minLimit) return minLimit;

        return value;
    }
    
    /// <summary>
    /// Checks if a given value is beyond the maximum limit or less than the minimum limit.
    /// </summary>
    /// <param name="value">The variable to be tested. (float)</param>
    /// <param name="minLimit">Minimum limit. (float)</param>
    /// <param name="maxLimit">Maximum limit. (float)</param>
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
    /// <param name="value">The variable to be tested. (int)</param>
    /// <param name="minLimit">Minimum limit. (int)</param>
    /// <param name="maxLimit">Maximum limit (int).</param>
    /// <returns></returns>
    public static int Clamp(int value, int minLimit, int maxLimit)
    {
        if (value > maxLimit) return maxLimit;
        if (value < minLimit) return minLimit;

        return value;
    }
    
    /// <summary>
    /// Converts a given value in degrees to radians
    /// </summary>
    /// <param name="degrees">Value to convert (double in degrees)</param>
    /// <returns></returns>
    public static double ToRadians(in double degrees)
    {
        return (Math.PI / 180) * degrees;
    }
    
    /// <summary>
    /// Converts a given value in degrees to radians
    /// </summary>
    /// <param name="degrees">Value to convert (float in degrees)</param>
    /// <returns></returns>
    public static float ToRadians(in float degrees)
    {
        return (float) (Math.PI / 180) * degrees;
    }
}