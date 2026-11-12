using System.Text.Json;
using System.Text.Json.Serialization;

namespace LanguageExtended.Result;

/// <summary>
/// Opt-in System.Text.Json support for <see cref="Result{T, TError}"/>.
/// Writes <c>{"value": ...}</c> on success and <c>{"error": ...}</c> on failure.
/// </summary>
/// <example><c>options.Converters.Add(new ResultJsonConverterFactory());</c></example>
public sealed class ResultJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<,>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(ResultJsonConverter<,>).MakeGenericType(typeToConvert.GetGenericArguments()))!;
}

internal sealed class ResultJsonConverter<T, TError> : JsonConverter<Result<T, TError>>
{
    // Fixed names: the discriminator is part of the wire format, not subject to naming policies.
    private const string ValueName = "value";
    private const string ErrorName = "error";
    private const string Shape = "Expected {\"value\": ...} or {\"error\": ...}.";

    public override Result<T, TError> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject || !reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
            throw new JsonException(Shape);

        // Discriminate on which property is present, so Success(null) stays a success.
        bool isSuccess = reader.ValueTextEquals(ValueName);
        if (!isSuccess && !reader.ValueTextEquals(ErrorName))
            throw new JsonException(Shape);

        reader.Read();
        // '!': a JSON null payload is kept as-is, matching Success(default)/Failure(default) for nullable T/TError.
        Result<T, TError> result = isSuccess
            ? Result<T, TError>.Success(JsonSerializer.Deserialize(ref reader, options.TypeInfo<T>())!)
            : Result<T, TError>.Failure(JsonSerializer.Deserialize(ref reader, options.TypeInfo<TError>())!);

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException(Shape);

        return result;
    }

    public override void Write(Utf8JsonWriter writer, Result<T, TError> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.TryGetValue(out T? success))
        {
            writer.WritePropertyName(ValueName);
            JsonSerializer.Serialize(writer, success, options.TypeInfo<T?>());
        }
        else
        {
            writer.WritePropertyName(ErrorName);
            JsonSerializer.Serialize(writer, value.Error, options.TypeInfo<TError>());
        }
        writer.WriteEndObject();
    }
}
