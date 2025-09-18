namespace ControllerMonitor.UPower.Native;

/// <summary>
/// Managed wrapper for native string pointers
/// </summary>
internal readonly struct SafeNativeString : IDisposable
{
    private readonly IntPtr _ptr;
    private readonly bool _shouldFree;
    
    public SafeNativeString(IntPtr ptr, bool shouldFree = true)
    {
        _ptr = ptr;
        _shouldFree = shouldFree;
    }
    
    public string? Value => UPowerNative.PtrToStringUTF8(_ptr);
    
    public void Dispose()
    {
        if (_shouldFree && _ptr != IntPtr.Zero)
        {
            UPowerNative.g_free(_ptr);
        }
    }
    
    public static implicit operator string?(SafeNativeString nativeString)
    {
        return nativeString.Value;
    }
}
