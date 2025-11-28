#if WINDOWS
using ControllerMonitor.XInput.Interfaces;
using ControllerMonitor.XInput.Helpers;
using System;
using System.Threading.Tasks;
using ControllerMonitor.ViewModels;
using ControllerMonitor.Services;
using ControllerMonitor.Interfaces;
using Microsoft.Extensions.Logging;
using ControllerMonitor.Models;

namespace ControllerMonitor.Platforms.Windows;

public class BatteryMonitorWindows : BatteryMonitorServiceBase
{
    private readonly IXInputService _xInputService;

    public BatteryMonitorWindows(ISettingsService settingsService, ILogger<IBatteryMonitorService> logger, IXInputService xInputService) 
        : base(settingsService, logger)
    {
        _xInputService = xInputService;
    }

    protected override async Task<BatteryInfo> GetBatteryInfoInternalAsync()
    {
        try
        {
            var controllerInfo = await _xInputService.GetFirstControllerBatteryInfoAsync();
            
            if (controllerInfo?.IsConnected == true)
            {
                // Convert XInput battery level to main project battery level
                var xInputBatteryLevel = BatteryLevelConverter.ConvertBatteryLevel(controllerInfo.BatteryLevel);
                var batteryInfo = new BatteryInfo
                {
                    IsConnected = true,
                    Level = ConvertXInputBatteryLevelToMainBatteryLevel(xInputBatteryLevel),
                    IsCharging = controllerInfo.IsWired,
                    Capacity = null, // XInput doesn't provide percentage
                    ModelName = controllerInfo.ModelName
                };
                return batteryInfo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battery information from XInput service");
        }

        return new();
    }


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

}

#endif