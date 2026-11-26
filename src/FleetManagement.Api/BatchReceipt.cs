namespace FleetManagement.Api;

// Immutable success receipt scoped to an authenticated actor and effective UUID.
public sealed class BatchReceipt
{
    public string ActorId {get;set;}="";
    public Guid Key {get;set;}
    public string PayloadHash {get;set;}="";
    public string ResponseJson {get;set;}="";
    public DateTime CreatedUtc {get;set;}
}

// Single shared policy for ordinary writes, batch writes and administrator monitoring.
// Receipts do not expire automatically: removal could permit duplicate execution.
public static class ReceiptPolicy
{
    public const int Capacity=50000;
    public const int WarningThreshold=40000;
    public const int CriticalThreshold=47500;
    public static string Pressure(int count)=>count>=Capacity?"Full":count>=CriticalThreshold?"Critical":count>=WarningThreshold?"Warning":"Healthy";
}
