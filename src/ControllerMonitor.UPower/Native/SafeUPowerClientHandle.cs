using Microsoft.Win32.SafeHandles;

namespace ControllerMonitor.UPower.Native;

/// <summary>
/// SafeHandle wrapper for UPower client objects
/// </summary>
public sealed class SafeUPowerClientHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeUPowerClientHandle() : base(true) { }

    public SafeUPowerClientHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            UPowerNative.g_object_unref(handle);
        }
        return true;
    }

    public static SafeUPowerClientHandle CreateClient()
    {
        var clientPtr = UPowerNative.up_client_new();
        return new SafeUPowerClientHandle(clientPtr);
    }
    
    protected override void Dispose(bool disposing)
    {
        ReleaseHandle();
        base.Dispose(disposing);
    }
}
