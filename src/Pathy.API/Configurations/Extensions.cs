using Microsoft.OpenApi.Models;
using Pathy.API.Endpoints;
using Pathy.API.Middlewares;
using Pathy.Infrastructure;

namespace Pathy.API.Configurations;

public static class Extensions
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddInfrastructure(configuration);
        services.AddEndpointsApiExplorer();
        services.AddSwagger();
        services.AddRateLimiting(configuration);
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Pathy API",
                Version = "v1"
            });
        });
        return services;
    }

    public static WebApplication ConfigureApp(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseHttpsRedirection();
        
        // Rate limiting middleware (must be before endpoints)
        app.UseRateLimitingMiddleware();
        app.UseRateLimitHeaders();
        
        app.MapEndpoints();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapShortLinkEndpoints();
        return app;
    }
}
