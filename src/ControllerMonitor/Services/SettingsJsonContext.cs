using System.Text.Json.Serialization;
using ControllerMonitor.Models;

namespace ControllerMonitor.Services;

[JsonSerializable(typeof(Settings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}