// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
using LanguageExtended.Option;

namespace LanguageExtended.Result;

/// <summary>
/// Provides extension methods for working with <see cref="Result{T, TError}"/> instances.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executes the provided action if the result is a failure, then returns the original result.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="action">The action to execute with the error value if the result is a failure.</param>
    /// <returns>The original result, allowing for method chaining.</returns>
    public static Result<T, TError> IfFailure<T, TError>(this Result<T, TError> result, Action<TError> action)
    {
        if (result.IsFailure)
            action(result.Error);
        return result;
    }

    /// <summary>
    /// Executes the provided action if the result is a success, then returns the original result.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="action">The action to execute with the success value if the result is a success.</param>
    /// <returns>The original result, allowing for method chaining.</returns>
    public static Result<T, TError> IfSuccess<T, TError>(this Result<T, TError> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        return result;
    }

    /// <summary>
    /// Converts a <see cref="Result{T, TError}"/> to an <see cref="Option{T}"/>.
    /// A successful result becomes Some; a failed result becomes None.
    /// </summary>
    /// <typeparam name="T">The success value type (must be a reference type).</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    public static Option<T> ToOption<T, TError>(this Result<T, TError> result) where T : class =>
        result.IsSuccess ? Option<T>.Some(result.Value) : Option<T>.None();
}
