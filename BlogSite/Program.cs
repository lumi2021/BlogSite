using System.Text.Json;
using BlogSite;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => { });


var config = JsonSerializer.Deserialize(File.ReadAllText("config.json"),
    ConfigJsonContext.Default.Configuration) ?? throw new NotImplementedException();
Console.WriteLine($"Configuration file loaded");

DirectoryInfo cacheDir;
if (!config.UseSystemTmp)
{
    if (Directory.Exists(".local-cache")) Directory.Delete(".local-cache", true);
    cacheDir = Directory.CreateDirectory(".local-cache");
}
else cacheDir = Directory.CreateTempSubdirectory(".local-api-cache-");
Console.WriteLine($"Cache directory created at '{cacheDir.FullName}'");

Api.Setup(cacheDir, config!);

builder.Services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.Use(async (context, next) =>
{
    await Api.Router.Route(context, CancellationToken.None);
    await next();
});
Api.Baker.CompileAllPages();

app.Run();
