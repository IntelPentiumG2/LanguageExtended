// ReSharper disable UnusedMember.Global
namespace LanguageExtended.Result;

/// <summary>
/// Represents the result of an operation, which can be either a success or a failure.
/// </summary>
/// <typeparam name="T">The type of the value in case of success.</typeparam>
/// <typeparam name="TError">The type of the error in case of failure.</typeparam>
public readonly struct Result<T, TError> : IEquatable<Result<T, TError>>
{
    private readonly T? _value;
    private readonly TError? _error;

    private Result(T? value, TError? error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Indicates whether the result is a success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value of the result if it is a success.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the result is a failure.</exception>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Result is a failure.");

    /// <summary>
    /// Gets the error of the result if it is a failure.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the result is a success.</exception>
    public TError Error => IsFailure ? _error! : throw new InvalidOperationException("Result is a success.");

    /// <summary>
    /// Tries to get the value of the result if it is a success.
    /// </summary>
    /// <param name="value">The value of the result.</param>
    /// <returns>true if the result is a success; otherwise, false.</returns>
    public bool TryGetValue(out T? value)
    {
        value = _value;
        return IsSuccess;
    }

    /// <summary>
    /// Tries to get the error of the result if it is a failure.
    /// </summary>
    /// <param name="error">The error of the result.</param>
    /// <returns>true if the result is a failure; otherwise, false.</returns>
    public bool TryGetError(out TError? error)
    {
        error = _error;
        return IsFailure;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The value of the result.</param>
    /// <returns>A successful result.</returns>
    public static Result<T, TError> Success(T value) => new(value, default, true);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error of the result.</param>
    /// <returns>A failed result.</returns>
    public static Result<T, TError> Failure(TError error) => new(default, error, false);

    /// <summary>
    /// Maps the value of a successful result to a new result using the provided mapping function.
    /// </summary>
    /// <typeparam name="TResult">The type of the value in the resulting result.</typeparam>
    /// <param name="map">A function to transform the value of the current result.</param>
    /// <returns>A new result containing the transformed value if the current result is a success; otherwise, a failed result with the same error.</returns>
    public Result<TResult, TError> Map<TResult>(Func<T, TResult> map) =>
        IsSuccess ? Result<TResult, TError>.Success(map(_value!)) : Result<TResult, TError>.Failure(_error!);

    /// <summary>
    /// Matches the result to one of the provided functions.
    /// </summary>
    /// <param name="onSuccess">A function to call if the result is a success.</param>
    /// <param name="onFailure">A function to call if the result is a failure.</param>
    /// <returns>
    /// The result of the <paramref name="onSuccess"/> function if the result is a success;
    /// otherwise, the result of the <paramref name="onFailure"/> function.
    /// </returns>
    public T Match(Func<T, T> onSuccess, Func<TError, T> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    /// <summary>
    /// Executes one of the provided actions based on whether the result is a success or failure.
    /// </summary>
    /// <param name="onSuccess">The action to execute if the result is a success.</param>
    /// <param name="onFailure">The action to execute if the result is a failure.</param>
    public void Match(Action<T> onSuccess, Action<TError> onFailure)
    {
        if (IsSuccess)
            onSuccess(Value);
        else
            onFailure(Error);
    }

    /// <summary>
    /// Maps the error of a failed result to a new result using the provided mapping function.
    /// </summary>
    /// <typeparam name="TResultError">The type of the error in the resulting result.</typeparam>
    /// <param name="map">A function to transform the error of the current result.</param>
    /// <returns>A new result containing the transformed error if the current result is a failure; otherwise, a successful result with the same value.</returns>
    public Result<T, TResultError> MapError<TResultError>(Func<TError, TResultError> map) =>
        IsFailure ? Result<T, TResultError>.Failure(map(_error!)) : Result<T, TResultError>.Success(_value!);

    /// <summary>
    /// Chains a function that returns a new Result, passing through the current error if this Result is a failure.
    /// Use this to compose operations where each step may fail.
    /// </summary>
    /// <typeparam name="TResult">The value type of the resulting Result.</typeparam>
    /// <param name="bind">A function that takes the success value and returns a new Result.</param>
    /// <returns>The Result returned by <paramref name="bind"/> if this is a success; otherwise, a failure with the current error.</returns>
    public Result<TResult, TError> Bind<TResult>(Func<T, Result<TResult, TError>> bind) =>
        IsSuccess ? bind(_value!) : Result<TResult, TError>.Failure(_error!);

    /// <summary>
    /// Returns the success value if this Result is a success; otherwise, returns <paramref name="defaultValue"/>.
    /// </summary>
    /// <param name="defaultValue">The fallback value to return on failure.</param>
    /// <returns>The success value or <paramref name="defaultValue"/>.</returns>
    public T Reduce(T defaultValue) => IsSuccess ? _value! : defaultValue;

    /// <summary>
    /// Returns the success value if this Result is a success; otherwise, invokes <paramref name="fallback"/> with the error.
    /// </summary>
    /// <param name="fallback">A function that produces a fallback value from the error.</param>
    /// <returns>The success value or the result of <paramref name="fallback"/>.</returns>
    public T Reduce(Func<TError, T> fallback) => IsSuccess ? _value! : fallback(_error!);

    /// <summary>
    /// Validates the success value against a predicate. If the predicate fails, converts the result to a failure.
    /// Has no effect if this Result is already a failure.
    /// </summary>
    /// <param name="predicate">A function to test the success value.</param>
    /// <param name="error">The error to use if the predicate fails.</param>
    /// <returns>The current Result if successful and predicate passes; otherwise, a failure with <paramref name="error"/>.</returns>
    public Result<T, TError> Ensure(Func<T, bool> predicate, TError error) =>
        IsSuccess && !predicate(_value!) ? Failure(error) : this;

    /// <summary>
    /// Implicitly converts a value of type <typeparamref name="T"/> to a successful <see cref="Result{T, TError}"/>.
    /// </summary>
    /// <param name="value">The value to convert to a successful result.</param>
    /// <returns>A successful result containing the specified value.</returns>
    public static implicit operator Result<T, TError>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts a value of type <typeparamref name="TError"/> to a failed <see cref="Result{T, TError}"/>.
    /// </summary>
    /// <param name="error">The error to convert to a failed result.</param>
    /// <returns>A failed result containing the specified error.</returns>
    public static implicit operator Result<T, TError>(TError error) => Failure(error);


    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_value, _error, IsSuccess);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<T, TError> other && Equals(other);

    /// <inheritdoc />
    public bool Equals(Result<T, TError> other) =>
        EqualityComparer<T?>.Default.Equals(_value, other._value) &&
        EqualityComparer<TError?>.Default.Equals(_error, other._error) &&
        IsSuccess == other.IsSuccess;

    /// <summary>
    /// Determines whether two specified instances of <see cref="Result{T, TError}"/> are equal.
    /// </summary>
    /// <param name="left">The left value to check</param>
    /// <param name="right">The value to compare to</param>
    /// <returns>true if equal, otherwise false</returns>
    public static bool operator ==(Result<T, TError> left, Result<T, TError> right) => left.Equals(right);
    /// <summary>
    /// Determines whether two specified instances of <see cref="Result{T, TError}"/> are not equal.
    /// </summary>
    /// <param name="left"> The left value to check </param>
    /// <param name="right">The value to compare to</param>
    /// <returns>true if unequal, otherwise false</returns>
    public static bool operator !=(Result<T, TError> left, Result<T, TError> right) => !left.Equals(right);
}
