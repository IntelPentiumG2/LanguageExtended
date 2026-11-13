using System.Collections;
using System.Collections.Immutable;
using System.Dynamic;
using LanguageExtended.Mapping;

namespace MappingTest;

public class MapperMemberTests
{
    private static readonly Mapper Strict = Mapper.Default;

    public class Address { public string City { get; set; } = ""; }
    public class Person
    {
        public string Name { get; set; } = "";
        public Address Address { get; set; }
        public List<int> Scores { get; set; } = [];
    }

    private static object Expando(params (string Key, object? Value)[] members)
    {
        IDictionary<string, object?> expando = new ExpandoObject();
        foreach ((string key, object? value) in members)
            expando[key] = value;
        return expando;
    }

    [Fact]
    public void Expando_NestedValues_AreMappedLikeRegularMembers()
    {
        Address poco = new() { City = "Hamburg" };
        object nestedExpando = Expando(("Name", "a"), ("Address", Expando(("City", "Berlin"))), ("Scores", new List<string> { "1", "2" }));
        object nestedPoco = Expando(("Name", "b"), ("Address", poco), ("Scores", new int[0]));

        Person fromExpando = Strict.Map<Person>(nestedExpando).Value;
        Person fromPoco = Strict.Map<Person>(nestedPoco).Value;

        Assert.Equal("Berlin", fromExpando.Address.City);
        Assert.Equal([1, 2], fromExpando.Scores);
        Assert.Equal("Hamburg", fromPoco.Address.City);
        Assert.NotSame(poco, fromPoco.Address);
    }

    [Fact]
    public void Expando_IgnoreCase_MatchesKeys()
    {
        Mapper ignoreCase = new(new MappingOptions { IgnoreCase = true });

        Person p = ignoreCase.Map<Person>(Expando(("name", "n"), ("ADDRESS", null), ("scores", new List<int>()))).Value;

        Assert.Equal("n", p.Name);
    }

    [Fact]
    public void Expando_IgnoreCase_KeysDifferingOnlyInCase_ReturnsFailure()
    {
        Mapper ignoreCase = new(new MappingOptions { IgnoreCase = true });

        var result = ignoreCase.Map<Person>(Expando(("name", "a"), ("Name", "b")));

        Assert.Equal(MappingErrorType.GeneralMappingError, result.Error.ErrorType);
        Assert.IsType<ArgumentException>(result.Error.Exception);
    }

    public class DynamicWithBadGetter : DynamicObject
    {
        public string Name { get; set; } = "d";
        public string Broken => throw new InvalidOperationException();
    }

    public class NameOnly { public string Name { get; set; } = ""; }

    [Fact]
    public void DynamicObject_UnrelatedThrowingGetter_IsNotRead()
    {
        Assert.Equal("d", Strict.Map<NameOnly>(new DynamicWithBadGetter()).Value.Name);
    }

    public class WithIndexer
    {
        public string Name { get; set; } = "";
        public string this[int i] { get => ""; set { } }
    }

    [Fact]
    public void Indexers_AreNotMappableMembers()
    {
        Assert.Equal("x", Strict.Map<WithIndexer>(new { Name = "x" }).Value.Name);
        Assert.Equal("y", Strict.Map<NameOnly>(new WithIndexer { Name = "y" }).Value.Name);
    }

    public class WriteOnlyName
    {
        private string _name = "";
        public string Name { set => _name = value; }
        public override string ToString() => _name;
    }

    public class FieldName { public int Other = 1; public string Name = "field"; }

    [Fact]
    public void SourceMembers_WriteOnlyPropertiesAreSkipped_FieldsAreRead()
    {
        Assert.Equal(MappingErrorType.MemberNotFound, Strict.Map<NameOnly>(new WriteOnlyName()).Error.ErrorType);
        Assert.Equal("field", Strict.Map<NameOnly>(new FieldName()).Value.Name);
    }

    public class EnumTarget { public DayOfWeek Day { get; set; } }

