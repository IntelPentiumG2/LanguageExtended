using System.Globalization;

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
    /// Has no effect: source members without a matching target member are always ignored.
    /// </summary>
    [Obsolete("Has no effect: source members without a matching target member are always ignored. Will be removed in the next major version.")]
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
    /// Gets or sets a value indicating whether <see cref="Mapper.Map{TTarget}"/> and
    /// <see cref="Mapper.MapWithoutDefaultConstructor{TTarget}"/> throw a <see cref="MappingException"/>
    /// instead of returning a failed result.
    /// </summary>
    public bool ThrowOnMappingFailure { get; set; } = false;
    
    /// <summary>
    /// Gets or sets a value indicating whether to create empty objects instead of null if the source value is null.
    /// </summary>
    public bool CreateEmptyObjectsInsteadOfNull { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the culture to use for parsing and formatting values.
    /// </summary>
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
}