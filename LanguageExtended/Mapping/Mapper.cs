// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using LanguageExtended.Option;
using LanguageExtended.Result;

namespace LanguageExtended.Mapping;

/// <summary>
/// Provides  methods for mapping properties and fields between objects.
/// This utility class supports mapping of primitive types, complex objects, and collections.
/// </summary>
/// <remarks>
/// The mapper automatically handles:
/// - Primitive type conversions
/// - Enum conversions
/// - Complex type mappings
/// - Collection mappings
/// </remarks>
public  class Mapper
{
    private readonly MappingOptions _options;
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties;
    private readonly ConcurrentDictionary<Type, FieldInfo[]> _fields;
    private readonly ConcurrentDictionary<(Type sourceType, Type targetType), Dictionary<string, MemberInfo>> _memberMappingCache;
    private readonly ConcurrentDictionary<Type,Type[]> _interfaceMappingCache;
    
    private static readonly Lazy<Mapper> DefaultInstance = new (() => new Mapper(new MappingOptions()), LazyThreadSafetyMode.ExecutionAndPublication);
    
    /// <summary>
    /// Gets the default instance of the Mapper.
    /// </summary>
    public static Mapper Default => DefaultInstance.Value;
    
    /// <summary>
    /// Initializes a new instance of the Mapper class with default options.
    /// </summary>
    public Mapper() : this(new MappingOptions()) { }

    /// <summary>
    /// Initializes a new instance of the Mapper class with the specified options.
    /// </summary>
    /// <param name="options"> The options of the Mapper </param>
    public Mapper(MappingOptions options)
    {
        _options = options;
        _properties = new ConcurrentDictionary<Type, PropertyInfo[]>();
        _fields = new ConcurrentDictionary<Type, FieldInfo[]>();
        _memberMappingCache =
            new ConcurrentDictionary<(Type sourceType, Type targetType), Dictionary<string, MemberInfo>>();
        _interfaceMappingCache = new ConcurrentDictionary<Type, Type[]>();
    }
    
    /// <summary>
    /// Maps the properties and fields from the source object to a new instance of the target type.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target object.</typeparam>
    /// <param name="source">The source object to map from.</param>
    /// <returns>A Result containing the mapped target object or an error message.</returns>
    public  Result<TTarget, MappingError> Map<TTarget>(object source) where TTarget : new()
    {
        if (source == null)
            return Result<TTarget, MappingError>.Failure(new MappingError("Source cannot be null"));

        try
        {
            var target = new TTarget();
            var mapResult = Map(source, target);

            return mapResult.IsSuccess
                ? Result<TTarget, MappingError>.Success(target)
                : Result<TTarget, MappingError>.Failure(new MappingError(mapResult.Error));
        }
        catch (Exception ex)
        {
            return Result<TTarget, MappingError>.Failure(new MappingError("Mapping failed.", "", ex));
        }
    }

    /// <summary>
    /// Maps the properties and fields from the source object to the target object.
    /// </summary>
    /// <param name="source">The source object to map from.</param>
    /// <param name="target">The target object to map to.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    private Result<bool, string> Map(object source, object target)
    {
        if (source == null)
            return Result<bool, string>.Failure("Source cannot be null");

        if (target == null)
            return Result<bool, string>.Failure("Target cannot be null");

        try
        {
            Type sourceType = source.GetType();
            Type targetType = target.GetType();
            List<string> criticalErrors = [];

            foreach (var targetMember in GetTargetMembers(targetType))
            {
                var sourceMemberOption = FindSourceMember(sourceType, targetMember.Name);

                sourceMemberOption.IfSome(sourceMember =>
                {
                    var valueOption = GetMemberValue(source, sourceMember);
                    valueOption.Match(
                        value =>
                        {
                            var result = SetMappedValue(target, targetMember, value);
                            // Only treat enum conversion errors as critical
                            if (result.IsFailure &&
                                (result.Error.Contains("enum") ||
                                 !result.Error.StartsWith("Conversion error")))
                            {
                                criticalErrors.Add($"Failed to map '{targetMember.Name}': {result.Error}");
                            }
                        },
                        () =>
                        {
                            if (!IsComplexType(GetMemberType(targetMember)))
                                return;

                            var result = CreateAndSetComplexType(target, targetMember);
                            if (result.IsFailure)
                                criticalErrors.Add($"Failed to initialize '{targetMember.Name}': {result.Error}");
                        }
                    );
                });
            }

            return criticalErrors.Count != 0
                ? Result<bool, string>.Failure(string.Join("; ", criticalErrors))
                : Result<bool, string>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool, string>.Failure($"Mapping failed: {ex.Message}");
        }
    }
    
