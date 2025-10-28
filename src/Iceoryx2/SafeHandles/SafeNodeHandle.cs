using System;

namespace Iceoryx2.SafeHandles;

/// <summary>
/// Safe handle for Node resources.
/// </summary>
internal sealed class SafeNodeHandle : SafeIox2Handle
{
    public SafeNodeHandle() : base()
    {
    }

    public SafeNodeHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && handle != IntPtr.Zero)
        {
            Native.Iox2NativeMethods.iox2_node_drop(handle);
            return true;
        }
        return false;
    }
}