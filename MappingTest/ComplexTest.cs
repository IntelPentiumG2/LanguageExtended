using System.Collections;
using LanguageExtended.Mapping;

namespace MappingTest;

public class ComplexMapperTests
{
    [Fact]
    public void ComplexMappingScenario_CoverageMaximization()
    {
        // Setup various mapping options for thorough testing
        var strictMapper = new Mapper(new MappingOptions 
        { 
            IgnoreCase = false, 
            LenientMappingErrors = false,
            ThrowOnMappingFailure = true
        });
        
        var lenientMapper = new Mapper(new MappingOptions 
        { 
            IgnoreCase = true, 
            LenientMappingErrors = true,
            ThrowOnMappingFailure = false,
            IgnoreMissingSourceMembers = true,
            IgnoreUnmappedTargetMembers = true
        });

        // Create a complex nested source with various types
        var source = new ComplexSource
        {
            Id = 42,
            Name = "Complex Test",
            CreatedDate = DateTime.Now,
            Tags = new[] { "tag1", "tag2", "tag3" },
            Properties = new Dictionary<string, object>
            {
                { "IntValue", 123 },
                { "StringValue", "test" },
                { "BoolValue", true }
            },
            Status = "Active",
            CustomCollection = new CustomCollection<string> { "CustomItem1", "CustomItem2" },
            Metrics = new[]
            {
                new Metric { Name = "Metric1", Value = 10.5 },
                new Metric { Name = "Metric2", Value = 20.3 }
            },
            Configuration = new Configuration
            {
                IsEnabled = true,
                DisplayName = "Config",
                Settings = new Dictionary<string, string>
                {
                    { "Setting1", "Value1" },
                    { "Setting2", "Value2" }
                },
                DeepNested = new DeepNested
                {
                    Level = 3,
                    Description = "Very Deep",
                    Items = new[] { 1, 2, 3, 4, 5 }
                }
            },
            NullProperty = null,
            DecimalValue = 123.456m
        };

        // Map to target with lenient mapper
        var result = lenientMapper.Map<ComplexTarget>(source);
        Assert.True(result.IsSuccess);
        
        var target = result.Value;
        
        // Verify basic properties
        Assert.Equal(42, target.Id);
        Assert.Equal("Complex Test", target.Name);
        Assert.Equal(source.CreatedDate.Date, target.CreatedDate.Date);
        Assert.Equal(ItemStatus.Active, target.Status);
        Assert.Equal(123.456m, target.DecimalValue);
        
        // Verify collections
        Assert.Equal(3, target.Tags.Count);
        Assert.Contains("tag1", target.Tags);
        Assert.Contains("tag2", target.Tags);
        Assert.Contains("tag3", target.Tags);
        
        // Verify dictionary
        Assert.Equal(3, target.Properties.Count);
        Assert.Equal(123, target.Properties["IntValue"]);
        Assert.Equal("test", target.Properties["StringValue"]);
        Assert.Equal(true, target.Properties["BoolValue"]);
        
        // Verify IEnumerable
        Assert.Equal(2, target.CustomCollection.Count());
        Assert.Contains("CustomItem1", target.CustomCollection);
        Assert.Contains("CustomItem2", target.CustomCollection);
        
        // Verify nested complex types
        Assert.Equal(2, target.Metrics.Length);
        Assert.Equal("Metric1", target.Metrics[0].Name);
        Assert.Equal(10.5, target.Metrics[0].Value);
        Assert.Equal("Metric2", target.Metrics[1].Name);
        Assert.Equal(20.3, target.Metrics[1].Value);
        
        // Verify deep nested complex types
        Assert.NotNull(target.Configuration);
        Assert.True(target.Configuration.IsEnabled);
        Assert.Equal("Config", target.Configuration.DisplayName);
        Assert.Equal(2, target.Configuration.Settings.Count);
        Assert.Equal("Value1", target.Configuration.Settings["Setting1"]);
        Assert.Equal("Value2", target.Configuration.Settings["Setting2"]);
        
        Assert.NotNull(target.Configuration.DeepNested);
        Assert.Equal(3, target.Configuration.DeepNested.Level);
        Assert.Equal("Very Deep", target.Configuration.DeepNested.Description);
        Assert.Equal(5, target.Configuration.DeepNested.Items.Length);
        Assert.Equal(1, target.Configuration.DeepNested.Items[0]);
        Assert.Equal(5, target.Configuration.DeepNested.Items[4]);
        
        // Verify null properties
        Assert.Null(target.NullProperty);
        
        // Test with a source that has case mismatches with strict mapper
        var sourceCaseMismatch = new 
        { 
            iD = 100,
            NaMe = "Case Test",
            STATUS = "Active"
        };
        
        var strictResult = strictMapper.Map<SimplifiedTarget>(sourceCaseMismatch);
        Assert.True(strictResult.IsFailure); // Should fail due to case mismatch and strict options
        
        // Test with lenient mapper
        var lenientResult = lenientMapper.Map<SimplifiedTarget>(sourceCaseMismatch);
        Assert.True(lenientResult.IsSuccess);
        Assert.Equal(100, lenientResult.Value.Id);
        Assert.Equal("Case Test", lenientResult.Value.Name);
        Assert.Equal(ItemStatus.Active, lenientResult.Value.Status);
        
        // Test mismatched types
        var typeMismatchSource = new 
        {
            Id = "not a number",
            Name = 12345,
            Status = "Invalid"
        };
        
        var strictTypeMismatchResult = strictMapper.Map<SimplifiedTarget>(typeMismatchSource);
        Assert.True(strictTypeMismatchResult.IsFailure);
        
        var lenientTypeMismatchResult = lenientMapper.Map<SimplifiedTarget>(typeMismatchSource);
        Assert.True(lenientTypeMismatchResult.IsSuccess);
        Assert.Equal(0, lenientTypeMismatchResult.Value.Id); // Default int value
        Assert.Equal("12345", lenientTypeMismatchResult.Value.Name); // Converted to string
        Assert.Equal(ItemStatus.Inactive, lenientTypeMismatchResult.Value.Status); // Default enum value
        
        // Test with inheritance
        var derivedSource = new DerivedSource
        {
            Id = 200,
            Name = "Derived",
            Status = "Active",
            ExtraProperty = "Extra"
        };
        
        var baseResult = lenientMapper.Map<SimplifiedTarget>(derivedSource);
        Assert.True(baseResult.IsSuccess);
        Assert.Equal(200, baseResult.Value.Id);
        Assert.Equal("Derived", baseResult.Value.Name);
        Assert.Equal(ItemStatus.Active, baseResult.Value.Status);
        
        // Test with init-only properties
        var initPropertiesResult = lenientMapper.Map<TargetWithInitProperties>(source);
        Assert.True(initPropertiesResult.IsSuccess);
        Assert.Equal(42, initPropertiesResult.Value.Id);
        Assert.Equal("Complex Test", initPropertiesResult.Value.Name);
        
        // Test with fields instead of properties
        var fieldsResult = lenientMapper.Map<TargetWithFields>(source);
        Assert.True(fieldsResult.IsSuccess);
        Assert.Equal(42, fieldsResult.Value.Id);
        Assert.Equal("Complex Test", fieldsResult.Value.Name);
        
        // Test with readonly fields
        var readonlyFieldResult = lenientMapper.Map<TargetWithReadonlyField>(source);
        Assert.True(readonlyFieldResult.IsSuccess);
        Assert.Equal(42, readonlyFieldResult.Value.Id);
        Assert.Equal(0, readonlyFieldResult.Value.ReadonlyField); // Should not be mapped
        
        // Test mapping to interfaces
        var interfaceResult = lenientMapper.Map<TargetWithInterfaces>(source);
        Assert.True(interfaceResult.IsSuccess);
        Assert.Equal(3, interfaceResult.Value.Tags.Count());
        Assert.Equal(2, interfaceResult.Value.CustomCollection.Count());
        
        // Test with invalid enum and strict mapper
        var invalidEnumSource = new { Status = "NonExistent" };
        var invalidEnumResult = strictMapper.Map<SimplifiedTarget>(invalidEnumSource);
        Assert.True(invalidEnumResult.IsFailure);
        Assert.Equal(MappingErrorType.EnumConversionError, invalidEnumResult.Error.ErrorType);
        
        // Test with invalid enum and lenient mapper
        var invalidEnumLenientResult = lenientMapper.Map<SimplifiedTarget>(invalidEnumSource);
        Assert.True(invalidEnumLenientResult.IsSuccess);
        Assert.Equal(ItemStatus.Inactive, invalidEnumLenientResult.Value.Status);
    }
}

