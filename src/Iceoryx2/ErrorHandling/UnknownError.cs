namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an unknown or unclassified error.
    /// This error type is used when a specific error cannot be determined or classified.
    /// </summary>
    /// <remarks>
    /// Possible scenarios:
    /// <list type="bullet">
    /// <item>Unexpected error code from native library</item>
    /// <item>Internal state corruption</item>
    /// <item>Unhandled edge case</item>
    /// </list>
    /// If you encounter this error frequently, it may indicate a bug. Please report it
    /// with the details to help improve error classification.
    /// </remarks>
    public class UnknownError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.Unknown;

        /// <summary>
        /// Gets additional details about the unknown error, if available.
        /// This may contain diagnostic information to help identify the root cause.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"An unknown error occurred. Details: {Details}"
            : "An unknown error occurred.";

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error for diagnostic purposes.</param>
        public UnknownError(string? details = null)
        {
            Details = details;
        }
    }
}