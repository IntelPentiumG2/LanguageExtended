namespace LanguageExtended.Mapping;
/// <summary>
/// Thrown by <see cref="Mapper"/> when a mapping fails and <see cref="MappingOptions.ThrowOnMappingFailure"/> is set.
/// </summary>
/// <param name="error">The MappingError that occured</param>
public class MappingException(MappingError error) : Exception(error.Message, error.Exception)
{
    /// <summary>
    /// The MappingError that occured 
    /// </summary>
    public MappingError MappingError { get; private set; } = error;
}