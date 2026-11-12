#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace LanguageExtended.Option;

/// <summary>
/// C# 15 union support: <see cref="ValueOption{T}"/> is a union of <typeparamref name="T"/> and <c>null</c> (None).
/// Enables exhaustive <c>o switch { int i =&gt; ..., null =&gt; ... }</c> without boxing.
/// </summary>
[Union]
public readonly partial struct ValueOption<T> : ValueOption<T>.IUnionMembers
{
    /// <summary>
    /// Union member provider. Used because the public <see cref="ValueOption{T}.Value"/> is typed <typeparamref name="T"/>
    /// and throws on None, while the union pattern needs an <c>object?</c> Value.
    /// </summary>
    public interface IUnionMembers : IUnion
    {
        /// <summary>Creates a ValueOption from a nullable value; <c>null</c> becomes None.</summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>Some when <paramref name="value"/> has a value; otherwise None.</returns>
        static ValueOption<T> Create(T? value) => value.HasValue ? Some(value.Value) : None();

        /// <summary>Indicates whether the union holds a value.</summary>
        bool HasValue { get; }

        /// <summary>Tries to get the value of the union without boxing.</summary>
        /// <param name="value">The value, when present.</param>
        /// <returns>true if the union holds a value; otherwise false.</returns>
        bool TryGetValue(out T value);
    }

    object? IUnion.Value => _hasValue ? _value : null;

    bool IUnionMembers.HasValue => _hasValue;
}
#endif
