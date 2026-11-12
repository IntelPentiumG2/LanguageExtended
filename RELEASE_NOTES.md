# LanguageExtended — .NET 11 / C# 15 release

## Breaking changes

1. **Dropped `net8.0` and `net9.0`.** The package now targets `net10.0` and `net11.0` only. Both dropped frameworks went out of support on 2026-11-10.
2. **New JSON format for `Option<T>` and `ValueOption<T>`.** Some is written as the plain value and None as `null`, e.g. `{"Name":"x","Age":null}`. JSON stored in the old shapes no longer deserializes:
   - `Option<T>` was `{"IsSome":true,"Value":"x","IsNone":false}`. None never serialized at all.
   - `ValueOption<T>` was `[5]` / `[]`.
3. **`MappingError.Exception` is no longer serialized** (`[JsonIgnore]`), so stack traces and internals don't leak to clients. It is `null` after deserialization.
4. **Pattern matching on `Option`/`ValueOption`/`Result` (net11 only).** These types are now C# 15 unions, so a type pattern checks the *contained* value: `result is int` is true for `Success(5)`. Property patterns without a type, such as `result is { IsFailure: true }`, still check the wrapper.
5. **Mapper: union-typed source members are unwrapped (net11 only).** `Option<string>` maps to a `string` target and `ValueOption<int>` to `int`. A **failed** `Result` source is now a `ConversionError` mapping error, or is skipped when `LenientMappingErrors` is on. On net10 such members still go through normal conversion, which fails.
6. **Mapper: `CreateEmptyObjectsInsteadOfNull` now works.** A null source value for a complex member creates an empty instance. Before, the mapping failed with a `NullReferenceException` wrapped in a `GeneralMappingError`.
7. **`Mapper()` parameterless constructor removed.** Use `Mapper.Default` or `new Mapper(new MappingOptions())`.
8. **`ValueOption<T>` no longer implements `IEnumerable<T>`.** Serializers, mappers and test frameworks treated it as a collection. Use the new `AsEnumerable()` for LINQ or `foreach`.

## New

- **C# 15 union support (net11).** `Option<T>`, `ValueOption<T>` and `Result<T, TError>` implement `IUnion` and carry `[Union]`:
  ```csharp
  Option<string> name = "Ann";                    // implicit conversion
  string text = name switch { string s => s, null => "none" };      // exhaustive
  Result<int, MappingError> r = 5;
  string msg = r switch { int v => $"ok {v}", MappingError e => e.Message };
  ```
  When `T` and `TError` are the same type (e.g. `Result<string, string>`), use `IsSuccess` or `Match` instead of type patterns. `default(Result<,>)` holds no value, so a two-arm switch throws on it.
- **System.Text.Json support for `Option<T>` / `ValueOption<T>`**, applied automatically through `[JsonConverter]`. Works with source-generated `JsonSerializerContext`s; list the wrapped types too, e.g. `[JsonSerializable(typeof(int?))]`.
- **Opt-in JSON support for `Result<T, TError>`.** The format is `{"value": ...}` / `{"error": ...}` and works for every `T`/`TError` pair:
  ```csharp
  var options = new JsonSerializerOptions { Converters = { new ResultJsonConverterFactory() } };
  ```
  Without it, serializing a `Result` still throws, as before.
- **More `Option<T>` members:** `Match<TResult>`, `OrElse`, `ToResult`, `ToNullable`, and `AsEnumerable()` (zero or one item) on both `Option<T>` and `ValueOption<T>`.
- **More `Result<T, TError>` members:** `Bind`, `Reduce`, `Ensure`, plus a `ToOption()` extension.
- **Sequence helpers:** `FirstOrNone`, `LastOrNone`, `SingleOrNone` (each with an optional predicate) and `Values()` for `IEnumerable<Option<T>>`.
- **Async extensions for `Task<Option<T>>` and `Task<Result<T, TError>>`:** `MapAsync`, `BindAsync`, `MatchAsync`, `ReduceAsync`, `WhereAsync` / `EnsureAsync`, `IfSomeAsync` / `IfNoneAsync`, `IfSuccessAsync` / `IfFailureAsync`, `MapErrorAsync`.
- **Mapper:** null values in dictionaries are now kept instead of being dropped silently.
- **Mapper:** `ValueOption<T>` members are copied when source and target have the same type. Before, they went down the collection path and failed.

## Other changes

- Nullability fixes across the mapper; the library builds with zero warnings and treats nullable warnings as errors.
- Replaced the internal reference-equality comparer with the built-in `ReferenceEqualityComparer.Instance`.
- Extension methods moved to C# 14 extension blocks, and the mapper internals were simplified.

## Known limitations

- **Native AOT:** the mapper is reflection-based and not AOT-compatible. The JSON converter factories close generic types at runtime and may fail under Native AOT for value-type arguments (not verified).
