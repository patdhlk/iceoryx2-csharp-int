namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred during WaitSet creation.
    /// WaitSets enable efficient multiplexing of multiple event sources.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Invalid WaitSet configuration</item>
    /// <item>Insufficient system resources</item>
    /// <item>Maximum capacity exceeded</item>
    /// </list>
    /// </remarks>
    public class WaitSetCreationError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.WaitSetCreationFailed;

        /// <summary>
        /// Gets additional details about why WaitSet creation failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to create WaitSet. Details: {Details}"
            : "Failed to create WaitSet.";

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitSetCreationError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public WaitSetCreationError(string? details = null)
        {
            Details = details;
        }
    }
}