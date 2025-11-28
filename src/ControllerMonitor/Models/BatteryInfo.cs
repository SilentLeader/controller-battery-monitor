using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.Models;

public record class BatteryInfo(bool IsConnected = false, int? Capacity = null, bool IsCharging = false, string? ModelName = null, BatteryLevel Level = BatteryLevel.Unknown);