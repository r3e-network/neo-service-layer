using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Shared.Utilities;

/// <summary>
/// Provides guard clauses for defensive programming.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws an ArgumentNullException if the argument is null.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's not null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
    public static T NotNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : class
    {
        if (argument == null)
            throw new ArgumentNullException(parameterName);

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentNullException if the nullable argument is null.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's not null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
    public static T NotNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : struct
    {
        if (argument == null)
            throw new ArgumentNullException(parameterName);

        return argument.Value;
    }

    /// <summary>
    /// Throws an ArgumentException if the string is null or empty.
    /// </summary>
    /// <param name="argument">The string argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's not null or empty.</returns>
    /// <exception cref="ArgumentException">Thrown when the argument is null or empty.</exception>
    public static string NotNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        if (string.IsNullOrEmpty(argument))
            throw new ArgumentException("Value cannot be null or empty.", parameterName);

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentException if the string is null, empty, or whitespace.
    /// </summary>
    /// <param name="argument">The string argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's not null, empty, or whitespace.</returns>
    /// <exception cref="ArgumentException">Thrown when the argument is null, empty, or whitespace.</exception>
    public static string NotNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentException if the collection is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="argument">The collection argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's not null or empty.</returns>
    /// <exception cref="ArgumentException">Thrown when the argument is null or empty.</exception>
    public static IEnumerable<T> NotNullOrEmpty<T>([NotNull] IEnumerable<T>? argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        NotNull(argument, parameterName);

        if (!argument.Any())
            throw new ArgumentException("Collection cannot be empty.", parameterName);

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if the argument is less than the minimum value.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="minimum">The minimum allowed value.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's greater than or equal to the minimum.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the argument is less than the minimum.</exception>
    public static T GreaterThanOrEqual<T>(T argument, T minimum, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(minimum) < 0)
            throw new ArgumentOutOfRangeException(parameterName, $"Argument must be greater than or equal to {minimum}.");

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if the argument is less than or equal to the minimum value.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="minimum">The minimum allowed value (exclusive).</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's greater than the minimum.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the argument is less than or equal to the minimum.</exception>
    public static T GreaterThan<T>(T argument, T minimum, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(minimum) <= 0)
            throw new ArgumentOutOfRangeException(parameterName, $"Argument must be greater than {minimum}.");

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if the argument is greater than the maximum value.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="maximum">The maximum allowed value.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's less than or equal to the maximum.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the argument is greater than the maximum.</exception>
    public static T LessThanOrEqual<T>(T argument, T maximum, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(maximum) > 0)
            throw new ArgumentOutOfRangeException(parameterName, $"Argument must be less than or equal to {maximum}.");

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if the argument is greater than or equal to the maximum value.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="maximum">The maximum allowed value (exclusive).</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's less than the maximum.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the argument is greater than or equal to the maximum.</exception>
    public static T LessThan<T>(T argument, T maximum, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(maximum) >= 0)
            throw new ArgumentOutOfRangeException(parameterName, $"Argument must be less than {maximum}.");

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if the argument is not within the specified range.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="minimum">The minimum allowed value.</param>
    /// <param name="maximum">The maximum allowed value.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's within the range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the argument is outside the range.</exception>
    public static T InRange<T>(T argument, T minimum, T maximum, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(minimum) < 0 || argument.CompareTo(maximum) > 0)
            throw new ArgumentOutOfRangeException(parameterName, $"Argument must be between {minimum} and {maximum}.");

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentException if the condition is false.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <exception cref="ArgumentException">Thrown when the condition is false.</exception>
    public static void IsTrue(bool condition, string message, [CallerArgumentExpression(nameof(condition))] string? parameterName = null)
    {
        if (!condition)
            throw new ArgumentException(message, parameterName);
    }

    /// <summary>
    /// Throws an ArgumentException if the condition is true.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <exception cref="ArgumentException">Thrown when the condition is true.</exception>
    public static void IsFalse(bool condition, string message, [CallerArgumentExpression(nameof(condition))] string? parameterName = null)
    {
        if (condition)
            throw new ArgumentException(message, parameterName);
    }

    /// <summary>
    /// Throws an ArgumentException if the GUID is empty.
    /// </summary>
    /// <param name="argument">The GUID argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's not empty.</returns>
    /// <exception cref="ArgumentException">Thrown when the GUID is empty.</exception>
    public static Guid NotEmpty(Guid argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        if (argument == Guid.Empty)
            throw new ArgumentException("GUID cannot be empty.", parameterName);

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if the index is negative.
    /// </summary>
    /// <param name="index">The index to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The index if it's non-negative.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is negative.</exception>
    public static int NotNegative(int index, [CallerArgumentExpression(nameof(index))] string? parameterName = null)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(parameterName, "Index cannot be negative.");

        return index;
    }

    /// <summary>
    /// Throws an InvalidOperationException if the operation is invalid.
    /// </summary>
    /// <param name="condition">The condition that must be true for the operation to be valid.</param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="InvalidOperationException">Thrown when the condition is false.</exception>
    public static void ValidOperation(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Throws an ArgumentException if the enum value is not defined.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="argument">The enum argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's a defined enum value.</returns>
    /// <exception cref="ArgumentException">Thrown when the enum value is not defined.</exception>
    public static T EnumDefined<T>(T argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), argument))
            throw new ArgumentException($"Enum value '{argument}' is not defined in {typeof(T).Name}.", parameterName);

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentException if the type is not assignable to the target type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The type if it's assignable to the target type.</returns>
    /// <exception cref="ArgumentException">Thrown when the type is not assignable to the target type.</exception>
    public static Type IsAssignableTo(Type type, Type targetType, [CallerArgumentExpression(nameof(type))] string? parameterName = null)
    {
        NotNull(type, parameterName);
        NotNull(targetType, nameof(targetType));

        if (!targetType.IsAssignableFrom(type))
            throw new ArgumentException($"Type '{type.Name}' is not assignable to '{targetType.Name}'.", parameterName);

        return type;
    }

    /// <summary>
    /// Throws an ArgumentException if the string doesn't match the specified pattern.
    /// </summary>
    /// <param name="argument">The string argument to check.</param>
    /// <param name="pattern">The regular expression pattern.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it matches the pattern.</returns>
    /// <exception cref="ArgumentException">Thrown when the string doesn't match the pattern.</exception>
    public static string MatchesPattern(string argument, string pattern, string? message = null, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        NotNullOrEmpty(argument, parameterName);
        NotNullOrEmpty(pattern, nameof(pattern));

        if (!System.Text.RegularExpressions.Regex.IsMatch(argument, pattern))
        {
            message ??= $"String does not match the required pattern: {pattern}";
            throw new ArgumentException(message, parameterName);
        }

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if the argument is negative.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's not negative.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the argument is negative.</exception>
    public static T AgainstNegative<T>(T argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(default(T)) < 0)
            throw new ArgumentOutOfRangeException(parameterName, "Argument cannot be negative.");

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if the argument is zero or negative.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's greater than zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the argument is zero or negative.</exception>
    public static T AgainstZeroOrNegative<T>(T argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(default(T)) <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "Argument must be greater than zero.");

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentException if the condition is true.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <exception cref="ArgumentException">Thrown when the condition is true.</exception>
    public static void Against(bool condition, string message, [CallerArgumentExpression(nameof(condition))] string? parameterName = null)
    {
        if (condition)
            throw new ArgumentException(message, parameterName);
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if the argument is outside the specified range.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The argument to check.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's within the range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the argument is outside the range.</exception>
    public static T AgainstOutOfRange<T>(T argument, T min, T max, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(min) < 0 || argument.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(parameterName, $"Argument must be between {min} and {max}.");

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentException if the enum value is invalid.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="argument">The enum argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's a valid enum value.</returns>
    /// <exception cref="ArgumentException">Thrown when the enum value is invalid.</exception>
    public static T AgainstInvalidEnumValue<T>(T argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), argument))
            throw new ArgumentException($"Invalid enum value '{argument}' for type {typeof(T).Name}.", parameterName);

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentException if the collection is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="argument">The collection argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's not empty.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is empty.</exception>
    public static ICollection<T> AgainstEmptyCollection<T>(ICollection<T> argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        NotNull(argument, parameterName);
        if (argument.Count == 0)
            throw new ArgumentException("Collection cannot be empty.", parameterName);
        return argument;
    }

    /// <summary>
    /// Throws an ArgumentException if the collection is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="argument">The collection argument to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The argument if it's not null or empty.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is null or empty.</exception>
    public static ICollection<T> NotNullOrEmptyCollection<T>(ICollection<T> argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        return AgainstEmptyCollection(argument, parameterName);
    }

    /// <summary>
    /// Throws an ArgumentException if the collection is null or empty.
    /// </summary>
    /// <typeparam name="T">The collection element type.</typeparam>
    /// <param name="argument">The collection to check.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The collection if it's not null or empty.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is null or empty.</exception>
    public static IEnumerable<T> AgainstNullOrEmptyCollection<T>(IEnumerable<T>? argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        if (argument == null)
            throw new ArgumentNullException(parameterName, "Collection cannot be null.");
        
        if (!argument.Any())
            throw new ArgumentException("Collection cannot be empty.", parameterName);

        return argument;
    }
}
