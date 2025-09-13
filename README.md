# Controller Monitor

A cross-platform Avalonia UI application for monitoring game controller battery levels.

## Features

- Linux support using system power supply information
- Windows support using XInput API
- Real-time battery level monitoring
- Charging status indication
- Simple GUI interface

## Installation

### Pre-built Packages

Download the latest release from [GitHub Releases](https://github.com/SilentLeader/controller-battery-monitor/releases).

#### Debian/Ubuntu (.deb)
```bash
sudo dpkg -i ControllerMonitor-Linux-*.deb
sudo apt-get install -f  # Install any missing dependencies
```

#### Arch Linux (.pkg.tar.zst)
```bash
sudo pacman -U ControllerMonitor-Linux-*.pkg.tar.zst
```

#### Manual Installation (tar.gz)
```bash
tar -xzf ControllerMonitor-Linux-*.tar.gz
cd ControllerMonitor-Linux-*
# Copy files to appropriate locations or run directly
```

#### Optional Dependencies

For system notifications support, install `libnotify` (provides `notify-send`):

**Debian/Ubuntu:**

```bash
sudo apt-get install libnotify-bin
```

**Arch Linux:**

```bash
sudo pacman -S libnotify
```

**Fedora/RHEL:**

```bash
sudo dnf install libnotify
```

**OpenSUSE:**

```bash
sudo zypper install libnotify-tools
```

After installation, you can:

- Launch from your desktop environment's application menu
- Run `controller-monitor` from the command line
- The application will appear in your system tray (if supported)
- Receive system notifications when libnotify is available

### From Source

#### Building and Running

1. Ensure you have .NET 9.0 installed.
2. Navigate to the project directory: `cd /mnt/data/Projects/ControllerMonitor/src/ControllerMonitor`
3. Restore packages: `dotnet restore`
4. Build the project: `dotnet build`
5. Run the application: `dotnet run`

## Linux Implementation

The Linux implementation reads battery information from `/sys/class/power_supply/` by:

- Scanning for battery devices
- Identifying game controller batteries by model name
- Parsing uevent files for capacity level and charging status

## Windows Implementation

The Windows implementation uses the XInput API to monitor game controller batteries by:

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
