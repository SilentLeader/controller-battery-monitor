using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

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
}

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
}

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

/// <summary>
/// SafeHandle wrapper for GLib allocated memory
/// </summary>
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

/// <summary>
/// RAII wrapper for GLib signal connections
/// </summary>
public sealed class SafeGSignalConnection : IDisposable
{
    private readonly SafeUPowerClientHandle _clientHandle;
    private readonly ulong _handlerId;
    private bool _disposed;
    
    public SafeGSignalConnection(SafeUPowerClientHandle clientHandle, ulong handlerId)
    {
        _clientHandle = clientHandle ?? throw new ArgumentNullException(nameof(clientHandle));
        _handlerId = handlerId;
    }
    
    public ulong HandlerId => _handlerId;
    public bool IsValid => !_disposed && !_clientHandle.IsInvalid && _handlerId != 0;
    
    public void Dispose()
    {
        if (_disposed) return;
        
        if (!_clientHandle.IsInvalid && _handlerId != 0)
        {
            UPowerNative.g_signal_handler_disconnect(_clientHandle.DangerousGetHandle(), _handlerId);
        }
        
        _disposed = true;
    }
}

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

/// <summary>
/// Exception thrown when a SafeHandle operation fails
/// </summary>
public class SafeHandleException : Exception
{
    public SafeHandleException(string message) : base(message) { }
    public SafeHandleException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Helper methods for working with SafeHandles
/// </summary>
internal static class SafeHandleExtensions
{
    /// <summary>
    /// Executes a function with the native handle, throwing if the handle is invalid
    /// </summary>
    public static T UseHandle<THandle, T>(this THandle safeHandle, Func<IntPtr, T> function) 
        where THandle : SafeHandle
    {
        if (safeHandle.IsInvalid)
            throw new SafeHandleException($"Cannot use invalid {typeof(THandle).Name}");
            
        return function(safeHandle.DangerousGetHandle());
    }
    
    /// <summary>
    /// Executes an action with the native handle, throwing if the handle is invalid
    /// </summary>
    public static void UseHandle<THandle>(this THandle safeHandle, Action<IntPtr> action) 
        where THandle : SafeHandle
    {
        if (safeHandle.IsInvalid)
            throw new SafeHandleException($"Cannot use invalid {typeof(THandle).Name}");
            
        action(safeHandle.DangerousGetHandle());
    }
}