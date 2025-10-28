namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred when loaning a response buffer.
    /// In request-response services, servers loan response buffers to send responses back to clients.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Out of response buffers (all buffers in use)</item>
    /// <item>Insufficient shared memory</item>
    /// <item>Server is in invalid state</item>
    /// </list>
    /// </remarks>
    public class ResponseLoanError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.ResponseLoanFailed;

        /// <summary>
        /// Gets additional details about why the response loan failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to loan response. Details: {Details}"
            : "Failed to loan response.";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseLoanError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public ResponseLoanError(string? details = null)
        {
            Details = details;
        }
    }
}