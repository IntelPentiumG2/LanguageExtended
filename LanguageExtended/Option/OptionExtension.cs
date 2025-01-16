namespace LanguageExtended.Option;

public static class OptionExtension
{
    public static Option<T> ToOption<T>(this T? value) where T : class => 
        value is null ? Option<T>.None() : Option<T>.Some(value);
    
    public static Option<T> Where<T>(this T? value, Func<T, bool> predicate) where T : class =>
        value is null || !predicate(value) ? Option<T>.None() : Option<T>.Some(value);
}