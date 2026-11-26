using System.Data;
using System.Text;
using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

public sealed record ScheduledReviewOptions(bool Enabled,int IntervalMinutes,string? HistoryPath,ReviewPolicy Policy)
{
    public static ScheduledReviewOptions Read(IConfiguration config){
        var enabled=config.GetValue<bool>("Fleet:ScheduledReviewEnabled");
        var minutes=config.GetValue("Fleet:ScheduledReviewIntervalMinutes",60);
        var policy=new ReviewPolicy(config.GetValue("Fleet:ScheduledReviewHighThreshold",1),config.GetValue("Fleet:ScheduledReviewMediumThreshold",3),config.GetValue("Fleet:ScheduledReviewIncludeMedium",true));
        var path=config["Fleet:ScheduledReviewPath"];
        if(enabled&&(minutes is <1 or >1440||!policy.Valid||string.IsNullOrWhiteSpace(path)||!Path.IsPathFullyQualified(path)))throw new InvalidOperationException("Scheduled review needs an absolute private history path, interval 1–1440 minutes and thresholds 1–100.");
        return new(enabled,minutes,enabled?Path.GetFullPath(path!):null,policy);
    }
}
public sealed record ScheduledReviewStatus(bool Enabled,int IntervalMinutes,string State,DateTime? LastAttemptUtc,IReadOnlyList<ReviewRun> History);
public sealed class ScheduledReview(ScheduledReviewOptions options,IServiceScopeFactory scopes,ILogger<ScheduledReview> logger):BackgroundService
{
    private readonly object sync=new();
    private ScheduledReviewStatus status=new(options.Enabled,options.IntervalMinutes,options.Enabled?"starting":"disabled",null,[]);
    public ScheduledReviewStatus GetStatus(){lock(sync)return status with{History=status.History.ToArray()};}
    private void Set(string state,DateTime? attempt,IReadOnlyList<ReviewRun>? history=null){lock(sync)status=status with{State=state,LastAttemptUtc=attempt,History=history??status.History};}
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){
        if(!options.Enabled)return;
        var path=options.HistoryPath!;
        // A single owner per shared history file; never truncate an existing lock.
        // This is a cooperating local process guard, not a distributed lease.
        FileStream? lease=null;
        try{
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            lease=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            IReadOnlyList<ReviewRun> history=[];
            if(File.Exists(path)){
                if(new FileInfo(path).Length>4096)throw new FormatException("Review history too large.");
                var raw=await File.ReadAllTextAsync(path,stoppingToken);
                history=ReviewHistory.Decode(raw);
                if(raw.Length>0&&history.Count==0)throw new FormatException("Invalid review history.");
            }
            Set("idle",null,history);
            while(!stoppingToken.IsCancellationRequested){
                var now=DateTime.UtcNow;
                var latest=history.FirstOrDefault();
                // No catch-up flood after restart; never suppress a run for a future clock.
                if(latest is null||latest.Clock>now||now-latest.Clock>=TimeSpan.FromMinutes(options.IntervalMinutes)){
                    Set("running",now);
                    try{
                        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        timeout.CancelAfter(TimeSpan.FromSeconds(15));
                        var run=await Capture(now,timeout.Token);
                        var next=ReviewHistory.Add(history,run);
                        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
                        try{
                            await File.WriteAllTextAsync(temp,ReviewHistory.Encode(next),new UTF8Encoding(false),timeout.Token);
                            File.Move(temp,path,true);
                        }finally{if(File.Exists(temp))File.Delete(temp);}
                        history=next;Set("idle",now,history);
                        logger.LogInformation("Scheduled operational review recorded at {Clock}; high {High}, medium {Medium}.",now,run.High,run.Medium);
                    }catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
                    catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or System.Data.Common.DbException or ArgumentException or InvalidOperationException or OperationCanceledException){
                        Set("error",now);logger.LogWarning("Scheduled operational review failed ({ErrorType}); no fleet records changed.",ex.GetType().Name);
                    }
                }
                // Retry only at a bounded interval; no busy loop after failure.
                await Task.Delay(TimeSpan.FromMinutes(Math.Min(options.IntervalMinutes,1)),stoppingToken);
            }
        }catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or FormatException){
            Set("unavailable",DateTime.UtcNow);logger.LogWarning("Scheduled review history unavailable ({ErrorType}); worker stopped.",ex.GetType().Name);
        }finally{lease?.Dispose();}
    }
    private async Task<ReviewRun> Capture(DateTime now,CancellationToken cancel){
        await using var scope=scopes.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<FleetDbContext>();
        var operations=scope.ServiceProvider.GetRequiredService<OperationsPolicy>();
        db.Database.SetCommandTimeout(5);
        // Read a consistent bounded register view; release the transaction before calculation.
        FleetData fleet;List<DriverShift> shifts;List<VehicleReservation> reservations;
        await using(var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,cancel)){
            var vehicles=await db.Vehicles.AsNoTracking().OrderBy(v=>v.Id).Take(SnapshotValidation.MaxVehicles+1).ToListAsync(cancel);
            var drivers=await db.Drivers.AsNoTracking().OrderBy(d=>d.Id).Take(SnapshotValidation.MaxDrivers+1).ToListAsync(cancel);
            var trips=await db.Trips.AsNoTracking().OrderBy(t=>t.Id).Take(SnapshotValidation.MaxTrips+1).ToListAsync(cancel);
            shifts=await db.DriverShifts.AsNoTracking().OrderBy(s=>s.Id).Take(ResourcePlanning.MaxShifts+1).ToListAsync(cancel);
            reservations=await db.VehicleReservations.AsNoTracking().OrderBy(x=>x.Id).Take(VehicleReservations.MaxReservations+1).ToListAsync(cancel);
            if(vehicles.Count>SnapshotValidation.MaxVehicles||drivers.Count>SnapshotValidation.MaxDrivers||trips.Count>SnapshotValidation.MaxTrips||shifts.Count>ResourcePlanning.MaxShifts||reservations.Count>VehicleReservations.MaxReservations)throw new InvalidOperationException("Review capacity exceeded.");
            fleet=new(vehicles,drivers,trips);
            await tx.CommitAsync(cancel);
        }
        cancel.ThrowIfCancellationRequested();
        return ReviewHistory.Capture(now,options.Policy,FleetAdvisor.Assess(fleet,now,shifts,operations.RequireShifts,reservations,clockLabel:"UTC"));
    }
}
