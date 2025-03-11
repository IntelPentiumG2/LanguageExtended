using System.Globalization;
using LanguageExtended.Result;

namespace LanguageExtended.Mapping;

/// <summary>
/// Provides methods to convert values to different types.
/// </summary>
internal class TypeConverter(bool ignoreCase = false)
{
    /// <summary>
    /// Tries to convert a value to the specified target type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <returns>A Result containing the converted value or an error message.</returns>
    internal Result<object, MappingError> TryConvertValue(object value, Type targetType)
    {
        try
        {
            if (targetType.IsEnum 
                && value is string strValue)
            {
                try
                {
                    return Result<object, MappingError>.Success(Enum.Parse(targetType, strValue, ignoreCase));
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

            if (targetType == typeof(string)
                && value is IConvertible convertibleValue)
            {
                return Result<object, MappingError>.Success(convertibleValue.ToString(CultureInfo.InvariantCulture));
            }
            
            object converted = Convert.ChangeType(value, targetType);
            return Result<object, MappingError>.Success(converted);
            
        }
        catch (Exception ex)
        {
            return Result<object, MappingError>.Failure(new MappingError(
                $"Failed to convert {targetType.Name} to {targetType.Name}",
                MappingErrorType.ConversionError,
                targetType.Name,
                ex));
        }
    }
}