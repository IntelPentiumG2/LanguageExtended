using LanguageExtended.Option;
using LanguageExtended.Result;

namespace MappingTest;

public class OptionTests
{
    // Not string: string is IEnumerable<char>, so T?.Where(...) would collide with LINQ.
    private record Item(int Id);

    private static readonly Option<string> Some = Option<string>.Some("a");
    private static readonly Option<string> None = Option<string>.None();

    [Fact]
    public void State_And_Value()
    {
        Assert.True(Some.IsSome);
        Assert.False(Some.IsNone);
        Assert.Equal("a", Some.Value);
        Assert.True(Some.TryGetValue(out string? v));
        Assert.Equal("a", v);

        Assert.True(None.IsNone);
        Assert.False(None.TryGetValue(out _));
        Assert.Throws<InvalidOperationException>(() => None.Value);
        Assert.True(default(Option<string>).IsNone);
    }

    [Fact]
    public void Map_And_MapOptional()
    {
        Assert.Equal("A", Some.Map(s => s.ToUpper()).Value);
        Assert.True(None.Map(s => s.ToUpper()).IsNone);
        Assert.Equal("ab", Some.MapOptional(s => Option<string>.Some(s + "b")).Value);
        Assert.True(Some.MapOptional(_ => Option<string>.None()).IsNone);
        Assert.True(None.MapOptional(s => Option<string>.Some(s)).IsNone);
    }

    [Fact]
    public void Match_AllOverloads()
    {
        Assert.Equal("a!", Some.Match(s => s + "!", () => "none"));
        Assert.Equal("none", None.Match(s => s + "!", () => "none"));

        Assert.Equal(1, Some.Match(s => s.Length, () => -1));
        Assert.Equal(-1, None.Match(s => s.Length, () => -1));

        string? hit = null;
        Some.Match(s => { hit = s; }, () => { hit = "none"; });
        Assert.Equal("a", hit);
        None.Match(s => { hit = s; }, () => { hit = "none"; });
        Assert.Equal("none", hit);
    }

    [Fact]
    public void IfSome_IfNone()
    {
        int some = 0, none = 0;
        Assert.Equal(Some, Some.IfSome(_ => some++).IfNone(() => none++));
        Assert.Equal(None, None.IfSome(_ => some++).IfNone(() => none++));
        Assert.Equal(1, some);
        Assert.Equal(1, none);
    }

    [Fact]
    public void Reduce_Where_OrElse()
    {
        Assert.Equal("a", Some.Reduce("d"));
        Assert.Equal("d", None.Reduce("d"));
        Assert.Equal("a", Some.Reduce(() => "d"));
        Assert.Equal("d", None.Reduce(() => "d"));

        Assert.True(Some.Where(s => s == "a").IsSome);
        Assert.True(Some.Where(s => s == "b").IsNone);
        Assert.True(None.Where(_ => true).IsNone);
        Assert.True(Some.WhereNot(s => s == "b").IsSome);
        Assert.True(Some.WhereNot(s => s == "a").IsNone);
        Assert.True(None.WhereNot(_ => false).IsNone);

        Option<string> other = Option<string>.Some("o");
        Assert.Equal(Some, Some.OrElse(other));
        Assert.Equal(other, None.OrElse(other));
        Assert.Equal(Some, Some.OrElse(() => other));
        Assert.Equal(other, None.OrElse(() => other));
    }

    [Fact]
    public void Conversions()
    {
        Assert.Equal("a", Some.ToResult(42).Value);
        Assert.Equal(42, None.ToResult(42).Error);
        Assert.Equal("a", Some.ToNullable());
        Assert.Null(None.ToNullable());
        Assert.Equal(["a"], Some.AsEnumerable());
        Assert.Empty(None.AsEnumerable());
        Assert.True(((Option<string>)"x").IsSome);
        Assert.True(((Option<string>)(string?)null).IsNone);
    }

