using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using LanguageExtended.Mapping;

namespace MappingTest;

public class MapperCollectionTests
{
    private static readonly Mapper Strict = Mapper.Default;
    private static readonly Mapper Lenient = new(new MappingOptions { LenientMappingErrors = true });

    public record Rec(int Id);
    public class Dto { public int V { get; set; } }
    public class Entity { public int V { get; set; } }
    public class Strict2 { public string Name { get; set; } = ""; public int Age { get; set; } }

    public struct Point { public int X { get; set; } public int Y { get; set; } }

    [Fact]
    public void StructTarget_IsMapped()
    {
        Point p = Strict.Map<Point>(new { X = 1, Y = 2 }).Value;

        Assert.Equal((1, 2), (p.X, p.Y));
    }

    [Fact]
    public void NullableStructTarget_ReturnsFailure()
    {
        // new int?() is null, so there is no instance to map into.
        Assert.Equal(MappingErrorType.GeneralMappingError, Strict.Map<int?>(new { }).Error.ErrorType);
    }

    public class Node
    {
        public string Name { get; set; } = "";
        public List<Node> Children { get; set; } = [];
    }

    [Fact]
    public void CycleThroughCollection_MapsToSameInstance()
    {
        // A regression here overflows the stack and kills the test host instead of failing this test.
        Node root = new() { Name = "root" };
        Node child = new() { Name = "child", Children = [root] };
        root.Children.Add(child);
        root.Children.Add(root);

        Node mapped = Strict.Map<Node>(root).Value;

        Assert.NotSame(root, mapped);
        Assert.Same(mapped, mapped.Children[1]);
        Assert.Same(mapped, mapped.Children[0].Children[0]);
    }

    public class RecHolder { public List<Rec> Items { get; set; } }

    [Fact]
    public void ElementsWithoutParameterlessCtor_AreMapped()
    {
        RecHolder t = Strict.Map<RecHolder>(new RecHolder { Items = [new Rec(1), new Rec(2)] }).Value;

        Assert.Equal([new Rec(1), new Rec(2)], t.Items);
    }

    public class DtoArrayHolder { public Dto[] Items { get; set; } }
    public class EntityArrayHolder { public Entity[] Items { get; set; } }

    [Fact]
    public void ArrayOfComplexElements_IsDeepMapped()
    {
        Dto shared = new() { V = 3 };
        EntityArrayHolder t = Strict.Map<EntityArrayHolder>(new DtoArrayHolder { Items = [shared, shared] }).Value;

        Assert.Equal(3, t.Items[0].V);
        Assert.Same(t.Items[0], t.Items[1]);
    }

    public class StrictHolder { public List<Strict2> Items { get; set; } }

    [Fact]
    public void ElementMappingError_FailsTheMapping()
    {
        var source = new { Items = new[] { new { Name = "n" } } };

        Assert.Equal(MappingErrorType.MemberNotFound, Strict.Map<StrictHolder>(source).Error.ErrorType);
    }

    public abstract class Shape { public int Size { get; set; } }
    public class Square : Shape;
    public class ShapeListHolder { public List<Shape> Items { get; set; } }

    [Fact]
    public void AbstractElementType_FailsUnlessLenient()
    {
        var source = new { Items = new List<Square> { new() { Size = 1 } } };

        Assert.Equal(MappingErrorType.ComplexTypeMappingError, Strict.Map<ShapeListHolder>(source).Error.ErrorType);
        Assert.Empty(Lenient.Map<ShapeListHolder>(source).Value.Items);
    }

    public class NestedListHolder { public List<List<int>> Items { get; set; } }

    [Fact]
    public void NestedCollections_AreConvertedRecursively()
    {
        NestedListHolder t = Strict.Map<NestedListHolder>(new { Items = new List<string[]> { new[] { "1", "2" }, new[] { "3" } } }).Value;

        Assert.Equal([[1, 2], [3]], t.Items);
    }

    public class Sets
    {
        public HashSet<int> Hash { get; set; }
        public ISet<int> ISet { get; set; }
        public IReadOnlySet<int> ReadOnlySet { get; set; }
        public SortedSet<int> Sorted { get; set; }
        public LinkedList<int> Linked { get; set; }
        public ObservableCollection<int> Observable { get; set; }
    }

    [Fact]
    public void SetsAndAddableCollections_AreMapped()
    {
        List<int> items = [3, 1, 2];
        Sets t = Strict.Map<Sets>(new { Hash = items, ISet = items, ReadOnlySet = items, Sorted = items, Linked = items, Observable = items }).Value;

        Assert.Equal([1, 2, 3], t.Hash.Order());
        Assert.IsType<HashSet<int>>(t.ISet);
        Assert.Equal(3, t.ReadOnlySet.Count);
        Assert.Equal([1, 2, 3], t.Sorted);
        Assert.Equal([3, 1, 2], t.Linked);
        Assert.Equal([3, 1, 2], t.Observable);
    }

