using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using LanguageExtended.Result;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global

namespace LanguageExtended.Mapping;

/// <summary>
/// Provides methods for mapping collections between objects.
/// </summary>
internal class CollectionMapper
{
    private readonly MappingOptions _options;
    private readonly ComplexTypeMapper _complexTypeMapper;
    private readonly TypeHelper _typeHelper;
    private readonly TypeConverter _typeConverter;

    /// <summary>
    /// Initializes a new instance of the CollectionMapper class.
    /// </summary>
    /// <param name="options">The mapping options</param>
    /// <param name="complexTypeMapper">Maps complex elements</param>
    /// <param name="typeHelper">The Helper</param>
    /// <param name="typeConverter">The <see cref="TypeConverter"/> to use for conversions</param>
    internal CollectionMapper(MappingOptions options, ComplexTypeMapper complexTypeMapper, TypeHelper typeHelper, TypeConverter typeConverter)
    {
        _options = options;
        _complexTypeMapper = complexTypeMapper;
        _typeHelper = typeHelper;
        _typeConverter = typeConverter;
    }

    /// <summary>
    /// Handles the mapping of collections.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="value">The value to map.</param>
    /// <param name="mappingContext">Tracks already mapped objects for circular reference handling.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    internal Result<bool, MappingError> HandleCollection(object target, MemberInfo targetMember, object value, MappingContext mappingContext) =>
        MapCollection(value, MemberAccessor.GetMemberType(targetMember), mappingContext)
            .Bind(collection => MemberAccessor.SetMemberValue(target, targetMember, collection));

    /// <summary>
    /// Maps a source collection into a new collection of <paramref name="collectionType"/>.
    /// </summary>
    private Result<object?, MappingError> MapCollection(object value, Type collectionType, MappingContext mappingContext)
    {
        try
        {
            if (GetDictionaryTypes(collectionType) is [Type keyType, Type valueType])
                return MapDictionary(value, collectionType, keyType, valueType, mappingContext);

            Type elementType = _typeHelper.GetElementType(collectionType);
            IList items = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

            foreach (object? item in (IEnumerable)value)
            {
                Result<object?, MappingError> mapped = MapValue(item, elementType, mappingContext);
                if (mapped.IsSuccess)
                    items.Add(mapped.Value);
                else if (!_options.LenientMappingErrors)
                    return mapped;
            }

            if (collectionType.IsArray)
            {
                Array array = Array.CreateInstance(elementType, items.Count);
                items.CopyTo(array, 0);
                return Result<object?, MappingError>.Success(array);
            }

            return Materialize(collectionType, items, elementType);
        }
        catch (Exception ex)
        {
            // Covers throwing collection constructors and duplicate keys after key conversion.
            return Result<object?, MappingError>.Failure(new MappingError(
                $"Failed to map collection of type {collectionType.Name}: {ex.Message}",
                MappingErrorType.CollectionMappingError,
                collectionType.Name,
                ex));
        }
    }

