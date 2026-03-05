using System.Text.Json;
using BlogSite;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => { });


// setup
var cacheDir = Directory.CreateTempSubdirectory("api-cache-");
Console.WriteLine($"Cache directory created at '{cacheDir.FullName}'");
var config = JsonSerializer.Deserialize(File.ReadAllText("config.json"), ConfigJsonContext.Default.Configuration);
Console.WriteLine($"Configuration file loaded");

Api.Setup(cacheDir, config!);

// setup OpenApi
builder.Services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

// setup router
var router = new Router();
app.Use(async (context, next) =>
{
    await router.Route(context, CancellationToken.None);
    await next();
});

app.Run();
