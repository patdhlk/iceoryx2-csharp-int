namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred during event service creation.
    /// Event services enable event-driven communication via notifiers and listeners.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Service already exists with incompatible settings</item>
    /// <item>Invalid service name</item>
    /// <item>Maximum number of event services reached</item>
    /// <item>Insufficient shared memory</item>
    /// </list>
    /// </remarks>
    public class EventServiceCreationError : Iox2Error
    {
        /// <summary>
        /// Gets the name of the event service that failed to create, if available.
        /// </summary>
        public string? ServiceName { get; }

        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.EventServiceCreationFailed;

        /// <summary>
        /// Gets additional details about why event service creation failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message including service name if available.
        /// </summary>
        public override string Message
        {
            get
            {
                var msg = ServiceName != null
                    ? $"Failed to create event service '{ServiceName}'"
                    : "Failed to create event service";
                return Details != null ? $"{msg}. Details: {Details}" : $"{msg}.";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventServiceCreationError"/> class.
        /// </summary>
        /// <param name="serviceName">The name of the event service that failed to create.</param>
        /// <param name="details">Optional details about the error.</param>
        public EventServiceCreationError(string? serviceName, string? details = null)
        {
            ServiceName = serviceName;
            Details = details;
        }
    }
}