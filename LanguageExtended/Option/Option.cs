namespace LanguageExtended.Option;

/// <summary>
/// Represents an optional value that may or may not have a value.
/// </summary>
/// <typeparam name="T"> The type of the value of the Option </typeparam>
public readonly struct Option<T>  : IEquatable<Option<T>> where T : class
{
    private readonly T? _value;

    /// <summary>
    /// Constructor to create an Option with a value
    /// </summary>
    /// <param name="value"> The value of the Option object </param>
    private Option(T? value) => _value = value;
    
    /// <summary>
    /// Factory method to create an Option with a value
    /// </summary>
    /// <param name="value"> The value of the Option object</param>
    /// <returns>A new Option with the given value</returns>
    public static Option<T> Some(T value) => new(value);
    /// <summary>
    /// Factory method to create an Option without a value
    /// </summary>
    /// <returns>A new empty Option object</returns>
    public static Option<T> None() => new(null);
    
    /// <summary>
    /// Indicates whether the current Option has a value.
    /// </summary>
    public bool IsSome => _value is not null;
    
    /// <summary>
    /// Trys to get the value of the current Option.
    /// </summary>
    /// <param name="value">The value of the Option</param>
    /// <returns>true if Option has a value, otherwise false</returns>
    public bool TryGetValue(out T? value)
    {
        value = _value;
        return IsSome;
    }
    
    /// <summary>
    /// Gets the value of the Option if it has a value; otherwise, throws an InvalidOperationException.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the Option has no value.</exception>
    public T Value => _value ?? throw new InvalidOperationException("Option has no value");
    
    /// <summary>
    /// Indicates whether the current Option has no value.
    /// </summary>
    public bool IsNone => _value is null;
    
    /// <summary>
    /// Maps the value of the current Option to a new Option using the provided mapping function.
    /// </summary>
    /// <typeparam name="TResult">The type of the value in the resulting Option.</typeparam>
    /// <param name="map">A function to transform the value of the current Option.</param>
    /// <returns>
    /// A new Option containing the transformed value if the current Option has a value; 
    /// otherwise, an empty Option of type TResult.
    /// </returns>
    public Option<TResult> Map<TResult>(Func<T, TResult> map) where TResult : class => 
        _value is null ? Option<TResult>.None() : Option<TResult>.Some(map(_value));
    
    /// <summary>
    /// Maps the value of the current Option to a new Option using the provided mapping function that returns an Option.
    /// </summary>
    /// <typeparam name="TResult">The type of the value in the resulting Option.</typeparam>
    /// <param name="map">A function to transform the value of the current Option into a new Option.</param>
    /// <returns>
    /// A new Option containing the transformed value if the current Option has a value;
    /// otherwise, an empty Option of type TResult.
    /// </returns>
    public Option<TResult> MapOptional<TResult>(Func<T, Option<TResult>> map) where TResult : class =>
        _value is null ? Option<TResult>.None() : map(_value);
    
    /// <summary>
    /// Matches the value of the current Option to one of the provided functions.
    /// </summary>
    /// <param name="onSome">A function to call if the Option has a value.</param>
    /// <param name="onNone">A function to call if the Option has no value.</param>
    /// <returns>
    /// The result of the <paramref name="onSome"/> function if the Option has a value;
    /// otherwise, the result of the <paramref name="onNone"/> function.
    /// </returns>
    public T Match(Func<T, T> onSome, Func<T> onNone) => _value is null ? onNone() : onSome(_value);
    
    /// <summary>
    /// Matches the value of the current Option to one of the provided actions.
    /// </summary>
    /// <param name="onSome">An action to call if the Option has a value.</param>
    /// <param name="onNone">An action to call if the Option has no value.</param>
    public void Match(Action<T> onSome, Action onNone)
    {
        if (_value is null)
            onNone();
        else
            onSome(_value);
    }
    
    /// <summary>
    /// Performs the specified action with the value if the option has a value.
    /// </summary>
    /// <param name="action">The action to perform with the value.</param>
    /// <returns>The current Option instance.</returns>
    public Option<T> IfSome(Action<T> action)
    {
        if (_value != null)
            action(_value);
        return this;
    }
    
    /// <summary>
    /// Performs the specified action if the option has no value.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <returns>The current Option instance.</returns>
    public Option<T> IfNone(Action action)
    {
        if (_value == null)
            action();
        return this;
    }
    
    /// <summary>
    /// Returns the value of the current Option if it has a value; otherwise, returns the specified default value.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the Option has no value.</param>
    /// <returns>The value of the current Option if it has a value; otherwise, the specified default value.</returns>
    public T Reduce(T defaultValue) => _value ?? defaultValue;
    /// <summary>
    /// Returns the value of the current Option if it has a value; otherwise, returns the value produced by the specified function.
    /// </summary>
    /// <param name="defaultValue">A function that produces the default value to return if the Option has no value.</param>
    /// <returns>The value of the current Option if it has a value; otherwise, the value produced by the specified function.</returns>
    public T Reduce(Func<T> defaultValue) => _value ?? defaultValue();
    
    /// <summary>
    /// Filters the current Option based on the provided predicate.
    /// </summary>
    /// <param name="predicate">A function to test the value of the current Option.</param>
    /// <returns>
    /// The current Option if it has a value and the value satisfies the predicate;
    /// otherwise, an empty Option.
    /// </returns>
    public Option<T> Where(Func<T, bool> predicate) => _value is null || !predicate(_value) ? None() : this;
    
    /// <summary>
    /// Filters the current Option based on the provided predicate, returning the Option if the predicate is not satisfied.
    /// </summary>
    /// <param name="predicate">A function to test the value of the current Option.</param>
    /// <returns>
    /// The current Option if it has a value and the value does not satisfy the predicate;
    /// otherwise, an empty Option.
    /// </returns>
    public Option<T> WhereNot(Func<T, bool> predicate) => _value is null || predicate(_value) ? None() : this;
    
    /// <summary>
    /// Explicitly converts a nullable value of type T to an Option&lt;T&gt;.
    /// </summary>
    /// <param name="value">The nullable value to convert to an Option&lt;T&gt;.</param>
    /// <returns>
    /// An Option&lt;T&gt; representing the input value:
    /// - If the input is null, returns None.
    /// - If the input is not null, returns Some with the input value.
    /// </returns>
    public static explicit operator Option<T>(T? value) => value is null ? None() : Some(value);
    
    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);
    
    /// <inheritdoc />
    public bool Equals(Option<T> other) => _value?.Equals(other._value) ?? other._value is null;
    
    
    /// <summary>
    /// Determines whether two specified Option objects have the same value.
    /// </summary>
    /// <param name="left">The first Option to compare.</param>
    /// <param name="right">The second Option to compare.</param>
    /// <returns>true if the value of left is the same as the value of right; otherwise, false.</returns>
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
    /// <summary>
    /// Determines whether two specified Option objects do not have the same value.
    /// </summary>
    /// <param name="left">The first Option to compare.</param>
    /// <param name="right">The second Option to compare.</param>
    /// <returns>true if the value of left is not the same as the value of right; otherwise, false.</returns>
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);
}