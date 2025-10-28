namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred when sending a request to a server.
    /// After loaning and writing a request, clients send it to the server via shared memory.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>No active servers available</item>
    /// <item>Server queue is full</item>
    /// <item>Communication channel corrupted</item>
    /// </list>
    /// </remarks>
    public class RequestSendError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.RequestSendFailed;

        /// <summary>
        /// Gets additional details about why the request send failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to send request. Details: {Details}"
            : "Failed to send request.";

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSendError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public RequestSendError(string? details = null)
        {
            Details = details;
        }
    }
}