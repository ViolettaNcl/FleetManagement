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
