namespace LanguageExtended.Mapping;

public class MappingOptions
{
    public bool IgnoreCase { get; set; } = false;
    public bool IgnoreMissingMembers { get; set; } = true;
    public bool ThrowOnMappingFailure { get; set; } = false;
}