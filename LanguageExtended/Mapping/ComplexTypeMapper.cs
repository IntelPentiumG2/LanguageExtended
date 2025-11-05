using System.Reflection;
using System.Runtime.CompilerServices;
using LanguageExtended.Result;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global

namespace LanguageExtended.Mapping;

internal class ReferenceEqualityComparer : IEqualityComparer<object>
{
    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
    public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
}

/// <summary>
/// Provides methods for mapping complex types.
/// </summary>
internal class ComplexTypeMapper
{
    private readonly Mapper _mapper;
    private readonly Dictionary<object , object> _alreadyMappedObjects;

    /// <summary>
    /// Initializes a new instance of the ComplexTypeMapper class.
    /// </summary>
    /// <param name="mapper">The mapper</param>
    /// <param name="createEmptyObjectsInsteadOfNull">If empty objects should be created instead of null values if source is null</param>
    internal ComplexTypeMapper(Mapper mapper)
    {
        _mapper = mapper;
        _alreadyMappedObjects = new Dictionary<object, object>(new ReferenceEqualityComparer());
    }
    
    internal void Reset()
    {
        _alreadyMappedObjects.Clear();
    }
    
    /// <summary>
    /// Handles the mapping of complex types.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="targetMember">The target member to set the value to.</param>
    /// <param name="value">The value to map.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    internal Result<bool, MappingError> HandleComplexType(object target, MemberInfo targetMember, object value)
    {
        try
        {
            Type targetType = MemberAccessor.GetMemberType(targetMember);

            // Check if we've already mapped this object to prevent circular reference issues
            // TODO: Fix circular reference issue, currently the results arent reference equal but value equal instead.
            if (_alreadyMappedObjects.TryGetValue(value, out var existingTarget))
                return MemberAccessor.SetMemberValue(target, targetMember, existingTarget);
            
            try
            {
                // Create a new instance of the complex type
                object? nestedTarget = CreateInstanceAdvanced(targetType);
                
                if (nestedTarget == null)
                    return Result<bool, MappingError>.Failure(new MappingError(
                        $"Failed to create instance of {targetType.Name}",
                        MappingErrorType.ComplexTypeMappingError,
                        targetType.Name));

                // Add the new instance to the mapped objects dictionary BEFORE mapping properties
                _alreadyMappedObjects[value] = nestedTarget;

                // Set the new instance on the target object and check result
                var setResult = MemberAccessor.SetMemberValue(target, targetMember, nestedTarget);
                return setResult.IsFailure 
                    ? Result<bool, MappingError>.Failure(setResult.Error) 
                    : _mapper.Map(value, nestedTarget);  // Now map properties from source to the nested target
            }
            catch (Exception ex)
            {
                return Result<bool, MappingError>.Failure(new MappingError(
                    $"Failed to create nested object: {ex.Message}",
                    MappingErrorType.ComplexTypeMappingError,
                    targetType.Name,
                    ex));
            }
        }
        catch (Exception ex)
        {
            return Result<bool, MappingError>.Failure(new MappingError(
                $"Failed to map complex type: {ex.Message}",
                MappingErrorType.ComplexTypeMappingError,
                targetMember.Name,
                ex));
        }
    }
    
    /// <summary>
    /// Creates an instance of the specified type using various strategies.
    /// Tries in order: parameterless constructor, constructor with default values, FormatterServices.
    /// </summary>
    /// <param name="type">The type to instantiate.</param>
    /// <returns>An instance of the type, or null if creation failed.</returns>
    internal static object? CreateInstanceAdvanced(Type type)
    {
        try
        {
            // Strategy 1: Try parameterless constructor (fastest)
            if (type.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(type);
            }

            // Strategy 2: Try to find a constructor and use default values for parameters
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(c => c.GetParameters().Length)
                .ToArray();

            foreach (var constructor in constructors)
            {
                try
                {
                    var parameters = constructor.GetParameters();
                    var parameterValues = new object?[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        
                        // Use default value if available
                        if (param.HasDefaultValue)
                        {
                            parameterValues[i] = param.DefaultValue;
                        }
                        // Use default for value types
                        else if (param.ParameterType.IsValueType)
                        {
                            parameterValues[i] = Activator.CreateInstance(param.ParameterType);
                        }
                        // Use null for nullable reference types or if nulls are allowed
                        else if (!param.ParameterType.IsValueType)
                        {
                            parameterValues[i] = null;
                        }
                        else
                        {
                            // Can't create this parameter, try next constructor
                            break;
                        }
                    }

                    // Try to create instance with these parameters
                    return constructor.Invoke(parameterValues);
                }
                catch
                {
                    // This constructor didn't work, try the next one
                    continue;
                }
            }

            // Strategy 3: Use FormatterServices to create uninitialized object (last resort)
            // This works even without any constructor but should be used carefully
            if (!type.IsAbstract && !type.IsInterface)
            {
                return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            }

            return null;
        }
        catch
        {
            return null;
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
            object? instance = CreateInstanceAdvanced(targetType);
            
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