    [Fact]
    public void Equality()
    {
        Option<string> same = Option<string>.Some("a");
        Assert.True(Some == same);
        Assert.True(Some.Equals((object)same));
        Assert.False(Some.Equals((object)"a"));
        Assert.True(Some != None);
        Assert.True(None != Some);
        Assert.True(None == Option<string>.None());
        Assert.False(Some == Option<string>.Some("b"));
        Assert.Equal(same.GetHashCode(), Some.GetHashCode());
        Assert.Equal(0, None.GetHashCode());
    }

    [Fact]
    public void ValueOption_State_And_Value()
    {
        ValueOption<int> some = ValueOption<int>.Some(5);
        ValueOption<int> none = ValueOption<int>.None();

        Assert.True(some.IsSome);
        Assert.False(some.IsNone);
        Assert.Equal(5, some.Value);
        Assert.True(some.TryGetValue(out int v));
        Assert.Equal(5, v);
        Assert.True(none.IsNone);
        Assert.False(none.TryGetValue(out _));
        Assert.Throws<InvalidOperationException>(() => none.Value);
    }

    [Fact]
    public void ValueOption_Operations()
    {
        ValueOption<int> some = ValueOption<int>.Some(5);
        ValueOption<int> none = ValueOption<int>.None();

        Assert.Equal(10, some.Map(x => x * 2).Value);
        Assert.True(none.Map(x => x * 2).IsNone);
        Assert.Equal("5", some.Match(x => x.ToString(), () => "none"));
        Assert.Equal("none", none.Match(x => x.ToString(), () => "none"));

        int hit = 0;
        some.Match(x => { hit = x; }, () => { hit = -1; });
        Assert.Equal(5, hit);
        none.Match(x => { hit = x; }, () => { hit = -1; });
        Assert.Equal(-1, hit);

        Assert.Equal(5, some.Reduce(0));
        Assert.Equal(0, none.Reduce(0));
        Assert.Equal(5, some.Reduce(() => 0));
        Assert.Equal(0, none.Reduce(() => 0));

        Assert.True(some.Where(x => x > 1).IsSome);
        Assert.True(some.Where(x => x > 9).IsNone);
        Assert.True(none.Where(_ => true).IsNone);

        int s = 0, n = 0;
        some.IfSome(_ => s++).IfNone(() => n++);
        none.IfSome(_ => s++).IfNone(() => n++);
        Assert.Equal((1, 1), (s, n));

        Assert.Equal(5, some.ToNullable());
        Assert.Null(none.ToNullable());
        Assert.Equal([5], some.AsEnumerable());
        Assert.Empty(none.AsEnumerable());
    }

    [Fact]
    public void ValueOption_Conversions_And_Equality()
    {
        ValueOption<int> implicitSome = 5;
        Assert.Equal(5, implicitSome.Value);
        Assert.True(((ValueOption<int>)(int?)7).IsSome);
        Assert.True(((ValueOption<int>)(int?)null).IsNone);

        ValueOption<int> some = ValueOption<int>.Some(5);
        ValueOption<int> none = ValueOption<int>.None();
        Assert.True(some == implicitSome);
        Assert.True(some.Equals((object)implicitSome));
        Assert.False(some.Equals((object)5));
        Assert.True(some != none);
        Assert.True(some != ValueOption<int>.Some(6));
        Assert.True(none == ValueOption<int>.None());
        Assert.Equal(5.GetHashCode(), some.GetHashCode());
        Assert.Equal(0, none.GetHashCode());
    }

    [Fact]
    public void Extension_ToOption_And_Where()
    {
        Item? item = new(1);
        Item? missing = null;

        Assert.Equal(item, item.ToOption().Value);
        Assert.True(missing.ToOption().IsNone);
        Assert.True(item.Where(i => i.Id == 1).IsSome);
        Assert.True(item.Where(i => i.Id == 2).IsNone);
        Assert.True(missing.Where(_ => true).IsNone);
    }

