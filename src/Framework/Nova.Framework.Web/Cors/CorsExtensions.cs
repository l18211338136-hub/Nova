using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nova.Framework.Web.Cors;

public static class CorsExtensions
{
    private const string CorsPolicy = "NovaCorsPolicy";

    public static IServiceCollection AddNovaCors(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new CorsSettings();
        configuration.GetSection(CorsSettings.Position).Bind(settings);

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, builder =>
            {
                if (settings.AllowedOrigins != null && settings.AllowedOrigins.Contains("*"))
                {
                    builder.AllowAnyOrigin();
                }
                else if (settings.AllowedOrigins != null && settings.AllowedOrigins.Length > 0)
                {
                    builder.WithOrigins(settings.AllowedOrigins);
                }

                if (settings.AllowedMethods != null && settings.AllowedMethods.Contains("*"))
                {
                    builder.AllowAnyMethod();
                }
                else if (settings.AllowedMethods != null && settings.AllowedMethods.Length > 0)
                {
                    builder.WithMethods(settings.AllowedMethods);
                }

                if (settings.AllowedHeaders != null && settings.AllowedHeaders.Contains("*"))
                {
                    builder.AllowAnyHeader();
                }
                else if (settings.AllowedHeaders != null && settings.AllowedHeaders.Length > 0)
                {
                    builder.WithHeaders(settings.AllowedHeaders);
                }

                if (settings.AllowCredentials)
                {
                    builder.AllowCredentials();
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseNovaCors(this IApplicationBuilder app)
    {
        return app.UseCors(CorsPolicy);
    }
}
