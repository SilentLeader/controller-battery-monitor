using ControllerMonitor.UPower.Exceptions;
using ControllerMonitor.UPower.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ControllerMonitor.UPower.Services;

/// <summary>
/// Interface for battery data providers with health monitoring and retry logic
/// </summary>
public interface IBatteryDataProvider : IDisposable
{
    /// <summary>
    /// Unique name of this provider
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Priority of this provider (lower values = higher priority)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Whether this provider is currently healthy and available
    /// </summary>
    bool IsHealthy { get; }
    
    /// <summary>
    /// Checks if the provider is available on this system
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all battery devices from this provider
    /// </summary>
    Task<IReadOnlyList<BatteryDevice>> GetBatteryDevicesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific battery device by object path
    /// </summary>
    Task<BatteryDevice?> GetBatteryDeviceAsync(string objectPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts monitoring for battery events
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops monitoring for battery events
    /// </summary>
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when battery devices change
    /// </summary>
    event EventHandler<BatteryDeviceEventArgs>? DevicesChanged;
    
    /// <summary>
    /// Gets health information about this provider
    /// </summary>
    ProviderHealthInfo GetHealthInfo();
}

/// <summary>
/// UPower implementation of IBatteryDataProvider
/// </summary>
public sealed class UPowerBatteryProvider : IBatteryDataProvider
{
    private readonly ILogger<UPowerBatteryProvider> _logger;
    private readonly UPowerConfiguration _configuration;
    private readonly UPowerClient _client;
    private readonly UPowerEventMonitor _eventMonitor;
    private readonly ProviderHealthMonitor _healthMonitor;
    
    private bool _disposed;
    
    public string ProviderName => "UPower";
    public int Priority => 1; // Higher priority than sysfs fallback
    public bool IsHealthy => _healthMonitor.IsHealthy;
    
    public event EventHandler<BatteryDeviceEventArgs>? DevicesChanged;
    
    public UPowerBatteryProvider(
        ILogger<UPowerBatteryProvider> logger,
        IOptions<UPowerConfiguration> configuration,
        UPowerClient client,
        UPowerEventMonitor eventMonitor,
        ProviderHealthMonitor healthMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _eventMonitor = eventMonitor ?? throw new ArgumentNullException(nameof(eventMonitor));
        _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
        
        // Wire up event forwarding
        _eventMonitor.DeviceAdded += OnDeviceChanged;
        _eventMonitor.DeviceRemoved += OnDeviceChanged;
        _eventMonitor.DeviceChanged += OnDeviceChanged;
        
        _client.DeviceAdded += OnDeviceChanged;
        _client.DeviceRemoved += OnDeviceChanged;
        _client.DeviceChanged += OnDeviceChanged;
    }
    
    /// <summary>
    /// Checks if UPower is available on this system
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return false;
            
