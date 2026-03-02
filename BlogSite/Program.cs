using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    //options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});


var cacheDir = Directory.CreateTempSubdirectory("api-cache");

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
