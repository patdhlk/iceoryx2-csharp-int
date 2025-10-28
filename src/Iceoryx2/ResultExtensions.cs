namespace Iceoryx2;

/// <summary>
/// Extension methods for Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a nullable reference to a Result.
    /// </summary>
    public static Result<T, E> ToResult<T, E>(this T? value, E error) where T : class
    {
        return value != null ? Result<T, E>.Ok(value) : Result<T, E>.Err(error);
    }
}