        try
        {
            return await _healthMonitor.ExecuteWithHealthCheckAsync(async () =>
            {
                await _client.InitializeAsync(cancellationToken);
                return true;
            }, "IsAvailableAsync");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "UPower availability check failed");
            return false;
        }
    }
    
    /// <summary>
    /// Gets all battery devices from UPower
    /// </summary>
    public async Task<IReadOnlyList<BatteryDevice>> GetBatteryDevicesAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerBatteryProvider));
        
        return await _healthMonitor.ExecuteWithHealthCheckAsync(async () =>
        {
            var devices = await _client.GetDevicesAsync(cancellationToken);
            
            _logger.LogDebug("Retrieved {Count} battery devices from UPower", devices.Count);
            
            return devices;
        }, "GetBatteryDevicesAsync");
    }
    
    /// <summary>
    /// Gets a specific battery device by object path
    /// </summary>
    public async Task<BatteryDevice?> GetBatteryDeviceAsync(string objectPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(objectPath))
            throw new ArgumentException("Object path cannot be null or empty", nameof(objectPath));
            
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerBatteryProvider));
        
        return await _healthMonitor.ExecuteWithHealthCheckAsync(async () =>
        {
            var device = await _client.GetDeviceAsync(objectPath, cancellationToken);
            
            if (device != null)
            {
                _logger.LogDebug("Retrieved device {ObjectPath}: {Model} - {Percentage}%", 
                    objectPath, device.Model, device.Percentage);
            }
            else
            {
                _logger.LogDebug("Device not found: {ObjectPath}", objectPath);
            }
            
            return device;
        }, "GetBatteryDeviceAsync");
    }
    
    /// <summary>
    /// Starts monitoring for battery events
    /// </summary>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UPowerBatteryProvider));
        
        try
        {
            await _eventMonitor.StartMonitoringAsync(cancellationToken);
            _logger.LogInformation("UPower battery monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start UPower event monitoring, falling back to polling");
            // Event monitoring failure is not fatal - polling will still work
        }
    }
    
    /// <summary>
    /// Stops monitoring for battery events
    /// </summary>
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;
        
        try
        {
            await _eventMonitor.StopMonitoringAsync(cancellationToken);
            _logger.LogInformation("UPower battery monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping UPower event monitoring");
        }
    }
    
    /// <summary>
    /// Gets health information about this provider
    /// </summary>
    public ProviderHealthInfo GetHealthInfo()
    {
        return _healthMonitor.GetHealthInfo();
    }
    
    private void OnDeviceChanged(object? sender, BatteryDeviceEventArgs e)
    {
        try
        {
            DevicesChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in device changed event handler");
        }
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
            
        try
        {
            StopMonitoringAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during disposal");
        }
        
        _eventMonitor.DeviceAdded -= OnDeviceChanged;
        _eventMonitor.DeviceRemoved -= OnDeviceChanged;
        _eventMonitor.DeviceChanged -= OnDeviceChanged;
        
        _client.DeviceAdded -= OnDeviceChanged;
        _client.DeviceRemoved -= OnDeviceChanged;
        _client.DeviceChanged -= OnDeviceChanged;
        
        _disposed = true;
    }
}

/// <summary>
/// Health monitoring service for battery providers
/// </summary>
public sealed class ProviderHealthMonitor
{
    private readonly ILogger<ProviderHealthMonitor> _logger;
    private readonly UPowerConfiguration _configuration;
    private readonly object _lockObject = new();
    
    private int _consecutiveFailures;
    private DateTimeOffset _lastFailure = DateTimeOffset.MinValue;
    private DateTimeOffset _lastSuccess = DateTimeOffset.UtcNow;
    private TimeSpan _totalDowntime = TimeSpan.Zero;
    private long _totalOperations;
    private long _successfulOperations;
    
