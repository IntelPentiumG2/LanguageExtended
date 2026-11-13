# LanguageExtended — Native AOT release

This release makes `Option`, `ValueOption`, `Result` and their JSON support work under trimming and Native AOT, and marks the mapper as not AOT-compatible so it warns instead of returning wrong results.

## Behavior changes

1. **The mapper now warns in trimmed and Native AOT apps.** `Mapper` is marked `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, so apps with `PublishTrimmed`, `PublishAot` or `IsAotCompatible` get IL2026/IL3050 warnings where they use it. Before, it compiled without warnings and under Native AOT could return success with members left empty. JIT apps are unaffected. A source-generated, AOT-compatible mapper is planned in [#1](https://github.com/IntelPentiumG2/LanguageExtended/issues/1).
2. **`OptionJsonConverterFactory` and `ResultJsonConverterFactory` are marked `[RequiresDynamicCode]`.** Native AOT apps that use them get an IL3050 warning; use the closed converters below instead.
3. **On .NET 11, `Option<T>` and `ValueOption<T>` no longer go through `OptionJsonConverterFactory`.** Their `[JsonConverter]` points at `OptionJsonConverter<T>` / `ValueOptionJsonConverter<T>`, which System.Text.Json closes itself. The JSON format is unchanged.

## New

- **Public converters** for registering one closed type at a time, which is Native AOT compatible: `OptionJsonConverter<T>`, `ValueOptionJsonConverter<T>` and `ResultJsonConverter<T, TError>`. They write the same format as the factories:
  ```csharp
  [JsonSourceGenerationOptions(Converters = [
      typeof(ResultJsonConverter<int, string>),
      typeof(ValueOptionJsonConverter<int>),    // only needed on .NET 10
      typeof(OptionJsonConverter<string>)])]    // only needed on .NET 10
  [JsonSerializable(typeof(MyDto))]
  public partial class AppJsonContext : JsonSerializerContext;
  ```
  At runtime, `options.Converters.Add(new ResultJsonConverter<int, string>())` works too.
- **The package is marked trimming and AOT compatible** (`IsAotCompatible`). `Option`, `ValueOption`, `Result`, their extensions and the JSON converters pass the trimming and AOT analyzers; the mapper is flagged as described above.

## Fixed

- **JSON for `Option<T>` and `ValueOption<T>` failed under Native AOT.** Every serialize and deserialize threw `MissingMethodException`, because the trimmer removed the converters the factory creates. On .NET 11 they now work without any setup; on .NET 10 register the closed converters as shown above.
- **JSON for `Result<T, TError>` with value-type arguments failed under Native AOT** with `NotSupportedException` (missing native code). Register `ResultJsonConverter<T, TError>` for each closed type instead of the factory.
- **The converter factories are trimming-safe.** Trimmed JIT apps no longer lose the converter constructors.

## Known limitations

- **The mapper is not trimming or Native AOT compatible.** See [#1](https://github.com/IntelPentiumG2/LanguageExtended/issues/1).
- **.NET 10 under Native AOT:** `Option<T>` and `ValueOption<T>` need their closed converters registered per type, because the .NET 10 JSON source generator doesn't support open generic converters.
- **.NET 11 JSON source generation warns `SYSLIB1228` for `Result<T, TError>`.** The generator doesn't recognize the way `Result` implements the C# 15 union pattern. Serialization works once a converter is registered; the warning can be suppressed with `<NoWarn>$(NoWarn);SYSLIB1228</NoWarn>`.
