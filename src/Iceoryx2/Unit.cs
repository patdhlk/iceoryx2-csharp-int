namespace Iceoryx2;

/// <summary>
/// Represents a unit type commonly used to indicate the absence of a meaningful value.
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// Represents the singleton instance of the <see cref="Unit"/> struct.
    /// It is used to signify the absence of a meaningful value in operations
    /// that return a result without a tangible return value, commonly used in
    /// conjunction with the <see cref="Result{T, E}"/> struct.
    /// </summary>
    public static readonly Unit Value = new();
}