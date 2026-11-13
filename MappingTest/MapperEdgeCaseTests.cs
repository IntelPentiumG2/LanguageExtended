using System.Collections;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using LanguageExtended.Mapping;

namespace MappingTest;

public class MapperEdgeCaseTests
{
    private static readonly Mapper Invariant = new(new MappingOptions { Culture = CultureInfo.InvariantCulture });
    private static readonly Mapper IgnoreUnmapped = new(new MappingOptions { IgnoreUnmappedTargetMembers = true });
    private static readonly Mapper Lenient = new(new MappingOptions { LenientMappingErrors = true });

    public class StringSource
    {
        public string D { get; set; } = "1.5";
        public string F { get; set; } = "2.5";
        public string L { get; set; } = "9000000000";
        public string Date { get; set; } = "2024-02-29";
        public string Time { get; set; } = "13:45";
        public string Offset { get; set; } = "2024-02-29T13:45:00+02:00";
        public string Span { get; set; } = "01:02:03";
        public string Id { get; set; } = "6f9619ff-8b86-d011-b42d-00cf4fc964ff";
    }

    public class ParsedTarget
    {
        public double D { get; set; }
        public float F { get; set; }
        public long L { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public DateTimeOffset Offset { get; set; }
        public TimeSpan Span { get; set; }
        public Guid Id { get; set; }
    }

