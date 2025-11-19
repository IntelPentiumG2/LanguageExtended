using System.Collections;
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
    private readonly TypeHelper _typeHelper;
    private readonly TypeConverter _typeConverter;
    private readonly Mapper _mapper;

    /// <summary>
    /// Initializes a new instance of the CollectionMapper class.
    /// </summary>
    /// <param name="typeHelper">The Helper</param>
    /// <param name="mapper">The Mapper</param>
    /// <param name="typeConverter">The <see cref="TypeConverter"/> to use for conversions</param>
    internal CollectionMapper(Mapper mapper, TypeHelper typeHelper, TypeConverter typeConverter)
    {
        _typeHelper = typeHelper;
        _mapper = mapper;
        _typeConverter = typeConverter;
    }
    
        /// <summary>
    /// Handles the mapping of collections.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="value">The value to map.</param>
    /// <param name="mappingContext">Dictionary to track already mapped objects for circular reference handling.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    internal Result<bool, MappingError> HandleCollection(object target, MemberInfo targetMember, object value, Dictionary<object, object> mappingContext)
    {
        try
        {
            Type collectionType = MemberAccessor.GetMemberType(targetMember);
            Type elementType = _typeHelper.GetElementType(collectionType);
            IEnumerable sourceCollection = (IEnumerable)value;
            
            if (typeof(IDictionary).IsAssignableFrom(collectionType))
            {
                return HandleDictionary(target, targetMember, value);
            }
            
            List<object?> mappedItems = [];

            foreach (object? item in sourceCollection)
            {
                if (item == null)
                {
                    mappedItems.Add(null);
                    continue;
                }

                if (TypeHelper.IsComplexType(elementType))
                {
                    object? nestedTarget = Activator.CreateInstance(elementType);

                    if (nestedTarget == null) 
                        continue;
                    
                    // Map the item to the nested target
                    _mapper.Map(item, nestedTarget, mappingContext);
                    mappedItems.Add(nestedTarget);
                }
                else
                {
                    var conversionResult = _typeConverter.TryConvertValue(item, elementType);
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
                return MemberAccessor.SetMemberValue(target, targetMember, array);
            }

            Result<IList, MappingError> collectionResult = TryCreateCollection(collectionType, elementType);
            if (collectionResult.IsFailure)
                return Result<bool, MappingError>.Failure(collectionResult.Error);

            IList targetCollection = collectionResult.Value;
            foreach (var item in mappedItems)
            {
                targetCollection.Add(item);
            }
            return MemberAccessor.SetMemberValue(target, targetMember, targetCollection);
        }
        catch (Exception ex)
        {
            return Result<bool, MappingError>.Failure(new MappingError(
                $"Failed to map collection: {ex.Message}",
                MappingErrorType.CollectionMappingError,
                targetMember.Name,
                ex));
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
    private Result<bool, MappingError> HandleDictionary(object target, MemberInfo targetMember, object value)
    {
        try
        {
            Type collectionType = MemberAccessor.GetMemberType(targetMember);
            Type[] genericArgs = collectionType.GetGenericArguments();
            Type keyType = genericArgs[0];
            Type valueType = genericArgs[1];
            IDictionary sourceDictionary = (IDictionary)value;

            var dictionaryResult = TryCreateDictionary(collectionType, keyType, valueType);
            if (dictionaryResult.IsFailure)
                return Result<bool, MappingError>.Failure(dictionaryResult.Error);

            IDictionary targetDictionary = dictionaryResult.Value;
            foreach (DictionaryEntry entry in sourceDictionary)
            {
                Result<object, MappingError> keyConversionResult = _typeConverter.TryConvertValue(entry.Key, keyType);
                Result<object, MappingError> valueConversionResult = _typeConverter.TryConvertValue(entry.Value, valueType);

                if (keyConversionResult.IsSuccess && valueConversionResult.IsSuccess)
                {
                    targetDictionary.Add(keyConversionResult.Value, valueConversionResult.Value);
                }
            }

            return MemberAccessor.SetMemberValue(target, targetMember, targetDictionary);
        }
        catch (Exception ex)
        {
            return Result<bool, MappingError>.Failure(new MappingError(
                $"Failed to map dictionary: {ex.Message}",
                MappingErrorType.CollectionMappingError,
                targetMember.Name,
                ex));
        }
    }
            
                /// <summary>
    /// Tries to create a dictionary instance of the specified type with the given key and value types.
    /// </summary>
    /// <param name="dictionaryType">The type of dictionary to create.</param>
    /// <param name="keyType">The type of keys in the dictionary.</param>
    /// <param name="valueType">The type of values in the dictionary.</param>
    /// <returns>A Result containing the created dictionary or an error message.</returns>
    private static Result<IDictionary, MappingError> TryCreateDictionary(Type dictionaryType, Type keyType, Type valueType)
    {
        try
        {
            Type dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

            if (dictionaryType.IsInterface && dictionaryType.IsAssignableFrom(dictType))
            {
                return Activator.CreateInstance(dictType) is IDictionary dict
                    ? Result<IDictionary, MappingError>.Success(dict)
                    : Result<IDictionary, MappingError>.Failure(new MappingError(
                        $"Failed to create dictionary of type {dictionaryType.Name}",
                        MappingErrorType.DictionaryCreationError,
                        dictionaryType.Name));
            }

            if (dictionaryType is { IsClass: true, IsAbstract: false } 
                && dictionaryType.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(dictionaryType) is IDictionary dict
                    ? Result<IDictionary, MappingError>.Success(dict)
                    : Result<IDictionary, MappingError>.Failure(new MappingError(
                        $"Failed to create dictionary of type {dictionaryType.Name}",
                        MappingErrorType.DictionaryCreationError,
                        dictionaryType.Name));
            }

            return Result<IDictionary, MappingError>.Failure(new MappingError(
                $"Failed to create dictionary of type {dictionaryType.Name}",
                MappingErrorType.DictionaryCreationError,
                dictionaryType.Name));
        }
        catch (Exception ex)
        {
            return Result<IDictionary, MappingError>.Failure(new MappingError(
                $"Failed to create dictionary of type {dictionaryType.Name}: {ex.Message}",
                MappingErrorType.DictionaryCreationError,
                dictionaryType.Name,
                ex));
        }
    }

    /// <summary>
    /// Tries to create a collection of the specified type and element type.
    /// </summary>
    /// <param name="collectionType">The type of the collection to create.</param>
    /// <param name="elementType">The type of the elements in the collection.</param>
    /// <returns>A Result containing the created collection or an error message.</returns>
    private static Result<IList, MappingError> TryCreateCollection(Type collectionType, Type elementType)
    {
        try
        {
            if (collectionType.IsArray)
            {
                return Result<IList, MappingError>.Success(new ArrayList());
            }

            Type listType = typeof(List<>).MakeGenericType(elementType);

            // Simply use a List<T> for any interface type
            if (collectionType.IsInterface)
            {
                object? instance = Activator.CreateInstance(listType);
                return instance is IList list
                    ? Result<IList, MappingError>.Success(list)
                    : Result<IList, MappingError>.Failure(new MappingError(
                        $"Failed to create collection of type {collectionType.Name}",
                        MappingErrorType.CollectionCreationError,
                        collectionType.Name));
            }

            // For concrete types with parameterless constructors
            if (collectionType is { IsClass: true, IsAbstract: false } &&
                collectionType.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(collectionType) is IList list
                    ? Result<IList, MappingError>.Success(list)
                    : Result<IList, MappingError>.Failure(new MappingError(
                        $"Failed to create collection of type {collectionType.Name}",
                        MappingErrorType.CollectionCreationError,
                        collectionType.Name));
            }

            // Fallback to creating a List<T>
            return Activator.CreateInstance(listType) is IList iList
                ? Result<IList, MappingError>.Success(iList)
                : Result<IList, MappingError>.Failure(new MappingError(
                    $"Failed to create collection of type {collectionType.Name}",
                    MappingErrorType.CollectionCreationError,
                    collectionType.Name));
        }
        catch (Exception ex)
        {
            return Result<IList, MappingError>.Failure(new MappingError(
                $"Failed to create collection of type {collectionType.Name}: {ex.Message}",
                MappingErrorType.CollectionCreationError,
                collectionType.Name,
                ex));
        }
    }
}