    public ProviderHealthMonitor(ILogger<ProviderHealthMonitor> logger, IOptions<UPowerConfiguration> configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    /// <summary>
    /// Whether the provider is currently healthy
    /// </summary>
    public bool IsHealthy
    {
        get
        {
            lock (_lockObject)
            {
                // Provider is unhealthy if:
                // 1. Too many consecutive failures
                // 2. Recent failure and not enough time has passed for recovery
                return _consecutiveFailures < _configuration.HealthCheckFailureThreshold &&
                       (DateTimeOffset.UtcNow - _lastFailure) > _configuration.HealthCheckRecoveryInterval;
            }
        }
    }
    
    /// <summary>
    /// Executes an operation with health monitoring and retry logic
    /// </summary>
    public async Task<T> ExecuteWithHealthCheckAsync<T>(Func<Task<T>> operation, string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        var attempt = 0;
        Exception? lastException = null;
        
        while (attempt < _configuration.MaxRetryAttempts + 1)
        {
            try
            {
                Interlocked.Increment(ref _totalOperations);
                
                var result = await operation();
                
                // Operation succeeded
                RecordSuccess(stopwatch.Elapsed);
                Interlocked.Increment(ref _successfulOperations);
                
                if (attempt > 0)
                {
                    _logger.LogInformation("Operation {Operation} succeeded after {Attempts} attempts", 
                        operationName, attempt + 1);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;
                
                _logger.LogDebug(ex, "Operation {Operation} failed on attempt {Attempt}", operationName, attempt);
                
                if (attempt > _configuration.MaxRetryAttempts)
                {
                    RecordFailure(stopwatch.Elapsed, ex);
                    break;
                }
                
                // Calculate retry delay with exponential backoff and jitter
                var delay = CalculateRetryDelay(attempt - 1);
                await Task.Delay(delay);
            }
        }
        
        // All retries exhausted
        throw new UPowerInteropException($"Operation {operationName} failed after {attempt} attempts: {lastException?.Message}", lastException ?? new InvalidOperationException("Unknown error"));
    }
    
    /// <summary>
    /// Executes an operation with health monitoring (no return value)
    /// </summary>
    public async Task ExecuteWithHealthCheckAsync(Func<Task> operation, string operationName)
    {
        await ExecuteWithHealthCheckAsync(async () =>
        {
            await operation();
            return true;
        }, operationName);
    }
    
    /// <summary>
    /// Gets detailed health information
    /// </summary>
    public ProviderHealthInfo GetHealthInfo()
    {
        lock (_lockObject)
        {
            var successRate = _totalOperations > 0 ? (double)_successfulOperations / _totalOperations : 1.0;
            
            return new ProviderHealthInfo
            {
                IsHealthy = IsHealthy,
                ConsecutiveFailures = _consecutiveFailures,
                LastFailure = _lastFailure,
                LastSuccess = _lastSuccess,
                TotalDowntime = _totalDowntime,
                SuccessRate = successRate,
                TotalOperations = _totalOperations,
                FailureThreshold = _configuration.HealthCheckFailureThreshold,
                RecoveryInterval = _configuration.HealthCheckRecoveryInterval
            };
        }
    }
    
    private void RecordSuccess(TimeSpan operationTime)
    {
        lock (_lockObject)
        {
            var now = DateTimeOffset.UtcNow;
            
            if (_consecutiveFailures > 0)
            {
                _logger.LogInformation("Provider recovered after {Failures} consecutive failures", _consecutiveFailures);
                
                // Calculate downtime if we were previously failing
                if (_lastFailure > _lastSuccess)
                {
                    _totalDowntime += now - _lastFailure;
                }
            }
            
            _consecutiveFailures = 0;
            _lastSuccess = now;
            
            if (_configuration.Advanced.EnablePerformanceMetrics)
            {
                _logger.LogDebug("Operation completed successfully in {Duration}ms", operationTime.TotalMilliseconds);
            }
        }
    }
    
    private void RecordFailure(TimeSpan operationTime, Exception exception)
    {
        lock (_lockObject)
        {
            var now = DateTimeOffset.UtcNow;
            
            _consecutiveFailures++;
            _lastFailure = now;
            
            var isRecoverable = UPowerExceptionHelpers.IsRecoverableException(exception);
            
            _logger.LogWarning(exception, 
                "Provider operation failed (attempt {Failures}/{Threshold}) - Recoverable: {Recoverable}", 
                _consecutiveFailures, _configuration.HealthCheckFailureThreshold, isRecoverable);
            
            // If this pushes us over the failure threshold, mark as unhealthy
            if (_consecutiveFailures >= _configuration.HealthCheckFailureThreshold)
            {
                _logger.LogError("Provider marked as unhealthy after {Failures} consecutive failures", 
                    _consecutiveFailures);
            }
        }
    }
    
    private TimeSpan CalculateRetryDelay(int attemptIndex)
    {
        // Exponential backoff with jitter
        var baseDelay = _configuration.RetryBaseDelay.TotalMilliseconds;
        var exponentialDelay = baseDelay * Math.Pow(2, attemptIndex);
        
        // Add jitter (±25% of the delay)
        var jitter = Random.Shared.NextDouble() * 0.5 - 0.25; // -0.25 to +0.25
        var delayWithJitter = exponentialDelay * (1 + jitter);
        
        // Clamp to maximum delay
        var finalDelay = Math.Min(delayWithJitter, _configuration.RetryMaxDelay.TotalMilliseconds);
        
        return TimeSpan.FromMilliseconds(Math.Max(finalDelay, 0));
    }
}

/// <summary>
/// Health information for a battery provider
/// </summary>
public sealed class ProviderHealthInfo
{
    public bool IsHealthy { get; init; }
    public int ConsecutiveFailures { get; init; }
    public DateTimeOffset LastFailure { get; init; }
    public DateTimeOffset LastSuccess { get; init; }
    public TimeSpan TotalDowntime { get; init; }
    public double SuccessRate { get; init; }
    public long TotalOperations { get; init; }
    public int FailureThreshold { get; init; }
    public TimeSpan RecoveryInterval { get; init; }
    
    public override string ToString()
    {
        return $"Healthy: {IsHealthy}, Success Rate: {SuccessRate:P2}, " +
               $"Consecutive Failures: {ConsecutiveFailures}/{FailureThreshold}";
    }
}