using System.Text.Json.Serialization;

namespace ControllerMonitor.Models;

[JsonSerializable(typeof(Settings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}