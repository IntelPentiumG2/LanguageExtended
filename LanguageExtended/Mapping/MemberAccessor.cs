// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
using System.Collections.Concurrent;
using System.Reflection;
using LanguageExtended.Option;
using LanguageExtended.Result;

namespace LanguageExtended.Mapping;

/// <summary>
/// Provides a way to access members (properties and fields) of objects.
/// </summary>
internal class MemberAccessor
{
    private readonly StringComparison _stringComparison;
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties;
    private readonly ConcurrentDictionary<Type, FieldInfo[]> _fields;
    private readonly ConcurrentDictionary<(Type, string), Option<MemberInfo>> _members;

    /// <summary>
    /// Constructor to create a new MemberAccessor object.
    /// </summary>
    internal MemberAccessor(bool ignoreCase)
    {
        _stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        _properties = new ConcurrentDictionary<Type, PropertyInfo[]>();
        _fields = new ConcurrentDictionary<Type, FieldInfo[]>();
        _members = new ConcurrentDictionary<(Type, string), Option<MemberInfo>>();
    }
    
    /// <summary>
    /// Gets the writable properties and non-readonly fields of the target type.
    /// </summary>
    /// <param name="targetType">The type of the target object.</param>
    /// <returns>An IEnumerable of MemberInfo representing the writable properties and fields.</returns>
    internal IEnumerable<MemberInfo> GetTargetMembers(Type targetType)
    {
        // Get writable properties; indexers need arguments, so they are not mappable members
        foreach (PropertyInfo prop in _properties.GetOrAdd(targetType, targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)))
        {
            if (prop.CanWrite && prop.GetIndexParameters().Length == 0)
                yield return prop;
        }

        // Get non-readonly fields
        foreach (FieldInfo field in _fields.GetOrAdd(targetType, targetType.GetFields(BindingFlags.Public | BindingFlags.Instance)))
        {
            if (!field.IsInitOnly)
                yield return field;
        }
    }
    
    /// <summary>
    /// Finds a member (property or field) in the source type by name.
    /// </summary>
    /// <param name="sourceType">The type of the source object.</param>
    /// <param name="memberName">The name of the member to find.</param>
    /// <returns>An Option containing the found MemberInfo or None if not found.</returns>
    internal Option<MemberInfo> FindSourceMember(Type sourceType, string memberName)
    {
        return _members.GetOrAdd((sourceType, memberName), _ =>
        {
            PropertyInfo[] props = _properties.GetOrAdd(sourceType, t => 
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        
            PropertyInfo? prop = props.FirstOrDefault(p => p.Name.Equals(memberName, _stringComparison) && p.GetIndexParameters().Length == 0);
        
            if (prop != null && prop.CanRead) 
                return Option<MemberInfo>.Some(prop);
            
            FieldInfo[] fields = _fields.GetOrAdd(sourceType, t => 
                t.GetFields(BindingFlags.Public | BindingFlags.Instance));
        
            FieldInfo? field = fields.FirstOrDefault(f => f.Name.Equals(memberName, _stringComparison));
        
            return field != null 
                ? Option<MemberInfo>.Some(field) 
                : Option<MemberInfo>.None();
        });
    }
    
    /// <summary>
    /// Gets the type of member (property or field).
    /// </summary>
    /// <param name="member">The member to get the type of.</param>
    /// <returns>The type of the member.</returns>
    /// <remarks>Members only come from <see cref="GetTargetMembers"/> and <see cref="FindSourceMember"/>, so they are properties or fields.</remarks>
    internal static Type GetMemberType(MemberInfo member) =>
        member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
    
    /// <summary>
    /// Gets the value of a member (property or field) from an object.
    /// </summary>
    /// <param name="obj">The object to get the value from.</param>
    /// <param name="member">The member to get the value of.</param>
    /// <returns>An Option containing the value of the member or None if not found.</returns>
    internal static Result<object?, MappingError> GetMemberValue(object obj, MemberInfo member)
    {
        try
        {
            object? value = member is PropertyInfo property ? property.GetValue(obj) : ((FieldInfo)member).GetValue(obj);
            return Result<object?, MappingError>.Success(value);
        }
        catch (Exception ex)
        {
            return Result<object?, MappingError>.Failure(new MappingError(
                $"Failed to get value for {member.Name}: {ex.Message}",
                MappingErrorType.InvalidMemberType,
                member.Name,
                ex));
        }
    }
    
    /// <summary>
    /// Sets the value of a member (property or field) on an object.
    /// </summary>
    /// <param name="target">The target object to set the value on.</param>
    /// <param name="member">The member to set the value to.</param>
    /// <param name="value">The value to set.</param>
    internal static Result<bool, MappingError> SetMemberValue(object target, MemberInfo member, object? value)
    {
        try
        {
            if (member is PropertyInfo property)
                property.SetValue(target, value);
            else
                ((FieldInfo)member).SetValue(target, value);
            return Result<bool, MappingError>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool, MappingError>.Failure(new MappingError(
                $"Failed to set value for {member.Name}: {ex.Message}",
                MappingErrorType.SetMemberValueError,
                member.Name,
                ex));
        }
    }
}