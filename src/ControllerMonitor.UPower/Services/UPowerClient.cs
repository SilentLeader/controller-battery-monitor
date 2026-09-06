using ControllerMonitor.UPower.Exceptions;
using ControllerMonitor.UPower.Models;
using ControllerMonitor.UPower.Native;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace ControllerMonitor.UPower.Services;

/// <summary>
/// Core UPower client service providing async access to battery devices
/// </summary>
public sealed partial class UPowerClient(
    ILogger<UPowerClient> logger,
    UPowerPropertyConverter propertyConverter) : IDisposable
{
    private readonly ILogger<UPowerClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly UPowerPropertyConverter _propertyConverter = propertyConverter ?? throw new ArgumentNullException(nameof(propertyConverter));
    private readonly SemaphoreSlim _clientSemaphore = new(1, 1);
    private SafeUPowerClientHandle? _clientHandle;
    private bool _disposed;
    private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(3);
    private static readonly string[] _nativeLibraries = ["libupower-glib.so.3", "libglib-2.0.so.0", "libgobject-2.0.so.0"];

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
            
            _logger.LogDebug("Initializing UPower client");
            
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
        {
            throw new ObjectDisposedException(nameof(UPowerClient));
        }

        if (!await EnsureInitializedAsync(cancellationToken))
        {
            return [];
        }
        
        try
        {
            using var timeout = new CancellationTokenSource(_operationTimeout);
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            
            return await GetDevicesInternalAsync(combined.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new UPowerOperationCancelledException("GetDevicesAsync", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw new UPowerTimeoutException("GetDevicesAsync", _operationTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get UPower devices");
            throw UPowerExceptionHelpers.WrapException(ex, "GetDevicesAsync");
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

                var deviceCount = devicesArray.Length;
                LogProcessingDevices(_logger, deviceCount);

                var devices = new List<BatteryDevice>((int)deviceCount);
                for (uint i = 0; i < deviceCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var device = ProcessDevice(devicesArray.GetElement(i));
                    if (device != null)
                    {
                        devices.Add(device);
                    }
                }

                LogFoundDevices(_logger, devices.Count, deviceCount);

                return (IReadOnlyList<BatteryDevice>)devices;
            });
        }, cancellationToken);
    }

    /// <summary>
    /// Processes a single device
    /// </summary>
    private BatteryDevice? ProcessDevice(IntPtr devicePtr)
    {
        if (devicePtr == IntPtr.Zero)
            return null;

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
            if (!_propertyConverter.ValidateDeviceProperties(device))
            {
                _logger.LogWarning("Device validation failed for {ObjectPath}, sanitizing data", objectPath);
                device = _propertyConverter.SanitizeDeviceProperties(device);
            }

            return device;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to process device {DevicePtr}", devicePtr);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing {DeviceCount} UPower devices")]
    private static partial void LogProcessingDevices(ILogger logger, uint deviceCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found {FilteredCount} relevant battery devices out of {TotalCount} total devices")]
    private static partial void LogFoundDevices(ILogger logger, int filteredCount, uint totalCount);
    
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
            foreach (var path in _nativeLibraries)
            {
                try
                {
                    var handle = NativeLibrary.Load(path);
                    if (handle != IntPtr.Zero)
                    {
                        _logger.LogDebug("Successfully loaded UPower library: {Path}", path);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to load library from path: {Path}", path);
                    return false;
                }
            }
            
            return true;
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
    
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _clientSemaphore?.Dispose();
        _clientHandle?.Dispose();
        
        _disposed = true;
    }
}