    public class Sequences
    {
        public Queue<int> Queue { get; set; }
        public Stack<int> Stack { get; set; }
        public ConcurrentStack<int> ConcurrentStack { get; set; }
        public Stack Untyped { get; set; }
    }

    [Fact]
    public void QueuesAndStacks_KeepTheirOrder()
    {
        Queue<int> queue = new([1, 2, 3]);
        Stack<int> stack = new([1, 2, 3]);
        Sequences t = Strict.Map<Sequences>(new
        {
            Queue = queue,
            Stack = stack,
            ConcurrentStack = new ConcurrentStack<int>([1, 2, 3]),
            Untyped = new Stack(new[] { 1, 2, 3 }),
        }).Value;

        Assert.Equal(queue, t.Queue);
        Assert.Equal(stack, t.Stack);
        Assert.Equal([3, 2, 1], t.ConcurrentStack);
        Assert.Equal(new object[] { 3, 2, 1 }, t.Untyped.Cast<object>());
    }

    public class ImmutableHolder { public ImmutableList<int> Items { get; set; } }

    [Fact]
    public void UnsupportedCollectionType_ReturnsFailure()
    {
        var result = Strict.Map<ImmutableHolder>(new { Items = new List<int> { 1 } });

        Assert.Equal(MappingErrorType.CollectionCreationError, result.Error.ErrorType);
    }

    public class ThrowingList : List<int>
    {
        public ThrowingList() => throw new InvalidOperationException("ctor");
    }

    public class ThrowingListHolder { public ThrowingList Items { get; set; } }

    [Fact]
    public void ThrowingCollectionCtor_ReturnsFailure()
    {
        var result = Strict.Map<ThrowingListHolder>(new { Items = new List<int> { 1 } });

        Assert.Equal(MappingErrorType.CollectionMappingError, result.Error.ErrorType);
    }

    public class EntityDictHolder { public Dictionary<string, Entity> Map { get; set; } }

    [Fact]
    public void DictionaryValues_AreDeepMapped()
    {
        EntityDictHolder t = Strict.Map<EntityDictHolder>(new { Map = new Dictionary<string, Dto> { ["a"] = new() { V = 1 } } }).Value;

        Assert.Equal(1, t.Map["a"].V);
    }

    public class Dictionaries
    {
        public SortedDictionary<string, int> Sorted { get; set; }
        public ConcurrentDictionary<string, int> Concurrent { get; set; }
    }

    [Fact]
    public void AddableDictionaries_AreMapped()
    {
        Dictionary<string, int> source = new() { ["b"] = 2, ["a"] = 1 };
        Dictionaries t = Strict.Map<Dictionaries>(new { Sorted = source, Concurrent = source }).Value;

        Assert.Equal(["a", "b"], t.Sorted.Keys);
        Assert.Equal(2, t.Concurrent["b"]);
    }

    public class IntKeyDict { public Dictionary<int, int> Map { get; set; } }

    [Fact]
    public void DictionaryKeyOrValueThatFailsConversion_FailsUnlessLenient()
    {
        var badKey = new { Map = new Dictionary<string, string> { ["1"] = "1", ["x"] = "2" } };
        var badValue = new { Map = new Dictionary<string, string> { ["1"] = "1", ["2"] = "x" } };

        Assert.Equal(MappingErrorType.ConversionError, Strict.Map<IntKeyDict>(badKey).Error.ErrorType);
        Assert.Equal(MappingErrorType.ConversionError, Strict.Map<IntKeyDict>(badValue).Error.ErrorType);
        Assert.Equal([1], Lenient.Map<IntKeyDict>(badKey).Value.Map.Keys);
        Assert.Equal([1], Lenient.Map<IntKeyDict>(badValue).Value.Map.Keys);
    }

    [Fact]
    public void DictionaryKeysCollidingAfterConversion_ReturnsFailure()
    {
        var source = new { Map = new Dictionary<string, int> { ["1"] = 1, ["01"] = 2 } };

        Assert.Equal(MappingErrorType.CollectionMappingError, Strict.Map<IntKeyDict>(source).Error.ErrorType);
    }

    [Fact]
    public void DictionaryFromNonDictionarySource_ReturnsFailure()
    {
        var result = Strict.Map<IntKeyDict>(new { Map = new List<int> { 1 } });

        Assert.Equal(MappingErrorType.CollectionMappingError, result.Error.ErrorType);
    }

    public class Person { public string Name { get; set; } = ""; }
    public class PersonSummary { public string Name { get; set; } = ""; }
    public class TwoViews { public Person Full { get; set; } public PersonSummary Summary { get; set; } }

    [Fact]
    public void SameSourceIntoDifferentTargetTypes_MapsBoth()
    {
        Person shared = new() { Name = "p" };
        TwoViews t = Strict.Map<TwoViews>(new { Full = shared, Summary = shared }).Value;

        Assert.Equal("p", t.Full.Name);
        Assert.Equal("p", t.Summary.Name);
    }
}
