# Xbox Battery Monitor

A cross-platform Avalonia UI application for monitoring Xbox controller battery levels.

## Features

- Linux support using system power supply information
- Windows support using GameInput API (Windows 11+)
- Real-time battery level monitoring
- Charging status indication
- Simple GUI interface

## Building and Running

1. Ensure you have .NET 9.0 installed.
2. Navigate to the project directory: `cd /mnt/data/Projects/ControllerMonitor/src/XboxBatteryMonitor`
3. Restore packages: `dotnet restore`
4. Build the project: `dotnet build`
5. Run the application: `dotnet run`

## Linux Implementation

The Linux implementation reads battery information from `/sys/class/power_supply/` by:

- Scanning for battery devices
- Identifying Xbox controller batteries by model name
- Parsing uevent files for capacity level and charging status

## Windows Implementation

The Windows implementation uses the XInput API to monitor Xbox controller batteries by:

- Checking up to 4 XInput controller slots for connected devices
- Using XInputGetState to detect controller presence
- Using XInputGetBatteryInformation to query battery details
- Converting XInput battery levels (empty, low, medium, full) to application format
- Detecting charging status based on battery type (wired controllers show as charging)

Note: Uses xinput1_4.dll which is available on Windows Vista and later. Battery percentage is not available through XInput - only discrete levels are provided.

## Dependencies

- Avalonia UI
- CommunityToolkit.Mvvm

## Sources

- [xbox-controller-battery-linux](https://github.com/nvhai245/xbox-controller-battery-linux)
