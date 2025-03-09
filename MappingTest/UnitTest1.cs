using LanguageExtended.Mapping;
// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace MappingTest;

public class MapperTests
{
    [Fact]
    public void Map_PrimitiveTypes_Success()
    {
        var source = new { Id = 1, Name = "Test" };
        var mapper = new Mapper();

        var result = mapper.Map<Destination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("Test", result.Value.Name);
    }

    [Fact]
    public void Map_NullSource_Failure()
    {
        var mapper = new Mapper();

        var result = mapper.Map<Destination>(null);

        Assert.False(result.IsSuccess);
        Assert.Equal("Source cannot be null", result.Error.Message);
    }

    [Fact]
    public void Map_ComplexType_Success()
    {
        var source = new { Id = 1, Nested = new { Value = "NestedValue" } };
        var mapper = new Mapper();

        var result = mapper.Map<ComplexDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("NestedValue", result.Value.Nested.Value);
    }

    [Fact]
    public void Map_Collection_Success()
    {
        var source = new { Items = new[] { "Item1", "Item2" } };
        var mapper = new Mapper();

        var result = mapper.Map<CollectionDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Length);
        Assert.Equal("Item1", result.Value.Items[0]);
        Assert.Equal("Item2", result.Value.Items[1]);
    }

    [Fact]
    public void Map_EnumConversion_Success()
    {
        var source = new { Status = "Active" };
        var mapper = new Mapper();

        var result = mapper.Map<EnumDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(Status.Active, result.Value.Status);
    }

    [Fact]
    public void Map_InvalidEnumConversion_Failure()
    {
        var source = new { Status = "Invalid" };
        var mapper = new Mapper();

        var result = mapper.Map<EnumDestination>(source);

        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public void Map_NullNestedSource_CreatesEmptyNestedObject()
    {
        var source = new { Id = 1, Nested = (object)null };
        var mapper = new Mapper();

        var result = mapper.Map<ComplexDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.NotNull(result.Value.Nested); // Nested should be created even if source is null
        Assert.Null(result.Value.Nested.Value);
    }

    [Fact]
    public void Map_ComplexNestedProperties_Success()
    {
        var source = new { 
            Id = 1, 
            Nested = new { 
                Value = "NestedValue",
                SubNested = new { Value = "SubNestedValue" }
            }
        };
        var mapper = new Mapper();

        var result = mapper.Map<ComplexNestedDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("NestedValue", result.Value.Nested.Value);
        Assert.Equal("SubNestedValue", result.Value.Nested.SubNested.Value);
    }

    [Fact]
    public void Map_MismatchedPropertyTypes_IgnoresMismatchedProperties()
    {
        var source = new { Id = "not-an-int", Name = "Test" };
        var mapper = new Mapper();

        var result = mapper.Map<Destination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Id); // Default value for int
        Assert.Equal("Test", result.Value.Name);
    }

    [Fact]
    public void Map_CollectionOfComplexTypes_Success()
    {
        var source = new { 
            Items = new[] { 
                new { Value = "Item1" }, 
                new { Value = "Item2" } 
            }
        };
        var mapper = new Mapper();

        var result = mapper.Map<ComplexCollectionDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Length);
        Assert.Equal("Item1", result.Value.Items[0].Value);
        Assert.Equal("Item2", result.Value.Items[1].Value);
    }
    
    [Fact]
    public void Map_WithIgnoreCaseOption_Success()
    {
        var source = new { id = 1, NAME = "Test" }; // Different case
        var mapper = new Mapper(new MappingOptions { IgnoreCase = true });

        var result = mapper.Map<Destination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("Test", result.Value.Name);
    }

    [Fact]
    public void Map_WithoutIgnoreCaseOption_MissesProperties()
    {
        var source = new { id = 1, NAME = "Test" }; // Different case
        var mapper = new Mapper(new MappingOptions { IgnoreCase = false });

        var result = mapper.Map<Destination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Id); // Default value
        Assert.Null(result.Value.Name); // Default value
    }

    [Fact]
    public void Map_ThrowOnMappingFailureOption_ThrowsOnEnumError()
    {
        var source = new { Status = "Invalid" };
        var mapper = new Mapper(new MappingOptions { ThrowOnMappingFailure = true });

        var result = mapper.Map<EnumDestination>(source);

        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot convert", result.Error.Message);
    }

    [Fact]
    public void Map_DefaultMapperInstance_Success()
    {
        var source = new { Id = 1, Name = "Test" };
        
        var result = Mapper.Default.Map<Destination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("Test", result.Value.Name);
    }

    [Fact]
    public void Map_ListToArray_Success()
    {
        var source = new { Items = new List<string> { "Item1", "Item2" } };
        var mapper = new Mapper();

        var result = mapper.Map<CollectionDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Length);
        Assert.Equal("Item1", result.Value.Items[0]);
        Assert.Equal("Item2", result.Value.Items[1]);
    }

    [Fact]
    public void Map_ArrayToList_Success()
    {
        var source = new { Items = new[] { "Item1", "Item2" } };
        var mapper = new Mapper();

        var result = mapper.Map<ListCollectionDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal("Item1", result.Value.Items[0]);
        Assert.Equal("Item2", result.Value.Items[1]);
    }

    [Fact]
    public void Map_WithFields_Success()
    {
        var source = new ClassWithFields { Id = 1, Name = "Test" };
        var mapper = new Mapper();

        var result = mapper.Map<ClassWithFields>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("Test", result.Value.Name);
    }

    [Fact]
    public void Map_WithReadOnlyProperties_OnlyMapsWritableProperties()
    {
        var source = new { Id = 1, Name = "Test", ReadOnly = "ReadOnly" };
        var mapper = new Mapper();

        var result = mapper.Map<ClassWithReadOnlyProperty>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("Test", result.Value.Name);
        Assert.Equal("DefaultReadOnly", result.Value.ReadOnly); // Should not be mapped
    }

    [Fact]
    public void Map_TypeConversion_Success()
    {
        var source = new { IntValue = "42", DoubleValue = 3.14f, BoolValue = 1 };
        var mapper = new Mapper();

        var result = mapper.Map<TypeConversionDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.IntValue);
        Assert.Equal(3.14d, result.Value.DoubleValue, 2);
        Assert.True(result.Value.BoolValue);
    }

    [Fact]
    public void Map_DictionaryCollection_Success()
    {
        var source = new { Items = new Dictionary<string, string> {
            { "Key1", "Value1" },
            { "Key2", "Value2" }
        }};
        var mapper = new Mapper();

        var result = mapper.Map<DictionaryCollectionDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal("Value1", result.Value.Items["Key1"]);
        Assert.Equal("Value2", result.Value.Items["Key2"]);
    }

    [Fact]
    public void Map_InterfaceCollection_Success()
    {
        var source = new { Items = new List<string> { "Item1", "Item2" } };
        var mapper = new Mapper();

        var result = mapper.Map<InterfaceCollectionDestination>(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count());
        Assert.Contains("Item1", result.Value.Items);
        Assert.Contains("Item2", result.Value.Items);
    }

    // Additional supporting classes for the tests
    private class ListCollectionDestination
    {
        public List<string> Items { get; set; }
    }

    private class ClassWithFields
    {
        public int Id;
        public string Name;
    }

    private class ClassWithReadOnlyProperty
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ReadOnly { get; } = "DefaultReadOnly";
    }

    private class TypeConversionDestination
    {
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
        public bool BoolValue { get; set; }
    }

    private class DictionaryCollectionDestination
    {
        public Dictionary<string, string> Items { get; set; }
    }

    private class InterfaceCollectionDestination
    {
        public IEnumerable<string> Items { get; set; }
    }

    private class ComplexNestedDestination
    {
        public int Id { get; set; }
        public NestedDestinationWithSub Nested { get; set; }
    }

    private class NestedDestinationWithSub
    {
        public string Value { get; set; }
        public SubNestedDestination SubNested { get; set; }
    }

    private class SubNestedDestination
    {
        public string Value { get; set; }
    }

    private class ComplexCollectionDestination
    {
        public NestedItem[] Items { get; set; }
    }

    private class NestedItem
    {
        public string Value { get; set; }
    }

    private class Destination
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private class ComplexDestination
    {
        public int Id { get; set; }
        public NestedDestination Nested { get; set; }
    }

    private class NestedDestination
    {
        public string Value { get; set; }
    }

    private class CollectionDestination
    {
        public string[] Items { get; set; }
    }

    private class EnumDestination
    {
        public Status Status { get; set; }
    }

    private enum Status
    {
        Active,
        Inactive
    }
}