namespace FleetManagement.Api;

public sealed class AuditEntry
{
    public long Id { get; set; }
    public DateTime OccurredUtc { get; set; }
    public string ActorId { get; set; } = "";
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string Action { get; set; } = "";
}
