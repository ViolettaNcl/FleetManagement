using Microsoft.AspNetCore.SignalR;

namespace FleetManagement.Api;

// Authenticated read-only notification channel. Clients fetch fresh state using
// the authorized API; no driver details or records are broadcast.
public sealed class FleetHub : Hub { }
public sealed record FleetChanged(DateTimeOffset OccurredUtc);
