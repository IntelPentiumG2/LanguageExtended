#if NET11_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LanguageExtended.Option;

/// <summary>
/// C# 15 union support: <see cref="Option{T}"/> is a union of <typeparamref name="T"/> and <c>null</c> (None).
/// Enables <c>Option&lt;string&gt; o = "x";</c> and exhaustive <c>o switch { string s =&gt; ..., null =&gt; ... }</c>.
/// </summary>
[Union]
public readonly partial struct Option<T> : Option<T>.IUnionMembers
{
    /// <summary>
    /// Union member provider. Used because the public <see cref="Option{T}.Value"/> is typed <typeparamref name="T"/>
    /// and throws on None, while the union pattern needs an <c>object?</c> Value.
    /// </summary>
    public interface IUnionMembers : IUnion
    {
        /// <summary>Creates an Option from a nullable value; <c>null</c> becomes None.</summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>Some when <paramref name="value"/> is not null; otherwise None.</returns>
        static Option<T> Create(T? value) => new(value);

        /// <summary>Indicates whether the union holds a value.</summary>
        bool HasValue { get; }

        /// <summary>Tries to get the value of the union.</summary>
        /// <param name="value">The value, when present.</param>
        /// <returns>true if the union holds a value; otherwise false.</returns>
        bool TryGetValue([NotNullWhen(true)] out T? value);
    }

    object? IUnion.Value => _value;

    bool IUnionMembers.HasValue => IsSome;
}
#endif
