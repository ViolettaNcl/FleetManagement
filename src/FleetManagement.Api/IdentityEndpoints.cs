using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

public static class IdentityEndpoints
{
    private static readonly string[] Roles=["Admin","Dispatcher","Viewer"];

    public static async Task Initialize(IServiceProvider services,IConfiguration config)
    {
        var roles=services.GetRequiredService<RoleManager<IdentityRole>>();
        var users=services.GetRequiredService<UserManager<IdentityUser>>();
        foreach(var role in Roles)
            if(!await roles.RoleExistsAsync(role))
                Ensure(await roles.CreateAsync(new IdentityRole(role)));
        var email=config["Fleet:BootstrapAdminEmail"];
        var password=config["Fleet:BootstrapAdminPassword"];
        if(string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))return;
        if(string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Provide both bootstrap administrator settings.");
        if((await users.GetUsersInRoleAsync("Admin")).Count>0)return;
        if(await users.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("Bootstrap email already belongs to a non-admin account.");
        var db=services.GetRequiredService<FleetDbContext>();
        await using var tx=await db.Database.BeginTransactionAsync();
        var admin=new IdentityUser{UserName=email,Email=email,LockoutEnabled=true};
        Ensure(await users.CreateAsync(admin,password));
        Ensure(await users.AddToRoleAsync(admin,"Admin"));
        await tx.CommitAsync();
    }

    public static void Map(RouteGroupBuilder api)
    {
        api.MapPost("/auth/login",async Task<IResult> (LoginInput input,UserManager<IdentityUser> users,SignInManager<IdentityUser> signIn)=>{
            if(string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrEmpty(input.Password) || input.Email.Length>254 || input.Password.Length>512)
                return Results.Unauthorized();
            var user=await users.FindByEmailAsync(input.Email.Trim());
            if(user is null || !(await signIn.CheckPasswordSignInAsync(user,input.Password,lockoutOnFailure:true)).Succeeded)
                return Results.Unauthorized();
            return Results.SignIn(await signIn.CreateUserPrincipalAsync(user),authenticationScheme:IdentityConstants.BearerScheme);
        }).AllowAnonymous().RequireRateLimiting("Login");

        api.MapGet("/auth/me",(HttpContext context)=>Results.Ok(new{
            id=context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            name=context.User.Identity?.Name,
            roles=context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c=>c.Value)
        }));

        api.MapPost("/auth/revoke-sessions",async Task<IResult> (HttpContext context,UserManager<IdentityUser> users)=>{
            if(context.User.IsInRole("Automation"))return Results.Forbid();
            var user=await users.GetUserAsync(context.User);if(user is null)return Results.Unauthorized();
            var result=await users.UpdateSecurityStampAsync(user);
            return result.Succeeded?Results.NoContent():Results.Conflict(new{error="Unable to revoke sessions; retry after signing in."});
        });

        api.MapPost("/admin/users/{id}/revoke-sessions",async Task<IResult> (string id,UserManager<IdentityUser> users)=>{
            var user=await users.FindByIdAsync(id);if(user is null)return Results.NotFound();
            if(await users.IsInRoleAsync(user,"Admin"))return Results.BadRequest(new{error="Administrator accounts must revoke their own sessions."});
            var result=await users.UpdateSecurityStampAsync(user);
            return result.Succeeded?Results.NoContent():Results.Conflict(new{error="Account changed; refresh and retry."});
        }).RequireAuthorization("FleetAdmin");

        api.MapPut("/admin/users/{id}/role",async Task<IResult> (string id,ChangeRoleInput input,UserManager<IdentityUser> users,FleetDbContext db)=>{
            if(input.Role is not ("Dispatcher" or "Viewer")||input.ExpectedRole is not ("Dispatcher" or "Viewer"))
                return Results.BadRequest(new{error="Provide existing and desired staff roles."});
            await using var tx=await db.Database.BeginTransactionAsync();
            var user=await users.FindByIdAsync(id);if(user is null)return Results.NotFound();
            var roles=await users.GetRolesAsync(user);
            if(roles.Count!=1||roles[0]=="Admin")return Results.BadRequest(new{error="Only single-role staff accounts can be changed."});
            if(roles[0]!=input.ExpectedRole)return Results.Conflict(new{error="Account role changed. Refresh and review again."});
            if(roles[0]==input.Role)return Results.NoContent();
            if(!(await users.RemoveFromRoleAsync(user,roles[0])).Succeeded||
               !(await users.AddToRoleAsync(user,input.Role)).Succeeded||
               !(await users.UpdateSecurityStampAsync(user)).Succeeded)
                return Results.Conflict(new{error="Account changed; no role change committed."});
            await tx.CommitAsync();
            return Results.NoContent();
        }).RequireAuthorization("FleetAdmin");

        api.MapGet("/admin/users",async Task<IResult> (FleetDbContext db,int? offset,int? limit,CancellationToken ct)=>{
            var skip=Math.Clamp(offset??0,0,1000000);var take=Math.Clamp(limit??50,1,100);
            var accounts=await db.Users.AsNoTracking().OrderBy(u=>u.Email).ThenBy(u=>u.Id).Skip(skip).Take(take).Select(u=>new{u.Id,u.Email,u.LockoutEnd}).ToListAsync(ct);
            var ids=accounts.Select(u=>u.Id).ToArray();
            var assignments=await (from ur in db.UserRoles.AsNoTracking() join role in db.Roles.AsNoTracking() on ur.RoleId equals role.Id where ids.Contains(ur.UserId) select new{ur.UserId,role.Name}).ToListAsync(ct);
            var total=await db.Users.CountAsync(ct);
            var now=DateTimeOffset.UtcNow;
            return Results.Ok(new{offset=skip,limit=take,total,accounts=accounts.Select(u=>new{u.Id,u.Email,locked=u.LockoutEnd>now,roles=assignments.Where(a=>a.UserId==u.Id).Select(a=>a.Name).ToArray()})});
        }).RequireAuthorization("FleetAdmin");

        api.MapPost("/admin/users",async Task<IResult> (CreateUserInput input,UserManager<IdentityUser> users,FleetDbContext db)=>{
            if(string.IsNullOrWhiteSpace(input.Email)||input.Email.Length>254||string.IsNullOrWhiteSpace(input.Password)||input.Password.Length>512||
               input.Role is not ("Dispatcher" or "Viewer"))
                return Results.BadRequest(new{error="Provide email, password and Dispatcher or Viewer role."});
            await using var tx=await db.Database.BeginTransactionAsync();
            var user=new IdentityUser{Email=input.Email.Trim(),UserName=input.Email.Trim(),LockoutEnabled=true};
            var created=await users.CreateAsync(user,input.Password);
            if(!created.Succeeded)return Results.BadRequest(new{errors=created.Errors.Select(e=>e.Description)});
            var assigned=await users.AddToRoleAsync(user,input.Role);
            if(!assigned.Succeeded)return Results.BadRequest(new{errors=assigned.Errors.Select(e=>e.Description)});
            await tx.CommitAsync();
            return Results.Created("/api/admin/users",new{id=user.Id,email=user.Email,role=input.Role});
        }).RequireAuthorization("FleetAdmin");

        // Aggregate operational metadata only. Never disclose actor IDs, keys or response bodies.
        api.MapGet("/admin/receipt-capacity",async (FleetDbContext db,CancellationToken ct)=>{
            var count=await db.BatchReceipts.CountAsync(ct);
            var oldest=await db.BatchReceipts.MinAsync(x=>(DateTime?)x.CreatedUtc,ct);
            var newest=await db.BatchReceipts.MaxAsync(x=>(DateTime?)x.CreatedUtc,ct);
            return Results.Ok(new{count,capacity=ReceiptPolicy.Capacity,remaining=Math.Max(0,ReceiptPolicy.Capacity-count),
                warningThreshold=ReceiptPolicy.WarningThreshold,criticalThreshold=ReceiptPolicy.CriticalThreshold,
                pressure=ReceiptPolicy.Pressure(count),oldestUtc=oldest,newestUtc=newest,automaticExpiry=false});
        }).RequireAuthorization("FleetAdmin");

        api.MapGet("/admin/audit",async (FleetDbContext db,int? limit,CancellationToken ct)=>
            await db.AuditEntries.AsNoTracking().OrderByDescending(a=>a.Id).Take(Math.Clamp(limit??100,1,500)).ToListAsync(ct))
            .RequireAuthorization("FleetAdmin");
    }
    private static void Ensure(IdentityResult result)
    {
        if(!result.Succeeded)throw new InvalidOperationException("Identity initialization failed: "+string.Join(", ",result.Errors.Select(e=>e.Code)));
    }
}
public sealed record LoginInput(string? Email,string? Password);
public sealed record CreateUserInput(string? Email,string? Password,string? Role);

public sealed record ChangeRoleInput(string? ExpectedRole,string? Role);
