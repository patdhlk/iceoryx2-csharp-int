namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred when sending a response back to a client.
    /// After loaning and writing a response, servers send it back to the requesting client.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Client disconnected or no longer exists</item>
    /// <item>Client response queue is full</item>
    /// <item>Communication channel corrupted</item>
    /// </list>
    /// </remarks>
    public class ResponseSendError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.ResponseSendFailed;

        /// <summary>
        /// Gets additional details about why the response send failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to send response. Details: {Details}"
            : "Failed to send response.";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseSendError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public ResponseSendError(string? details = null)
        {
            Details = details;
        }
    }
}