using System.Runtime.CompilerServices;

namespace LanguageExtended.Mapping;

/// <summary>
/// Tracks the target instance already created for each (source object, target type) pair during one mapping,
/// so shared and circular references map to a single instance. Keyed by type as well, because the same
/// source object may be mapped into members of different target types.
/// </summary>
internal sealed class MappingContext() : Dictionary<(object Source, Type Target), object>(KeyComparer.Instance)
{
    private sealed class KeyComparer : IEqualityComparer<(object Source, Type Target)>
    {
        internal static readonly KeyComparer Instance = new();

        public bool Equals((object Source, Type Target) x, (object Source, Type Target) y) =>
            ReferenceEquals(x.Source, y.Source) && x.Target == y.Target;

        public int GetHashCode((object Source, Type Target) key) =>
            HashCode.Combine(RuntimeHelpers.GetHashCode(key.Source), key.Target);
    }
}
