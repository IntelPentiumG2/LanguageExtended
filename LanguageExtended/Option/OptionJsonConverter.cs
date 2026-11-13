using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LanguageExtended.Option;

/// <summary>
/// Serializes <see cref="Option{T}"/> and <see cref="ValueOption{T}"/> as their value (Some) or <c>null</c> (None).
/// Applied automatically via <c>[JsonConverter]</c> on .NET 10; .NET 11 uses <see cref="OptionJsonConverter{T}"/>
/// and <see cref="ValueOptionJsonConverter{T}"/> directly.
/// </summary>
/// <remarks>
/// Trimming-safe, but not Native AOT compatible for <see cref="ValueOption{T}"/>, because it closes the converter types at
/// runtime. Under Native AOT on .NET 10, register the closed converters instead,
/// e.g. <c>[JsonSourceGenerationOptions(Converters = [typeof(ValueOptionJsonConverter&lt;int&gt;)])]</c>.
/// </remarks>
[RequiresDynamicCode("Creates converters at runtime. Under Native AOT use OptionJsonConverter<T> / ValueOptionJsonConverter<T> "
    + "(automatic on .NET 11; on .NET 10 register the closed types in JsonSourceGenerationOptions.Converters).")]
public sealed class OptionJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType
        && (typeToConvert.GetGenericTypeDefinition() == typeof(Option<>) || typeToConvert.GetGenericTypeDefinition() == typeof(ValueOption<>));

    /// <inheritdoc />
    // typeof(...) directly on MakeGenericType lets the trimmer see and keep the converter constructors.
    [UnconditionalSuppressMessage("Trimming", "IL2055",
        Justification = "ValueOptionJsonConverter<T>'s only requirement is the new() implied by 'struct'; T comes from the ValueOption<T> being converted, and value types always have a default constructor.")]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeToConvert.GetGenericTypeDefinition() == typeof(Option<>)
            ? typeof(OptionJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments())
            : typeof(ValueOptionJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()))!;
}

/// <summary>
/// Serializes an <see cref="Option{T}"/> as its value (Some) or <c>null</c> (None). Native AOT compatible.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class OptionJsonConverter<T> : JsonConverter<Option<T>> where T : class
{
    /// <inheritdoc />
    // Read must also run for JSON null, which becomes None.
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize(ref reader, options.TypeInfo<T?>()).ToOption();

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.TryGetValue(out T? some) ? some : null, options.TypeInfo<T?>());
}

/// <summary>
/// Serializes a <see cref="ValueOption{T}"/> as its value (Some) or <c>null</c> (None). Native AOT compatible.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class ValueOptionJsonConverter<T> : JsonConverter<ValueOption<T>> where T : struct
{
    /// <inheritdoc />
    // Read must also run for JSON null, which becomes None.
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override ValueOption<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        (ValueOption<T>)JsonSerializer.Deserialize(ref reader, options.TypeInfo<T?>());

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ValueOption<T> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.ToNullable(), options.TypeInfo<T?>());
}
