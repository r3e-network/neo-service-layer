namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Extension methods for various types used in pattern recognition.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Calculates the standard deviation of a collection of TimeSpan values.
    /// </summary>
    /// <param name="values">The TimeSpan values.</param>
    /// <returns>The standard deviation in hours.</returns>
    public static double StandardDeviation(this IEnumerable<TimeSpan> values)
    {
        var timeSpans = values.ToArray();
        if (timeSpans.Length <= 1) return 0.0;

        var hours = timeSpans.Select(ts => ts.TotalHours).ToArray();
        var average = hours.Average();
        var sumOfSquares = hours.Sum(h => Math.Pow(h - average, 2));

        return Math.Sqrt(sumOfSquares / (hours.Length - 1));
    }

    /// <summary>
    /// Converts a collection of doubles to a comma-separated string.
    /// </summary>
    /// <param name="values">The double values.</param>
    /// <returns>A comma-separated string representation.</returns>
    public static string ToCommaSeparatedString(this IEnumerable<double> values)
    {
        return string.Join(", ", values.Select(v => v.ToString("F2")));
    }

    /// <summary>
    /// Gets the value or default from a dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The value if found, otherwise the default value.</returns>
    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default!)
        where TKey : notnull
    {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Safely gets a boolean value from an object.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <param name="defaultValue">The default value if conversion fails.</param>
    /// <returns>The boolean value.</returns>
    public static bool SafeGetBoolean(this object? obj, bool defaultValue = false)
    {
        if (obj == null) return defaultValue;

        if (obj is bool boolValue) return boolValue;

        if (bool.TryParse(obj.ToString(), out var parsedBool))
            return parsedBool;

        return defaultValue;
    }

    /// <summary>
    /// Safely gets an integer value from an object.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <param name="defaultValue">The default value if conversion fails.</param>
    /// <returns>The integer value.</returns>
    public static int SafeGetInt(this object? obj, int defaultValue = 0)
    {
        if (obj == null) return defaultValue;

        if (obj is int intValue) return intValue;

        if (int.TryParse(obj.ToString(), out var parsedInt))
            return parsedInt;

        return defaultValue;
    }

    /// <summary>
    /// Safely gets a decimal value from an object.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <param name="defaultValue">The default value if conversion fails.</param>
    /// <returns>The decimal value.</returns>
    public static decimal SafeGetDecimal(this object? obj, decimal defaultValue = 0)
    {
        if (obj == null) return defaultValue;

        if (obj is decimal decimalValue) return decimalValue;

        if (decimal.TryParse(obj.ToString(), out var parsedDecimal))
            return parsedDecimal;

        return defaultValue;
    }

    /// <summary>
    /// Safely gets a DateTime value from an object.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <param name="defaultValue">The default value if conversion fails.</param>
    /// <returns>The DateTime value.</returns>
    public static DateTime SafeGetDateTime(this object? obj, DateTime defaultValue = default)
    {
        if (obj == null) return defaultValue;

        if (obj is DateTime dateTimeValue) return dateTimeValue;

        if (DateTime.TryParse(obj.ToString(), out var parsedDateTime))
            return parsedDateTime;

        return defaultValue;
    }
}
