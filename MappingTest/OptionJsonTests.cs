using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExtended.Option;

namespace MappingTest;

public class OptionJsonTests
{
    public record SourceGenDto(Option<string> Name, ValueOption<int> Age);

    private record Dto(Option<string> Name, Option<Inner> Inner);
    private record ValueDto(ValueOption<int> Age, ValueOption<int> Score);
    private record Inner(int Id);

    [Fact]
    public void Some_SerializesAsValue_None_AsNull()
    {
        var json = JsonSerializer.Serialize(new Dto(Option<string>.Some("x"), Option<Inner>.None()));

        Assert.Equal("""{"Name":"x","Inner":null}""", json);
    }

    [Fact]
    public void RoundTrips()
    {
        var dto = JsonSerializer.Deserialize<Dto>("""{"Name":null,"Inner":{"Id":3}}""")!;

        Assert.True(dto.Name.IsNone);
        Assert.Equal(3, dto.Inner.Value.Id);
    }

    [Fact]
    public void MissingProperty_IsNone()
    {
        var dto = JsonSerializer.Deserialize<Dto>("{}")!;

        Assert.True(dto.Name.IsNone);
    }

    [Fact]
    public void ValueOption_SerializesAsValueOrNull_AndRoundTrips()
    {
        var json = JsonSerializer.Serialize(new ValueDto(5, ValueOption<int>.None()));
        var dto = JsonSerializer.Deserialize<ValueDto>(json)!;

        Assert.Equal("""{"Age":5,"Score":null}""", json);
        Assert.Equal(5, dto.Age.Value);
        Assert.True(dto.Score.IsNone);
    }

    [Fact]
    public void SourceGeneratedContext_RoundTrips()
    {
        var json = JsonSerializer.Serialize(new SourceGenDto(Option<string>.Some("x"), ValueOption<int>.None()), OptionJsonContext.Default.SourceGenDto);
        var back = JsonSerializer.Deserialize(json, OptionJsonContext.Default.SourceGenDto)!;

        Assert.Equal("""{"Name":"x","Age":null}""", json);
        Assert.Equal("x", back.Name.Value);
        Assert.True(back.Age.IsNone);
    }
}

// Inner types must be listed too: the converter resolves them through the context.
[JsonSerializable(typeof(OptionJsonTests.SourceGenDto))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int?))]
internal partial class OptionJsonContext : JsonSerializerContext;
