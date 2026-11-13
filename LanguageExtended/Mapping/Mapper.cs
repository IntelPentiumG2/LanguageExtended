// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

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
        _collectionMapper = new CollectionMapper(_options, _complexTypeMapper, new TypeHelper(), _typeConverter);
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
    /// <exception cref="MappingException">The mapping failed and <see cref="MappingOptions.ThrowOnMappingFailure"/> is set.</exception>
    private Result<TTarget, MappingError> MapInternal<TTarget>(object source, Func<TTarget> targetFactory)
    {
        // Thrown here, outside TryMap's catch, so the exception is not turned back into a Result.
        Result<TTarget, MappingError> result = TryMap(source, targetFactory);
        if (result.IsFailure && _options.ThrowOnMappingFailure)
            throw new MappingException(result.Error);
        return result;
    }

    private Result<TTarget, MappingError> TryMap<TTarget>(object source, Func<TTarget> targetFactory)
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
            
            // Box once so a struct target is mapped in place and the mapped copy is returned.
            object boxed = target;
            MappingContext mappingContext = new() { [(source, typeof(TTarget))] = boxed };

            Result<bool, MappingError> mapResult = Map(source, boxed, mappingContext);

            return mapResult.IsSuccess
                ? Result<TTarget, MappingError>.Success((TTarget)boxed)
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
    /// <param name="mappingContext">Tracks already mapped objects for circular reference handling.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    internal Result<bool, MappingError> Map(object source, object target, MappingContext mappingContext)
    {
        try
        {
            // ExpandoObject-style sources hold their members in a dictionary; other dynamic objects
            // (e.g. DynamicObject subclasses) expose public properties and use the regular member lookup.
            // A case-insensitive copy throws on keys differing only in case, failing the mapping instead of picking one.
            IDictionary<string, object?>? dynamicValues = source is IDynamicMetaObjectProvider and IDictionary<string, object?> values
                ? _options.IgnoreCase ? new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase) : values
                : null;

            foreach (MemberInfo targetMember in _memberAccessor.GetTargetMembers(target.GetType()))
            {
                Result<bool, MappingError> memberResult = MapSingleMember(source, dynamicValues, target, targetMember, mappingContext);
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
    private Result<bool, MappingError> MapSingleMember(object source, IDictionary<string, object?>? dynamicValues, object target, MemberInfo targetMember, MappingContext mappingContext)
    {
        if (!TryGetSourceValue(source, dynamicValues, targetMember.Name, out Result<object?, MappingError> sourceValueResult))
        {
            if (!_options.IgnoreUnmappedTargetMembers)
                return Result<bool, MappingError>.Failure(new MappingError(
                    $"Failed to find source member for '{targetMember.Name}'",
                    MappingErrorType.MemberNotFound,
                    targetMember.Name));
            
            return Result<bool, MappingError>.Success(true);
        }

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
    /// Looks up the source value for a target member, by key for dictionary-backed dynamic sources and by member otherwise.
    /// </summary>
    /// <returns>false if the source has no such member; otherwise true, with the value or the error reading it.</returns>
    private bool TryGetSourceValue(object source, IDictionary<string, object?>? dynamicValues, string name, out Result<object?, MappingError> value)
    {
        if (dynamicValues != null)
        {
            bool found = dynamicValues.TryGetValue(name, out object? dynamicValue);
            value = Result<object?, MappingError>.Success(dynamicValue);
            return found;
        }

        Option<MemberInfo> member = _memberAccessor.FindSourceMember(source.GetType(), name);
        value = member.TryGetValue(out MemberInfo? sourceMember)
            ? MemberAccessor.GetMemberValue(source, sourceMember)
            : default;
        return member.IsSome;
    }

    /// <summary>
    /// Sets the mapped source value to the target member.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="source">The source value to map.</param>
    /// <param name="mappingContext">Tracks already mapped objects for circular reference handling.</param>
    private Result<bool, MappingError> SetMappedValue(object target, MemberInfo targetMember, object? source, MappingContext mappingContext)
    {
        Type targetMemberType = MemberAccessor.GetMemberType(targetMember);
#if NET11_0_OR_GREATER
        // Unwrap Option/Result/any C# 15 union to its case value, unless the target member takes the union itself.
        if (source is System.Runtime.CompilerServices.IUnion union && !targetMemberType.IsInstanceOfType(source))
        {
            // A failed Result holds an error, not data.
            if (source is IResult { IsFailure: true })
                return _options.LenientMappingErrors
                    ? Result<bool, MappingError>.Success(true)
                    : Result<bool, MappingError>.Failure(new MappingError(
                        $"Source for '{targetMember.Name}' is a failed Result",
                        MappingErrorType.ConversionError,
                        targetMember.Name));
            source = union.Value;
        }
#endif

        if (source is null)
            return _options.CreateEmptyObjectsInsteadOfNull && TypeHelper.IsComplexType(targetMemberType)
                ? ComplexTypeMapper.CreateAndSetComplexType(target, targetMember)
                : MemberAccessor.SetMemberValue(target, targetMember, null);

        Type valueType = source.GetType();

        // Structs the target takes as-is (e.g. Option/ValueOption) are copied directly.
        if (valueType.IsValueType && targetMemberType.IsInstanceOfType(source))
            return MemberAccessor.SetMemberValue(target, targetMember, source);

        // Handle complex types
        if (TypeHelper.IsComplexType(targetMemberType) && TypeHelper.IsComplexSource(valueType))
            return _complexTypeMapper.HandleComplexType(target, targetMember, source, mappingContext);

        // Handle collections
        if (TypeHelper.IsCollection(targetMemberType) && TypeHelper.IsCollection(valueType))
            return _collectionMapper.HandleCollection(target, targetMember, source, mappingContext);

        // Handle primitive type conversion
        Result<object?, MappingError> conversionResult = _typeConverter.TryConvertValue(source, targetMemberType);

        if (conversionResult.IsSuccess) 
            return MemberAccessor.SetMemberValue(target, targetMember, conversionResult.Value);
        
        return _options.LenientMappingErrors 
            ? Result<bool, MappingError>.Success(true) 
            : Result<bool, MappingError>.Failure(conversionResult.Error);
    }
}