// Classes for complex mapping scenario
public class ComplexSource
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public string[] Tags { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public string Status { get; set; }
    public CustomCollection<string> CustomCollection { get; set; }
    public Metric[] Metrics { get; set; }
    public Configuration Configuration { get; set; }
    public object? NullProperty { get; set; }
    public decimal DecimalValue { get; set; }
    public readonly int ReadOnlyField = 999;
}

public class DerivedSource : ComplexSource
{
    public string ExtraProperty { get; set; }
}

public class ComplexTarget
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<string> Tags { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public ItemStatus Status { get; set; }
    public IEnumerable<string> CustomCollection { get; set; }
    public Metric[] Metrics { get; set; }
    public Configuration Configuration { get; set; }
    public object NullProperty { get; set; }
    public decimal DecimalValue { get; set; }
    public int ReadonlyField { get; }
}

public class SimplifiedTarget
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ItemStatus Status { get; set; }
}

public class TargetWithInitProperties
{
    public int Id { get; init; }
    public string Name { get; init; }
}

public class TargetWithFields
{
    public int Id;
    public string Name;
}

public class TargetWithReadonlyField
{
    public int Id { get; set; }
    public readonly int ReadonlyField = 0;
}

public class TargetWithInterfaces
{
    public IEnumerable<string> Tags { get; set; }
    public ICollection<string> CustomCollection { get; set; }
}

public class Metric
{
    public string Name { get; set; }
    public double Value { get; set; }
}

public class Configuration
{
    public bool IsEnabled { get; set; }
    public string DisplayName { get; set; }
    public Dictionary<string, string> Settings { get; set; }
    public DeepNested DeepNested { get; set; }
}

public class DeepNested
{
    public int Level { get; set; }
    public string Description { get; set; }
    public int[] Items { get; set; }
}

public enum ItemStatus
{
    Inactive = 0,
    Active = 1
}

public class CustomCollection<T> : IEnumerable<T>
{
    private readonly List<T> _items = new();

    public void Add(T item) => _items.Add(item);

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}