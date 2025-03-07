// ReSharper disable UnusedMember.Global
using System.Collections;
using System.Reflection;
using LanguageExtended.Option;
using LanguageExtended.Result;

namespace LanguageExtended.Mapping;

/// <summary>
/// Provides static methods for mapping properties and fields between objects.
/// This utility class supports mapping of primitive types, complex objects, and collections.
/// </summary>
/// <remarks>
/// The mapper automatically handles:
/// - Primitive type conversions
/// - Enum conversions
/// - Complex type mappings
/// - Collection mappings
/// </remarks>
public static class Mapper
{
    /// <summary>
    /// Maps the properties and fields from the source object to a new instance of the target type.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target object.</typeparam>
    /// <param name="source">The source object to map from.</param>
    /// <returns>A Result containing the mapped target object or an error message.</returns>
    public static Result<TTarget, string> Map<TTarget>(object source) where TTarget : new()
    {
        if (source == null)
            return Result<TTarget, string>.Failure("Source cannot be null");

        try
        {
            var target = new TTarget();
            var mapResult = Map(source, target);

            return mapResult.IsSuccess
                ? Result<TTarget, string>.Success(target)
                : Result<TTarget, string>.Failure(mapResult.Error);
        }
        catch (Exception ex)
        {
            return Result<TTarget, string>.Failure($"Mapping failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps the properties and fields from the source object to the target object.
    /// </summary>
    /// <param name="source">The source object to map from.</param>
    /// <param name="target">The target object to map to.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    private static Result<bool, string> Map(object source, object target)
    {
        if (source == null)
            return Result<bool, string>.Failure("Source cannot be null");

        if (target == null)
            return Result<bool, string>.Failure("Target cannot be null");

        try
        {
            Type sourceType = source.GetType();
            Type targetType = target.GetType();

            foreach (var targetMember in GetTargetMembers(targetType))
            {
                var sourceMemberOption = FindSourceMember(sourceType, targetMember.Name);

                sourceMemberOption.Match(
                    sourceMember =>
                    {
                        Type sourceMemberType = GetMemberType(sourceMember);
                        Type targetMemberType = GetMemberType(targetMember);

                        if (!CanMapTypes(targetMemberType, sourceMemberType)) return;

                        var valueOption = GetMemberValue(source, sourceMember);
                        valueOption.Match(
                            value => SetMappedValue(target, targetMember, value),
                            () => { } // Do nothing if no value
                        );
                    },
                    () => { } // Do nothing if no matching source member
                );
            }

            return Result<bool, string>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool, string>.Failure($"Mapping failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the writable properties and non-readonly fields of the target type.
    /// </summary>
    /// <param name="targetType">The type of the target object.</param>
    /// <returns>An enumerable of MemberInfo representing the writable properties and fields.</returns>
    private static IEnumerable<MemberInfo> GetTargetMembers(Type targetType)
    {
        // Get writable properties
        foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanWrite)
                yield return prop;
        }

        // Get non-readonly fields
        foreach (var field in targetType.GetFields(BindingFlags.Public | BindingFlags.Instance))
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
    private static Option<MemberInfo> FindSourceMember(Type sourceType, string memberName)
    {
        // Check for property first
        var prop = sourceType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.CanRead) return Option<MemberInfo>.Some(prop);

        // Then check for field
        var field = sourceType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        return field != null ? Option<MemberInfo>.Some(field) : Option<MemberInfo>.None();
    }

    /// <summary>
    /// Gets the type of a member (property or field).
    /// </summary>
    /// <param name="member">The member to get the type of.</param>
    /// <returns>The type of the member.</returns>
    private static Type GetMemberType(MemberInfo member)
    {
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
    private static void SetMappedValue(object target, MemberInfo targetMember, object value)
    {
        Type targetMemberType = GetMemberType(targetMember);
        Type valueType = value.GetType();

        if (IsComplexType(targetMemberType) && IsComplexType(valueType))
        {
            HandleComplexType(target, targetMember, value)
                .IfFailure(error => { /* Could log error here */ });
        }
        else if (IsCollection(targetMemberType) && IsCollection(valueType))
        {
            HandleCollection(target, targetMember, value)
                .IfFailure(error => { /* Could log error here */ });
        }
        else
        {
            TryConvertValue(value, targetMemberType)
                .Match(
                    convertedValue => SetMemberValue(target, targetMember, convertedValue),
                    _ => SetMemberValue(target, targetMember, value)
                );
        }
    }

    /// <summary>
    /// Sets the value of a member (property or field) on an object.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="member">The member to set the value to.</param>
    /// <param name="value">The value to set.</param>
    private static void SetMemberValue(object target, MemberInfo member, object value)
    {
        try
        {
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    ((PropertyInfo)member).SetValue(target, value);
                    break;
                case MemberTypes.Field:
                    ((FieldInfo)member).SetValue(target, value);
                    break;
                default:
                    throw new ArgumentException("Member must be a property or field", nameof(member));
            }
        }
        catch (Exception)
        {
            // Silently fail - could return Result in future
        }
    }

    /// <summary>
    /// Handles the mapping of complex types.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="value">The value to map.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    private static Result<bool, string> HandleComplexType(object target, MemberInfo targetMember, object value)
    {
        try
        {
            Type targetType = GetMemberType(targetMember);

            object? nestedTarget = Activator.CreateInstance(targetType);

            if (nestedTarget == null)
                return Result<bool, string>.Failure($"Failed to create instance of {targetType.Name}");

            var mapResult = Map(value, nestedTarget);
            if (mapResult.IsFailure)
                return mapResult;

            SetMemberValue(target, targetMember, nestedTarget);
            return Result<bool, string>.Success(true);
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
    private static Result<bool, string> HandleCollection(object target, MemberInfo targetMember, object value)
    {
        try
        {
            Type collectionType = GetMemberType(targetMember);
            Type elementType = GetElementType(collectionType);
            IEnumerable sourceCollection = (IEnumerable)value;

            var collectionResult = TryCreateCollection(collectionType, elementType);
            if (collectionResult.IsFailure)
                return Result<bool, string>.Failure(collectionResult.Error);

            IList targetCollection = collectionResult.Value;

            foreach (var item in sourceCollection)
            {
                if (item == null) continue;

                object mappedItem = item;
                if (IsComplexType(elementType))
                {
                    object? nestedTarget = Activator.CreateInstance(elementType);
                    if (nestedTarget != null)
                    {
                        Map(item, nestedTarget);
                        mappedItem = nestedTarget;
                    }
                }

                targetCollection.Add(mappedItem);
            }

            if (collectionType.IsArray)
            {
                Array array = Array.CreateInstance(elementType, targetCollection.Count);
                targetCollection.CopyTo(array, 0);
                SetMemberValue(target, targetMember, array);
            }
            else
            {
                SetMemberValue(target, targetMember, targetCollection);
            }

            return Result<bool, string>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool, string>.Failure($"Failed to map collection: {ex.Message}");
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

            if (collectionType.IsInterface &&
                (collectionType.IsAssignableFrom(listType) || collectionType == typeof(IEnumerable) || collectionType == typeof(ICollection)))
            {
                return Activator.CreateInstance(listType) is IList list
                    ? Result<IList, string>.Success(list)
                    : Result<IList, string>.Failure($"Failed to create collection of type {collectionType.Name}");
            }

            if (collectionType is { IsClass: true, IsAbstract: false } &&
                collectionType.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(collectionType) is IList list
                    ? Result<IList, string>.Success(list)
                    : Result<IList, string>.Failure($"Failed to create collection of type {collectionType.Name}");
            }

            return Activator.CreateInstance(listType) is not IList ilist
                ? Result<IList, string>.Failure($"Failed to create collection of type {collectionType.Name}")
                : Result<IList, string>.Success(ilist);
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
            if (targetType.IsEnum && value is string strValue)
            {
                return Result<object, string>.Success(Enum.Parse(targetType, strValue, true));
            }

            if (value.GetType() != targetType && Convert.ChangeType(value, targetType) is { } converted)
            {
                return Result<object, string>.Success(converted);
            }

            return Result<object, string>.Success(value);
        }
        catch (Exception ex)
        {
            return Result<object, string>.Failure($"Conversion error: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines if the target type can be mapped from the source type.
    /// </summary>
    /// <param name="targetType">The target type.</param>
    /// <param name="sourceType">The source type.</param>
    /// <returns>True if the types can be mapped, otherwise false.</returns>
    private static bool CanMapTypes(Type targetType, Type sourceType)
    {
        // Check direct assignability
        if (targetType.IsAssignableFrom(sourceType)) return true;

        // Check if we can convert between primitive types
        return (targetType.IsPrimitive || targetType == typeof(string) || targetType.IsEnum) &&
               (sourceType.IsPrimitive || sourceType == typeof(string) || sourceType.IsEnum);
    }

    /// <summary>
    /// Determines if the type is a complex type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a complex type, otherwise false.</returns>
    private static bool IsComplexType(Type type)
    {
        return !type.IsValueType &&
               type != typeof(string) &&
               !IsCollection(type) &&
               type.IsClass;
    }

    /// <summary>
    /// Determines if the type is a collection.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a collection, otherwise false.</returns>
    private static bool IsCollection(Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type) &&
               type != typeof(string);
    }

    /// <summary>
    /// Gets the element type of a collection.
    /// </summary>
    /// <param name="collectionType">The type of the collection.</param>
    /// <returns>The element type of the collection.</returns>
    private static Type GetElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType() ?? typeof(object);

        foreach (Type interfaceType in collectionType.GetInterfaces())
        {
            if (interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        return typeof(object);
    }
}