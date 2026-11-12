using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace LanguageExtended;

internal static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Gets <typeparamref name="T"/>'s metadata from the configured resolver (reflection or source-generated),
    /// so converters avoid the reflection-only <c>JsonSerializer</c> overloads.
    /// </summary>
    internal static JsonTypeInfo<T> TypeInfo<T>(this JsonSerializerOptions options) =>
#if NET11_0_OR_GREATER
        options.GetTypeInfo<T>();
#else
        (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
#endif
}
