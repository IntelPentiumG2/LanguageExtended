// ReSharper disable UnusedType.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace LanguageExtended.Mapping;
    
    /// <summary>
    /// Represents an error that occurred during the mapping process.
    /// </summary>
    public class MappingError
    {
        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }
    
        /// <summary>
        /// Gets the path of the property where the error occurred.
        /// </summary>
        public string PropertyPath { get; }
    
        /// <summary>
        /// Gets the exception that caused the error, if any.
        /// </summary>
        public Exception? Exception { get; }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingError"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyPath">The path of the property where the error occurred.</param>
        /// <param name="exception">The exception that caused the error, if any.</param>
        public MappingError(string message, string propertyPath = "", Exception? exception = null)
        {
            Message = message;
            PropertyPath = propertyPath;
            Exception = exception;
        }
    }