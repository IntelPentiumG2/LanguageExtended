#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;
using LanguageExtended.Mapping;
using LanguageExtended.Option;
using LanguageExtended.Result;

namespace MappingTest;

public class UnionTests
{
    private static string Describe(Option<string> option) => option switch
    {
        string s => $"some {s}",
        null => "none",
    };

    private static string Describe(ValueOption<int> option) => option switch
    {
        int i => $"some {i}",
        null => "none",
    };

    private static string Describe(Result<int, MappingError> result) => result switch
    {
        int value => $"ok {value}",
        MappingError error => $"error {error.Message}",
    };

    [Fact]
    public void Option_IsUnionOfValueAndNull()
    {
        Option<string> some = "x";
        Option<string> none = (string?)null;

        Assert.Equal("some x", Describe(some));
        Assert.Equal("none", Describe(none));
        Assert.Equal("none", Describe(default(Option<string>)));
        Assert.Equal("x", ((IUnion)some).Value);
        Assert.True(some.IsSome);
    }

    [Fact]
    public void ValueOption_IsUnionOfValueAndNull()
    {
        Assert.Equal("some 5", Describe(ValueOption<int>.Some(5)));
        Assert.Equal("none", Describe(ValueOption<int>.None()));
        Assert.Null(((IUnion)ValueOption<int>.None()).Value);
    }

    [Fact]
    public void Result_IsUnionOfValueAndError()
    {
        Result<int, MappingError> ok = 5;
        Result<int, MappingError> failed = new MappingError("boom", MappingErrorType.Other);

        Assert.Equal("ok 5", Describe(ok));
        Assert.Equal("error boom", Describe(failed));
        // Property patterns without a type still apply to the Result itself.
        Assert.True(failed is { IsFailure: true });
    }

    [Fact]
    public void Result_WithOverlappingCaseTypes_MatchesByValueType()
    {
        Result<string, string> failed = Result<string, string>.Failure("f");

        Assert.True(failed is "f");
        Assert.True(failed.IsFailure);
    }

    private class UnionSource
    {
        public Option<string> Name { get; set; }
        public ValueOption<int> Age { get; set; }
        public Result<int, string> Score { get; set; }
        public Option<string> Nickname { get; set; }
    }

    private class UnionTarget
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public int Score { get; set; }
        public Option<string> Nickname { get; set; }
    }

    [Fact]
    public void Mapper_UnwrapsUnionSourceValues()
    {
        var source = new UnionSource { Name = "Ann", Age = 30, Score = 7, Nickname = "A" };

        var result = Mapper.Default.Map<UnionTarget>(source);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : "");
        Assert.Equal("Ann", result.Value.Name);
        Assert.Equal(30, result.Value.Age);
        Assert.Equal(7, result.Value.Score);
        Assert.Equal("A", result.Value.Nickname.Value);
    }

    private class FailedSource { public Result<string, string> Name { get; set; } }
    private class NameTarget { public string? Name { get; set; } }
    private class ItemsSource { public ValueOption<int> Items { get; set; } }
    private class ItemsTarget { public List<int>? Items { get; set; } }

    [Fact]
    public void Mapper_FailedResultSource_IsMappingError()
    {
        var result = Mapper.Default.Map<NameTarget>(new FailedSource { Name = Result<string, string>.Failure("ERR") });

        Assert.True(result.IsFailure);
        Assert.Equal(MappingErrorType.ConversionError, result.Error.ErrorType);
        Assert.Equal(nameof(NameTarget.Name), result.Error.PropertyPath);
    }

    [Fact]
    public void Mapper_FailedResultSource_IsSkippedWhenLenient()
    {
        var mapper = new Mapper(new MappingOptions { LenientMappingErrors = true });

        var result = mapper.Map<NameTarget>(new FailedSource { Name = Result<string, string>.Failure("ERR") });

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : "");
        Assert.Null(result.Value.Name);
    }

    [Fact]
    public void Mapper_EnumerableUnionIntoCollection_UsesCollectionPath()
    {
        var result = Mapper.Default.Map<ItemsTarget>(new ItemsSource { Items = ValueOption<int>.Some(3) });

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : "");
        Assert.Equal([3], result.Value.Items);
    }

    [Fact]
    public void Mapper_MapsNoneToNull()
    {
        var source = new UnionSource { Score = 1 };

        var result = Mapper.Default.Map<UnionTarget>(source);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : "");
        Assert.Null(result.Value.Name);
        Assert.True(result.Value.Nickname.IsNone);
    }
}
#endif
