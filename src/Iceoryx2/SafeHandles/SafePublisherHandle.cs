using System;

namespace Iceoryx2.SafeHandles;

/// <summary>
/// Safe handle for Publisher resources.
/// </summary>
internal sealed class SafePublisherHandle : SafeIox2Handle
{
    public SafePublisherHandle() : base()
    {
    }

    public SafePublisherHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && handle != IntPtr.Zero)
        {
            Native.Iox2NativeMethods.iox2_publisher_drop(handle);
            return true;
        }
        return false;
    }
}