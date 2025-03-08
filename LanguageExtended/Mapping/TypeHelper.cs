using System.Collections;
using System.Collections.Concurrent;
// ReSharper disable UnusedMember.Local

namespace LanguageExtended.Mapping;

// ReSharper disable once UnusedType.Global
/// <summary>
/// Provides helper methods for working with types.
/// </summary>
internal class TypeHelper
{
    private readonly ConcurrentDictionary<Type,Type[]> _interfaceMappingCache = new();

    /// <summary>
    /// Determines if the type is a complex type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a complex type, otherwise false.</returns>
    internal static bool IsComplexType(Type type)
    {
        return !type.IsValueType 
               && type != typeof(string) 
               && !IsCollection(type) 
               && type.IsClass;
    }
    
    /// <summary>
    /// Determines if the type is a collection.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a collection, otherwise false.</returns>
    internal static bool IsCollection(Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type) 
               && type != typeof(string);
    }
    
    /// <summary>
    /// Gets the element type of collection.
    /// </summary>
    /// <param name="collectionType">The type of the collection.</param>
    /// <returns>The element type of the collection.</returns>
    internal Type GetElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType() ?? typeof(object);
        
        if (collectionType.IsGenericType)
        {
            Type genericDef = collectionType.GetGenericTypeDefinition();
        
            // Check common collection interfaces
            if (genericDef == typeof(IEnumerable<>) 
                || genericDef == typeof(ICollection<>) 
                || genericDef == typeof(IList<>))
            {
                return collectionType.GetGenericArguments()[0];
            }
        }
        
        foreach (Type interfaceType in _interfaceMappingCache.GetOrAdd(collectionType, t => t.GetInterfaces()))
        {
            if (interfaceType.IsGenericType 
                && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        return typeof(object);
    }
}