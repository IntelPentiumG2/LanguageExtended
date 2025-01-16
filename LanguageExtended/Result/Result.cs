namespace LanguageExtended.Result;

/// <summary>
/// Represents the result of an operation, which can be either a success or a failure.
/// </summary>
/// <typeparam name="T">The type of the value in case of success.</typeparam>
/// <typeparam name="TError">The type of the error in case of failure.</typeparam>
public readonly struct Result<T, TError>
{
    private readonly T? _value;
    private readonly TError? _error;

    private Result(T? value, TError? error, bool isSuccess)
    {
        this._value = value;
        this._error = error;
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
    /// Maps the error of a failed result to a new result using the provided mapping function.
    /// </summary>
    /// <typeparam name="TResultError">The type of the error in the resulting result.</typeparam>
    /// <param name="map">A function to transform the error of the current result.</param>
    /// <returns>A new result containing the transformed error if the current result is a failure; otherwise, a successful result with the same value.</returns>
    public Result<T, TResultError> MapError<TResultError>(Func<TError, TResultError> map) =>
        IsFailure ? Result<T, TResultError>.Failure(map(_error!)) : Result<T, TResultError>.Success(_value!);
    
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
}