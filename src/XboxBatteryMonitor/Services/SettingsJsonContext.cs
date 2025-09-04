using System.Text.Json.Serialization;
using XboxBatteryMonitor.Models;

namespace XboxBatteryMonitor.Services;

[JsonSerializable(typeof(SettingsData))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}