    private static Result<bool, string> CreateAndSetComplexType(object target, MemberInfo targetMember)
    {
        Type targetType = GetMemberType(targetMember);
        try
        {
            object? instance = Activator.CreateInstance(targetType);
            
            if (instance == null)
                return Result<bool, string>.Failure($"Failed to create instance of {targetType.Name}");
            
            var setResult = SetMemberValue(target, targetMember, instance);
            return setResult;
        }
        catch (Exception ex)
        {
            return Result<bool, string>.Failure($"Failed to create instance of {targetType.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the writable properties and non-readonly fields of the target type.
    /// </summary>
    /// <param name="targetType">The type of the target object.</param>
    /// <returns>An enumerable of MemberInfo representing the writable properties and fields.</returns>
    private  IEnumerable<MemberInfo> GetTargetMembers(Type targetType)
    {
        // Get writable properties
        foreach (var prop in _properties.GetOrAdd(targetType, targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)))
        {
            if (prop.CanWrite)
                yield return prop;
        }

        // Get non-readonly fields
        foreach (var field in _fields.GetOrAdd(targetType, targetType.GetFields(BindingFlags.Public | BindingFlags.Instance)))
        {
            if (!field.IsInitOnly)
                yield return field;
        }
    }

    /// <summary>
    /// Finds a member (property or field) in the source type by name.
    /// </summary>
    /// <param name="sourceType">The type of the source object.</param>
    /// <param name="memberName">The name of the member to find.</param>
    /// <returns>An Option containing the found MemberInfo or None if not found.</returns>
    private  Option<MemberInfo> FindSourceMember(Type sourceType, string memberName)
    {
        // Get cached properties safely
        var props = _properties.GetOrAdd(sourceType, t => 
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        
        var prop = props.FirstOrDefault(p => p.Name.Equals(memberName, 
                _options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        
        if (prop != null && prop.CanRead) 
            return Option<MemberInfo>.Some(prop);

        // Get cached fields safely
        var fields = _fields.GetOrAdd(sourceType, t => 
            t.GetFields(BindingFlags.Public | BindingFlags.Instance));
        
        var field = fields.FirstOrDefault(f => f.Name.Equals(memberName,
                _options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        
        return field != null 
            ? Option<MemberInfo>.Some(field) 
            : Option<MemberInfo>.None();
    }

    /// <summary>
    /// Gets the type of member (property or field).
    /// </summary>
    /// <param name="member">The member to get the type of.</param>
    /// <returns>The type of the member.</returns>
    private static Type GetMemberType(MemberInfo member)
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return member.MemberType switch
        {
            MemberTypes.Property => ((PropertyInfo)member).PropertyType,
            MemberTypes.Field => ((FieldInfo)member).FieldType,
            _ => throw new ArgumentException("Member must be a property or field", nameof(member))
        };
    }

    /// <summary>
    /// Gets the value of a member (property or field) from an object.
    /// </summary>
    /// <param name="obj">The object to get the value from.</param>
    /// <param name="member">The member to get the value of.</param>
    /// <returns>An Option containing the value of the member or None if not found.</returns>
    private static Option<object> GetMemberValue(object obj, MemberInfo member)
    {
        try
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            object? value = member.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo)member).GetValue(obj),
                MemberTypes.Field => ((FieldInfo)member).GetValue(obj),
                _ => throw new ArgumentException("Member must be a property or field", nameof(member))
            };

            return value != null ? Option<object>.Some(value) : Option<object>.None();
        }
        catch
        {
            return Option<object>.None();
        }
    }

    /// <summary>
    /// Sets the mapped value to the target member.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="value">The value to set.</param>
    private Result<bool, string> SetMappedValue(object target, MemberInfo targetMember, object value)
    {
        Type targetMemberType = GetMemberType(targetMember);
        Type valueType = value.GetType();

        if (IsComplexType(targetMemberType) && IsComplexType(valueType))
        {
            return HandleComplexType(target, targetMember, value);
        }

        if (IsCollection(targetMemberType) && IsCollection(valueType))
        {
            return HandleCollection(target, targetMember, value);
        }

        var conversionResult = TryConvertValue(value, targetMemberType);

        if (conversionResult.IsSuccess) 
            return SetMemberValue(target, targetMember, conversionResult.Value);
        
        // Special handling for enum conversion errors
        if (targetMemberType.IsEnum && value is string)
        {
            // Enum conversion failures should cause mapping failure
            return Result<bool, string>.Failure(conversionResult.Error);
        }
        
        // Ignore other conversion errors
        return Result<bool, string>.Success(true);
    }

    /// <summary>
    /// Sets the value of a member (property or field) on an object.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="member">The member to set the value to.</param>
    /// <param name="value">The value to set.</param>
    private static Result<bool, string> SetMemberValue(object target, MemberInfo member, object value)
    {
        try
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    ((PropertyInfo)member).SetValue(target, value);
                    break;
                case MemberTypes.Field:
                    ((FieldInfo)member).SetValue(target, value);
                    break;
                default:
                    return Result<bool, string>.Failure($"Member {member.Name} must be a property or field");
            }
            return Result<bool, string>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool, string>.Failure($"Failed to set value for {member.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the mapping of complex types.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="value">The value to map.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    private Result<bool, string> HandleComplexType(object target, MemberInfo targetMember, object value)
    {
        try
        {
            Type targetType = GetMemberType(targetMember);

            // Create a new instance of the complex type
            object nestedTarget;
            try
            {
                nestedTarget = Activator.CreateInstance(targetType)
                               ?? throw new InvalidOperationException($"Failed to create instance of {targetType}");

                // Set the new instance on the target object and check result
                var setResult = SetMemberValue(target, targetMember, nestedTarget);
                if (setResult.IsFailure)
                    return setResult;
            }
            catch (Exception ex)
            {
                return Result<bool, string>.Failure($"Failed to create nested object: {ex.Message}");
            }

            // Now map properties from source to the nested target
            return Map(value, nestedTarget);
        }
        catch (Exception ex)
        {
            return Result<bool, string>.Failure($"Failed to map complex type: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the mapping of collections.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="value">The value to map.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    private Result<bool, string> HandleCollection(object target, MemberInfo targetMember, object value)
    {
        try
        {
            Type collectionType = GetMemberType(targetMember);
            Type elementType = GetElementType(collectionType);
            IEnumerable sourceCollection = (IEnumerable)value;
            
            if (typeof(IDictionary).IsAssignableFrom(collectionType))
            {
                return HandleDictionary(target, targetMember, value);
            }
            
            List<object> mappedItems = [];

            foreach (object? item in sourceCollection)
            {
                if (item == null) 
                    continue;

                if (IsComplexType(elementType))
                {
                    object? nestedTarget = Activator.CreateInstance(elementType);

                    if (nestedTarget == null) 
                        continue;
                    
                    // Map the item to the nested target
                    Map(item, nestedTarget);
                    mappedItems.Add(nestedTarget);
                }
                else
                {
                    var conversionResult = TryConvertValue(item, elementType);
                    if (conversionResult.IsSuccess)
                    {
                        mappedItems.Add(conversionResult.Value);
                    }
                }
            }

            // Create and set the appropriate collection
            if (collectionType.IsArray)
            {
                // Create an array of the correct size and type
                Array array = Array.CreateInstance(elementType, mappedItems.Count);
                
                // Copy items to the array
                for (int i = 0; i < mappedItems.Count; i++)
                {
                    array.SetValue(mappedItems[i], i);
                }
                
                // Set the array on the target member
                return SetMemberValue(target, targetMember, array);
            }

            var collectionResult = TryCreateCollection(collectionType, elementType);
            if (collectionResult.IsFailure)
                return Result<bool, string>.Failure(collectionResult.Error);

            IList targetCollection = collectionResult.Value;
            foreach (var item in mappedItems)
            {
                targetCollection.Add(item);
            }
            return SetMemberValue(target, targetMember, targetCollection);
        }
        catch (Exception ex)
        {
            return Result<bool, string>.Failure($"Failed to map collection: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handles the mapping of dictionary collections from source to target.
    /// </summary>
    /// <param name="target">The target object where the mapped dictionary will be set.</param>
    /// <param name="targetMember">The member (property or field) on the target object that will receive the dictionary.</param>
    /// <param name="value">The source dictionary to map from.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    /// <remarks>
    /// This method extracts key and value types from the target dictionary type,
    /// creates an appropriate target dictionary instance, converts source dictionary entries
    /// to match the target types, and sets the resulting dictionary on the target member.
    /// </remarks>
    private static Result<bool, string> HandleDictionary(object target, MemberInfo targetMember, object value)
    {
        try
        {
            Type collectionType = GetMemberType(targetMember);
            Type[] genericArgs = collectionType.GetGenericArguments();
            Type keyType = genericArgs[0];
            Type valueType = genericArgs[1];
            IDictionary sourceDictionary = (IDictionary)value;

            var dictionaryResult = TryCreateDictionary(collectionType, keyType, valueType);
            if (dictionaryResult.IsFailure)
                return Result<bool, string>.Failure(dictionaryResult.Error);

            IDictionary targetDictionary = dictionaryResult.Value;
            foreach (DictionaryEntry entry in sourceDictionary)
            {
                var keyConversionResult = TryConvertValue(entry.Key, keyType);
                var valueConversionResult = TryConvertValue(entry.Value, valueType);

                if (keyConversionResult.IsSuccess && valueConversionResult.IsSuccess)
                {
                    targetDictionary.Add(keyConversionResult.Value, valueConversionResult.Value);
                }
            }

            return SetMemberValue(target, targetMember, targetDictionary);
        }
        catch (Exception ex)
        {
            return Result<bool, string>.Failure($"Failed to map dictionary: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Tries to create a dictionary instance of the specified type with the given key and value types.
    /// </summary>
    /// <param name="dictionaryType">The type of dictionary to create.</param>
    /// <param name="keyType">The type of keys in the dictionary.</param>
    /// <param name="valueType">The type of values in the dictionary.</param>
    /// <returns>A Result containing the created dictionary or an error message.</returns>
    private static Result<IDictionary, string> TryCreateDictionary(Type dictionaryType, Type keyType, Type valueType)
    {
        try
        {
            Type dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

            if (dictionaryType.IsInterface && dictionaryType.IsAssignableFrom(dictType))
            {
                return Activator.CreateInstance(dictType) is IDictionary dict
                    ? Result<IDictionary, string>.Success(dict)
                    : Result<IDictionary, string>.Failure($"Failed to create dictionary of type {dictionaryType.Name}");
            }

            if (dictionaryType is { IsClass: true, IsAbstract: false } 
                && dictionaryType.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(dictionaryType) is IDictionary dict
                    ? Result<IDictionary, string>.Success(dict)
                    : Result<IDictionary, string>.Failure($"Failed to create dictionary of type {dictionaryType.Name}");
            }

            return Result<IDictionary, string>.Failure($"Failed to create dictionary of type {dictionaryType.Name}");
        }
        catch (Exception ex)
        {
            return Result<IDictionary, string>.Failure($"Failed to create dictionary of type {dictionaryType.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Tries to create a collection of the specified type and element type.
    /// </summary>
    /// <param name="collectionType">The type of the collection to create.</param>
    /// <param name="elementType">The type of the elements in the collection.</param>
    /// <returns>A Result containing the created collection or an error message.</returns>
    private static Result<IList, string> TryCreateCollection(Type collectionType, Type elementType)
    {
        try
        {
            if (collectionType.IsArray)
            {
                return Result<IList, string>.Success(new ArrayList());
            }

            Type listType = typeof(List<>).MakeGenericType(elementType);

            // Simply use a List<T> for any interface type
            if (collectionType.IsInterface)
            {
                object? instance = Activator.CreateInstance(listType);
                return instance is IList list
                    ? Result<IList, string>.Success(list)
                    : Result<IList, string>.Failure($"Failed to create collection of type {collectionType.Name}");
            }

            // For concrete types with parameterless constructors
            if (collectionType is { IsClass: true, IsAbstract: false } &&
                collectionType.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(collectionType) is IList list
                    ? Result<IList, string>.Success(list)
                    : Result<IList, string>.Failure($"Failed to create collection of type {collectionType.Name}");
            }

            // Fallback to creating a List<T>
            return Activator.CreateInstance(listType) is IList iList
                ? Result<IList, string>.Success(iList)
                : Result<IList, string>.Failure($"Failed to create collection of type {collectionType.Name}");
        }
        catch (Exception ex)
        {
            return Result<IList, string>.Failure($"Failed to create collection of type {collectionType.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Tries to convert a value to the specified target type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <returns>A Result containing the converted value or an error message.</returns>
    private static Result<object, string> TryConvertValue(object value, Type targetType)
    {
        try
        {
            if (targetType.IsEnum 
                && value is string strValue)
            {
                try
                {
                    return Result<object, string>.Success(Enum.Parse(targetType, strValue, true));
                }
                catch
                {
                    return Result<object, string>.Failure($"Cannot convert '{strValue}' to enum {targetType.Name}");
                }
            }

            if (value.GetType() == targetType) 
                return Result<object, string>.Success(value);
            
            try
            {
                object converted = Convert.ChangeType(value, targetType);
                return Result<object, string>.Success(converted);
            }
            catch
            {
                return Result<object, string>.Failure($"Cannot convert value to {targetType.Name}");
            }
        }
        catch (Exception ex)
        {
            return Result<object, string>.Failure($"Conversion error: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines if the type is a complex type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a complex type, otherwise false.</returns>
    private static bool IsComplexType(Type type)
    {
        return !type.IsValueType 
               && type != typeof(string) 
               && !IsCollection(type) 
               && type.IsClass;
    }

    /// <summary>
    /// Determines if the type is a collection.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a collection, otherwise false.</returns>
    private static bool IsCollection(Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type) 
               && type != typeof(string);
    }

    /// <summary>
    /// Gets the element type of collection.
    /// </summary>
    /// <param name="collectionType">The type of the collection.</param>
    /// <returns>The element type of the collection.</returns>
    private Type GetElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType() ?? typeof(object);
        
        if (collectionType.IsGenericType)
        {
            Type genericDef = collectionType.GetGenericTypeDefinition();
        
            // Check common collection interfaces
            if (genericDef == typeof(IEnumerable<>) 
                || genericDef == typeof(ICollection<>) 
                || genericDef == typeof(IList<>))
            {
                return collectionType.GetGenericArguments()[0];
            }
        }
        
        foreach (Type interfaceType in _interfaceMappingCache.GetOrAdd(collectionType, t => t.GetInterfaces()))
        {
            if (interfaceType.IsGenericType 
                && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        return typeof(object);
    }
}