    [Fact]
    public void Extension_SequenceHelpers()
    {
        List<string> items = ["a", "bb", "cc"];
        List<string> empty = [];

        Assert.Equal("a", items.FirstOrNone().Value);
        Assert.True(empty.FirstOrNone().IsNone);
        Assert.Equal("bb", items.FirstOrNone(s => s.Length == 2).Value);
        Assert.True(items.FirstOrNone(s => s.Length == 9).IsNone);

        Assert.Equal("cc", items.LastOrNone().Value);
        Assert.True(empty.LastOrNone().IsNone);
        Assert.Equal("cc", items.LastOrNone(s => s.Length == 2).Value);
        Assert.True(items.LastOrNone(s => s.Length == 9).IsNone);

        Assert.Equal("a", new List<string> { "a" }.SingleOrNone().Value);
        Assert.True(empty.SingleOrNone().IsNone);
        Assert.Throws<InvalidOperationException>(() => items.SingleOrNone());

        Assert.Equal("a", items.SingleOrNone(s => s.Length == 1).Value);
        Assert.True(items.SingleOrNone(s => s.Length == 9).IsNone);
        Assert.Throws<InvalidOperationException>(() => items.SingleOrNone(s => s.Length == 2));

        Assert.Equal(["a", "b"], new[] { Option<string>.Some("a"), Option<string>.None(), Option<string>.Some("b") }.Values());
    }

    private static Task<Option<string>> SomeTask => Task.FromResult(Some);
    private static Task<Option<string>> NoneTask => Task.FromResult(None);

    [Fact]
    public async Task Task_Map_Bind()
    {
        Assert.Equal("A", (await SomeTask.MapAsync(s => Task.FromResult(s.ToUpper()))).Value);
        Assert.True((await NoneTask.MapAsync(s => Task.FromResult(s.ToUpper()))).IsNone);
        Assert.Equal("A", (await SomeTask.MapAsync(s => s.ToUpper())).Value);
        Assert.True((await NoneTask.MapAsync(s => s.ToUpper())).IsNone);

        Assert.Equal("ab", (await SomeTask.BindAsync(s => Task.FromResult(Option<string>.Some(s + "b")))).Value);
        Assert.True((await NoneTask.BindAsync(s => Task.FromResult(Option<string>.Some(s)))).IsNone);
    }

    [Fact]
    public async Task Task_Match()
    {
        Assert.Equal(1, await SomeTask.MatchAsync(s => Task.FromResult(s.Length), () => Task.FromResult(-1)));
        Assert.Equal(-1, await NoneTask.MatchAsync(s => Task.FromResult(s.Length), () => Task.FromResult(-1)));
        Assert.Equal(1, await SomeTask.MatchAsync(s => s.Length, () => -1));
        Assert.Equal(-1, await NoneTask.MatchAsync(s => s.Length, () => -1));
    }

    [Fact]
    public async Task Task_IfSome_IfNone_Reduce_Where()
    {
        int some = 0, none = 0;
        await SomeTask.IfSomeAsync(_ => { some++; return Task.CompletedTask; });
        await NoneTask.IfSomeAsync(_ => { some++; return Task.CompletedTask; });
        await SomeTask.IfNoneAsync(() => { none++; return Task.CompletedTask; });
        Assert.True((await NoneTask.IfNoneAsync(() => { none++; return Task.CompletedTask; })).IsNone);
        Assert.Equal((1, 1), (some, none));

        Assert.Equal("a", await SomeTask.ReduceAsync("d"));
        Assert.Equal("d", await NoneTask.ReduceAsync("d"));
        Assert.Equal("a", await SomeTask.ReduceAsync(() => Task.FromResult("d")));
        Assert.Equal("d", await NoneTask.ReduceAsync(() => Task.FromResult("d")));

        Assert.True((await SomeTask.WhereAsync(_ => Task.FromResult(true))).IsSome);
        Assert.True((await SomeTask.WhereAsync(_ => Task.FromResult(false))).IsNone);
        Assert.True((await NoneTask.WhereAsync(_ => Task.FromResult(true))).IsNone);
    }
}
