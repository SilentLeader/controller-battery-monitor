#if WINDOWS
using ControllerMonitor.XInput.Interfaces;
using ControllerMonitor.XInput.Helpers;
#endif
using System;
using System.Threading.Tasks;
using ControllerMonitor.ViewModels;
using ControllerMonitor.Services;
using ControllerMonitor.Interfaces;
using Microsoft.Extensions.Logging;

namespace ControllerMonitor.Platforms.Windows;

public class BatteryMonitorWindows : BatteryMonitorServiceBase
{
#if WINDOWS
    private readonly IXInputService _xInputService;

    public BatteryMonitorWindows(ISettingsService settingsService, ILogger<IBatteryMonitorService> logger, IXInputService xInputService) 
        : base(settingsService, logger)
    {
        _xInputService = xInputService;
    }
#else
    public BatteryMonitorWindows(ISettingsService settingsService, ILogger<IBatteryMonitorService> logger) 
        : base(settingsService, logger)
    {
    }
#endif

    public override async Task<BatteryInfoViewModel> GetBatteryInfoAsync()
    {
        var batteryInfo = new BatteryInfoViewModel { IsConnected = false };

#if WINDOWS
        try
        {
            var controllerInfo = await _xInputService.GetFirstControllerBatteryInfoAsync();
            
            if (controllerInfo?.IsConnected == true)
            {
                batteryInfo.IsConnected = true;
                // Convert XInput battery level to main project battery level
                var xInputBatteryLevel = BatteryLevelConverter.ConvertBatteryLevel(controllerInfo.BatteryLevel);
                batteryInfo.Level = ConvertXInputBatteryLevelToMainBatteryLevel(xInputBatteryLevel);
                batteryInfo.IsCharging = controllerInfo.IsWired;
                batteryInfo.Capacity = null; // XInput doesn't provide percentage
                batteryInfo.ModelName = controllerInfo.ModelName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battery information from XInput service");
            batteryInfo.IsConnected = false;
        }
#endif

        return batteryInfo;
    }

#if WINDOWS
    /// <summary>
    /// Converts XInput project BatteryLevel to main project BatteryLevel
    /// </summary>
    private static ValueObjects.BatteryLevel ConvertXInputBatteryLevelToMainBatteryLevel(XInput.ValueObjects.XInputBatteryLevel xInputLevel)
    {
        return xInputLevel switch
        {
            XInput.ValueObjects.XInputBatteryLevel.Empty => ValueObjects.BatteryLevel.Empty,
            XInput.ValueObjects.XInputBatteryLevel.Low => ValueObjects.BatteryLevel.Low,
            XInput.ValueObjects.XInputBatteryLevel.Normal => ValueObjects.BatteryLevel.Normal,
            XInput.ValueObjects.XInputBatteryLevel.High => ValueObjects.BatteryLevel.High,
            XInput.ValueObjects.XInputBatteryLevel.Full => ValueObjects.BatteryLevel.Full,
            _ => ValueObjects.BatteryLevel.Unknown
        };
    }
#endif
}