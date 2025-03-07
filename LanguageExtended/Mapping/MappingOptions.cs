// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
namespace LanguageExtended.Mapping;

/// <summary>
/// Provides configuration options for the mapping process.
/// </summary>
public class MappingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to ignore case when matching member names.
    /// </summary>
    public bool IgnoreCase { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore missing members in the target type.
    /// </summary>
    public bool IgnoreMissingMembers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to throw an exception on mapping failure.
    /// </summary>
    public bool ThrowOnMappingFailure { get; set; } = false;
}