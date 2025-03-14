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
        _memberAccessor = new MemberAccessor(options.IgnoreCase);
        _complexTypeMapper = new ComplexTypeMapper(this);
        _typeConverter = new TypeConverter(_options.IgnoreCase);
        _collectionMapper = new CollectionMapper(this, new TypeHelper(), _typeConverter);
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
            _complexTypeMapper.Reset();
            
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
            int mapCount = 0;
            MemberInfo[] targetMembers = [ .. _memberAccessor.GetTargetMembers(target.GetType()) ];
            
            foreach (MemberInfo targetMember in targetMembers)
            {
                Option<MemberInfo> sourceMemberOption = _memberAccessor.FindSourceMember(source.GetType(), targetMember.Name);

                sourceMemberOption.IfSome(sourceMember =>
                {
                    Result<object?, MappingError> valueOption = MemberAccessor.GetMemberValue(source, sourceMember);
                    valueOption.Match(
                        value =>
                        {
                            Result<bool, MappingError> result = SetMappedValue(target, targetMember, value);

                            if (result.IsFailure)
                                throw new MappingException(result.Error);
                            
                            mapCount++;
                        },
                        _ =>
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

            if (mapCount == targetMembers.Length
                || _options.IgnoreUnmappedTargetMembers)
                return Result<bool, MappingError>.Success(true);
            
            return Result<bool, MappingError>.Failure(new MappingError(
                "Mapping failed: not all members were mapped",
                MappingErrorType.GeneralMappingError));
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
    /// Sets the mapped source to the target member.
    /// </summary>
    /// <param name="target">The target object to set the source on.</param>
    /// <param name="targetMember">The target member to set the source to.</param>
    /// <param name="source">The source object to set the target to.</param>
    private Result<bool, MappingError> SetMappedValue(object target, MemberInfo targetMember, object? source)
    {
        Type targetMemberType = MemberAccessor.GetMemberType(targetMember);

        if (source is null
            && !_options.CreateEmptyObjectsInsteadOfNull)
        {
            return MemberAccessor.SetMemberValue(target, targetMember, null);
        }
        
        Type valueType = source.GetType();
        
        //TODO: Add support for dynamic types like ExpandoObject

        if (TypeHelper.IsComplexType(targetMemberType) && TypeHelper.IsComplexType(valueType))
            return _complexTypeMapper.HandleComplexType(target, targetMember, source);

        if (TypeHelper.IsCollection(targetMemberType) && TypeHelper.IsCollection(valueType))
            return _collectionMapper.HandleCollection(target, targetMember, source);

        Result<object, MappingError> conversionResult = _typeConverter.TryConvertValue(source, targetMemberType);

        if (conversionResult.IsSuccess) 
            return MemberAccessor.SetMemberValue(target, targetMember, conversionResult.Value);
        
        return _options.LenientMappingErrors 
            ? Result<bool, MappingError>.Success(true) 
            : Result<bool, MappingError>.Failure(conversionResult.Error);
    }
}