namespace FleetManagement.Api;

public static class ConnectedConsole
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/console",(HttpContext context)=>Asset(context,"index.html","text/html; charset=utf-8"));
        app.MapGet("/console/locale.js",(HttpContext context)=>Asset(context,"locale.js","text/javascript; charset=utf-8"));
        app.MapGet("/console/app.js",(HttpContext context)=>Asset(context,"app.js","text/javascript; charset=utf-8"));
        app.MapGet("/console/style.css",(HttpContext context)=>Asset(context,"style.css","text/css; charset=utf-8"));
    }
    private static IResult Asset(HttpContext context,string file,string contentType)
    {
        context.Response.Headers.CacheControl="no-store";
        context.Response.Headers["X-Content-Type-Options"]="nosniff";
        context.Response.Headers["Referrer-Policy"]="no-referrer";
        context.Response.Headers["Content-Security-Policy"]="default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
        var stream=typeof(ConnectedConsole).Assembly.GetManifestResourceStream("Fleet.Console."+file);
        return stream is null?Results.NotFound():Results.Stream(stream,contentType);
    }
}
