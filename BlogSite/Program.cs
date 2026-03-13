using System.Text.Json;
using BlogSite;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => { });

Api.LoadConfiguration();
Api.CreateCacheDir();
Api.Setup();

builder.Services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.Use(async (context, next) =>
{
    await Api.Router.Route(context, CancellationToken.None);
    await next();
});

#if DEBUG
PwdWatcher.Init();
#endif
Api.Baker.CompileAllPages();

app.Run();
