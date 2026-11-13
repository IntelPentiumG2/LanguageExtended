# LanguageExtended — Unit release

This release adds `Unit` for results that carry no value, and a `Match` overload on `Result` that can return any type. Existing code keeps working unchanged.

## New

- **`Unit`** (namespace `LanguageExtended`): a type with a single value, for operations that succeed or fail without returning data. Use `Result<Unit, TError>` instead of `Result<bool, TError>`:
  ```csharp
  Result<Unit, string> Save(User user) =>
      db.TrySave(user) ? Unit.Value : "Could not save user.";
  ```
  It works with all `Result` members, the async extensions and the JSON converters. A success is written as `{"value":{}}`. If another library you use also defines `Unit` (MediatR, System.Reactive, LanguageExt), pick one with `using Unit = LanguageExtended.Unit;`.
- **`Result.Match<TResult>(onSuccess, onFailure)`** returns a value of any type, like `Option.Match<TResult>` and `Result.MatchAsync<TResult>` already do. Before, the synchronous `Match` had to return `T`:
  ```csharp
  IResult response = Save(user).Match(_ => Results.NoContent(), e => Results.BadRequest(e));
  ```

## Notes

- Calls where the lambdas return a type that converts to `T` may now pick the new overload, e.g. `Result<long, string>.Match(x => 1, e => 0)` returns `int` instead of `long`. Assigning the result to a `long` still works; only `var` sees the new type.
