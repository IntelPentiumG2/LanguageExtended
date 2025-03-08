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
    internal static Result<object, string> TryConvertValue(object value, Type targetType)
    {
        try
        {
            if (targetType.IsEnum 
                && value is string strValue)
            {
                try
                {
                    return Result<object, string>.Success(Enum.Parse(targetType, strValue, true));
                }
                catch
                {
                    return Result<object, string>.Failure($"Cannot convert '{strValue}' to enum {targetType.Name}");
                }
            }

            if (value.GetType() == targetType) 
                return Result<object, string>.Success(value);
            
            try
            {
                object converted = Convert.ChangeType(value, targetType);
                return Result<object, string>.Success(converted);
            }
            catch
            {
                return Result<object, string>.Failure($"Cannot convert value to {targetType.Name}");
            }
        }
        catch (Exception ex)
        {
            return Result<object, string>.Failure($"Conversion error: {ex.Message}");
        }
    }
}