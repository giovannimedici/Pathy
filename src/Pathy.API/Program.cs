using Pathy.API.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServices();

var app = builder.Build();

app.ConfigureApp();

app.Run();
