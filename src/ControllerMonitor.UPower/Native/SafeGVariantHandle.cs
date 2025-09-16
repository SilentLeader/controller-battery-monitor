using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace ControllerMonitor.UPower.Native;

/// <summary>
/// SafeHandle wrapper for GVariant objects
/// </summary>
public sealed class SafeGVariantHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeGVariantHandle() : base(true) { }
    
    public SafeGVariantHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }
    
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            UPowerNative.g_variant_unref(handle);
        }
        return true;
    }
}
