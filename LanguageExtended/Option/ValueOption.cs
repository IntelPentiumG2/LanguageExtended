using System.Collections;

namespace LanguageExtended.Option;

/// <summary>
/// Represents an optional value that may or may not have a value.
/// This class works with value types (structs).
/// </summary>
/// <typeparam name="T">The type of the value of the ValueOption</typeparam>
public readonly struct ValueOption<T> : IEquatable<ValueOption<T>>, IEnumerable<T> where T : struct
{
    private readonly T _value;
    private readonly bool _hasValue;

    /// <summary>
    /// Constructor to create an ValueOption with a value
    /// </summary>
    /// <param name="value">The value of the ValueOption object</param>
    private ValueOption(T value)
    {
        _value = value;
        _hasValue = true;
    }

    /// <summary>
    /// Factory method to create an ValueOption with a value
    /// </summary>
    /// <param name="value">The value of the ValueOption object</param>
    /// <returns>A new ValueOption with the given value</returns>
    public static ValueOption<T> Some(T value) => new(value);

    /// <summary>
    /// Factory method to create an ValueOption without a value
    /// </summary>
    /// <returns>A new empty ValueOption object</returns>
    public static ValueOption<T> None() => new();

    /// <summary>
    /// Indicates whether the current ValueOption has a value.
    /// </summary>
    public bool IsSome => _hasValue;

    /// <summary>
    /// Indicates whether the current ValueOption has no value.
    /// </summary>
    public bool IsNone => !_hasValue;

    /// <summary>
    /// Tries to get the value of the current ValueOption.
    /// </summary>
    /// <param name="value">The value of the ValueOption</param>
    /// <returns>true if ValueOption has a value, otherwise false</returns>
    public bool TryGetValue(out T value)
    {
        value = _value;
        return _hasValue;
    }

    /// <summary>
    /// Gets the value of the ValueOption if it has a value; otherwise, throws an InvalidOperationException.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the ValueOption has no value.</exception>
    public T Value => _hasValue ? _value : throw new InvalidOperationException("ValueOption has no value");

    /// <summary>
    /// Maps the value of the current ValueOption to a new ValueOption using the provided mapping function.
    /// </summary>
    /// <typeparam name="TResult">The type of the value in the resulting ValueOption.</typeparam>
    /// <param name="map">A function to transform the value of the current ValueOption.</param>
    /// <returns>
    /// A new ValueOption containing the transformed value if the current ValueOption has a value; 
    /// otherwise, an empty ValueOption of type TResult.
    /// </returns>
    public ValueOption<TResult> Map<TResult>(Func<T, TResult> map) where TResult : struct =>
        !_hasValue ? ValueOption<TResult>.None() : ValueOption<TResult>.Some(map(_value));

    /// <summary>
    /// Matches the value of the current ValueOption to one of the provided functions.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="onSome">A function to call if the ValueOption has a value.</param>
    /// <param name="onNone">A function to call if the ValueOption has no value.</param>
    /// <returns>
    /// The result of the <paramref name="onSome"/> function if the ValueOption has a value;
    /// otherwise, the result of the <paramref name="onNone"/> function.
    /// </returns>
    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
        _hasValue ? onSome(_value) : onNone();

    /// <summary>
    /// Matches the value of the current ValueOption to one of the provided actions.
    /// </summary>
    /// <param name="onSome">An action to call if the ValueOption has a value.</param>
    /// <param name="onNone">An action to call if the ValueOption has no value.</param>
    public void Match(Action<T> onSome, Action onNone)
    {
        if (_hasValue)
            onSome(_value);
        else
            onNone();
    }

    /// <summary>
    /// Returns the value of the current ValueOption if it has a value; otherwise, returns the specified default value.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the ValueOption has no value.</param>
    /// <returns>The value of the current ValueOption if it has a value; otherwise, the specified default value.</returns>
    public T Reduce(T defaultValue) => _hasValue ? _value : defaultValue;

    /// <summary>
    /// Returns the value of the current ValueOption if it has a value; otherwise, returns the value produced by the specified function.
    /// </summary>
    /// <param name="defaultValue">A function that produces the default value to return if the ValueOption has no value.</param>
    /// <returns>The value of the current ValueOption if it has a value; otherwise, the value produced by the specified function.</returns>
    public T Reduce(Func<T> defaultValue) => _hasValue ? _value : defaultValue();

    /// <summary>
    /// Filters the current ValueOption based on the provided predicate.
    /// </summary>
    /// <param name="predicate">A function to test the value of the current ValueOption.</param>
    /// <returns>
    /// The current ValueOption if it has a value and the value satisfies the predicate;
    /// otherwise, an empty ValueOption.
    /// </returns>
    public ValueOption<T> Where(Func<T, bool> predicate) =>
        !_hasValue || !predicate(_value) ? None() : this;

    /// <summary>
    /// Performs the specified action with the value if the ValueOption has a value.
    /// </summary>
    /// <param name="action">The action to perform with the value.</param>
    /// <returns>The current ValueOption instance.</returns>
    public ValueOption<T> IfSome(Action<T> action)
    {
        if (_hasValue)
            action(_value);
        return this;
    }

    /// <summary>
    /// Performs the specified action if the ValueOption has no value.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <returns>The current ValueOption instance.</returns>
    public ValueOption<T> IfNone(Action action)
    {
        if (!_hasValue)
            action();
        return this;
    }

    /// <summary>
    /// Converts the ValueOption&lt;T&gt; to a nullable T.
    /// </summary>
    /// <returns>The value if present, otherwise null.</returns>
    public T? ToNullable() => _hasValue ? _value : null;

    /// <summary>
    /// Implicitly converts a value of type T to an ValueOption&lt;T&gt;.
    /// </summary>
    /// <param name="value">The value to convert to an ValueOption&lt;T&gt;.</param>
    /// <returns>An ValueOption&lt;T&gt; representing the input value.</returns>
    public static implicit operator ValueOption<T>(T value) => Some(value);

    /// <summary>
    /// Explicitly converts a nullable value of type T to an ValueOption&lt;T&gt;.
    /// </summary>
    /// <param name="value">The nullable value to convert to an ValueOption&lt;T&gt;.</param>
    /// <returns>
    /// An ValueOption&lt;T&gt; representing the input value:
    /// - If the input is null, returns None.
    /// - If the input is not null, returns Some with the input value.
    /// </returns>
    public static explicit operator ValueOption<T>(T? value) => value.HasValue ? Some(value.Value) : None();

    /// <inheritdoc />
    public override int GetHashCode() => _hasValue ? _value.GetHashCode() : 0;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValueOption<T> other && Equals(other);

    /// <inheritdoc />
    public bool Equals(ValueOption<T> other) =>
        (_hasValue == other._hasValue) && (!_hasValue || EqualityComparer<T>.Default.Equals(_value, other._value));

    /// <summary>
    /// Determines whether two specified ValueOption objects have the same value.
    /// </summary>
    /// <param name="left">The first ValueOption to compare.</param>
    /// <param name="right">The second ValueOption to compare.</param>
    /// <returns>true if the value of left is the same as the value of right; otherwise, false.</returns>
    public static bool operator ==(ValueOption<T> left, ValueOption<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two specified ValueOption objects do not have the same value.
    /// </summary>
    /// <param name="left">The first ValueOption to compare.</param>
    /// <param name="right">The second ValueOption to compare.</param>
    /// <returns>true if the value of left is not the same as the value of right; otherwise, false.</returns>
    public static bool operator !=(ValueOption<T> left, ValueOption<T> right) => !left.Equals(right);

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        if (_hasValue)
            yield return _value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}