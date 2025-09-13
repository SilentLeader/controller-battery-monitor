# Copilot Instructions for Controller Monitor

## Project Overview
Cross-platform Avalonia UI application (.NET 9) that monitors game controller battery levels using platform-specific APIs. Features single-instance management, system tray integration, and real-time battery monitoring.

## Key Architecture Patterns

### Platform Abstraction Strategy
- **Core Pattern**: Interface + Base Class + Platform Implementations
- **Services**: `IBatteryMonitorService` → `BatteryMonitorServiceBase` → `BatteryMonitorLinux`/`BatteryMonitorWindows`
- **Conditional Compilation**: Use `#if WINDOWS` / `#if LINUX` preprocessor directives
- **DI Registration**: Platform detection in `Program.ConfigureServices()` using `RuntimeInformation.IsOSPlatform()`

### Battery Monitoring Implementation
- **Linux**: Reads `/sys/class/power_supply/` uevent files for controller battery data
- **Windows**: Uses XInput API (`xinput1_4.dll`) with P/Invoke declarations
- **Common Pattern**: Timer-based polling with event-driven updates via `BatteryInfoChanged` event
- **Data Flow**: Platform Service → Base Service → ViewModel → UI

### Dependency Injection Structure
```csharp
// In Program.cs - Platform-specific service registration
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    services.AddSingleton<IBatteryMonitorService, BatteryMonitorWindows>();
    services.AddSingleton<ISettingsService, SettingsServiceWindows>();
}
// Resolve via static ServiceProvider property
```

### MVVM + CommunityToolkit.Mvvm
- **ViewModels**: Use `[ObservableProperty]` for automatic property change notifications
- **Commands**: Use `[RelayCommand]` for command binding
- **Key ViewModels**: `MainWindowViewModel`, `BatteryInfoViewModel`, `ControllerInfoViewModel`
- **Converters**: Multi-value converters for battery icons (`BatteryLevelToIconConverter`)

## Development Workflows

### Building & Running
```bash
# From project root
cd src/ControllerMonitor
dotnet restore
dotnet build
dotnet run

# Use existing VS Code task: run_task("process: build")
```

### Platform-Specific Development
- **Adding Platform Features**: Extend base classes in `Platforms/Linux/` or `Platforms/Windows/`
- **Cross-Platform Testing**: Use conditional compilation constants `WINDOWS`/`LINUX`
- **AOT Publishing**: Project configured for `PublishAot=true` - avoid reflection-heavy patterns

### Settings & Configuration
- **Settings**: JSON serialization with `SettingsJsonContext` for AOT compatibility
- **Platform Storage**: Linux uses `~/.config/`, Windows uses AppData
- **Auto-persistence**: Settings automatically saved on property changes

## Critical Implementation Details

### Battery Level Mapping
- **Enum**: `BatteryLevel.Empty/Low/Normal/High/Full` (discrete levels)
- **Platform Differences**: Linux provides percentage, Windows only discrete XInput levels
- **Icon System**: Dual theme (light/dark) PNG assets in `Assets/icons/`

### Single Instance Management
- **Service**: `SingleInstanceService` handles mutex-based single instance enforcement
- **Window Management**: Brings existing instance to front when second instance launched
- **Critical**: Must be used in `using` statement in `Program.Main()`

### Avalonia-Specific Patterns
- **Assets**: Use `avares://` URIs for embedded resources
- **Theming**: Supports light/dark theme detection via `Application.Current?.ActualThemeVariant`
- **System Tray**: Custom tray icon with battery level indication and tooltip
- **Notifications**: `WindowNotificationManager` for in-app notifications

## Common Implementation Patterns

### Adding New Battery Sources
1. Create platform-specific implementation extending `BatteryMonitorServiceBase`
2. Override `GetBatteryInfoAsync()` method
3. Register in `Program.ConfigureServices()` with platform detection
4. Follow timer-based polling pattern from base class

### Extending Settings
1. Add property to `Models/Settings.cs` with `[JsonPropertyName]`
2. Update `SettingsViewModel` with corresponding `[ObservableProperty]`
3. Add UI binding in AXAML files
4. Settings auto-persist via `ISettingsService.SaveSettings()`

### Adding Converters
- Implement `IValueConverter` or `IMultiValueConverter`
- Register in AXAML resources section
- Use for complex property transformations (e.g., battery level → icon path)

## Testing Considerations
- **Platform Testing**: Each platform implementation needs separate validation
- **Battery States**: Test disconnected, low battery, charging scenarios
- **UI States**: Verify tray icon updates, notification triggers, window positioning