    /// <summary>
    /// Maps the entries of a source dictionary into a Dictionary&lt;K,V&gt;, then into <paramref name="dictionaryType"/>.
    /// </summary>
    private Result<object?, MappingError> MapDictionary(object value, Type dictionaryType, Type keyType, Type valueType, MappingContext mappingContext)
    {
        if (value is not IDictionary source)
            return Result<object?, MappingError>.Failure(new MappingError(
                $"Cannot map {value.GetType().Name} to dictionary type {dictionaryType.Name}: source is not a dictionary",
                MappingErrorType.CollectionMappingError,
                dictionaryType.Name));

        IDictionary entries = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))!;
        foreach (DictionaryEntry entry in source)
        {
            Result<object?, MappingError> key = MapValue(entry.Key, keyType, mappingContext);
            Result<object?, MappingError> mapped = key.IsSuccess ? MapValue(entry.Value, valueType, mappingContext) : key;
            if (mapped.IsSuccess)
                entries.Add(key.Value!, mapped.Value); // Mapping a non-null key never yields null; Add throws on collisions instead of losing data.
            else if (!_options.LenientMappingErrors)
                return mapped;
        }

        Type entryType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
        return Materialize(dictionaryType, entries, entryType);
    }

    /// <summary>
    /// Maps a single element, key or value the same way the mapper maps a member value.
    /// </summary>
    private Result<object?, MappingError> MapValue(object? item, Type targetType, MappingContext mappingContext)
    {
        // object has no members to map into, so untyped collections (ArrayList, List<object>) keep their items.
        if (item is null || targetType == typeof(object))
            return Result<object?, MappingError>.Success(item);

        Type itemType = item.GetType();
        if (TypeHelper.IsComplexType(targetType) && TypeHelper.IsComplexType(itemType))
            return _complexTypeMapper.MapNested(item, targetType, mappingContext).Map<object?>(nested => nested);

        if (TypeHelper.IsCollection(targetType) && TypeHelper.IsCollection(itemType))
            return MapCollection(item, targetType, mappingContext);

        return _typeConverter.TryConvertValue(item, targetType);
    }

    /// <summary>
    /// Turns the mapped items (a List&lt;T&gt; or Dictionary&lt;K,V&gt;) into an instance of <paramref name="targetType"/>.
    /// </summary>
    /// <param name="targetType">The collection type the target member declares.</param>
    /// <param name="items">The mapped items.</param>
    /// <param name="itemType">The type of a single item (T, or KeyValuePair&lt;K,V&gt; for dictionaries).</param>
    private static Result<object?, MappingError> Materialize(Type targetType, IEnumerable items, Type itemType)
    {
        // List<T>, IList<T>, IEnumerable<T>, IReadOnlyList<T>, Dictionary<K,V>, IDictionary<K,V>, ...
        if (targetType.IsInstanceOfType(items))
            return Result<object?, MappingError>.Success(items);

        Type setType = typeof(HashSet<>).MakeGenericType(itemType);
        if (targetType.IsInterface && targetType.IsAssignableFrom(setType))
            return Result<object?, MappingError>.Success(Activator.CreateInstance(setType, items));

        if (targetType is { IsAbstract: false, IsInterface: false })
        {
            Type genericCollection = typeof(ICollection<>).MakeGenericType(itemType);
            bool hasDefaultCtor = targetType.GetConstructor(Type.EmptyTypes) != null;

            // HashSet<T>, SortedSet<T>, LinkedList<T>, SortedDictionary<K,V>, ConcurrentDictionary<K,V>, custom collections
            if (hasDefaultCtor && genericCollection.IsAssignableFrom(targetType))
            {
                object collection = Activator.CreateInstance(targetType)!;
                MethodInfo add = genericCollection.GetMethod(nameof(ICollection<object>.Add))!;
                foreach (object? item in items)
                    add.Invoke(collection, [item]);
                return Result<object?, MappingError>.Success(collection);
            }

            // Non-generic collections such as ArrayList and Hashtable
            if (hasDefaultCtor && typeof(IList).IsAssignableFrom(targetType))
            {
                IList list = (IList)Activator.CreateInstance(targetType)!;
                foreach (object? item in items)
                    list.Add(item);
                return Result<object?, MappingError>.Success(list);
            }

            if (hasDefaultCtor && typeof(IDictionary).IsAssignableFrom(targetType) && items is IDictionary entries)
            {
                IDictionary dictionary = (IDictionary)Activator.CreateInstance(targetType)!;
                foreach (DictionaryEntry entry in entries)
                    dictionary.Add(entry.Key, entry.Value);
                return Result<object?, MappingError>.Success(dictionary);
            }

            // Types built from their items: Queue<T>, Stack<T>, ReadOnlyCollection<T>, ReadOnlyDictionary<K,V>, ...
            ConstructorInfo? fromItems = targetType.GetConstructors()
                .FirstOrDefault(c => c.GetParameters() is [{ } p] && p.ParameterType.IsInstanceOfType(items));
            if (fromItems != null)
                return Result<object?, MappingError>.Success(fromItems.Invoke([IsStack(targetType) ? Reverse(items, itemType) : items]));
        }

        return Result<object?, MappingError>.Failure(new MappingError(
            $"Cannot create collection of type {targetType.Name}",
            MappingErrorType.CollectionCreationError,
            targetType.Name));
    }

    /// <summary>
    /// Stacks enumerate top first but push in enumeration order, so their items must be reversed to keep the order.
    /// </summary>
    private static bool IsStack(Type type) =>
        type == typeof(Stack)
        || type.IsGenericType && type.GetGenericTypeDefinition() is var d && (d == typeof(Stack<>) || d == typeof(ConcurrentStack<>));

    private static IList Reverse(IEnumerable items, Type itemType)
    {
        IList reversed = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
        foreach (object? item in items)
            reversed.Insert(0, item);
        return reversed;
    }

    /// <summary>
    /// Gets the key and value types of a dictionary type, or null if the type is not a dictionary.
    /// Checks the generic IDictionary/IReadOnlyDictionary interfaces (which do not imply the non-generic IDictionary)
    /// and falls back to object keys and values for a non-generic IDictionary such as Hashtable.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The key and value types, or null.</returns>
    private static Type[]? GetDictionaryTypes(Type type) =>
        type.GetInterfaces().Prepend(type)
            .FirstOrDefault(t => t.IsGenericType
                                 && (t.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                                     || t.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)))
            ?.GetGenericArguments()
        ?? (typeof(IDictionary).IsAssignableFrom(type) ? [typeof(object), typeof(object)] : null);
}
