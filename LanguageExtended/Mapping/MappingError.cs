namespace LanguageExtended.Mapping;

public class MappingError
{
    public string Message { get; }
    public string PropertyPath { get; }
    public Exception? Exception { get; }
    
    public MappingError(string message, string propertyPath = "", Exception? exception = null)
    {
        Message = message;
        PropertyPath = propertyPath;
        Exception = exception;
    }
}