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

The Windows implementation uses the GameInput API (available on Windows 11+) to monitor Xbox controller batteries by:
- Enumerating connected game input devices
- Filtering for Xbox One and Xbox 360 controllers
- Querying battery state for level (empty, low, medium, full) and charging status
- Providing percentage capacity when available

Note: Requires Windows 11 or later. On older Windows versions, the app will show disconnected status.

## Dependencies

- Avalonia UI
- CommunityToolkit.Mvvm
- Microsoft.GameInput (for Windows battery monitoring)

## Sources

- [xbox-controller-battery-linux](https://github.com/nvhai245/xbox-controller-battery-linux)