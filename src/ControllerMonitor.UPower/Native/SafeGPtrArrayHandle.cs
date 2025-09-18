using Microsoft.Win32.SafeHandles;

namespace ControllerMonitor.UPower.Native;

/// <summary>
/// SafeHandle wrapper for GLib pointer arrays
/// </summary>
public sealed class SafeGPtrArrayHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeGPtrArrayHandle() : base(true) { }

    public SafeGPtrArrayHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            UPowerNative.g_ptr_array_unref(handle);
        }
        return true;
    }

    public uint Length => IsInvalid ? 0 : UPowerNative.g_ptr_array_len(handle);

    public IntPtr GetElement(uint index)
    {
        if (IsInvalid || index >= Length)
            return IntPtr.Zero;

        return UPowerNative.g_ptr_array_index(handle, index);
    }
}
