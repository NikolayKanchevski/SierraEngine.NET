namespace SierraEngine.Engine.Classes;

/// <summary>
/// Contains many useful mathematical methods. Extends the functionality of <see cref="Math"/>.
/// </summary>
public static class Mathematics
{
    /// <summary>
    /// Rounds a given value to N decimal places. By default, it round the value to a whole number.
    /// </summary>
    /// <param name="value">The value to round. (double)</param>
    /// <param name="decimalPlaces">To how many decimal places to round the value.</param>
    /// <returns></returns>
    public static double Round(in double value, in int decimalPlaces = 0)
    {
        return Math.Round(value, decimalPlaces);
    }
    /// <summary>
    /// Rounds a given value to N decimal places. By default, it round the value to a whole number.
    /// </summary>
    /// <param name="value">The value to round. (float)</param>
    /// <param name="decimalPlaces">To how many decimal places to round the value.</param>
    /// <returns></returns>
    public static float Round(in float value, in int decimalPlaces = 0)
    {
        return (float) Math.Round(value, decimalPlaces);
    }

    /// <summary>
    /// Returns the difference between two values. It is always positive.
    /// </summary>
    /// <param name="x">First value. (double)</param>
    /// <param name="y">Second value. (double)</param>
    /// <returns></returns>
    private static double Difference(in double x, in double y)
    {
        return Math.Abs(x - y);
    }

    /// <summary>
    /// Returns the difference between two values. It is always positive.
    /// </summary>
    /// <param name="x">First value. (float)</param>
    /// <param name="y">Second value. (float)</param>
    /// <returns></returns>
    private static float Difference(in float x, in float y)
    {
        return Math.Abs(x - y);
    }

    /// <summary>
    /// Returns the difference between two values. It is always positive.
    /// </summary>
    /// <param name="x">First value. (int)</param>
    /// <param name="y">Second value. (int)</param>
    /// <returns></returns>
    private static int Difference(in int x, in int y)
    {
        return Math.Abs(x - y);
    }

    /// <summary>
    /// Checks if the difference between two values is bigger than or equal to some value.
    /// </summary>
    /// <param name="x">First value. (double)</param>
    /// <param name="y">Second value. (double)</param>
    /// <param name="value">Minimum difference. (double)</param>
    /// <returns></returns>
    public static bool DifferenceIsBiggerThan(in double x, in double y, in double value)
    {
        return Difference(x, y) > value;
    }

    /// <summary>
    /// Checks if the difference between two values is bigger than or equal to some value.
    /// </summary>
    /// <param name="x">First value. (float)</param>
    /// <param name="y">Second value. (float)</param>
    /// <param name="value">Minimum difference. (float)</param>
    /// <returns></returns>
    public static bool DifferenceIsBiggerThan(in float x, in float y, in float value)
    {
        return Difference(x, y) > value;
    }

    /// <summary>
    /// Checks if the difference between two values is bigger than or equal to some value.
    /// </summary>
    /// <param name="x">First value. (int)</param>
    /// <param name="y">Second value. (int)</param>
    /// <param name="value">Minimum difference. (int)</param>
    /// <returns></returns>
    public static bool DifferenceIsBiggerThan(in int x, in int y, in int value)
    {
        return Difference(x, y) > value;
    }
    
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
    /// <param name="maxLimit">Maximum limit. (int)</param>
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