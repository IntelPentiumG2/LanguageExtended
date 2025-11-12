namespace LanguageExtended.Option;

/// <summary>
/// Contains extension methods for working with Option types.
/// </summary>
public static class OptionExtension
{
    /// <summary>
    /// Converts a nullable reference type to an Option.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <returns><paramref name="value"/> wrapped in an <see cref="Option{T}"/> object</returns>
    public static Option<T> ToOption<T>(this T? value) where T : class => 
        value is null ? Option<T>.None() : Option<T>.Some(value);
    
    /// <summary>
    /// Converts a nullable value type to an Option based on a predicate.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="predicate">The predicate to determine if it hase a valid value</param>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <returns><paramref name="value"/> wrapped in an <see cref="Option{T}"/> object</returns>
    public static Option<T> Where<T>(this T? value, Func<T, bool> predicate) where T : class =>
        value is null || !predicate(value) ? Option<T>.None() : Option<T>.Some(value);
}