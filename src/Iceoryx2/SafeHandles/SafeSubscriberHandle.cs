using System;

namespace Iceoryx2.SafeHandles;

/// <summary>
/// Safe handle for Subscriber resources.
/// </summary>
internal sealed class SafeSubscriberHandle : SafeIox2Handle
{
    public SafeSubscriberHandle() : base()
    {
    }

    public SafeSubscriberHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && handle != IntPtr.Zero)
        {
            Native.Iox2NativeMethods.iox2_subscriber_drop(handle);
            return true;
        }
        return false;
    }
}