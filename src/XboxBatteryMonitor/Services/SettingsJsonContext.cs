using System.Text.Json.Serialization;
using XboxBatteryMonitor.Models;

namespace XboxBatteryMonitor.Services;

[JsonSerializable(typeof(Settings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}