using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using FleetManagement.Api;
using FleetManagement.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder=WebApplication.CreateBuilder(args);
const long MaxRequestBytes=256*1024;
builder.WebHost.ConfigureKestrel(options=>options.Limits.MaxRequestBodySize=MaxRequestBytes);
var secret=builder.Configuration["Fleet:ApiKey"];
if (string.IsNullOrWhiteSpace(secret) || secret.Length<32)
    throw new InvalidOperationException("Set Fleet__ApiKey to a randomly generated secret of at least 32 characters.");
builder.Services.AddSingleton(new FleetKeySecret(SHA256.HashData(Encoding.UTF8.GetBytes(secret))));
var provider=builder.Configuration["Fleet:Provider"]??"Sqlite";
var connection=builder.Configuration.GetConnectionString("Fleet")??"Data Source=fleet-demo-v2.db";
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<FleetDbContext>(options=>{
    if(provider.Equals("SqlServer",StringComparison.OrdinalIgnoreCase)) options.UseSqlServer(connection);
    else if(provider.Equals("Sqlite",StringComparison.OrdinalIgnoreCase)) options.UseSqlite(connection);
    else throw new InvalidOperationException("Fleet:Provider must be Sqlite or SqlServer.");
});
builder.Services.AddIdentityCore<IdentityUser>(options=>{
    options.User.RequireUniqueEmail=true;
    options.Password.RequiredLength=12;
    options.Password.RequireNonAlphanumeric=true;
    options.Lockout.MaxFailedAccessAttempts=5;
    options.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<FleetDbContext>().AddSignInManager();
builder.Services.AddAuthentication(options=>{
    options.DefaultScheme="FleetAuth";
    options.DefaultChallengeScheme="FleetAuth";
    options.DefaultForbidScheme="FleetAuth";
}).AddPolicyScheme("FleetAuth","Fleet bearer or automation key",options=>{
    options.ForwardDefaultSelector=context=>context.Request.Headers.ContainsKey("X-Fleet-Key")?"FleetKey":IdentityConstants.BearerScheme;
}).AddBearerToken(IdentityConstants.BearerScheme,options=>{
    options.BearerTokenExpiration=TimeSpan.FromMinutes(15);
}).AddScheme<AuthenticationSchemeOptions,FleetKeyHandler>("FleetKey",_=>{});
builder.Services.AddAuthorization(options=>{
    options.AddPolicy("FleetRead",p=>p.RequireAuthenticatedUser().RequireRole("Admin","Dispatcher","Viewer","Automation"));
    options.AddPolicy("FleetWrite",p=>p.RequireAuthenticatedUser().RequireRole("Admin","Dispatcher","Automation"));
    options.AddPolicy("FleetAdmin",p=>p.RequireAuthenticatedUser().RequireRole("Admin"));
});
builder.Services.AddRateLimiter(options=>{
    options.RejectionStatusCode=429;
    options.AddPolicy("Login",context=>RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{
            PermitLimit=15,Window=TimeSpan.FromMinutes(1),QueueLimit=0
        }));
});
builder.Services.AddSingleton<FleetWriteGate>();
builder.Services.AddSingleton(new OperationsPolicy(builder.Configuration.GetValue<bool>("Fleet:RequireShifts")));
builder.Services.AddScoped<FleetStore>();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
var app=builder.Build();
app.UseExceptionHandler();
app.Use(async (context,next)=>{
    if(context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/openapi")){
        context.Response.Headers.CacheControl="no-store";
        context.Response.Headers["X-Content-Type-Options"]="nosniff";
        if(context.Request.ContentLength>MaxRequestBytes){context.Response.StatusCode=413;await context.Response.WriteAsJsonAsync(new{error="Request body exceeds 256 KiB."});return;}
    }
    await next();
});
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
// Identity bearer tickets are short-lived, but role changes and explicit revocation
// must also invalidate tickets before their expiry on subsequent HTTP requests.
app.Use(async (context,next)=>{
    if(context.User.Identity?.IsAuthenticated==true&&!context.User.IsInRole("Automation")
       &&!context.Request.Path.Equals(new PathString("/api/auth/login"))){
        var signIn=context.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
        if(await signIn.ValidateSecurityStampAsync(context.User) is null){
            context.Response.StatusCode=401;return;
        }
    }
    await next();
});
app.UseAuthorization();

if(builder.Configuration.GetValue<bool>("Fleet:UpgradeV2Schema")&&!builder.Configuration.GetValue<bool>("Fleet:InitializeDatabase"))
    throw new InvalidOperationException("Fleet:UpgradeV2Schema requires Fleet:InitializeDatabase.");
if(builder.Configuration.GetValue<bool>("Fleet:InitializeDatabase")){
    await using var scope=app.Services.CreateAsyncScope();
    var db=scope.ServiceProvider.GetRequiredService<FleetDbContext>();
    if(builder.Configuration.GetValue<bool>("Fleet:UpgradeV2Schema")){
        await OperationsSchemaUpgrade.Run(db);
        app.Logger.LogInformation("Operational schema upgrade verified. Disable Fleet:UpgradeV2Schema for normal startup.");
    }else await db.Database.EnsureCreatedAsync();
    // Fail before identity initialization or demo seeding on an incompatible schema.
    try { await OperationsSchemaUpgrade.Verify(db); }
    catch (System.Data.Common.DbException ex) { throw new InvalidOperationException("V3 operational schema missing or inaccessible. Back up the existing database; use the documented explicit upgrade or a fresh V3 database.",ex); }
    await IdentityEndpoints.Initialize(scope.ServiceProvider,builder.Configuration);
    if(builder.Configuration.GetValue<bool>("Fleet:SeedDemo") &&
        !await db.Vehicles.AnyAsync() && !await db.Drivers.AnyAsync() && !await db.Trips.AnyAsync()){
        var sample=FleetRules.Sample(DateTime.UtcNow);
        db.Vehicles.AddRange(sample.Vehicles);db.Drivers.AddRange(sample.Drivers);db.Trips.AddRange(sample.Trips);
        await db.SaveChangesAsync();
    }
}
ConnectedConsole.Map(app);
app.MapGet("/health",()=>Results.Ok(new { status="ok" }));
app.MapOpenApi().RequireAuthorization("FleetRead");
app.MapHub<FleetHub>("/hubs/fleet",options=>options.CloseOnAuthenticationExpiration=true).RequireAuthorization("FleetRead");
var api=app.MapGroup("/api").RequireAuthorization("FleetRead");
FleetEndpoints.Map(api);
FinancialEndpoints.Map(api);
OperationsEndpoints.Map(api);
OverviewEndpoints.Map(api);
IdentityEndpoints.Map(api);
app.Run();
