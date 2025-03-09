using System.Reflection;
using LanguageExtended.Result;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global

namespace LanguageExtended.Mapping;

/// <summary>
/// Provides methods for mapping complex types.
/// </summary>
internal class ComplexTypeMapper
{
    private readonly Mapper _mapper;

    /// <summary>
    /// Initializes a new instance of the ComplexTypeMapper class.
    /// </summary>
    /// <param name="mapper">The mapper</param>
    internal ComplexTypeMapper(Mapper mapper)
    {
        _mapper = mapper;
    }
    
    /// <summary>
    /// Handles the mapping of complex types.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="value">The value to map.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    internal Result<bool, MappingError[]> HandleComplexType(object target, MemberInfo targetMember, object value)
    {
        try
        {
            Type targetType = MemberAccessor.GetMemberType(targetMember);

            // Create a new instance of the complex type
            object nestedTarget;
            try
            {
                nestedTarget = Activator.CreateInstance(targetType)
                               ?? throw new InvalidOperationException($"Failed to create instance of {targetType}");

                // Set the new instance on the target object and check result
                var setResult = MemberAccessor.SetMemberValue(target, targetMember, nestedTarget);
                if (setResult.IsFailure)
                    return Result<bool, MappingError[]>.Failure([setResult.Error]);
            }
            catch (Exception ex)
            {
                return Result<bool, MappingError[]>.Failure([new MappingError(
                    $"Failed to create nested object: {ex.Message}",
                    MappingErrorType.ComplexTypeMappingError,
                    targetType.Name,
                    ex)]);
            }

            // Now map properties from source to the nested target
            return _mapper.Map(value, nestedTarget);
        }
        catch (Exception ex)
        {
            return Result<bool, MappingError[]>.Failure([new MappingError(
                $"Failed to map complex type: {ex.Message}",
                MappingErrorType.ComplexTypeMappingError,
                targetMember.Name,
                ex)]);
        }
    }
    
/// <summary>
    /// Creates a new instance of a complex type and sets it to the specified member of the target object.
    /// </summary>
    /// <param name="target">The target object on which to set the new instance.</param>
    /// <param name="targetMember">The member (property or field) to set with the new instance.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    internal static Result<bool, MappingError> CreateAndSetComplexType(object target, MemberInfo targetMember)
    {
        Type targetType = MemberAccessor.GetMemberType(targetMember);
        try
        {
            object? instance = Activator.CreateInstance(targetType);
            
            if (instance == null)
                return Result<bool, MappingError>.Failure(new MappingError(
                    $"Failed to create instance of {targetType.Name}",
                    MappingErrorType.ComplexTypeMappingError,
                    targetType.Name));
            
            var setResult = MemberAccessor.SetMemberValue(target, targetMember, instance);
            return setResult;
        }
        catch (Exception ex)
        {
            return Result<bool, MappingError>.Failure(new MappingError(
                $"Failed to create instance of {targetType.Name}: {ex.Message}",
                MappingErrorType.ComplexTypeMappingError,
                targetType.Name,
                ex));
        }
    }
}