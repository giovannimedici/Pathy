using DotNetEnv;
using Pathy.API.Configurations;
using QuickJwt.AspNetCore;

// Load environment variables from .env file (if exists)
// The .env file should be in the solution root directory
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddDependencyInjection(builder.Configuration);

var app = builder.Build();

app.UseJwtAuthentication();

app.ConfigureApp();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
