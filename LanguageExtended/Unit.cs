namespace LanguageExtended;

/// <summary>
/// A type with a single value, for results that carry no data: <c>Result&lt;Unit, string&gt;</c> instead of
/// <c>Result&lt;bool, string&gt;</c>. All instances are equal. Serializes to JSON as <c>{}</c>.
/// </summary>
/// <example><c>Result&lt;Unit, string&gt; Save() =&gt; ok ? Unit.Value : "failed";</c></example>
public readonly record struct Unit
{
    /// <summary>The only value of <see cref="Unit"/>.</summary>
    public static readonly Unit Value = default;

    /// <inheritdoc />
    public override string ToString() => "()";
}
