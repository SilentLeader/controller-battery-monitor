using System.Runtime.InteropServices;
using ControllerMonitor.XInput.ValueObjects;
using ControllerMonitor.XInput.Interfaces;
using ControllerMonitor.XInput.Models;
using Microsoft.Extensions.Logging;

namespace ControllerMonitor.XInput.Services;

/// <summary>
/// Service for interacting with XInput controllers on Windows
/// </summary>
public class XInputService : IXInputService
{
    private readonly ILogger<XInputService> _logger;

    public XInputService(ILogger<XInputService> logger)
    {
        _logger = logger;
    }

    public async Task<ControllerBatteryInfo?> GetFirstControllerBatteryInfoAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                for (uint i = 0; i < 4; i++)
                {
                    var controllerInfo = GetControllerBatteryInfo(i);
                    if (controllerInfo?.IsConnected == true)
                    {
                        return controllerInfo;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting first controller battery info");
                return null;
            }
        });
    }

    public async Task<ControllerBatteryInfo?> GetControllerBatteryInfoAsync(uint controllerIndex)
    {
        return await Task.Run(() =>
        {
            try
            {
                return GetControllerBatteryInfo(controllerIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting controller battery info for index {ControllerIndex}", controllerIndex);
                return null;
            }
        });
    }

    public async Task<IEnumerable<ControllerBatteryInfo>> GetAllControllersBatteryInfoAsync()
    {
        return await Task.Run(() =>
        {
            var controllers = new List<ControllerBatteryInfo>();
            try
            {
                for (uint i = 0; i < 4; i++)
                {
                    var controllerInfo = GetControllerBatteryInfo(i);
                    if (controllerInfo != null)
                    {
                        controllers.Add(controllerInfo);
                    }
                }
                return controllers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all controllers battery info");
                return controllers;
            }
        });
    }

    private ControllerBatteryInfo? GetControllerBatteryInfo(uint controllerIndex)
    {
        try
        {
            var state = new XInputState();
            uint result = XInputGetState(controllerIndex, ref state);

            var controllerInfo = new ControllerBatteryInfo
            {
                ControllerIndex = controllerIndex,
                IsConnected = result == 0 // ERROR_SUCCESS
            };

            if (!controllerInfo.IsConnected)
            {
                return controllerInfo;
            }

            // Controller is connected, get battery info
            var batteryInfo = new XInputBatteryInformation();
            uint batteryResult = XInputGetBatteryInformation(controllerIndex, BatteryDeviceType.BATTERY_DEVTYPE_GAMEPAD, ref batteryInfo);

            if (batteryResult == 0) // ERROR_SUCCESS
            {
                controllerInfo.BatteryType = batteryInfo.BatteryType;
                controllerInfo.BatteryLevel = batteryInfo.BatteryLevel;
            }

            return controllerInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battery info for controller {ControllerIndex}", controllerIndex);
            return new ControllerBatteryInfo
            {
                ControllerIndex = controllerIndex,
                IsConnected = false
            };
        }
    }

    #region XInput P/Invoke declarations

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState(uint dwUserIndex, ref XInputState pState);

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation")]
    private static extern uint XInputGetBatteryInformation(uint dwUserIndex, byte devType, ref XInputBatteryInformation pBatteryInformation);

    #endregion
}