using ControllerMonitor.UPower.Exceptions;
using ControllerMonitor.UPower.Models;
using ControllerMonitor.UPower.Native;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace ControllerMonitor.UPower.Services;

/// <summary>
/// Service for monitoring UPower events in real-time using GLib signals
/// </summary>
public sealed class UPowerEventMonitor : IDisposable
{
    private readonly ILogger<UPowerEventMonitor> _logger;
    private readonly UPowerConfiguration _configuration;
    private readonly UPowerClient _client;
    private readonly UPowerPropertyConverter _propertyConverter;
    
    private readonly ConcurrentQueue<BatteryDeviceEvent> _eventQueue = new();
    private readonly Timer _eventProcessingTimer;
    private readonly SemaphoreSlim _monitoringSemaphore = new(1, 1);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private SafeUPowerClientHandle? _clientHandle;
    private SafeGSignalConnection? _deviceAddedConnection;
    private SafeGSignalConnection? _deviceRemovedConnection;
    
    private bool _isMonitoring;
    private bool _disposed;
    private DateTimeOffset _lastEventProcessed = DateTimeOffset.UtcNow;
    
    // Delegate for signal callbacks
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DeviceSignalCallback(IntPtr client, IntPtr device, IntPtr userData);
    
    // GC handles for callback delegates to prevent collection
    private GCHandle _deviceAddedCallbackHandle;
    private GCHandle _deviceRemovedCallbackHandle;
    private GCHandle _thisHandle;
    
    /// <summary>
    /// Event fired when a device is added
    /// </summary>
    public event EventHandler<BatteryDeviceEventArgs>? DeviceAdded;
    
    /// <summary>
    /// Event fired when a device is removed
    /// </summary>
    public event EventHandler<BatteryDeviceEventArgs>? DeviceRemoved;
    
    /// <summary>
    /// Event fired when a device's properties change
    /// </summary>
    public event EventHandler<BatteryDeviceEventArgs>? DeviceChanged;
    
    /// <summary>
    /// Gets whether event monitoring is currently active
    /// </summary>
    public bool IsMonitoring => _isMonitoring && !_disposed;
    
    public UPowerEventMonitor(
        ILogger<UPowerEventMonitor> logger,
        IOptions<UPowerConfiguration> configuration,
        UPowerClient client,
        UPowerPropertyConverter propertyConverter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _propertyConverter = propertyConverter ?? throw new ArgumentNullException(nameof(propertyConverter));
        
        // Create GC handle for this instance to prevent collection during callbacks
        _thisHandle = GCHandle.Alloc(this, GCHandleType.Weak);
        
        // Setup event processing timer with debouncing
        _eventProcessingTimer = new Timer(ProcessEventQueue, null, 
            _configuration.EventDebounceInterval,
            _configuration.EventDebounceInterval);
    }
    
    /// <summary>
    /// Starts monitoring UPower events
    /// </summary>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerEventMonitor));
            
