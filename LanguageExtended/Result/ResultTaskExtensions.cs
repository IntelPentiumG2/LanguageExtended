// ReSharper disable UnusedMember.Global
namespace LanguageExtended.Result;

/// <summary>
/// Provides async extension methods for <see cref="Task{T}"/> of <see cref="Result{T,TError}"/>,
/// enabling pipeline composition without manual awaiting at each step.
/// </summary>
public static class ResultTaskExtensions
{
    /// <summary>
    /// Asynchronously maps the success value of the Result inside the Task using the provided async function.
    /// Failures pass through unchanged.
    /// </summary>
    /// <typeparam name="T">The source success value type.</typeparam>
    /// <typeparam name="TResult">The result success value type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="resultTask">The task producing a Result.</param>
    /// <param name="map">An async function to transform the success value.</param>
    public static async Task<Result<TResult, TError>> MapAsync<T, TResult, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task<TResult>> map)
    {
        Result<T, TError> result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? Result<TResult, TError>.Success(await map(result.Value).ConfigureAwait(false))
            : Result<TResult, TError>.Failure(result.Error);
    }

    /// <summary>
    /// Asynchronously maps the success value of the Result inside the Task using a synchronous function.
    /// </summary>
    public static async Task<Result<TResult, TError>> MapAsync<T, TResult, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, TResult> map)
    {
        Result<T, TError> result = await resultTask.ConfigureAwait(false);
        return result.Map(map);
    }

    /// <summary>
    /// Asynchronously chains a function returning a new Result, passing failures through unchanged.
    /// Use this to compose a sequence of operations where each step can independently fail.
    /// </summary>
    /// <typeparam name="T">The source success value type.</typeparam>
    /// <typeparam name="TResult">The result success value type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="resultTask">The task producing a Result.</param>
    /// <param name="bind">An async function that takes the success value and returns a new Result.</param>
    public static async Task<Result<TResult, TError>> BindAsync<T, TResult, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task<Result<TResult, TError>>> bind)
    {
        Result<T, TError> result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await bind(result.Value).ConfigureAwait(false)
            : Result<TResult, TError>.Failure(result.Error);
    }

    extension<T, TError>(Task<Result<T, TError>> resultTask)
    {
        /// <summary>
        /// Asynchronously executes an action if the Result inside the Task is a success, then returns the Result.
        /// </summary>
        public async Task<Result<T, TError>> IfSuccessAsync(Func<T, Task> action)
        {
            Result<T, TError> result = await resultTask.ConfigureAwait(false);
            if (result.IsSuccess)
                await action(result.Value).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Asynchronously executes an action if the Result inside the Task is a failure, then returns the Result.
        /// </summary>
        public async Task<Result<T, TError>> IfFailureAsync(Func<TError, Task> action)
        {
            Result<T, TError> result = await resultTask.ConfigureAwait(false);
            if (result.IsFailure)
                await action(result.Error).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Asynchronously maps the error of the Result inside the Task using the provided async function.
        /// Successes pass through unchanged.
        /// </summary>
        public async Task<Result<T, TResultError>> MapErrorAsync<TResultError>(Func<TError, Task<TResultError>> map)
        {
            Result<T, TError> result = await resultTask.ConfigureAwait(false);
            return result.IsFailure
                ? Result<T, TResultError>.Failure(await map(result.Error).ConfigureAwait(false))
                : Result<T, TResultError>.Success(result.Value);
        }

        /// <summary>
        /// Asynchronously returns the success value if present; otherwise, returns <paramref name="defaultValue"/>.
        /// </summary>
        public async Task<T> ReduceAsync(T defaultValue)
        {
            Result<T, TError> result = await resultTask.ConfigureAwait(false);
            return result.Reduce(defaultValue);
        }

        /// <summary>
        /// Asynchronously returns the success value if present; otherwise, invokes the async fallback with the error.
        /// </summary>
        public async Task<T> ReduceAsync(Func<TError, Task<T>> fallback)
        {
            Result<T, TError> result = await resultTask.ConfigureAwait(false);
            return result.IsSuccess ? result.Value : await fallback(result.Error).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously matches the Result inside the Task, producing a value of type <typeparamref name="TResult"/>.
        /// </summary>
        public async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess,
            Func<TError, Task<TResult>> onFailure)
        {
            Result<T, TError> result = await resultTask.ConfigureAwait(false);
            return result.IsSuccess
                ? await onSuccess(result.Value).ConfigureAwait(false)
                : await onFailure(result.Error).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously matches the Result inside the Task using synchronous functions.
        /// </summary>
        public async Task<TResult> MatchAsync<TResult>(Func<T, TResult> onSuccess,
            Func<TError, TResult> onFailure)
        {
            Result<T, TError> result = await resultTask.ConfigureAwait(false);
            return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
        }

        /// <summary>
        /// Asynchronously validates the success value against an async predicate.
        /// If the predicate fails, converts the Result to a failure. Has no effect on failures.
        /// </summary>
        public async Task<Result<T, TError>> EnsureAsync(Func<T, Task<bool>> predicate,
            TError error)
        {
            Result<T, TError> result = await resultTask.ConfigureAwait(false);
            if (!result.IsSuccess) return result;
            return await predicate(result.Value).ConfigureAwait(false)
                ? result
                : Result<T, TError>.Failure(error);
        }
    }
}
