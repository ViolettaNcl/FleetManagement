using System.Text.Json.Serialization;
using FleetManagement.Core;

namespace FleetManagement.Web;

public sealed record DemoSnapshot(FleetData Data, DateTime Now, List<MaintenanceEntry>? MaintenanceEntries=null, List<DriverShift>? DriverShifts=null, bool RequireShifts=false);

[JsonSerializable(typeof(DemoSnapshot))]
internal partial class DemoJsonContext : JsonSerializerContext { }
