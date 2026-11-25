using System.Security.Cryptography;
using System.Text;
using FleetManagement.Api;
using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;

var builder=WebApplication.CreateBuilder(args);
var secret=builder.Configuration["Fleet:ApiKey"];
if (string.IsNullOrWhiteSpace(secret) || secret.Length<32)
    throw new InvalidOperationException("Set Fleet__ApiKey to a randomly generated secret of at least 32 characters.");
var expectedHash=SHA256.HashData(Encoding.UTF8.GetBytes(secret));
var provider=builder.Configuration["Fleet:Provider"]??"Sqlite";
var connection=builder.Configuration.GetConnectionString("Fleet")??"Data Source=fleet-demo.db";
builder.Services.AddDbContext<FleetDbContext>(options=>{
    if(provider.Equals("SqlServer",StringComparison.OrdinalIgnoreCase)) options.UseSqlServer(connection);
    else if(provider.Equals("Sqlite",StringComparison.OrdinalIgnoreCase)) options.UseSqlite(connection);
    else throw new InvalidOperationException("Fleet:Provider must be Sqlite or SqlServer.");
});
builder.Services.AddSingleton<FleetWriteGate>();
builder.Services.AddScoped<FleetStore>();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
var app=builder.Build();
app.UseExceptionHandler();
app.Use(async (context,next)=>{
    if(context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/openapi")){
        var key=context.Request.Headers["X-Fleet-Key"].ToString();
        if(key.Length>512 || !CryptographicOperations.FixedTimeEquals(expectedHash,SHA256.HashData(Encoding.UTF8.GetBytes(key)))){
            context.Response.StatusCode=401;return;
        }
        context.Response.Headers.CacheControl="no-store";
    }
    await next();
});

if(builder.Configuration.GetValue<bool>("Fleet:InitializeDatabase")){
    await using var scope=app.Services.CreateAsyncScope();
    var db=scope.ServiceProvider.GetRequiredService<FleetDbContext>();
    await db.Database.EnsureCreatedAsync();
    if(builder.Configuration.GetValue<bool>("Fleet:SeedDemo") &&
        !await db.Vehicles.AnyAsync() && !await db.Drivers.AnyAsync() && !await db.Trips.AnyAsync()){
        var sample=FleetRules.Sample(DateTime.UtcNow);
        db.Vehicles.AddRange(sample.Vehicles);db.Drivers.AddRange(sample.Drivers);db.Trips.AddRange(sample.Trips);
        await db.SaveChangesAsync();
    }
}
app.MapGet("/health",()=>Results.Ok(new { status="ok" }));
app.MapOpenApi();
FleetEndpoints.Map(app.MapGroup("/api"));
app.Run();
