using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using LanguageExtended.Result;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global

namespace LanguageExtended.Mapping;

/// <summary>
/// Provides methods for mapping complex types.
/// </summary>
[RequiresUnreferencedCode(Mapper.RequiresUnreferencedCodeMessage)]
[RequiresDynamicCode(Mapper.RequiresDynamicCodeMessage)]
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
    /// <param name="mappingContext">Tracks already mapped objects for circular reference handling.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    internal Result<bool, MappingError> HandleComplexType(object target, MemberInfo targetMember, object value, MappingContext mappingContext) =>
        MapNested(value, MemberAccessor.GetMemberType(targetMember), mappingContext)
            .Bind(nested => MemberAccessor.SetMemberValue(target, targetMember, nested));

    /// <summary>
    /// Maps a source object into a new instance of <paramref name="targetType"/>, reusing the instance already
    /// created for the same source and target type so shared and circular references are preserved.
    /// </summary>
    /// <param name="value">The source object.</param>
    /// <param name="targetType">The type to map into.</param>
    /// <param name="mappingContext">Tracks already mapped objects for circular reference handling.</param>
    /// <returns>The mapped instance, or the error that stopped the mapping.</returns>
    internal Result<object, MappingError> MapNested(object value, Type targetType, MappingContext mappingContext)
    {
        if (mappingContext.TryGetValue((value, targetType), out object? existing))
            return Result<object, MappingError>.Success(existing);

        if (CreateInstanceAdvanced(targetType) is not { } nested)
            return Result<object, MappingError>.Failure(new MappingError(
                $"Failed to create instance of {targetType.Name}",
                MappingErrorType.ComplexTypeMappingError,
                targetType.Name));

        // Register before mapping members so a reference back to this source resolves to this instance.
        mappingContext[(value, targetType)] = nested;
        return _mapper.Map(value, nested, mappingContext).Map(_ => nested);
    }

    /// <summary>
    /// Creates an instance of the specified type using various strategies.
    /// Tries in order: parameterless constructor, constructor with default values, uninitialized object.
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
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(c => c.GetParameters().Length)
                .ToArray();

            foreach (ConstructorInfo constructor in constructors)
            {
                try
                {
                    // Declared default if any, else default(T) for value types and null for reference types.
                    object?[] parameterValues = constructor.GetParameters()
                        .Select(p => p.HasDefaultValue ? p.DefaultValue
                            : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)
                            : null)
                        .ToArray();

                    return constructor.Invoke(parameterValues);
                }
                catch
                {
                    // This constructor didn't work, try the next one
                }
            }

            // Strategy 3: Create an uninitialized object (last resort).
            // This works even without any usable constructor but skips constructor logic.
            return type is { IsAbstract: false, IsInterface: false } ? RuntimeHelpers.GetUninitializedObject(type) : null;
        }
        catch
        {
            // A throwing parameterless constructor ends up here.
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
        return CreateInstanceAdvanced(targetType) is { } instance
            ? MemberAccessor.SetMemberValue(target, targetMember, instance)
            : Result<bool, MappingError>.Failure(new MappingError(
                $"Failed to create instance of {targetType.Name}",
                MappingErrorType.ComplexTypeMappingError,
                targetType.Name));
    }
}
