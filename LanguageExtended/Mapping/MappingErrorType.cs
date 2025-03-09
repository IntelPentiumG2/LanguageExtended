// ReSharper disable UnusedMember.Global
namespace LanguageExtended.Mapping;

/// <summary>
/// Categorizes the type of mapping error that occurred
/// </summary>
public enum MappingErrorType
{
    /// <summary>
    /// The error occurred during the conversion of a value.
    /// </summary>
    ConversionError,
    /// <summary>
    /// The error occurred because a property was missing.
    /// </summary>
    MissingProperty,
    /// <summary>
    /// The error occurred because the type of the source and target properties did not match.
    /// </summary>
    TypeMismatch,
    /// <summary>
    /// The error occurred because a collection could not be mapped.
    /// </summary>
    CollectionMappingError,
    /// <summary>
    /// The error occurred because a complex type could not be mapped.
    /// </summary>
    ComplexTypeMappingError,
    /// <summary>
    /// The error occurred because a null reference was encountered.
    /// </summary>
    NullReference,
    /// <summary>
    /// The error occurred because of a general mapping error.
    /// </summary>
    GeneralMappingError,
    /// <summary>
    /// The error occurred because of an unknown reason.
    /// </summary>
    Other
}