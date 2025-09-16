using System;

namespace ControllerMonitor.UPower.Native;

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
