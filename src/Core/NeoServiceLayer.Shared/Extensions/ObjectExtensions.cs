using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace NeoServiceLayer.Shared.Extensions;

/// <summary>
/// Extension methods for object operations.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Serializes the object to JSON string.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <returns>The JSON string representation of the object.</returns>
    public static string ToJson(this object? obj, JsonSerializerOptions? options = null)
    {
        if (obj == null)
            return "null";

        options ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Creates a deep copy of the object using JSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object to clone.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <returns>A deep copy of the object.</returns>
    public static T? DeepClone<T>(this T obj, JsonSerializerOptions? options = null) where T : class
    {
        if (obj == null)
            return null;

        // Ensure consistent options for both serialization and deserialization
        options ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = obj.ToJson(options);
        return json.FromJson<T>(options);
    }

    /// <summary>
    /// Validates the object using data annotations.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results.</returns>
    public static IEnumerable<ValidationResult> Validate(this object obj, ValidationContext? validationContext = null)
    {
        validationContext ??= new ValidationContext(obj);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(obj, validationContext, results, true);
        return results;
    }

    /// <summary>
    /// Checks if the object is valid according to data annotations.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>True if the object is valid; otherwise, false.</returns>
    public static bool IsValid(this object obj, ValidationContext? validationContext = null)
    {
        return !obj.Validate(validationContext).Any();
    }

    /// <summary>
    /// Gets the value of a property by name using reflection.
    /// </summary>
    /// <param name="obj">The object containing the property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The value of the property or null if not found.</returns>
    public static object? GetPropertyValue(this object obj, string propertyName)
    {
        if (obj == null || propertyName.IsNullOrEmpty())
            return null;

        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Sets the value of a property by name using reflection.
    /// </summary>
    /// <param name="obj">The object containing the property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>True if the property was set successfully; otherwise, false.</returns>
    public static bool SetPropertyValue(this object obj, string propertyName, object? value)
    {
        if (obj == null || propertyName.IsNullOrEmpty())
            return false;

        try
        {
            var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite)
                return false;

            property.SetValue(obj, value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the object has a property with the specified name.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>True if the property exists; otherwise, false.</returns>
    public static bool HasProperty(this object obj, string propertyName)
    {
        if (obj == null || propertyName.IsNullOrEmpty())
            return false;

        return obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) != null;
    }

    /// <summary>
    /// Gets all public properties of the object.
    /// </summary>
    /// <param name="obj">The object to inspect.</param>
    /// <returns>A dictionary of property names and values.</returns>
    public static Dictionary<string, object?> GetProperties(this object obj)
    {
        if (obj == null)
            return new Dictionary<string, object?>();

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.ToDictionary(prop => prop.Name, prop => prop.GetValue(obj));
    }

    /// <summary>
    /// Converts the object to a dictionary of key-value pairs.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <param name="includeNulls">Whether to include null values.</param>
    /// <returns>A dictionary representation of the object.</returns>
    public static Dictionary<string, object?> ToDictionary(this object obj, bool includeNulls = true)
    {
        var properties = obj.GetProperties();

        if (!includeNulls)
        {
            properties = properties.Where(kvp => kvp.Value != null)
                                   .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return properties;
    }

    /// <summary>
    /// Maps properties from one object to another.
    /// </summary>
    /// <typeparam name="TSource">The source object type.</typeparam>
    /// <typeparam name="TDestination">The destination object type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="destination">The destination object.</param>
    /// <param name="ignoreCase">Whether to ignore case when matching property names.</param>
    /// <returns>The destination object with mapped properties.</returns>
    public static TDestination MapTo<TSource, TDestination>(this TSource source, TDestination destination, bool ignoreCase = true)
        where TSource : class
        where TDestination : class
    {
        if (source == null)
            return destination;
        if (destination == null)
            return default(TDestination);

        var sourceProps = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Where(p => p.CanRead);
        var destProps = destination.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(p => p.CanWrite)
                                  .ToDictionary(p => p.Name, p => p,
                                    ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        foreach (var sourceProp in sourceProps)
        {
            if (destProps.TryGetValue(sourceProp.Name, out var destProp))
            {
                try
                {
                    var value = sourceProp.GetValue(source);
                    if (value != null && destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                    {
                        destProp.SetValue(destination, value);
                    }
                }
                catch
                {
                    // Ignore mapping errors for individual properties
                }
            }
        }

        return destination;
    }

    /// <summary>
    /// Creates a new instance of the destination type and maps properties from the source.
    /// </summary>
    /// <typeparam name="TDestination">The destination object type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="ignoreCase">Whether to ignore case when matching property names.</param>
    /// <returns>A new instance of TDestination with mapped properties.</returns>
    public static TDestination MapTo<TDestination>(this object source, bool ignoreCase = true)
        where TDestination : class, new()
    {
        var destination = new TDestination();
        return source.MapTo(destination, ignoreCase);
    }

    /// <summary>
    /// Checks if the object is null.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>True if the object is null; otherwise, false.</returns>
    public static bool IsNull(this object? obj)
    {
        return obj == null;
    }

    /// <summary>
    /// Checks if the object is not null.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>True if the object is not null; otherwise, false.</returns>
    public static bool IsNotNull(this object? obj)
    {
        return obj != null;
    }

    /// <summary>
    /// Returns the object if it's not null, otherwise returns the default value.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object to check.</param>
    /// <param name="defaultValue">The default value to return if the object is null.</param>
    /// <returns>The object or the default value.</returns>
    public static T IfNull<T>(this T? obj, T defaultValue) where T : class
    {
        return obj ?? defaultValue;
    }

    /// <summary>
    /// Returns the object if it's not null, otherwise returns the result of the function.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object to check.</param>
    /// <param name="defaultValueFactory">The function to call if the object is null.</param>
    /// <returns>The object or the result of the function.</returns>
    public static T IfNull<T>(this T? obj, Func<T> defaultValueFactory) where T : class
    {
        return obj ?? defaultValueFactory();
    }

    /// <summary>
    /// Executes an action if the object is not null.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object to check.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original object.</returns>
    public static T? IfNotNull<T>(this T? obj, Action<T> action) where T : class
    {
        if (obj != null)
            action(obj);
        return obj;
    }

    /// <summary>
    /// Executes a function if the object is not null and returns the result.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="obj">The object to check.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>The result of the function or default value if the object is null.</returns>
    public static TResult? IfNotNull<T, TResult>(this T? obj, Func<T, TResult> func) where T : class
    {
        return obj != null ? func(obj) : default;
    }
}
