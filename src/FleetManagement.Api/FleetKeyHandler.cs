using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FleetManagement.Api;

public sealed record FleetKeySecret(byte[] Hash);
public sealed class FleetKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, FleetKeySecret secret)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var key=Request.Headers["X-Fleet-Key"].ToString();
        if(key.Length is 0 or >512 || !CryptographicOperations.FixedTimeEquals(secret.Hash,SHA256.HashData(Encoding.UTF8.GetBytes(key))))
            return Task.FromResult(AuthenticateResult.Fail("Invalid automation key."));
        var identity=new ClaimsIdentity(new[]{
            new Claim(ClaimTypes.NameIdentifier,"automation"),
            new Claim(ClaimTypes.Name,"Local automation"),
            new Claim(ClaimTypes.Role,"Automation")
        },Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity),Scheme.Name)));
    }
}
