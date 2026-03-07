using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.Models;

public readonly record struct BatteryInfo(bool IsConnected = false, int? Capacity = null, bool IsCharging = false, string? ModelName = null, BatteryLevel Level = BatteryLevel.Unknown);