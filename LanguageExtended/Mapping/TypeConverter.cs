using LanguageExtended.Result;

namespace LanguageExtended.Mapping;

/// <summary>
/// Provides methods to convert values to different types.
/// </summary>
internal class TypeConverter
{
    /// <summary>
    /// Tries to convert a value to the specified target type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <returns>A Result containing the converted value or an error message.</returns>
    internal static Result<object, MappingError> TryConvertValue(object value, Type targetType)
    {
        try
        {
            if (targetType.IsEnum 
                && value is string strValue)
            {
                try
                {
                    //TODO: Implement ingnore case
                    return Result<object, MappingError>.Success(Enum.Parse(targetType, strValue, true));
                }
                catch (Exception ex)
                {
                    return Result<object, MappingError>.Failure(new MappingError(
                        $"Cannot convert '{strValue}' to enum {targetType.Name}",
                        MappingErrorType.EnumConversionError,
                        targetType.Name,
                        ex));
                }
            }

            if (value.GetType() == targetType) 
                return Result<object, MappingError>.Success(value);
            
            try
            {
                object converted = Convert.ChangeType(value, targetType);
                return Result<object, MappingError>.Success(converted);
            }
            catch (Exception ex)
            {
                return Result<object, MappingError>.Failure(new MappingError(
                    $"Cannot convert value to {targetType.Name}",
                    MappingErrorType.ConversionError,
                    targetType.Name,
                    ex));
            }
        }
        catch (Exception ex)
        {
            return Result<object, MappingError>.Failure(new MappingError(
                $"Conversion error: {ex.Message}",
                MappingErrorType.ConversionError,
                targetType.Name,
                ex));
        }
    }
}