using System.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;

// Portable offline demonstration only: no API, account, keys or database.
int port=5185;bool noBrowser=false;
for(int i=0;i<args.Length;i++){
    if(args[i]=="--no-browser"){noBrowser=true;continue;}
    if(args[i]=="--port"&&i+1<args.Length&&int.TryParse(args[++i],out var value)&&(value==0||value is >=1024 and <=65535)){port=value;continue;}
    Console.Error.WriteLine("Usage: FleetManagement.Demo [--no-browser] [--port 1024-65535 or 0 for ephemeral]");return 2;
}
var root=AppContext.BaseDirectory;
if(!File.Exists(Path.Combine(root,"wwwroot","index.html"))){Console.Error.WriteLine("Missing wwwroot/index.html. Extract the entire demo archive.");return 2;}
var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=[],ContentRootPath=root,WebRootPath=Path.Combine(root,"wwwroot")});
builder.WebHost.ConfigureKestrel(options=>options.Listen(System.Net.IPAddress.Loopback,port));
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
try{await app.StartAsync();}
catch(IOException){Console.Error.WriteLine("Local port unavailable. Close the other instance or choose --port; another port has separate browser storage.");return 1;}
var url=app.Urls.Single();
Console.WriteLine("DEMO_URL="+url);
Console.WriteLine("Keep this window open. Ctrl+C stops the demo. Synthetic data stays in your browser.");
if(!noBrowser){
    try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}
    catch(Exception){Console.WriteLine("Open the URL above in your browser.");}
}
await app.WaitForShutdownAsync();
return 0;
