using System.Text.Json.Serialization;
using FleetManagement.Core;

namespace FleetManagement.Web;

public sealed record DemoSnapshot(FleetData Data, DateTime Now);

[JsonSerializable(typeof(DemoSnapshot))]
internal partial class DemoJsonContext : JsonSerializerContext { }
