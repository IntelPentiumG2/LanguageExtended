using System.Reflection;
using LanguageExtended.Result;

namespace LanguageExtended.Mapping;

internal class DynamicObjectMapper
{
    private readonly MappingOptions _options;
    private readonly TypeConverter _typeConverter;
    private readonly MemberAccessor _memberAccessor;

    internal DynamicObjectMapper(MappingOptions options, TypeConverter typeConverter, MemberAccessor memberAccessor)
    {
        _options = options;
        _typeConverter = typeConverter;
        _memberAccessor = memberAccessor;
    }

    internal Result<bool, MappingError> MapDynamicObject(object source, object target)
    {
        try
        {
            IDictionary<string, object?> dynamicProps = GetDynamicProperties(source);
            MemberInfo[] targetMembers = _memberAccessor.GetTargetMembers(target.GetType()).ToArray();

            foreach (MemberInfo targetMember in targetMembers)
            {
                if (dynamicProps.TryGetValue(targetMember.Name, out object? value))
                {
                    // If the source value is null, assign null
                    if (value == null)
                    {
                        Result<bool, MappingError> setNullResult = MemberAccessor.SetMemberValue(target, targetMember, null);
                        if (setNullResult.IsFailure && !_options.LenientMappingErrors)
                            return Result<bool, MappingError>.Failure(setNullResult.Error);
                    }
                    else
                    {
                        Type targetType = MemberAccessor.GetMemberType(targetMember);
                        Result<object?, MappingError> conversionResult = _typeConverter.TryConvertValue(value, targetType);
                        if (conversionResult.IsSuccess)
                        {
                            Result<bool, MappingError> setResult = MemberAccessor.SetMemberValue(target, targetMember, conversionResult.Value);
                            if (setResult.IsFailure && !_options.LenientMappingErrors)
                                return Result<bool, MappingError>.Failure(setResult.Error);
                        }
                        else if (!_options.LenientMappingErrors)
                        {
                            return Result<bool, MappingError>.Failure(conversionResult.Error);
                        }
                    }
                }
                else if (!_options.IgnoreUnmappedTargetMembers)
                {
                    return Result<bool, MappingError>.Failure(new MappingError(
                        $"Source member for '{targetMember.Name}' not found",
                        MappingErrorType.MemberNotFound,
                        targetMember.Name));
                }
            }
            return Result<bool, MappingError>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool, MappingError>.Failure(new MappingError(
                $"Dynamic object mapping failed: {ex.Message}",
                MappingErrorType.GeneralMappingError,
                string.Empty,
                ex));
        }
    }

    private static IDictionary<string, object?> GetDynamicProperties(object source)
    {
        // Nullability is not part of the runtime type, so this also matches IDictionary<string, object> (e.g. ExpandoObject).
        if (source is IDictionary<string, object?> dict)
            return dict;

        // Other dynamic objects (e.g. DynamicObject subclasses): read their public properties.
        Dictionary<string, object?> result = new Dictionary<string, object?>();
        foreach (PropertyInfo prop in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            result[prop.Name] = prop.GetValue(source);
        return result;
    }
}