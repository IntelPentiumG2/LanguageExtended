// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
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
    private static readonly Lazy<Mapper> DefaultInstance = new (() => new Mapper(new MappingOptions()), LazyThreadSafetyMode.ExecutionAndPublication);
    
    private readonly MemberAccessor _memberAccessor;
    private readonly ComplexTypeMapper _complexTypeMapper;
    private readonly CollectionMapper _collectionMapper;
    private readonly DynamicObjectMapper _dynamicObjectMapper;
    private readonly TypeConverter _typeConverter;

    /// <summary>
    /// Gets the default instance of the Mapper.
    /// </summary>
    public static Mapper Default => DefaultInstance.Value;

    /// <summary>
    /// Initializes a new instance of the Mapper class with the specified options.
    /// </summary>
    /// <param name="options"> The options of the Mapper </param>
    public Mapper(MappingOptions options)
    {
        _options = options;
        _memberAccessor = new MemberAccessor(options.IgnoreCase);
        _complexTypeMapper = new ComplexTypeMapper(this);
        _typeConverter = new TypeConverter(_options.IgnoreCase, _options.Culture);
        _collectionMapper = new CollectionMapper(this, new TypeHelper(), _typeConverter);
        _dynamicObjectMapper = new DynamicObjectMapper(_options, _typeConverter, _memberAccessor);
    }
    
    /// <summary>
    /// Maps the properties and fields from the source object to a new instance of the target type.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target object.</typeparam>
    /// <param name="source">The source object to map from.</param>
    /// <returns>A Result containing the mapped target object or an error message.</returns>
    public Result<TTarget, MappingError> Map<TTarget>(object source) where TTarget : new()
    {
        return MapInternal<TTarget>(source, () => new TTarget());
    }

    /// <summary>
    /// Maps the properties and fields from the source object to a new instance of the target type.
    /// This overload supports types without a parameterless constructor.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target object.</typeparam>
    /// <param name="source">The source object to map from.</param>
    /// <returns>A Result containing the mapped target object or an error message.</returns>
    public Result<TTarget, MappingError> MapWithoutDefaultConstructor<TTarget>(object source)
    {
        return MapInternal<TTarget>(source, () =>
        {
            object? instance = ComplexTypeMapper.CreateInstanceAdvanced(typeof(TTarget));
            if (instance == null)
                throw new InvalidOperationException($"Failed to create instance of {typeof(TTarget).Name}");
            return (TTarget)instance;
        });
    }

    /// <summary>
    /// Internal method that handles the common mapping logic for both Map methods.
    /// </summary>
    private Result<TTarget, MappingError> MapInternal<TTarget>(object source, Func<TTarget> targetFactory)
    {
        if (source == null)
            return Result<TTarget, MappingError>.Failure(new MappingError(
                "Source cannot be null", 
                MappingErrorType.NullReference));

        try
        {
            if (targetFactory() is not { } target)
            {
                return Result<TTarget, MappingError>.Failure(new MappingError(
                    "Failed to create target instance", 
                    MappingErrorType.GeneralMappingError));
            }
            
            Dictionary<object, object> mappingContext = ComplexTypeMapper.CreateMappingContext();
            mappingContext[source] = target;
            
            Result<bool, MappingError> mapResult = Map(source, target, mappingContext);

            return mapResult.IsSuccess
                ? Result<TTarget, MappingError>.Success(target)
                : Result<TTarget, MappingError>.Failure(mapResult.Error);
        }
        catch (Exception ex)
        {
            return Result<TTarget, MappingError>.Failure(new MappingError(
                "Mapping failed.", 
                MappingErrorType.Other,
                "", 
                ex));
        }
    }

    /// <summary>
    /// Maps the properties and fields from the source object to the target object.
    /// </summary>
    /// <param name="source">The source object to map from.</param>
    /// <param name="target">The target object to map to.</param>
    /// <param name="mappingContext">Dictionary to track already mapped objects for circular reference handling.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    internal Result<bool, MappingError> Map([MaybeNull] object source, [MaybeNull] object target, Dictionary<object, object> mappingContext)
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
            if (source is IDynamicMetaObjectProvider)
                return _dynamicObjectMapper.MapDynamicObject(source, target);

            foreach (MemberInfo targetMember in _memberAccessor.GetTargetMembers(target.GetType()))
            {
                Result<bool, MappingError> memberResult = MapSingleMember(source, target, targetMember, mappingContext);
                if (memberResult.IsFailure)
                    return memberResult;
            }

            return Result<bool, MappingError>.Success(true);
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
    /// Maps a single member from source to target.
    /// </summary>
    private Result<bool, MappingError> MapSingleMember(object source, object target, MemberInfo targetMember, Dictionary<object, object> mappingContext)
    {
        Option<MemberInfo> sourceMemberOption = _memberAccessor.FindSourceMember(source.GetType(), targetMember.Name);

        if (sourceMemberOption.IsNone)
        {
            if (!_options.IgnoreUnmappedTargetMembers)
                return Result<bool, MappingError>.Failure(new MappingError(
                    $"Failed to find source member for '{targetMember.Name}'",
                    MappingErrorType.MemberNotFound,
                    targetMember.Name));
            
            return Result<bool, MappingError>.Success(true);
        }

        MemberInfo sourceMember = sourceMemberOption.Value;
        Result<object?, MappingError> sourceValueResult = MemberAccessor.GetMemberValue(source, sourceMember);

        if (!sourceValueResult.IsFailure)
            return SetMappedValue(target, targetMember, sourceValueResult.Value, mappingContext);
        
        if (!_options.IgnoreUnmappedTargetMembers)
            return sourceValueResult.Map(_ => true);

        if (TypeHelper.IsComplexType(MemberAccessor.GetMemberType(targetMember)))
        {
            return ComplexTypeMapper.CreateAndSetComplexType(target, targetMember)
                .MapError(error => new MappingError(
                    $"Failed to initialize '{targetMember.Name}': {error}",
                    MappingErrorType.ComplexTypeMappingError,
                    targetMember.Name));
        }

        return Result<bool, MappingError>.Success(true);
    }
    


    /// <summary>
    /// Sets the mapped source value to the target member.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="source">The source value to map.</param>
    /// <param name="mappingContext">Dictionary to track already mapped objects for circular reference handling.</param>
    private Result<bool, MappingError> SetMappedValue(object target, MemberInfo targetMember, object? source, Dictionary<object, object> mappingContext)
    {
        // Handle null source values
        if (source is null && !_options.CreateEmptyObjectsInsteadOfNull)
            return MemberAccessor.SetMemberValue(target, targetMember, null);
        
        Type targetMemberType = MemberAccessor.GetMemberType(targetMember);
        Type valueType = source.GetType();

        // Handle complex types
        if (TypeHelper.IsComplexType(targetMemberType) && TypeHelper.IsComplexType(valueType))
            return _complexTypeMapper.HandleComplexType(target, targetMember, source, mappingContext);

        // Handle collections
        if (TypeHelper.IsCollection(targetMemberType) && TypeHelper.IsCollection(valueType))
            return _collectionMapper.HandleCollection(target, targetMember, source, mappingContext);

        // Handle primitive type conversion
        Result<object, MappingError> conversionResult = _typeConverter.TryConvertValue(source, targetMemberType);

        if (conversionResult.IsSuccess) 
            return MemberAccessor.SetMemberValue(target, targetMember, conversionResult.Value);
        
        return _options.LenientMappingErrors 
            ? Result<bool, MappingError>.Success(true) 
            : Result<bool, MappingError>.Failure(conversionResult.Error);
    }
}