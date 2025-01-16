namespace LanguageExtended.Option;

public struct Option<T>  : IEquatable<Option<T>> where T : class
{
    private readonly T? value;
    

    /// <summary>
    /// Constructor to create an Option with a value
    /// </summary>
    /// <param name="value"> The value of the Option object </param>
    private Option(T? value) => this.value = value;
    
    /// <summary>
    /// Factory method to create an Option with a value
    /// </summary>
    /// <param name="value"> The value of the Option object</param>
    /// <returns>A new Option with the given value</returns>
    public static Option<T> Some(T value)
    {
        return new Option<T>(value);
    }
    /// <summary>
    /// Factory method to create an Option without a value
    /// </summary>
    /// <returns>A new empty Option object</returns>
    public static Option<T> None() => new(null);
    
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
        value is null ? Option<TResult>.None() : Option<TResult>.Some(map(value));
    
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
        value is null ? Option<TResult>.None() : map(value);
    
    /// <summary>
    /// Returns the value of the current Option if it has a value; otherwise, returns the specified default value.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the Option has no value.</param>
    /// <returns>The value of the current Option if it has a value; otherwise, the specified default value.</returns>
    public T Reduce(T defaultValue) => value ?? defaultValue;
    /// <summary>
    /// Returns the value of the current Option if it has a value; otherwise, returns the value produced by the specified function.
    /// </summary>
    /// <param name="defaultValue">A function that produces the default value to return if the Option has no value.</param>
    /// <returns>The value of the current Option if it has a value; otherwise, the value produced by the specified function.</returns>
    public T Reduce(Func<T> defaultValue) => value ?? defaultValue();
    
    /// <summary>
    /// Filters the current Option based on the provided predicate.
    /// </summary>
    /// <param name="predicate">A function to test the value of the current Option.</param>
    /// <returns>
    /// The current Option if it has a value and the value satisfies the predicate;
    /// otherwise, an empty Option.
    /// </returns>
    public Option<T> Where(Func<T, bool> predicate) => value is not null && predicate(value) ? this : None();
    
    /// <summary>
    /// Filters the current Option based on the provided predicate, returning the Option if the predicate is not satisfied.
    /// </summary>
    /// <param name="predicate">A function to test the value of the current Option.</param>
    /// <returns>
    /// The current Option if it has a value and the value does not satisfy the predicate;
    /// otherwise, an empty Option.
    /// </returns>
    public Option<T> WhereNot(Func<T, bool> predicate) => value is not null && !predicate(value) ? this : None();
    
    public override int GetHashCode() => value?.GetHashCode() ?? 0;
    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);
    
    public bool Equals(Option<T> other) => value?.Equals(other.value) ?? other.value is null;
    
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);
}