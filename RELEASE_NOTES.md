# LanguageExtended 2.1.0 — mapper fixes

This release fixes the mapper's handling of collections, dictionaries, dynamic sources and struct targets, and adds support for more collection types. Option and Result are unchanged.

## Behavior changes

1. **Collection items and dictionary entries that fail conversion now fail the mapping** with a `ConversionError`, the same as a single property. Before, they were dropped silently, e.g. `["1", "x"]` into `List<int>` gave `[1]`. Set `LenientMappingErrors` to skip them as before.
2. **Errors inside collection elements are no longer ignored.** If a complex element can't be mapped, for example because a member is missing, the whole mapping fails. Before, it reported success and returned the half-mapped element.
3. **`ThrowOnMappingFailure` now works.** `Map` and `MapWithoutDefaultConstructor` throw a `MappingException` instead of returning a failed result. The exception carries the `MappingError`, uses its message, and wraps its `Exception` as `InnerException`. Before, the option had no effect.
4. **`ExpandoObject` sources go through the same logic as other sources.** Nested objects, collections, `IgnoreCase` and `CreateEmptyObjectsInsteadOfNull` now apply to them; before, `IgnoreCase` had no effect on `ExpandoObject` sources. Two side effects:
   - With `IgnoreCase`, an `ExpandoObject` with keys that differ only in case (`name` and `Name`) fails the mapping with a `GeneralMappingError`.
   - A target setter that throws now fails the mapping even with `LenientMappingErrors`, as it already did for other sources.
5. **Nested objects inside an `ExpandoObject` are copied.** Before, the target got the same instance as the source.
6. **`DynamicObject` subclasses are read like regular objects:** only the properties being mapped are read. A throwing getter on an unrelated property no longer fails the mapping.
7. **Dictionary error types:** a dictionary type that can't be created now reports `CollectionCreationError` (was `DictionaryCreationError`). A non-dictionary source for a dictionary target reports `CollectionMappingError`.

## Fixed

- **Crash on circular references through collections.** A list item that referenced its parent made the process die with a stack overflow, which can't be caught. Cycles now map to the same target instance, as they already did for plain properties.
- **Struct targets came back unmapped.** `Map<MyStruct>(source)` returned success with every member left at its default.
- **Lists and arrays of types without a parameterless constructor (such as records) failed.** Single properties of those types already worked.
- **`IDictionary<K,V>` and `IReadOnlyDictionary<K,V>` targets always failed.** Non-generic dictionaries such as `Hashtable` threw.
- **Dictionary values of a different class type were dropped.** `Dictionary<string, Dto>` into `Dictionary<string, Entity>` gave an empty dictionary. Values are now mapped like properties.
- **`ArrayList` and `List<object>` lost their items.** Each item was replaced with an empty `object`.
- **Mapping one source object into two members of different types failed.** For example, the same `person` instance into a `Person` member and a `PersonSummary` member.
- **Target classes with an indexer could not be mapped.** They failed with `MemberNotFound` for `Item`.
- **A nested `ExpandoObject` failed** with a `ConversionError`.
- **Nested collections are converted element by element.** For example, `List<string[]>` into `List<List<int>>`.

## New collection types

Collection members can now also be:

- `ISet<T>`, `IReadOnlySet<T>` (filled as a `HashSet<T>`)
- any type with a parameterless constructor and `ICollection<T>.Add`: `HashSet<T>`, `SortedSet<T>`, `LinkedList<T>`, `ObservableCollection<T>`, `SortedDictionary<K,V>`, `ConcurrentDictionary<K,V>`, custom collections
- types built from their items: `Queue<T>`, `Stack<T>` and `ConcurrentStack<T>` (order kept), `ReadOnlyCollection<T>`, `ReadOnlyDictionary<K,V>`

Types the mapper still can't create, such as `ImmutableList<T>`, fail with a `CollectionCreationError`.

## Deprecated

- **`MappingOptions.IgnoreMissingSourceMembers`** is now `[Obsolete]`. It never had an effect: source members without a matching target member are always ignored. It will be removed in the next major version.

## Known limitations

- **Native AOT:** the mapper is reflection-based and not AOT-compatible.
- **Generic-only dictionaries as sources:** the source for a dictionary-typed member must implement the non-generic `IDictionary`. All BCL dictionaries do, but `ExpandoObject` does not, so an `ExpandoObject` can't be mapped into a dictionary member.
