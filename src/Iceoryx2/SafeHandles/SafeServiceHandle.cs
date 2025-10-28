using System;

namespace Iceoryx2.SafeHandles;

/// <summary>
/// Safe handle for Service resources (Port Factory).
/// </summary>
internal sealed class SafeServiceHandle : SafeIox2Handle
{
    public SafeServiceHandle() : base()
    {
    }

    public SafeServiceHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && handle != IntPtr.Zero)
        {
            Native.Iox2NativeMethods.iox2_port_factory_pub_sub_drop(handle);
            return true;
        }
        return false;
    }
}