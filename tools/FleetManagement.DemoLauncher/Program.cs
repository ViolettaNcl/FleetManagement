using System.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;

// Portable offline demonstration only: no API, account, keys or database.
if(args.Any(a=>a!="--no-browser")){Console.Error.WriteLine("Only --no-browser is supported.");return 2;}
var root=AppContext.BaseDirectory;
if(!File.Exists(Path.Combine(root,"wwwroot","index.html"))){Console.Error.WriteLine("Missing wwwroot/index.html. Extract the entire demo archive.");return 2;}
var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=[],ContentRootPath=root,WebRootPath=Path.Combine(root,"wwwroot")});
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app=builder.Build();
var types=new FileExtensionContentTypeProvider();
types.Mappings[".wasm"]="application/wasm";
types.Mappings[".dat"]="application/octet-stream";
types.Mappings[".webcil"]="application/octet-stream";
types.Mappings[".blat"]="application/octet-stream";
app.Use(async (context,next)=>{
    if(context.Connection.LocalIpAddress is not System.Net.IPAddress local||!System.Net.IPAddress.IsLoopback(local)){
        context.Response.StatusCode=403;return;
    }
    context.Response.Headers["X-Content-Type-Options"]="nosniff";
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions{ContentTypeProvider=types});
await app.StartAsync();
var url=app.Urls.Single();
Console.WriteLine("DEMO_URL="+url);
Console.WriteLine("Keep this window open. Ctrl+C stops the demo. Synthetic data stays in your browser.");
if(!args.Contains("--no-browser")){
    try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}
    catch(Exception){Console.WriteLine("Open the URL above in your browser.");}
}
await app.WaitForShutdownAsync();
return 0;
