namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred during a receive operation.
    /// Receive operations retrieve samples from the subscriber's queue.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Subscriber is in invalid state</item>
    /// <item>Communication channel was closed or corrupted</item>
    /// <item>System resource error during receive</item>
    /// </list>
    /// Note: An empty queue (no samples available) is not an error and returns None/null.
    /// </remarks>
    public class ReceiveError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.ReceiveFailed;

        /// <summary>
        /// Gets additional details about why the receive operation failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to receive. Details: {Details}"
            : "Failed to receive.";

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public ReceiveError(string? details = null)
        {
            Details = details;
        }
    }
}