using System.Text.Json;
using LanguageExtended;
using LanguageExtended.Mapping;
using LanguageExtended.Result;

namespace MappingTest;

public class ResultJsonTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new ResultJsonConverterFactory() } };

    private record Dto(Result<int, MappingError> Result, string After);

    private static Result<T, TError> RoundTrip<T, TError>(Result<T, TError> result, string expectedJson)
    {
        string json = JsonSerializer.Serialize(result, Options);
        Assert.Equal(expectedJson, json);
        return JsonSerializer.Deserialize<Result<T, TError>>(json, Options);
    }

    [Fact]
    public void ClosedConverter_MatchesFactoryFormat()
    {
        // The AOT-safe registration: one closed converter instead of the factory.
        JsonSerializerOptions closed = new() { Converters = { new ResultJsonConverter<int, string>() } };

        Assert.Equal("""{"value":5}""", JsonSerializer.Serialize(Result<int, string>.Success(5), closed));
        Assert.Equal("e", JsonSerializer.Deserialize<Result<int, string>>("""{"error":"e"}""", closed).Error);
    }

    [Fact]
    public void Success_RoundTrips()
    {
        Assert.Equal(5, RoundTrip(Result<int, string>.Success(5), """{"value":5}""").Value);
    }

    [Fact]
    public void Failure_RoundTrips_WithoutException()
    {
        var error = new MappingError("boom", MappingErrorType.ConversionError, "Name", new InvalidOperationException("secret"));

        string json = JsonSerializer.Serialize(Result<int, MappingError>.Failure(error), Options);
        var back = JsonSerializer.Deserialize<Result<int, MappingError>>(json, Options);

        Assert.DoesNotContain("secret", json);
        Assert.True(back.IsFailure);
        Assert.Equal((error.Message, error.ErrorType, error.PropertyPath), (back.Error.Message, back.Error.ErrorType, back.Error.PropertyPath));
        Assert.Null(back.Error.Exception);
    }

    [Fact]
    public void OverlappingTypes_KeepState()
    {
        Assert.True(RoundTrip(Result<string, string>.Success("x"), """{"value":"x"}""").IsSuccess);
        Assert.True(RoundTrip(Result<string, string>.Failure("x"), """{"error":"x"}""").IsFailure);
    }

    [Fact]
    public void NullSuccess_StaysSuccess()
    {
        var back = RoundTrip(Result<string?, string>.Success(null), """{"value":null}""");

        Assert.True(back.IsSuccess);
        Assert.Null(back.Value);
    }

    [Fact]
    public void NestedInDto_ConsumesWholeObject()
    {
        var dto = new Dto(Result<int, MappingError>.Success(1), "next");

        var back = JsonSerializer.Deserialize<Dto>(JsonSerializer.Serialize(dto, Options), Options)!;

        Assert.Equal(1, back.Result.Value);
        Assert.Equal("next", back.After);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("{}")]
    [InlineData("""{"other":1}""")]
    [InlineData("""{"value":1,"error":"e"}""")]
    public void MalformedInput_Throws(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Result<int, string>>(json, Options));
    }

    [Fact]
    public void Unit_RoundTrips()
    {
        Assert.True(RoundTrip(Result<Unit, string>.Success(Unit.Value), """{"value":{}}""").IsSuccess);
        Assert.Equal("e", RoundTrip(Result<Unit, string>.Failure("e"), """{"error":"e"}""").Error);
    }
}
