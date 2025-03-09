namespace LanguageExtended.Mapping;
/// <summary>
/// A Exception for Mapping errors
/// </summary>
/// <param name="error">The MappingError that occured</param>
public class MappingException(MappingError error) : Exception
{
    /// <summary>
    /// The MappingError that occured 
    /// </summary>
    public MappingError MappingError { get; private set; } = error;
}