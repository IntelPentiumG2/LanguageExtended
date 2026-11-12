using LanguageExtended.Mapping;
using LanguageExtended.Option;

namespace MappingTest;

public class OptionMappingTests
{
    private class Holder
    {
        public Option<string> Name { get; set; }
        public ValueOption<int> Age { get; set; }
    }

    [Fact]
    public void SameTypeOptionMembers_AreCopied()
    {
        var source = new Holder { Name = Option<string>.Some("Ann"), Age = ValueOption<int>.Some(30) };

        var result = Mapper.Default.Map<Holder>(source);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : "");
        Assert.Equal("Ann", result.Value.Name.Value);
        Assert.Equal(30, result.Value.Age.Value);
    }
}
