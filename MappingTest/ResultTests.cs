using LanguageExtended.Mapping;
using LanguageExtended.Result;

namespace MappingTest;

public class ResultTests
{
    // Distinct T and TError so the two implicit operators don't clash.
    private static readonly Result<int, string> Ok = Result<int, string>.Success(5);
    private static readonly Result<int, string> Fail = Result<int, string>.Failure("bad");

    [Fact]
    public void State_Value_Error()
    {
        Assert.True(Ok.IsSuccess);
        Assert.False(Ok.IsFailure);
        Assert.Equal(5, Ok.Value);
        Assert.Throws<InvalidOperationException>(() => Ok.Error);
        Assert.True(Ok.TryGetValue(out int v));
        Assert.Equal(5, v);
        Assert.False(Ok.TryGetError(out _));

        Assert.True(Fail.IsFailure);
        Assert.Equal("bad", Fail.Error);
        Assert.Throws<InvalidOperationException>(() => Fail.Value);
        Assert.False(Fail.TryGetValue(out _));
        Assert.True(Fail.TryGetError(out string? e));
        Assert.Equal("bad", e);
    }

    [Fact]
    public void Map_MapError_Bind()
    {
        Assert.Equal(10, Ok.Map(x => x * 2).Value);
        Assert.Equal("bad", Fail.Map(x => x * 2).Error);

        Assert.Equal(5, Ok.MapError(s => s.Length).Value);
        Assert.Equal(3, Fail.MapError(s => s.Length).Error);

        Assert.Equal("5", Ok.Bind(x => Result<string, string>.Success(x.ToString())).Value);
        Assert.Equal("no", Ok.Bind(_ => Result<string, string>.Failure("no")).Error);
        Assert.Equal("bad", Fail.Bind(x => Result<string, string>.Success(x.ToString())).Error);
    }

    [Fact]
    public void Match_Reduce_Ensure()
    {
        Assert.Equal(6, Ok.Match(x => x + 1, e => e.Length));
        Assert.Equal(3, Fail.Match(x => x + 1, e => e.Length));

        string? hit = null;
        Ok.Match(x => hit = $"ok {x}", e => hit = e);
        Assert.Equal("ok 5", hit);
        Fail.Match(x => hit = $"ok {x}", e => hit = e);
        Assert.Equal("bad", hit);

        Assert.Equal(5, Ok.Reduce(0));
        Assert.Equal(0, Fail.Reduce(0));
        Assert.Equal(5, Ok.Reduce(e => e.Length));
        Assert.Equal(3, Fail.Reduce(e => e.Length));

        Assert.True(Ok.Ensure(x => x > 1, "small").IsSuccess);
        Assert.Equal("small", Ok.Ensure(x => x > 9, "small").Error);
        Assert.Equal("bad", Fail.Ensure(_ => false, "small").Error);
    }

    [Fact]
    public void ImplicitConversions_And_Equality()
    {
        Result<int, string> ok = 5;
        Result<int, string> fail = "bad";
        Assert.Equal(Ok, ok);
        Assert.Equal(Fail, fail);

        Assert.True(Ok == ok);
        Assert.True(Ok.Equals((object)ok));
        Assert.False(Ok.Equals((object)5));
        Assert.True(Ok != Fail);
        Assert.True(Ok != Result<int, string>.Success(6));
        Assert.Equal(ok.GetHashCode(), Ok.GetHashCode());
    }

    [Fact]
    public void Extensions_IfSuccess_IfFailure_ToOption()
    {
        int s = 0, f = 0;
        Assert.Equal(Ok, Ok.IfSuccess(_ => s++).IfFailure(_ => f++));
        Assert.Equal(Fail, Fail.IfSuccess(_ => s++).IfFailure(_ => f++));
        Assert.Equal((1, 1), (s, f));

        Assert.Equal("v", Result<string, int>.Success("v").ToOption().Value);
        Assert.True(Result<string, int>.Failure(1).ToOption().IsNone);
    }

    private static Task<Result<int, string>> OkTask => Task.FromResult(Ok);
    private static Task<Result<int, string>> FailTask => Task.FromResult(Fail);

    [Fact]
    public async Task Task_Map_Bind_MapError()
    {
        Assert.Equal(10, (await OkTask.MapAsync(x => Task.FromResult(x * 2))).Value);
        Assert.Equal("bad", (await FailTask.MapAsync(x => Task.FromResult(x * 2))).Error);
        Assert.Equal(10, (await OkTask.MapAsync(x => x * 2)).Value);
        Assert.Equal("bad", (await FailTask.MapAsync(x => x * 2)).Error);

        Assert.Equal("5", (await OkTask.BindAsync(x => Task.FromResult(Result<string, string>.Success(x.ToString())))).Value);
        Assert.Equal("bad", (await FailTask.BindAsync(x => Task.FromResult(Result<string, string>.Success(x.ToString())))).Error);

        Assert.Equal(5, (await OkTask.MapErrorAsync(e => Task.FromResult(e.Length))).Value);
        Assert.Equal(3, (await FailTask.MapErrorAsync(e => Task.FromResult(e.Length))).Error);
    }

    [Fact]
    public async Task Task_IfSuccess_IfFailure_Reduce()
    {
        int s = 0, f = 0;
        await OkTask.IfSuccessAsync(_ => { s++; return Task.CompletedTask; });
        await FailTask.IfSuccessAsync(_ => { s++; return Task.CompletedTask; });
        await OkTask.IfFailureAsync(_ => { f++; return Task.CompletedTask; });
        Assert.True((await FailTask.IfFailureAsync(_ => { f++; return Task.CompletedTask; })).IsFailure);
        Assert.Equal((1, 1), (s, f));

        Assert.Equal(5, await OkTask.ReduceAsync(0));
        Assert.Equal(0, await FailTask.ReduceAsync(0));
        Assert.Equal(5, await OkTask.ReduceAsync(e => Task.FromResult(e.Length)));
        Assert.Equal(3, await FailTask.ReduceAsync(e => Task.FromResult(e.Length)));
    }

    [Fact]
    public async Task Task_Match_Ensure()
    {
        Assert.Equal("ok", await OkTask.MatchAsync(_ => Task.FromResult("ok"), e => Task.FromResult(e)));
        Assert.Equal("bad", await FailTask.MatchAsync(_ => Task.FromResult("ok"), e => Task.FromResult(e)));
        Assert.Equal("ok", await OkTask.MatchAsync(_ => "ok", e => e));
        Assert.Equal("bad", await FailTask.MatchAsync(_ => "ok", e => e));

        Assert.True((await OkTask.EnsureAsync(_ => Task.FromResult(true), "x")).IsSuccess);
        Assert.Equal("x", (await OkTask.EnsureAsync(_ => Task.FromResult(false), "x")).Error);
        Assert.Equal("bad", (await FailTask.EnsureAsync(_ => Task.FromResult(false), "x")).Error);
    }

    [Fact]
    public void MappingException_CarriesError()
    {
        MappingError error = new("boom", MappingErrorType.ConversionError, "A.B");
        Assert.Same(error, new MappingException(error).MappingError);
    }
}
