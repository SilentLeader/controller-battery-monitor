using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace ControllerMonitor.UPower.Native;

/// <summary>
/// SafeHandle wrapper for UPower device objects
/// </summary>
public sealed class SafeUPowerDeviceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeUPowerDeviceHandle() : base(true) { }

    public SafeUPowerDeviceHandle(IntPtr handle) : base(true)
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

    public static SafeUPowerDeviceHandle CreateFromPtr(IntPtr devicePtr)
    {
        if (devicePtr == IntPtr.Zero)
            return new SafeUPowerDeviceHandle();

        // Increase reference count to take ownership
        var refPtr = UPowerNative.g_object_ref(devicePtr);
        return new SafeUPowerDeviceHandle(refPtr);
    }

    protected override void Dispose(bool disposing)
    {
        ReleaseHandle();
        base.Dispose(disposing);
    }
}
