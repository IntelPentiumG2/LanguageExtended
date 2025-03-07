using System.Collections;
using System.Reflection;
using LanguageExtended.Option;
using LanguageExtended.Result;

namespace LanguageExtended.Mapping;

public static class Mapper
{
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

    private static Option<MemberInfo> FindSourceMember(Type sourceType, string memberName)
    {
        // Check for property first
        var prop = sourceType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.CanRead) return Option<MemberInfo>.Some(prop);

        // Then check for field
        var field = sourceType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        return field != null ? Option<MemberInfo>.Some(field) : Option<MemberInfo>.None();
    }

    private static Type GetMemberType(MemberInfo member)
    {
        return member.MemberType switch
        {
            MemberTypes.Property => ((PropertyInfo)member).PropertyType,
            MemberTypes.Field => ((FieldInfo)member).FieldType,
            _ => throw new ArgumentException("Member must be a property or field", nameof(member))
        };
    }

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

    private static bool CanMapTypes(Type targetType, Type sourceType)
    {
        // Check direct assignability
        if (targetType.IsAssignableFrom(sourceType)) return true;

        // Check if we can convert between primitive types
        return (targetType.IsPrimitive || targetType == typeof(string) || targetType.IsEnum) &&
               (sourceType.IsPrimitive || sourceType == typeof(string) || sourceType.IsEnum);
    }

    private static bool IsComplexType(Type type)
    {
        return !type.IsValueType &&
               type != typeof(string) &&
               !IsCollection(type) &&
               type.IsClass;
    }

    private static bool IsCollection(Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type) &&
               type != typeof(string);
    }

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