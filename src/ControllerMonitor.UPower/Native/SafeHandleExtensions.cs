using Microsoft.Win32.SafeHandles;
using System;

namespace ControllerMonitor.UPower.Native;

/// <summary>
/// Helper methods for working with SafeHandles
/// </summary>
internal static class SafeHandleExtensions
{
    /// <summary>
    /// Executes a function with the native handle, throwing if the handle is invalid
    /// </summary>
    public static T UseHandle<THandle, T>(this THandle safeHandle, Func<IntPtr, T> function) 
        where THandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        if (safeHandle.IsInvalid)
            throw new SafeHandleException($"Cannot use invalid {typeof(THandle).Name}");
            
        return function(safeHandle.DangerousGetHandle());
    }
    
    /// <summary>
    /// Executes an action with the native handle, throwing if the handle is invalid
    /// </summary>
    public static void UseHandle<THandle>(this THandle safeHandle, Action<IntPtr> action) 
        where THandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        if (safeHandle.IsInvalid)
            throw new SafeHandleException($"Cannot use invalid {typeof(THandle).Name}");
            
        action(safeHandle.DangerousGetHandle());
    }
}
