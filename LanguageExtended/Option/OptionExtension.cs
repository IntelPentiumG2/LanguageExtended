namespace LanguageExtended.Option;

/// <summary>
/// Contains extension methods for working with Option types.
/// </summary>
public static class OptionExtension
{
    /// <param name="value">The value to convert</param>
    /// <typeparam name="T">The type of the value</typeparam>
    extension<T>(T? value) where T : class
    {
        /// <summary>
        /// Converts a nullable reference type to an Option.
        /// </summary>
        /// <returns><paramref name="value"/> wrapped in an <see cref="Option{T}"/> object</returns>
        public Option<T> ToOption() =>
            value is null ? Option<T>.None() : Option<T>.Some(value);

        /// <summary>
        /// Converts a nullable value type to an Option based on a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to determine if it hase a valid value</param>
        /// <returns><paramref name="value"/> wrapped in an <see cref="Option{T}"/> object</returns>
        public Option<T> Where(Func<T, bool> predicate) =>
            value is null || !predicate(value) ? Option<T>.None() : Option<T>.Some(value);
    }

    extension<T>(IEnumerable<T> source) where T : class
    {
        /// <summary>
        /// Returns an Option containing the first element of the sequence, or None if the sequence is empty.
        /// </summary>
        public Option<T> FirstOrNone()
        {
            foreach (T item in source)
                return Option<T>.Some(item);
            return Option<T>.None();
        }

        /// <summary>
        /// Returns an Option containing the first element satisfying the predicate, or None if no element matches.
        /// </summary>
        public Option<T> FirstOrNone(Func<T, bool> predicate)
        {
            foreach (T item in source)
                if (predicate(item))
                    return Option<T>.Some(item);
            return Option<T>.None();
        }

        /// <summary>
        /// Returns an Option containing the last element of the sequence, or None if the sequence is empty.
        /// </summary>
        public Option<T> LastOrNone()
        {
            T? last = null;
            foreach (T item in source)
                last = item;
            return last.ToOption();
        }

        /// <summary>
        /// Returns an Option containing the last element satisfying the predicate, or None if no element matches.
        /// </summary>
        public Option<T> LastOrNone(Func<T, bool> predicate)
        {
            T? last = null;
            foreach (T item in source)
                if (predicate(item))
                    last = item;
            return last.ToOption();
        }

        /// <summary>
        /// Returns an Option containing the single element of the sequence, or None if the sequence is empty.
        /// Throws <see cref="InvalidOperationException"/> if the sequence contains more than one element.
        /// </summary>
        public Option<T> SingleOrNone()
        {
            using IEnumerator<T> enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext()) return Option<T>.None();
            T single = enumerator.Current;
            if (enumerator.MoveNext()) throw new InvalidOperationException("Sequence contains more than one element.");
            return Option<T>.Some(single);
        }

        /// <summary>
        /// Returns an Option containing the single element satisfying the predicate, or None if no element matches.
        /// Throws <see cref="InvalidOperationException"/> if more than one element satisfies the predicate.
        /// </summary>
        public Option<T> SingleOrNone(Func<T, bool> predicate)
        {
            T? found = null;
            foreach (T item in source)
            {
                if (!predicate(item)) continue;
                if (found is not null) throw new InvalidOperationException("Sequence contains more than one matching element.");
                found = item;
            }
            return found.ToOption();
        }
    }

    /// <summary>
    /// Filters out None values from a sequence of Options and unwraps the Some values.
    /// </summary>
    public static IEnumerable<T> Values<T>(this IEnumerable<Option<T>> source) where T : class =>
        source.Where(o => o.IsSome).Select(o => o.Value);
}
