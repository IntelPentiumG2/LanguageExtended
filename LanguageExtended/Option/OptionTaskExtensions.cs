// ReSharper disable UnusedMember.Global
namespace LanguageExtended.Option;

/// <summary>
/// Provides async extension methods for <see cref="Task{T}"/> of <see cref="Option{T}"/>,
/// enabling pipeline composition without manual awaiting at each step.
/// </summary>
public static class OptionTaskExtensions
{
    /// <summary>
    /// Asynchronously maps the value of the Option inside the Task using the provided async function.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="optionTask">The task producing an Option.</param>
    /// <param name="map">An async function to transform the value.</param>
    /// <returns>A task containing a new Option with the mapped value, or None if the source was None.</returns>
    public static async Task<Option<TResult>> MapAsync<T, TResult>(
        this Task<Option<T>> optionTask,
        Func<T, Task<TResult>> map)
        where T : class where TResult : class
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.IsSome
            ? Option<TResult>.Some(await map(option.Value).ConfigureAwait(false))
            : Option<TResult>.None();
    }

    /// <summary>
    /// Asynchronously maps the value of the Option inside the Task using a synchronous function.
    /// </summary>
    public static async Task<Option<TResult>> MapAsync<T, TResult>(
        this Task<Option<T>> optionTask,
        Func<T, TResult> map)
        where T : class where TResult : class
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.Map(map);
    }

    /// <summary>
    /// Asynchronously flat-maps the Option inside the Task, chaining a function that itself returns an Option.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="optionTask">The task producing an Option.</param>
    /// <param name="bind">An async function returning an Option of the result type.</param>
    /// <returns>A task containing the chained Option.</returns>
    public static async Task<Option<TResult>> BindAsync<T, TResult>(
        this Task<Option<T>> optionTask,
        Func<T, Task<Option<TResult>>> bind)
        where T : class where TResult : class
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.IsSome
            ? await bind(option.Value).ConfigureAwait(false)
            : Option<TResult>.None();
    }

    /// <summary>
    /// Asynchronously matches the Option inside the Task, returning a result of a different type.
    /// </summary>
    /// <typeparam name="T">The Option value type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="optionTask">The task producing an Option.</param>
    /// <param name="onSome">An async function invoked when the Option has a value.</param>
    /// <param name="onNone">An async function invoked when the Option has no value.</param>
    public static async Task<TResult> MatchAsync<T, TResult>(
        this Task<Option<T>> optionTask,
        Func<T, Task<TResult>> onSome,
        Func<Task<TResult>> onNone)
        where T : class
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.IsSome
            ? await onSome(option.Value).ConfigureAwait(false)
            : await onNone().ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously matches the Option inside the Task using synchronous functions.
    /// </summary>
    public static async Task<TResult> MatchAsync<T, TResult>(
        this Task<Option<T>> optionTask,
        Func<T, TResult> onSome,
        Func<TResult> onNone)
        where T : class
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.Match(onSome, onNone);
    }

    /// <summary>
    /// Asynchronously executes an action if the Option inside the Task has a value, then returns the Option.
    /// </summary>
    public static async Task<Option<T>> IfSomeAsync<T>(
        this Task<Option<T>> optionTask,
        Func<T, Task> action)
        where T : class
    {
        var option = await optionTask.ConfigureAwait(false);
        if (option.IsSome)
            await action(option.Value).ConfigureAwait(false);
        return option;
    }

    /// <summary>
    /// Asynchronously executes an action if the Option inside the Task has no value, then returns the Option.
    /// </summary>
    public static async Task<Option<T>> IfNoneAsync<T>(
        this Task<Option<T>> optionTask,
        Func<Task> action)
        where T : class
    {
        var option = await optionTask.ConfigureAwait(false);
        if (option.IsNone)
            await action().ConfigureAwait(false);
        return option;
    }

    /// <summary>
    /// Asynchronously returns the Option's value if present; otherwise, returns the provided default value.
    /// </summary>
    public static async Task<T> ReduceAsync<T>(
        this Task<Option<T>> optionTask,
        T defaultValue)
        where T : class
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.Reduce(defaultValue);
    }

    /// <summary>
    /// Asynchronously returns the Option's value if present; otherwise, invokes the async factory.
    /// </summary>
    public static async Task<T> ReduceAsync<T>(
        this Task<Option<T>> optionTask,
        Func<Task<T>> defaultValue)
        where T : class
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.IsSome ? option.Value : await defaultValue().ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously filters the Option inside the Task based on an async predicate.
    /// </summary>
    public static async Task<Option<T>> WhereAsync<T>(
        this Task<Option<T>> optionTask,
        Func<T, Task<bool>> predicate)
        where T : class
    {
        var option = await optionTask.ConfigureAwait(false);
        if (option.IsNone) return option;
        return await predicate(option.Value).ConfigureAwait(false) ? option : Option<T>.None();
    }
}