    [Fact]
    public void Strings_AreParsedIntoEverySupportedType()
    {
        ParsedTarget t = Invariant.Map<ParsedTarget>(new StringSource()).Value;

        Assert.Equal(1.5, t.D);
        Assert.Equal(2.5f, t.F);
        Assert.Equal(9_000_000_000L, t.L);
        Assert.Equal(new DateOnly(2024, 2, 29), t.Date);
        Assert.Equal(new TimeOnly(13, 45), t.Time);
        Assert.Equal(new DateTimeOffset(2024, 2, 29, 13, 45, 0, TimeSpan.FromHours(2)), t.Offset);
        Assert.Equal(new TimeSpan(1, 2, 3), t.Span);
        Assert.Equal(Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff"), t.Id);
    }

    public class WithDefaults
    {
        public WithDefaults(int n, string origin = "ctor-default") => (N, Origin) = (n, origin);
        public int N { get; set; }
        public string Origin { get; set; }
    }

    [Fact]
    public void MapWithoutDefaultConstructor_FillsParametersWithDefaults()
    {
        WithDefaults t = IgnoreUnmapped.MapWithoutDefaultConstructor<WithDefaults>(new { N = 3 }).Value;

        Assert.Equal(3, t.N);
        Assert.Equal("ctor-default", t.Origin);
    }

    public class ThrowingCtor
    {
        public ThrowingCtor(string s) => throw new InvalidOperationException(s);
        public string Name { get; set; }
    }

    [Fact]
    public void MapWithoutDefaultConstructor_ThrowingCtor_FallsBackToUninitializedObject()
    {
        ThrowingCtor t = Mapper.Default.MapWithoutDefaultConstructor<ThrowingCtor>(new { Name = "x" }).Value;

        Assert.Equal("x", t.Name);
    }

    public abstract class AbstractTarget
    {
        public string Name { get; set; }
    }

    [Fact]
    public void MapWithoutDefaultConstructor_AbstractTarget_ReturnsFailure()
    {
        var result = Mapper.Default.MapWithoutDefaultConstructor<AbstractTarget>(new { Name = "x" });

        Assert.Equal(MappingErrorType.Other, result.Error.ErrorType);
        Assert.IsType<InvalidOperationException>(result.Error.Exception);
    }

    public class Nested { public int X { get; set; } }

    public class ThrowingGetterSource
    {
        public int A => throw new InvalidOperationException("A");
        public Nested B => throw new InvalidOperationException("B");
    }

    public class ThrowingGetterTarget
    {
        public int A { get; set; }
        public Nested B { get; set; }
    }

    [Fact]
    public void ThrowingSourceGetter_FailsByDefault()
    {
        var result = Mapper.Default.Map<ThrowingGetterTarget>(new ThrowingGetterSource());

        Assert.Equal(MappingErrorType.InvalidMemberType, result.Error.ErrorType);
        Assert.Equal("A", result.Error.PropertyPath);
    }

    [Fact]
    public void ThrowingSourceGetter_IgnoreUnmapped_SkipsValueAndCreatesEmptyComplex()
    {
        ThrowingGetterTarget t = IgnoreUnmapped.Map<ThrowingGetterTarget>(new ThrowingGetterSource()).Value;

        Assert.Equal(0, t.A);
        Assert.NotNull(t.B);
    }

    public class ThrowingGetterAbstractSource
    {
        public AbstractTarget Child => throw new InvalidOperationException();
    }

    public class AbstractChildTarget
    {
        public AbstractTarget Child { get; set; }
    }

    [Fact]
    public void ThrowingSourceGetter_IgnoreUnmapped_AbstractMember_ReturnsFailure()
    {
        var result = IgnoreUnmapped.Map<AbstractChildTarget>(new ThrowingGetterAbstractSource());

        Assert.Equal(MappingErrorType.ComplexTypeMappingError, result.Error.ErrorType);
        Assert.Equal("Child", result.Error.PropertyPath);
    }

    public class ConcreteTarget : AbstractTarget;

    public class ConcreteChildSource
    {
        public ConcreteTarget Child { get; set; } = new() { Name = "c" };
    }

    [Fact]
    public void NestedAbstractTargetMember_ReturnsFailure()
    {
        var result = Mapper.Default.Map<AbstractChildTarget>(new ConcreteChildSource());

        Assert.Equal(MappingErrorType.ComplexTypeMappingError, result.Error.ErrorType);
    }

    public class ThrowingSetterTarget
    {
        public int A { get => 0; set => throw new InvalidOperationException(); }
    }

    [Fact]
    public void ThrowingTargetSetter_ReturnsFailure()
    {
        var result = Mapper.Default.Map<ThrowingSetterTarget>(new { A = 1 });

        Assert.Equal(MappingErrorType.SetMemberValueError, result.Error.ErrorType);
        Assert.Equal("A", result.Error.PropertyPath);
    }

    public class Person
    {
        public string Name { get; set; } = "unset";
        public int Age { get; set; }
    }

    [Fact]
    public void Expando_NullValue_SetsNull()
    {
        dynamic source = new ExpandoObject();
        source.Name = null;
        source.Age = 1;

        Person p = Mapper.Default.Map<Person>((object)source).Value;

        Assert.Null(p.Name);
        Assert.Equal(1, p.Age);
    }

    [Fact]
    public void Expando_MissingMember_FailsUnlessIgnored()
    {
        dynamic source = new ExpandoObject();
        source.Name = "n";

        var result = Mapper.Default.Map<Person>((object)source);
        Assert.Equal(MappingErrorType.MemberNotFound, result.Error.ErrorType);
        Assert.Equal("Age", result.Error.PropertyPath);

        Assert.Equal("n", IgnoreUnmapped.Map<Person>((object)source).Value.Name);
    }

    [Fact]
    public void Expando_UnconvertibleValue_FailsUnlessLenient()
    {
        dynamic source = new ExpandoObject();
        source.Name = "n";
        source.Age = "not a number";

        Assert.True(Mapper.Default.Map<Person>((object)source).IsFailure);

        Person p = Lenient.Map<Person>((object)source).Value;
        Assert.Equal("n", p.Name);
        Assert.Equal(0, p.Age);
    }

    public class DynamicPerson : DynamicObject
    {
        public string Name { get; set; } = "dyn";
        public int Age { get; set; } = 7;
    }

    [Fact]
    public void DynamicObject_PublicPropertiesAreMapped()
    {
        Person p = Mapper.Default.Map<Person>(new DynamicPerson()).Value;

        Assert.Equal("dyn", p.Name);
        Assert.Equal(7, p.Age);
    }

    public class ArrayListHolder { public ArrayList Items { get; set; } }

    [Fact]
    public void NonGenericCollection_IsCopied()
    {
        ArrayListHolder t = Mapper.Default.Map<ArrayListHolder>(new ArrayListHolder { Items = [1, "two"] }).Value;

        Assert.Equal(new ArrayList { 1, "two" }, t.Items);
    }

    public class IntListHolder { public List<int> Items { get; set; } }

    [Fact]
    public void CollectionItemThatFailsConversion_FailsUnlessLenient()
    {
        var source = new { Items = new List<string> { "1", "x", "3" } };

        Assert.Equal(MappingErrorType.ConversionError, Mapper.Default.Map<IntListHolder>(source).Error.ErrorType);
        Assert.Equal([1, 3], Lenient.Map<IntListHolder>(source).Value.Items);
    }

    public class ReadOnlyHolder
    {
        public ReadOnlyCollection<int> List { get; set; }
    }

    [Fact]
    public void CollectionBuiltFromItems_IsMapped()
    {
        ReadOnlyHolder t = Mapper.Default.Map<ReadOnlyHolder>(new { List = new List<int> { 1, 2 } }).Value;

        Assert.Equal([1, 2], t.List);
    }

    public class ReadOnlyDictHolder
    {
        public ReadOnlyDictionary<string, int> Map { get; set; }
    }

    [Fact]
    public void DictionaryBuiltFromEntries_IsMapped()
    {
        ReadOnlyDictHolder t = Mapper.Default.Map<ReadOnlyDictHolder>(new { Map = new Dictionary<string, int> { ["a"] = 1 } }).Value;

        Assert.Equal(1, t.Map["a"]);
    }

    public class DictInterfaceHolder
    {
        public IDictionary<string, int> Map { get; set; }
    }

    [Fact]
    public void DictionaryInterfaceTarget_IsMapped()
    {
        var result = Mapper.Default.Map<DictInterfaceHolder>(new { Map = new Dictionary<string, int> { ["a"] = 1 } });

        Assert.Equal(1, result.Value.Map["a"]);
    }

    public class ObjectListHolder { public List<object> Items { get; set; } }

    [Fact]
    public void ObjectList_KeepsItems()
    {
        Nested nested = new() { X = 1 };
        ObjectListHolder t = Mapper.Default.Map<ObjectListHolder>(new { Items = new List<object> { 1, "two", nested } }).Value;

        Assert.Equal([1, "two", nested], t.Items);
    }

    public class OtherDictionariesHolder
    {
        public IReadOnlyDictionary<string, long> ReadOnly { get; set; }
        public Hashtable Table { get; set; }
    }

    [Fact]
    public void ReadOnlyDictionaryInterface_And_Hashtable_AreMapped()
    {
        OtherDictionariesHolder t = Mapper.Default.Map<OtherDictionariesHolder>(new
        {
            ReadOnly = new Dictionary<string, int> { ["a"] = 1 },
            Table = new Hashtable { ["k"] = "v" },
        }).Value;

        Assert.Equal(1L, t.ReadOnly["a"]);
        Assert.Equal("v", t.Table["k"]);
    }
}