        await _monitoringSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isMonitoring)
            {
                _logger.LogDebug("Event monitoring is already active");
                return;
            }
            
            if (!_configuration.EnableEventMonitoring)
            {
                _logger.LogInformation("Event monitoring is disabled in configuration");
                return;
            }
            
            _logger.LogInformation("Starting UPower event monitoring");
            
            // Ensure the client is initialized
            if (!await _client.InitializeAsync(cancellationToken))
            {
                throw new UPowerEventMonitoringException("Failed to initialize UPower client for event monitoring");
            }
            
            // Get the client handle for signal connections
            _clientHandle = GetClientHandle();
            if (_clientHandle?.IsInvalid != false)
            {
                throw new UPowerEventMonitoringException("Invalid UPower client handle for event monitoring");
            }
            
            // Connect to UPower signals
            await ConnectSignalsAsync(cancellationToken);
            
            _isMonitoring = true;
            _logger.LogInformation("UPower event monitoring started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start UPower event monitoring");
            await CleanupSignalConnectionsAsync();
            throw UPowerExceptionHelpers.WrapException(ex, "StartMonitoringAsync");
        }
        finally
        {
            _monitoringSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Stops monitoring UPower events
    /// </summary>
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;
            
        await _monitoringSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_isMonitoring)
            {
                _logger.LogDebug("Event monitoring is not active");
                return;
            }
            
            _logger.LogInformation("Stopping UPower event monitoring");
            
            _isMonitoring = false;
            
            // Disconnect signal handlers
            await CleanupSignalConnectionsAsync();
            
            // Process any remaining events
            ProcessEventQueue(null);
            
            _logger.LogInformation("UPower event monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping UPower event monitoring");
        }
        finally
        {
            _monitoringSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Connects to UPower signals
    /// </summary>
    private async Task ConnectSignalsAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Create callback delegates and pin them
            var deviceAddedCallback = new DeviceSignalCallback(OnDeviceAdded);
            var deviceRemovedCallback = new DeviceSignalCallback(OnDeviceRemoved);
            
            _deviceAddedCallbackHandle = GCHandle.Alloc(deviceAddedCallback);
            _deviceRemovedCallbackHandle = GCHandle.Alloc(deviceRemovedCallback);
            
            // Get function pointers
            var addedCallbackPtr = Marshal.GetFunctionPointerForDelegate(deviceAddedCallback);
            var removedCallbackPtr = Marshal.GetFunctionPointerForDelegate(deviceRemovedCallback);
            // Note: UPower doesn't have a 'device-changed' signal. Device property changes
            // need to be handled differently (e.g., through polling or individual device monitoring)
            
            // Connect signals
            _clientHandle!.UseHandle(clientPtr =>
            {
                var addedId = UPowerNative.g_signal_connect_data(clientPtr, "device-added", 
                    addedCallbackPtr, GCHandle.ToIntPtr(_thisHandle), IntPtr.Zero, 0);
                _deviceAddedConnection = new SafeGSignalConnection(_clientHandle, addedId);
                
                var removedId = UPowerNative.g_signal_connect_data(clientPtr, "device-removed", 
                    removedCallbackPtr, GCHandle.ToIntPtr(_thisHandle), IntPtr.Zero, 0);
                _deviceRemovedConnection = new SafeGSignalConnection(_clientHandle, removedId);
                
                // Connect to custom signals if configured
                foreach (var customSignal in _configuration.Advanced.CustomSignalNames)
                {
                    try
                    {
                        // Use the device added callback for custom signals as a fallback
                        var customId = UPowerNative.g_signal_connect_data(clientPtr, customSignal, 
                            addedCallbackPtr, GCHandle.ToIntPtr(_thisHandle), IntPtr.Zero, 0);
                        _logger.LogDebug("Connected to custom signal: {Signal} (ID: {Id})", customSignal, customId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to connect to custom signal: {Signal}", customSignal);
                    }
                }
            });
            
            _logger.LogDebug("Connected to UPower signals: device-added, device-removed");
        }, cancellationToken);
    }
    
    /// <summary>
    /// Signal callback for device-added events
    /// </summary>
    private void OnDeviceAdded(IntPtr client, IntPtr device, IntPtr userData)
    {
        try
        {
            if (!_isMonitoring || _disposed)
                return;
                
            _logger.LogDebug("Device added signal received");
            
            var deviceEvent = new BatteryDeviceEvent
            {
                EventType = BatteryDeviceEventType.Added,
                DevicePtr = device,
                Timestamp = DateTimeOffset.UtcNow
            };
            
            EnqueueEvent(deviceEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in device-added signal callback");
        }
    }
    
    /// <summary>
    /// Signal callback for device-removed events
    /// </summary>
    private void OnDeviceRemoved(IntPtr client, IntPtr device, IntPtr userData)
    {
        try
        {
            if (!_isMonitoring || _disposed)
                return;
                
            _logger.LogDebug("Device removed signal received");
            
            var deviceEvent = new BatteryDeviceEvent
            {
                EventType = BatteryDeviceEventType.Removed,
                DevicePtr = device,
                Timestamp = DateTimeOffset.UtcNow
            };
            
            EnqueueEvent(deviceEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in device-removed signal callback");
        }
    }
    
    /// <summary>
    /// Signal callback for device-changed events
    /// </summary>
    private void OnDeviceChanged(IntPtr client, IntPtr device, IntPtr userData)
    {
        try
        {
            if (!_isMonitoring || _disposed)
                return;
                
            _logger.LogDebug("Device changed signal received");
            
            var deviceEvent = new BatteryDeviceEvent
            {
                EventType = BatteryDeviceEventType.Changed,
                DevicePtr = device,
                Timestamp = DateTimeOffset.UtcNow
            };
            
            EnqueueEvent(deviceEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in device-changed signal callback");
        }
    }
    
    /// <summary>
    /// Enqueues a device event for processing
    /// </summary>
    private void EnqueueEvent(BatteryDeviceEvent deviceEvent)
    {
        // Ensure queue doesn't grow unbounded
        while (_eventQueue.Count >= _configuration.MaxEventQueueSize)
        {
            _eventQueue.TryDequeue(out _);
            _logger.LogWarning("Event queue full, dropping oldest event");
        }
        
        _eventQueue.Enqueue(deviceEvent);
    }
    
    /// <summary>
    /// Processes the event queue with debouncing
    /// </summary>
    private void ProcessEventQueue(object? state)
    {
        if (_disposed || !_isMonitoring)
            return;
            
        try
        {
            var now = DateTimeOffset.UtcNow;
            var processedCount = 0;
            var events = new Dictionary<IntPtr, BatteryDeviceEvent>();
            
            // Collect and deduplicate events (keep latest per device)
            while (_eventQueue.TryDequeue(out var deviceEvent) && processedCount < 50)
            {
                // Skip very recent events (debouncing)
                if (now - deviceEvent.Timestamp < _configuration.EventDebounceInterval)
                {
                    _eventQueue.Enqueue(deviceEvent); // Re-queue for later
                    break;
                }
                
                events[deviceEvent.DevicePtr] = deviceEvent;
                processedCount++;
            }
            
            if (events.Count == 0)
                return;
            
            _logger.LogDebug("Processing {Count} debounced device events", events.Count);
            
            // Process each unique device event
            Parallel.ForEach(events.Values, 
                new ParallelOptions 
                { 
                    MaxDegreeOfParallelism = _configuration.Advanced.MaxConcurrentDeviceQueries,
                    CancellationToken = _cancellationTokenSource.Token
                },
                ProcessSingleEvent);
            
            _lastEventProcessed = now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event queue");
        }
    }
    
    /// <summary>
    /// Processes a single device event
    /// </summary>
    private void ProcessSingleEvent(BatteryDeviceEvent deviceEvent)
    {
        try
        {
            if (deviceEvent.DevicePtr == IntPtr.Zero)
                return;
            
            using var deviceHandle = SafeUPowerDeviceHandle.CreateFromPtr(deviceEvent.DevicePtr);
            if (deviceHandle.IsInvalid)
                return;
            
            var objectPath = deviceHandle.UseHandle(ptr => 
                UPowerNative.PtrToStringUTF8(UPowerNative.up_device_get_object_path(ptr)) ?? "");
            
            if (string.IsNullOrEmpty(objectPath))
                return;
            
            BatteryDevice? device = null;
            
            // For removed events, we might not be able to read device properties
            if (deviceEvent.EventType != BatteryDeviceEventType.Removed)
            {
                try
                {
                    device = _propertyConverter.ExtractBatteryDevice(deviceHandle, objectPath);
                    
                    // Filter based on configuration
                    if (device != null && !_configuration.DeviceFilter.ShouldIncludeDevice(device))
                    {
                        _logger.LogDebug("Device {ObjectPath} filtered out by configuration", objectPath);
                        return;
                    }
                    
                    // Validate if enabled
                    if (device != null && _configuration.Advanced.EnableDataValidation)
                    {
                        if (!_propertyConverter.ValidateDeviceProperties(device))
                        {
                            device = _propertyConverter.SanitizeDeviceProperties(device);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to extract device properties for {ObjectPath}", objectPath);
                    return;
                }
            }
            
            if (device == null && deviceEvent.EventType != BatteryDeviceEventType.Removed)
                return;
            
            // Create a minimal device for removal events
            if (device == null && deviceEvent.EventType == BatteryDeviceEventType.Removed)
            {
                device = new BatteryDevice { ObjectPath = objectPath };
            }
            
            // Fire appropriate event
            var eventArgs = new BatteryDeviceEventArgs(device!);
            
            switch (deviceEvent.EventType)
            {
                case BatteryDeviceEventType.Added:
                    _logger.LogInformation("Battery device added: {Model} ({ObjectPath})", device.Model, objectPath);
                    DeviceAdded?.Invoke(this, eventArgs);
                    break;
                    
                case BatteryDeviceEventType.Removed:
                    _logger.LogInformation("Battery device removed: {ObjectPath}", objectPath);
                    DeviceRemoved?.Invoke(this, eventArgs);
                    break;
                    
                case BatteryDeviceEventType.Changed:
                    _logger.LogDebug("Battery device changed: {Model} - {Percentage}% ({State})", 
                        device.Model, device.Percentage, device.State);
                    DeviceChanged?.Invoke(this, eventArgs);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device event");
        }
    }
    
    /// <summary>
    /// Gets the client handle from the UPowerClient
    /// </summary>
    private SafeUPowerClientHandle? GetClientHandle()
    {
        return _client.GetClientHandle();
    }
    
    /// <summary>
    /// Cleans up signal connections
    /// </summary>
    private async Task CleanupSignalConnectionsAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                _deviceAddedConnection?.Dispose();
                _deviceAddedConnection = null;
                
                _deviceRemovedConnection?.Dispose();
                _deviceRemovedConnection = null;
                
                // Free callback handles
                if (_deviceAddedCallbackHandle.IsAllocated)
                    _deviceAddedCallbackHandle.Free();
                    
                if (_deviceRemovedCallbackHandle.IsAllocated)
                    _deviceRemovedCallbackHandle.Free();
                
                _logger.LogDebug("Cleaned up signal connections");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up signal connections");
            }
        });
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
            
        _disposed = true;
        _isMonitoring = false;
        
        _cancellationTokenSource.Cancel();
        _eventProcessingTimer?.Dispose();
        _monitoringSemaphore?.Dispose();
        
        CleanupSignalConnectionsAsync().Wait(TimeSpan.FromSeconds(5));
        
        if (_thisHandle.IsAllocated)
            _thisHandle.Free();
            
        _cancellationTokenSource.Dispose();
    }
}

/// <summary>
/// Internal event structure for queuing device events
/// </summary>
internal sealed class BatteryDeviceEvent
{
    public BatteryDeviceEventType EventType { get; init; }
    public IntPtr DevicePtr { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Types of battery device events
/// </summary>
internal enum BatteryDeviceEventType
{
    Added,
    Removed,
    Changed
}