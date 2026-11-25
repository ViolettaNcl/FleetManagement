namespace FleetManagement.Api;

// Immutable success receipt scoped to an authenticated actor and caller-generated UUID.
public sealed class BatchReceipt
{
    public string ActorId {get;set;}="";
    public Guid Key {get;set;}
    public string PayloadHash {get;set;}="";
    public string ResponseJson {get;set;}="";
    public DateTime CreatedUtc {get;set;}
}