    [Fact]
    public void ThrowOnMappingFailure_ThrowsFromBothEntryPoints_AndNotOnSuccess()
    {
        Mapper throwing = new(new MappingOptions { ThrowOnMappingFailure = true });

        MappingException fromMap = Assert.Throws<MappingException>(() => throwing.Map<EnumTarget>(new { Day = "Someday" }));
        MappingException fromCtorless = Assert.Throws<MappingException>(() => throwing.MapWithoutDefaultConstructor<EnumTarget>(new { }));

        Assert.Equal(MappingErrorType.EnumConversionError, fromMap.MappingError.ErrorType);
        Assert.Equal(MappingErrorType.MemberNotFound, fromCtorless.MappingError.ErrorType);
        Assert.Equal(DayOfWeek.Friday, throwing.Map<EnumTarget>(new { Day = "Friday" }).Value.Day);
    }

    [Fact]
    public void MappingException_WrapsTheErrorsException()
    {
        InvalidOperationException cause = new("cause");
        MappingException ex = new(new MappingError("boom", MappingErrorType.Other, "", cause));

        Assert.Equal("boom", ex.Message);
        Assert.Same(cause, ex.InnerException);
    }

    public class TwoCtors
    {
        public TwoCtors(int a, int b) => Used = "two";
        public TwoCtors(int a) => Used = "one";
        public string Used { get; set; }
        public int Id { get; set; }
    }

    [Fact]
    public void MapWithoutDefaultConstructor_PrefersFewestParameters()
    {
        Mapper ignoreUnmapped = new(new MappingOptions { IgnoreUnmappedTargetMembers = true });

        // Mapped twice: each call must build a fresh instance.
        TwoCtors first = ignoreUnmapped.MapWithoutDefaultConstructor<TwoCtors>(new { Id = 1 }).Value;
        TwoCtors second = ignoreUnmapped.MapWithoutDefaultConstructor<TwoCtors>(new { Id = 2 }).Value;

        Assert.Equal(("one", 1), (first.Used, first.Id));
        Assert.Equal(2, second.Id);
    }

    public class ThrowingDefaultCtor
    {
        public ThrowingDefaultCtor() => throw new InvalidOperationException();
        public int X { get; set; }
    }

    public class HoldsThrowingCtor { public ThrowingDefaultCtor Child { get; set; } }

    [Fact]
    public void NestedTypeWithThrowingDefaultCtor_ReturnsFailure()
    {
        var result = Strict.Map<HoldsThrowingCtor>(new { Child = new { X = 1 } });

        Assert.Equal(MappingErrorType.ComplexTypeMappingError, result.Error.ErrorType);
    }

    public class ThrowingSetterChild
    {
        public Address Child { get => null!; set => throw new InvalidOperationException(); }
    }

    [Fact]
    public void ComplexMemberWithThrowingSetter_ReturnsFailure()
    {
        var result = Strict.Map<ThrowingSetterChild>(new { Child = new Address() });

        Assert.Equal(MappingErrorType.SetMemberValueError, result.Error.ErrorType);
    }

    public class UnsupportedCollections
    {
        public IImmutableList<int> Interface { get; set; }
        public ReadOnlyCollectionBase Abstract { get; set; }
    }

    [Fact]
    public void UnsupportedInterfaceOrAbstractCollection_ReturnsFailure()
    {
        Mapper oneMember = new(new MappingOptions { IgnoreUnmappedTargetMembers = true });

        Assert.Equal(MappingErrorType.CollectionCreationError,
            oneMember.Map<UnsupportedCollections>(new { Interface = new List<int>() }).Error.ErrorType);
        Assert.Equal(MappingErrorType.CollectionCreationError,
            oneMember.Map<UnsupportedCollections>(new { Abstract = new List<int>() }).Error.ErrorType);
    }

    public class UntypedQueue { public Queue Items { get; set; } }

    [Fact]
    public void NonGenericQueue_KeepsOrder()
    {
        UntypedQueue t = Strict.Map<UntypedQueue>(new { Items = new Queue(new[] { 1, 2, 3 }) }).Value;

        Assert.Equal(new object[] { 1, 2, 3 }, t.Items.Cast<object>());
    }
}
