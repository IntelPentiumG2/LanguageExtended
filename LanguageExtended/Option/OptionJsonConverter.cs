using System.Text.Json;
using System.Text.Json.Serialization;

namespace LanguageExtended.Option;

/// <summary>
/// Serializes <see cref="Option{T}"/> and <see cref="ValueOption{T}"/> as their value (Some) or <c>null</c> (None).
/// Needed on .NET 11, where System.Text.Json cannot build a union contract for them.
/// Applied automatically via <c>[JsonConverter]</c>; public so the System.Text.Json source generator can use it.
/// </summary>
public sealed class OptionJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => ConverterFor(typeToConvert) is not null;

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            ConverterFor(typeToConvert)!.MakeGenericType(typeToConvert.GetGenericArguments()))!;

    private static Type? ConverterFor(Type type) =>
        !type.IsGenericType ? null
        : type.GetGenericTypeDefinition() == typeof(Option<>) ? typeof(OptionJsonConverter<>)
        : type.GetGenericTypeDefinition() == typeof(ValueOption<>) ? typeof(ValueOptionJsonConverter<>)
        : null;
}

internal sealed class OptionJsonConverter<T> : JsonConverter<Option<T>> where T : class
{
    // Read must also run for JSON null, which becomes None.
    public override bool HandleNull => true;

    public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize(ref reader, options.TypeInfo<T?>()).ToOption();

    public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.TryGetValue(out T? some) ? some : null, options.TypeInfo<T?>());
}

internal sealed class ValueOptionJsonConverter<T> : JsonConverter<ValueOption<T>> where T : struct
{
    // Read must also run for JSON null, which becomes None.
    public override bool HandleNull => true;

    public override ValueOption<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        (ValueOption<T>)JsonSerializer.Deserialize(ref reader, options.TypeInfo<T?>());

    public override void Write(Utf8JsonWriter writer, ValueOption<T> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.ToNullable(), options.TypeInfo<T?>());
}
