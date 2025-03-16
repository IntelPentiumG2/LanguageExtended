using System.Globalization;
using LanguageExtended.Result;

namespace LanguageExtended.Mapping;

/// <summary>
/// Provides methods to convert values to different types.
/// </summary>
internal class TypeConverter(bool ignoreCase = false, CultureInfo? culture = null)
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
                if (Enum.TryParse(targetType, strValue, ignoreCase, out object? parsedEnum))
                    return Result<object, MappingError>.Success(parsedEnum);
                
                return Result<object, MappingError>.Failure(new MappingError(
                        $"Cannot convert '{strValue}' to enum {targetType.Name}",
                        MappingErrorType.EnumConversionError,
                        targetType.Name));
            }

            if (value is string stringValue)
            {
                switch (targetType)
                {
                    case { } t when t == typeof(decimal) 
                                    || t == typeof(decimal?):
                        return Result<object, MappingError>.Success(decimal.Parse(stringValue, culture));
                    
                    case { } t when t == typeof(double) 
                                    || t == typeof(double?):
                        return Result<object, MappingError>.Success(double.Parse(stringValue, culture));
                    
                    case { } t when t == typeof(float) 
                                    || t == typeof(float?):
                        return Result<object, MappingError>.Success(float.Parse(stringValue, culture));
                    
                    case { } t when t == typeof(int) 
                                    || t == typeof(int?):
                        return Result<object, MappingError>.Success(int.Parse(stringValue, culture));

                    case { } t when t == typeof(long) 
                                    || t == typeof(long?):
                        return Result<object, MappingError>.Success(long.Parse(stringValue, culture));
                    
                    
                    
                    case { } t when t == typeof(DateTime) 
                                    || t == typeof(DateTime?):
                        return Result<object, MappingError>.Success(DateTime.Parse(stringValue, culture));
                    
                    case { } t when t == typeof(DateOnly) 
                                    || t == typeof(DateOnly?):
                        return Result<object, MappingError>.Success(DateOnly.Parse(stringValue, culture));

                    case { } t when t == typeof(TimeOnly) 
                                    || t == typeof(TimeOnly?):
                        return Result<object, MappingError>.Success(TimeOnly.Parse(stringValue, culture));
                    
                    case { } t when t == typeof(DateTimeOffset) 
                                    || t == typeof(DateTimeOffset?):
                        return Result<object, MappingError>.Success(DateTimeOffset.Parse(stringValue, culture));

                    case { } t when t == typeof(TimeSpan) 
                                    || t == typeof(TimeSpan?):
                        return Result<object, MappingError>.Success(TimeSpan.Parse(stringValue, culture));
                    
                    case { } t when t == typeof(Guid) 
                                    || t == typeof(Guid?):
                        return Result<object, MappingError>.Success(Guid.Parse(stringValue));
                }
            }
            
            if (targetType == typeof(string) 
                && value is IFormattable formattableValue)
                return Result<object, MappingError>.Success(formattableValue.ToString(null, culture));
            
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