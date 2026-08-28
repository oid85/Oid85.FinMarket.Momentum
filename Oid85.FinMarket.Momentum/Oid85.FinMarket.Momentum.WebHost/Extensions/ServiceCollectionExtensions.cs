using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NLog;
using ILogger = NLog.ILogger;

namespace Oid85.FinMarket.Algo.WebHost.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureLogger(this IServiceCollection services)
    {
        LogManager
            .Setup()
            .LoadConfigurationFromFile("nlog.config");

        services.AddTransient(typeof(ILogger), _ => 
            LogManager.GetLogger(AppDomain.CurrentDomain.FriendlyName));
    }

    public static void ConfigureSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            options.MapType<DateOnly>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "date",
                Example = new OpenApiString(DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"))
            });            
            
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Api",
                Description = AppDomain.CurrentDomain.FriendlyName
            });

            options.IncludeXmlComments(GetXmlCommentsPath());
        });
    }

    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.SetIsOriginAllowed(_ => true);
                builder.AllowCredentials();
            });
        });
    }

    private static string GetXmlCommentsPath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SwaggerTest.XML");
    }
}