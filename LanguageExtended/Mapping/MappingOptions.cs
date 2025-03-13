// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
namespace LanguageExtended.Mapping;

/// <summary>
/// Provides configuration options for the mapping process.
/// </summary>
public record MappingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to ignore case when matching member names.
    /// </summary>
    public bool IgnoreCase { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore missing members in the target type.
    /// </summary>
    public bool IgnoreMissingSourceMembers { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether to ignore unmapped members in the target type.
    /// </summary>
    public bool IgnoreUnmappedTargetMembers { get; set; } = false;
    
    /// <summary>
    /// Gets or sets a value indicating whether to ignore failed mappings.
    /// </summary>
    public bool LenientMappingErrors { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to throw an exception on mapping failure.
    /// </summary>
    public bool ThrowOnMappingFailure { get; set; } = false;
    
    /// <summary>
    /// Gets or sets a value indicating whether to create empty objects instead of null if the source value is null.
    /// </summary>
    public bool CreateEmptyObjectsInsteadOfNull { get; set; } = false;
}