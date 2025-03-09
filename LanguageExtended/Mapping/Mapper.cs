// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
using System.Reflection;
using LanguageExtended.Result;
using LanguageExtended.Option;

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
    // private readonly ConcurrentDictionary<(Type sourceType, Type targetType), Dictionary<string, MemberInfo>> _memberMappingCache;

    private static readonly Lazy<Mapper> DefaultInstance = new (() => new Mapper(new MappingOptions()), LazyThreadSafetyMode.ExecutionAndPublication);
    
    private readonly MemberAccessor _memberAccessor;
    private readonly ComplexTypeMapper _complexTypeMapper;
    private readonly CollectionMapper _collectionMapper;

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
        // _memberMappingCache =
        //     new ConcurrentDictionary<(Type sourceType, Type targetType), Dictionary<string, MemberInfo>>();
        
        _options = options;
        _memberAccessor = new MemberAccessor(options.IgnoreCase);
        _complexTypeMapper = new ComplexTypeMapper(this);
        _collectionMapper = new CollectionMapper(this, new TypeHelper());
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
            return Result<TTarget, MappingError>.Failure(new MappingError(
                "Source cannot be null", 
                MappingErrorType.NullReference));

        try
        {
            TTarget target = new TTarget();
            Result<bool, MappingError> mapResult = Map(source, target);

            return mapResult.IsSuccess
                ? Result<TTarget, MappingError>.Success(target)
                : Result<TTarget, MappingError>.Failure(mapResult.Error);
        }
        catch (Exception ex)
        {
            return Result<TTarget, MappingError>.Failure(new MappingError(
                "Mapping failed.", 
                MappingErrorType.Other ,
                "", 
                ex));
        }
    }

    /// <summary>
    /// Maps the properties and fields from the source object to the target object.
    /// </summary>
    /// <param name="source">The source object to map from.</param>
    /// <param name="target">The target object to map to.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    internal Result<bool, MappingError> Map(object source, object target)
    {
        if (source == null)
            return Result<bool, MappingError>.Failure(new MappingError(
                "Source cannot be null",
                MappingErrorType.NullReference));

        if (target == null)
            return Result<bool, MappingError>.Failure(new MappingError(
                "Target cannot be null",
                MappingErrorType.NullReference));

        try
        {
            Type sourceType = source.GetType();
            Type targetType = target.GetType();

            foreach (MemberInfo targetMember in _memberAccessor.GetTargetMembers(targetType))
            {
                Option<MemberInfo> sourceMemberOption = _memberAccessor.FindSourceMember(sourceType, targetMember.Name);

                sourceMemberOption.IfSome(sourceMember =>
                {
                    Option<object> valueOption = MemberAccessor.GetMemberValue(source, sourceMember);
                    valueOption.Match(
                        value =>
                        {
                            var result = SetMappedValue(target, targetMember, value);
                            // Only treat enum conversion errors as critical
                            if (result is { IsFailure: true, Error.ErrorType: MappingErrorType.EnumConversionError })
                            {
                                throw new MappingException(result.Error);
                            }
                        },
                        () =>
                        {
                            if (!TypeHelper.IsComplexType(MemberAccessor.GetMemberType(targetMember)))
                                return;

                            Result<bool, MappingError> result = ComplexTypeMapper.CreateAndSetComplexType(target, targetMember);
                            if (result.IsFailure)
                                throw new MappingException(new MappingError(
                                    $"Failed to initialize '{targetMember.Name}': {result.Error}",
                                    MappingErrorType.ComplexTypeMappingError,
                                    targetMember.Name));
                        }
                    );
                });
            }

            return Result<bool, MappingError>.Success(true);
        }
        catch (MappingException ex)
        {
            return Result<bool, MappingError>.Failure(ex.MappingError);
        }
        catch (Exception ex)
        {
            return Result<bool, MappingError>.Failure(new MappingError(
                $"Mapping failed: {ex.Message}",
                MappingErrorType.GeneralMappingError,
                "",
                ex));
        }
    }
    


    /// <summary>
    /// Sets the mapped value to the target member.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="value">The value to set.</param>
    private Result<bool, MappingError> SetMappedValue(object target, MemberInfo targetMember, object value)
    {
        Type targetMemberType = MemberAccessor.GetMemberType(targetMember);
        Type valueType = value.GetType();

        if (TypeHelper.IsComplexType(targetMemberType) && TypeHelper.IsComplexType(valueType))
        {
            // return _complexTypeMapper.HandleComplexType(target, targetMember, value);
            
            Result<bool, MappingError> result = _complexTypeMapper.HandleComplexType(target, targetMember, value);
            
            return result.IsSuccess 
                ? Result<bool, MappingError>.Success(true) 
                : Result<bool, MappingError>.Failure(result.Error);
        }

        if (TypeHelper.IsCollection(targetMemberType) && TypeHelper.IsCollection(valueType))
        {
            return _collectionMapper.HandleCollection(target, targetMember, value);
        }

        Result<object, MappingError> conversionResult = TypeConverter.TryConvertValue(value, targetMemberType);

        if (conversionResult.IsSuccess) 
            return MemberAccessor.SetMemberValue(target, targetMember, conversionResult.Value);
        
        // Special handling for enum conversion errors
        if (targetMemberType.IsEnum && value is string)
        {
            // Enum conversion failures should cause mapping failure
            return Result<bool, MappingError>.Failure(conversionResult.Error);
        }
        
        // Ignore other conversion errors
        return Result<bool, MappingError>.Success(true);
    }
}