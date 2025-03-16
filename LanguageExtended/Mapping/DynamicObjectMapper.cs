using System.Dynamic;
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
                if (dynamicProps.TryGetValue(targetMember.Name, out var value))
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
                        var conversionResult = _typeConverter.TryConvertValue(value, targetType);
                        if (conversionResult.IsSuccess)
                        {
                            var setResult = MemberAccessor.SetMemberValue(target, targetMember, conversionResult.Value);
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

    private IDictionary<string, object?> GetDynamicProperties(object source)
    {
        if (source is IDictionary<string, object> dict)
            return dict;

        if (source is IDictionary<string, object?> dictNullable)
            return dictNullable;

        var result = new Dictionary<string, object?>();
        if (source is IDynamicMetaObjectProvider)
        {
            var props = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in props)
                result[prop.Name] = prop.GetValue(source);
        }
        return result;
    }
}