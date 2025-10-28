namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred during WaitSet run operation.
    /// The run operation waits for and processes events from attached sources.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>WaitSet is in invalid state</item>
    /// <item>Wait was interrupted by signal</item>
    /// <item>System error during event processing</item>
    /// <item>Callback function threw exception</item>
    /// </list>
    /// </remarks>
    public class WaitSetRunError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.WaitSetRunFailed;

        /// <summary>
        /// Gets additional details about why the run operation failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to run WaitSet. Details: {Details}"
            : "Failed to run WaitSet.";

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitSetRunError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public WaitSetRunError(string? details = null)
        {
            Details = details;
        }
    }
}