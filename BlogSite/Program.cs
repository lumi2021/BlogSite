using System.Text.Json;
using BlogSite;

var builder = WebApplication.CreateSlimBuilder(args);



builder.Services.ConfigureHttpJsonOptions(options => { });


// setup
var cacheDir = Directory.CreateTempSubdirectory("api-cache-");
Console.WriteLine($"Cache directory created at '{cacheDir.FullName}'");
var config = JsonSerializer.Deserialize(
    File.ReadAllText("config.json"), ConfigJsonContext.Default.Configuration);
Console.WriteLine($"Configuration file loaded");

Api.Setup(cacheDir, config!);

builder.Services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

var root = app.MapGroup("/");

root.MapGet("/tutorials/", (connection)
    => connection.Response.WriteAsync($"<h1>Tutorials:</h1>"));
root.MapGet("/blog/", (connection)
    => connection.Response.WriteAsync($"<h1>Blogs:</h1>"));

root.MapFallback(async connection =>
{
    Console.WriteLine(connection.Request.Path);
    
    connection.Response.StatusCode = 404;
    connection.Response.ContentType = "text/html";
    await connection.Response.WriteAsync("<h1>404</h1>");
});

app.Run();
