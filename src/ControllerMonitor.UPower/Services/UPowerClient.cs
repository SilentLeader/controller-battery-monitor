using ControllerMonitor.UPower.Exceptions;
using ControllerMonitor.UPower.Models;
using ControllerMonitor.UPower.Native;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace ControllerMonitor.UPower.Services;

/// <summary>
/// Core UPower client service providing async access to battery devices
/// </summary>
public sealed class UPowerClient : IDisposable
{
    private readonly ILogger<UPowerClient> _logger;
    private readonly UPowerConfiguration _configuration;
    private readonly UPowerPropertyConverter _propertyConverter;
    private readonly SemaphoreSlim _clientSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, BatteryDevice> _deviceCache = new();
    private readonly Timer _cacheCleanupTimer;
    
    private SafeUPowerClientHandle? _clientHandle;
    private bool _disposed;
    private DateTimeOffset _lastCacheCleanup = DateTimeOffset.UtcNow;
    
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
    
    public UPowerClient(
        ILogger<UPowerClient> logger,
        IOptions<UPowerConfiguration> configuration,
        UPowerPropertyConverter propertyConverter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _propertyConverter = propertyConverter ?? throw new ArgumentNullException(nameof(propertyConverter));
        
        // Setup cache cleanup timer
        _cacheCleanupTimer = new Timer(CleanupCache, null, 
            _configuration.Advanced.GCCollectionInterval,
            _configuration.Advanced.GCCollectionInterval);
    }
    
