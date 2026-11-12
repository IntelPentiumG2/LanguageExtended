#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace LanguageExtended.Result;

/// <summary>
/// C# 15 union support: <see cref="Result{T, TError}"/> is a union of <typeparamref name="T"/> and <typeparamref name="TError"/>.
/// Enables exhaustive <c>r switch { int v =&gt; ..., MyError e =&gt; ... }</c>.
/// </summary>
/// <remarks>
/// Type patterns match the contained value's type. When <typeparamref name="T"/> and <typeparamref name="TError"/>
/// overlap (e.g. <c>Result&lt;string, string&gt;</c>), use <see cref="Result{T, TError}.IsSuccess"/> or <c>Match</c> instead.
/// <c>default(Result&lt;T, TError&gt;)</c> holds no value, so a switch over only <typeparamref name="T"/> and
/// <typeparamref name="TError"/> throws <see cref="System.Runtime.CompilerServices.SwitchExpressionException"/> on it.
/// </remarks>
[Union]
public readonly partial struct Result<T, TError> : Result<T, TError>.IUnionMembers, IResult
{
    /// <summary>
    /// Union member provider. Used because the public <see cref="Result{T, TError}.Value"/> is typed <typeparamref name="T"/>
    /// and throws on failure, while the union pattern needs an <c>object?</c> Value.
    /// No TryGetValue members: with overlapping case types they would disagree with Value and break pattern matching.
    /// </summary>
    public interface IUnionMembers : IUnion
    {
        /// <summary>Creates a successful result.</summary>
        /// <param name="value">The success value.</param>
        /// <returns>A successful result.</returns>
        static Result<T, TError> Create(T value) => Success(value);

        /// <summary>Creates a failed result.</summary>
        /// <param name="error">The error value.</param>
        /// <returns>A failed result.</returns>
        static Result<T, TError> Create(TError error) => Failure(error);
    }

    object? IUnion.Value => IsSuccess ? _value : _error;
}

/// <summary>Non-generic view of <see cref="Result{T, TError}"/> so the mapper can detect failures.</summary>
internal interface IResult
{
    bool IsFailure { get; }
}
#endif
