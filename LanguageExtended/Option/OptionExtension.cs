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

    /// <summary>
    /// Returns an Option containing the first element of the sequence, or None if the sequence is empty.
    /// </summary>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source) where T : class
    {
        foreach (var item in source)
            return Option<T>.Some(item);
        return Option<T>.None();
    }

    /// <summary>
    /// Returns an Option containing the first element satisfying the predicate, or None if no element matches.
    /// </summary>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : class
    {
        foreach (var item in source)
            if (predicate(item))
                return Option<T>.Some(item);
        return Option<T>.None();
    }

    /// <summary>
    /// Returns an Option containing the last element of the sequence, or None if the sequence is empty.
    /// </summary>
    public static Option<T> LastOrNone<T>(this IEnumerable<T> source) where T : class
    {
        T? last = null;
        foreach (var item in source)
            last = item;
        return last.ToOption();
    }

    /// <summary>
    /// Returns an Option containing the last element satisfying the predicate, or None if no element matches.
    /// </summary>
    public static Option<T> LastOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : class
    {
        T? last = null;
        foreach (var item in source)
            if (predicate(item))
                last = item;
        return last.ToOption();
    }

    /// <summary>
    /// Returns an Option containing the single element of the sequence, or None if the sequence is empty.
    /// Throws <see cref="InvalidOperationException"/> if the sequence contains more than one element.
    /// </summary>
    public static Option<T> SingleOrNone<T>(this IEnumerable<T> source) where T : class
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return Option<T>.None();
        var single = enumerator.Current;
        if (enumerator.MoveNext()) throw new InvalidOperationException("Sequence contains more than one element.");
        return Option<T>.Some(single);
    }

    /// <summary>
    /// Returns an Option containing the single element satisfying the predicate, or None if no element matches.
    /// Throws <see cref="InvalidOperationException"/> if more than one element satisfies the predicate.
    /// </summary>
    public static Option<T> SingleOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : class
    {
        T? found = null;
        foreach (var item in source)
        {
            if (!predicate(item)) continue;
            if (found is not null) throw new InvalidOperationException("Sequence contains more than one matching element.");
            found = item;
        }
        return found.ToOption();
    }

    /// <summary>
    /// Filters out None values from a sequence of Options and unwraps the Some values.
    /// </summary>
    public static IEnumerable<T> Values<T>(this IEnumerable<Option<T>> source) where T : class =>
        source.Where(o => o.IsSome).Select(o => o.Value);
}
