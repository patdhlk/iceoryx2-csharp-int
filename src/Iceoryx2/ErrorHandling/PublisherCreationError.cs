namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred during publisher creation.
    /// Publishers send data samples to subscribers via shared memory.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Maximum number of publishers for the service reached</item>
    /// <item>Insufficient shared memory for publisher metadata</item>
    /// <item>Service does not exist or has incompatible type</item>
    /// </list>
    /// </remarks>
    public class PublisherCreationError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.PublisherCreationFailed;

        /// <summary>
        /// Gets additional details about why publisher creation failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to create publisher. Details: {Details}"
            : "Failed to create publisher.";

        /// <summary>
        /// Initializes a new instance of the <see cref="PublisherCreationError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public PublisherCreationError(string? details = null)
        {
            Details = details;
        }
    }
}