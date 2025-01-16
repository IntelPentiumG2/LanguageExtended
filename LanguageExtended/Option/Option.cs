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
    private Option(T? value) => this._value = value;
    
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