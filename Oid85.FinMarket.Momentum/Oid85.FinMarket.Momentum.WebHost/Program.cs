using System.Globalization;
using System.Text.Json.Serialization;
using Oid85.FinMarket.Algo.Application.Extensions;
using Oid85.FinMarket.Algo.Common.Converters;
using Oid85.FinMarket.Algo.Common.KnownConstants;
using Oid85.FinMarket.Algo.Core.Configuration;
using Oid85.FinMarket.Algo.Infrastructure.Extensions;
using Oid85.FinMarket.Algo.WebHost.Extensions;

namespace Oid85.FinMarket.Algo.WebHost
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<AlgoSettings>(
                builder.Configuration.GetSection("AlgoSettings")
            );

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
                    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals;
                });

            builder.Services.AddMemoryCache();
            builder.Services.ConfigureLogger();
            builder.Services.ConfigureSwagger(builder.Configuration);
            builder.Services.ConfigureCors(builder.Configuration);
            builder.Services.ConfigureApplicationServices();
            builder.Services.ConfigureInfrastructure(builder.Configuration);
            builder.Services.ConfigureStorageApiClient(builder.Configuration);

            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "Oid85.FinMarket.Algo";
            });

            bool applyMigrations = builder.Configuration.GetValue<bool>(KnownSettingsKeys.PostgresApplyMigrationsOnStart);
            int port = builder.Configuration.GetValue<int>(KnownSettingsKeys.DeployPort);

            var app = builder.Build();

            if (applyMigrations)
                await app.ApplyMigrations();

            app.UseRouting();

            app.UseCors("CorsPolicy");

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api v1");
            });

            app.MapControllers();

            app.Urls.Add($"http://0.0.0.0:{port}");

            await app.RunAsync();
        }
    }
}
