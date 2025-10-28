namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred during service creation.
    /// Services are the communication channels in iceoryx2 (publish-subscribe, event, request-response).
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Service with this name already exists with incompatible settings</item>
    /// <item>Invalid service name or configuration</item>
    /// <item>Insufficient shared memory</item>
    /// <item>Maximum number of services reached</item>
    /// </list>
    /// </remarks>
    public class ServiceCreationError : Iox2Error
    {
        /// <summary>
        /// Gets the name of the service that failed to create, if available.
        /// </summary>
        public string? ServiceName { get; }

        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.ServiceCreationFailed;

        /// <summary>
        /// Gets additional details about why service creation failed.
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
                    ? $"Failed to create service '{ServiceName}'"
                    : "Failed to create service";
                return Details != null ? $"{msg}. Details: {Details}" : $"{msg}.";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceCreationError"/> class.
        /// </summary>
        /// <param name="serviceName">The name of the service that failed to create.</param>
        /// <param name="details">Optional details about the error.</param>
        public ServiceCreationError(string? serviceName, string? details = null)
        {
            ServiceName = serviceName;
            Details = details;
        }
    }
}