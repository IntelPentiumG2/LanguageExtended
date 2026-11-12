using LanguageExtended.Mapping;

namespace MappingTest;

public class NullHandlingTests
{
    private class Child { public string? Name { get; set; } }
    private class Holder { public Child? Child { get; set; } public string? Text { get; set; } }

    [Fact]
    public void CreateEmptyObjectsInsteadOfNull_CreatesEmptyComplexMember()
    {
        var mapper = new Mapper(new MappingOptions { CreateEmptyObjectsInsteadOfNull = true });

        var result = mapper.Map<Holder>(new Holder());

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : "");
        Assert.NotNull(result.Value.Child);
        Assert.Null(result.Value.Text);
    }

    [Fact]
    public void NullDictionaryValues_AreKept()
    {
        var source = new DictHolder { Map = new() { ["a"] = null } };

        var result = Mapper.Default.Map<DictHolder>(source);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : "");
        Assert.True(result.Value.Map!.ContainsKey("a"));
        Assert.Null(result.Value.Map["a"]);
    }

    private class DictHolder { public Dictionary<string, string?>? Map { get; set; } }
}
