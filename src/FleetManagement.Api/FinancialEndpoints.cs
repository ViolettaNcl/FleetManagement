using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

public sealed record FinanceReportInput(DateOnly From,DateOnly To,CostAssumptions? Rates,decimal Budget,decimal FixedAllowance)
{
    public DateTime Start=>From.ToDateTime(TimeOnly.MinValue);
    public DateTime End=>To.ToDateTime(TimeOnly.MinValue);
    public bool Valid=>FleetReport.ValidRange(Start,End)&&Rates is {Valid:true}&&Budget is >=0 and <=1000000000&&FixedAllowance is >=0 and <=1000000000;
}

public static class FinancialEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapPost("/reports/finance/summary",async Task<IResult> (FinanceReportInput input,FleetStore store,FleetDbContext db,CancellationToken ct)=>{
            if(!input.Valid)return Invalid();
            var now=DateTime.UtcNow;var fleet=await store.Read(ct);
            var work=await Work(db,input,ct);
            return Results.Ok(new{asOfUtc=now,completedEstimate=FleetEconomics.Build(fleet,now,input.Start,input.End,input.Rates!),
                budgetProjection=FleetBudget.Build(fleet,now,input.Start,input.End,input.Rates!,input.Budget,input.FixedAllowance),
                recordedMaintenance=MaintenanceJournal.Totals(work,input.Start,input.End)});
        });
        api.MapPost("/reports/finance/excel",async Task<IResult> (FinanceReportInput input,FleetStore store,FleetDbContext db,CancellationToken ct)=>{
            if(!input.Valid)return Invalid();
            var now=DateTime.UtcNow;var fleet=await store.Read(ct);var work=await Work(db,input,ct);
            var bytes=FleetReport.ExportFinanceXlsx(fleet,now,input.Start,input.End,input.Rates!,input.Budget,input.FixedAllowance,work);
            return Results.File(bytes,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"fleet-finance-{input.From:yyyy-MM-dd}-{input.To:yyyy-MM-dd}.xlsx");
        });
    }
    private static Task<List<MaintenanceEntry>> Work(FleetDbContext db,FinanceReportInput input,CancellationToken ct){
        var start=input.Start;var until=input.End.AddDays(1);
        return db.MaintenanceEntries.AsNoTracking().Where(e=>e.RecordedAt>=start&&e.RecordedAt<until).OrderBy(e=>e.Id).ToListAsync(ct);
    }
    private static IResult Invalid()=>Results.BadRequest(new{error="Choose an inclusive period of up to 366 days and valid nonnegative cost/budget assumptions."});
}
