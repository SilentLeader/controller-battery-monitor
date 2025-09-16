using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace ControllerMonitor.UPower.Native;

internal sealed class SafeGLibMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeGLibMemoryHandle() : base(true) { }
    
    public SafeGLibMemoryHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }
    
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            UPowerNative.g_free(handle);
        }
        return true;
    }
}