    /// <summary>
    /// Initializes the UPower client connection
    /// </summary>
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerClient));
            
        await _clientSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_clientHandle?.IsInvalid == false)
            {
                return true; // Already initialized
            }
            
            _logger.LogInformation("Initializing UPower client");
            
            // Try to load the native library first
            if (!await TryLoadNativeLibraryAsync(cancellationToken))
            {
                throw new UPowerLibraryLoadException("Failed to load libupower-glib");
            }
            
            // Create the client
            _clientHandle = await CreateClientAsync(cancellationToken);
            
            if (_clientHandle.IsInvalid)
            {
                throw new UPowerDaemonUnavailableException("Failed to create UPower client - daemon may not be running");
            }
            
            _logger.LogInformation("UPower client initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize UPower client");
            _clientHandle?.Dispose();
            _clientHandle = null;
            throw UPowerExceptionHelpers.WrapException(ex, "InitializeAsync");
        }
        finally
        {
            _clientSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Gets all battery devices available through UPower
    /// </summary>
    public async Task<IReadOnlyList<BatteryDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerClient));
            
        if (!await EnsureInitializedAsync(cancellationToken))
        {
            return Array.Empty<BatteryDevice>();
        }
        
        try
        {
            using var timeout = new CancellationTokenSource(_configuration.OperationTimeout);
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            
            return await GetDevicesInternalAsync(combined.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new UPowerOperationCancelledException("GetDevicesAsync", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw new UPowerTimeoutException("GetDevicesAsync", _configuration.OperationTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get UPower devices");
            throw UPowerExceptionHelpers.WrapException(ex, "GetDevicesAsync");
        }
    }
    
    /// <summary>
    /// Gets a specific device by its object path
    /// </summary>
    public async Task<BatteryDevice?> GetDeviceAsync(string objectPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(objectPath))
            throw new ArgumentException("Object path cannot be null or empty", nameof(objectPath));
            
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerClient));
        
        // Check cache first
        if (_configuration.Advanced.EnablePropertyCaching &&
            _deviceCache.TryGetValue(objectPath, out var cachedDevice) &&
            DateTimeOffset.UtcNow - cachedDevice.UpdateTime < _configuration.Advanced.PropertyCacheExpiry)
        {
            _logger.LogDebug("Returning cached device for {ObjectPath}", objectPath);
            return cachedDevice;
        }
        
        var devices = await GetDevicesAsync(cancellationToken);
        return devices.FirstOrDefault(d => d.ObjectPath == objectPath);
    }
    
    /// <summary>
    /// Refreshes a specific device's properties
    /// </summary>
    public async Task<BatteryDevice?> RefreshDeviceAsync(string objectPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(objectPath))
            throw new ArgumentException("Object path cannot be null or empty", nameof(objectPath));
            
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerClient));
            
        if (!await EnsureInitializedAsync(cancellationToken))
        {
            return null;
        }
        
        try
        {
            using var timeout = new CancellationTokenSource(_configuration.OperationTimeout);
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            
            return await RefreshDeviceInternalAsync(objectPath, combined.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new UPowerOperationCancelledException("RefreshDeviceAsync", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw new UPowerTimeoutException("RefreshDeviceAsync", _configuration.OperationTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh device {ObjectPath}", objectPath);
            throw UPowerExceptionHelpers.WrapException(ex, "RefreshDeviceAsync");
        }
    }
    
    /// <summary>
    /// Checks whether the system is running on battery power
    /// </summary>
    public async Task<bool> IsOnBatteryAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerClient));
            
        if (!await EnsureInitializedAsync(cancellationToken))
        {
            return false;
        }
        
        try
        {
            using var timeout = new CancellationTokenSource(_configuration.OperationTimeout);
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            
            return await Task.Run(() =>
            {
                combined.Token.ThrowIfCancellationRequested();
                return _clientHandle!.UseHandle(clientPtr => 
                    UPowerNative.up_client_get_on_battery(clientPtr));
            }, combined.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new UPowerOperationCancelledException("IsOnBatteryAsync", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw new UPowerTimeoutException("IsOnBatteryAsync", _configuration.OperationTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check battery status");
            throw UPowerExceptionHelpers.WrapException(ex, "IsOnBatteryAsync");
        }
    }
    
    /// <summary>
    /// Sets the laptop lid closed state
    /// </summary>
    public async Task SetLidIsClosedAsync(bool isClosed, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerClient));
            
        if (!await EnsureInitializedAsync(cancellationToken))
        {
            throw new UPowerDaemonUnavailableException("Cannot set lid state - UPower client not initialized");
        }
        
        try
        {
            using var timeout = new CancellationTokenSource(_configuration.OperationTimeout);
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            
            await Task.Run(() =>
            {
                combined.Token.ThrowIfCancellationRequested();
                _clientHandle!.UseHandle(clientPtr => 
                    UPowerNative.up_client_set_lid_is_closed(clientPtr, isClosed));
            }, combined.Token);
            
            _logger.LogDebug("Set lid closed state to {IsClosed}", isClosed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new UPowerOperationCancelledException("SetLidIsClosedAsync", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw new UPowerTimeoutException("SetLidIsClosedAsync", _configuration.OperationTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set lid closed state");
            throw UPowerExceptionHelpers.WrapException(ex, "SetLidIsClosedAsync");
        }
    }
    
    /// <summary>
    /// Internal method to get devices with proper error handling
    /// </summary>
    private async Task<IReadOnlyList<BatteryDevice>> GetDevicesInternalAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            return _clientHandle!.UseHandle(clientPtr =>
            {
                using var devicesArray = new SafeGPtrArrayHandle(UPowerNative.up_client_get_devices(clientPtr));
                if (devicesArray.IsInvalid)
                {
                    _logger.LogWarning("No devices returned from UPower");
                    return (IReadOnlyList<BatteryDevice>)Array.Empty<BatteryDevice>();
                }
                
                var devices = new List<BatteryDevice>();
                var deviceCount = devicesArray.Length;
                
                _logger.LogDebug("Processing {DeviceCount} UPower devices", deviceCount);
                
                // Use semaphore to limit concurrent device processing
                using var concurrencySemaphore = new SemaphoreSlim(_configuration.Advanced.MaxConcurrentDeviceQueries);
                var tasks = new Task<BatteryDevice?>[deviceCount];
                
                for (uint i = 0; i < deviceCount; i++)
                {
                    var devicePtr = devicesArray.GetElement(i);
                    tasks[i] = ProcessDeviceAsync(devicePtr, concurrencySemaphore, cancellationToken);
                }
                
                var results = Task.WhenAll(tasks).GetAwaiter().GetResult();
                
                foreach (var device in results)
                {
                    if (device != null && _configuration.DeviceFilter.ShouldIncludeDevice(device))
                    {
                        devices.Add(device);
                        
                        // Update cache
                        if (_configuration.Advanced.EnablePropertyCaching)
                        {
                            _deviceCache.AddOrUpdate(device.ObjectPath, device, (_, _) => device);
                        }
                    }
                }
                
                _logger.LogDebug("Found {FilteredCount} relevant battery devices out of {TotalCount} total devices", 
                    devices.Count, deviceCount);
                
                return (IReadOnlyList<BatteryDevice>)devices.AsReadOnly();
            });
        }, cancellationToken);
    }
    
    /// <summary>
    /// Processes a single device asynchronously
    /// </summary>
    private async Task<BatteryDevice?> ProcessDeviceAsync(IntPtr devicePtr, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        if (devicePtr == IntPtr.Zero)
            return null;
            
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            using var deviceHandle = SafeUPowerDeviceHandle.CreateFromPtr(devicePtr);
            if (deviceHandle.IsInvalid)
                return null;
            
            var objectPath = deviceHandle.UseHandle(ptr => 
                UPowerNative.PtrToStringUTF8(UPowerNative.up_device_get_object_path(ptr)) ?? "");
                
            if (string.IsNullOrEmpty(objectPath))
                return null;
            
            var device = _propertyConverter.ExtractBatteryDevice(deviceHandle, objectPath);
            
            // Validate and sanitize if enabled
            if (_configuration.Advanced.EnableDataValidation)
            {
                if (!_propertyConverter.ValidateDeviceProperties(device))
                {
                    _logger.LogWarning("Device validation failed for {ObjectPath}, sanitizing data", objectPath);
                    device = _propertyConverter.SanitizeDeviceProperties(device);
                }
            }
            
            return device;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to process device {DevicePtr}", devicePtr);
            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }
    
    /// <summary>
    /// Refreshes a specific device internally
    /// </summary>
    private async Task<BatteryDevice?> RefreshDeviceInternalAsync(string objectPath, CancellationToken cancellationToken)
    {
        var devices = await GetDevicesInternalAsync(cancellationToken);
        var device = devices.FirstOrDefault(d => d.ObjectPath == objectPath);
        
        if (device != null)
        {
            // Invalidate cache entry
            _deviceCache.TryRemove(objectPath, out _);
            
            // Fire changed event
            DeviceChanged?.Invoke(this, new BatteryDeviceEventArgs(device));
        }
        
        return device;
    }
    
    /// <summary>
    /// Ensures the client is initialized
    /// </summary>
    private async Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_clientHandle?.IsInvalid == false)
            return true;
            
        return await InitializeAsync(cancellationToken);
    }
    
    /// <summary>
    /// Attempts to load the native library
    /// </summary>
    private async Task<bool> TryLoadNativeLibraryAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var libraryPaths = _configuration.Library.CustomLibraryPaths
                .Concat(new[] { "libupower-glib.so.3", "libupower-glib.so.1", "libupower-glib.so" })
                .ToArray();
            
            foreach (var path in libraryPaths)
            {
                try
                {
                    var handle = NativeLibrary.Load(path);
                    if (handle != IntPtr.Zero)
                    {
                        if (_configuration.Library.VerifyLibraryAfterLoad)
                        {
                            // Test basic functionality
                            var testClient = UPowerNative.up_client_new();
                            if (testClient != IntPtr.Zero)
                            {
                                UPowerNative.g_object_unref(testClient);
                                _logger.LogDebug("Successfully loaded and verified UPower library: {Path}", path);
                                return true;
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Successfully loaded UPower library: {Path}", path);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to load library from path: {Path}", path);
                }
            }
            
            return false;
        }, cancellationToken);
    }
    
    /// <summary>
    /// Creates the UPower client handle
    /// </summary>
    private async Task<SafeUPowerClientHandle> CreateClientAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SafeUPowerClientHandle.CreateClient();
        }, cancellationToken);
    }
    
    /// <summary>
    /// Cleans up the device cache periodically
    /// </summary>
    private void CleanupCache(object? state)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var expiredKeys = _deviceCache
                .Where(kvp => now - kvp.Value.UpdateTime > _configuration.Advanced.PropertyCacheExpiry)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in expiredKeys)
            {
                _deviceCache.TryRemove(key, out _);
            }
            
            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
            }
            
            // Force GC if requested
            if (now - _lastCacheCleanup > _configuration.Advanced.GCCollectionInterval)
            {
                GC.Collect(0, GCCollectionMode.Optimized);
                _lastCacheCleanup = now;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cache cleanup");
        }
    }
    
    /// <summary>
    /// Gets the internal client handle for event monitoring
    /// </summary>
    internal SafeUPowerClientHandle? GetClientHandle()
    {
        return _clientHandle;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
            
        _cacheCleanupTimer?.Dispose();
        _clientSemaphore?.Dispose();
        _clientHandle?.Dispose();
        _deviceCache.Clear();
        
        _disposed = true;
    }
}

/// <summary>
/// Event arguments for battery device events
/// </summary>
public sealed class BatteryDeviceEventArgs : EventArgs
{
    public BatteryDevice Device { get; }
    
    public BatteryDeviceEventArgs(BatteryDevice device)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
    }
}