using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oid85.FinMarket.Algo.Application.Interfaces.ApiClients;
using Oid85.FinMarket.Algo.Application.Interfaces.Repositories;
using Oid85.FinMarket.Algo.Common.KnownConstants;
using Oid85.FinMarket.Algo.Infrastructure.ApiClients;
using Oid85.FinMarket.Algo.Infrastructure.Database;
using Oid85.FinMarket.Algo.Infrastructure.Database.Repositories;

namespace Oid85.FinMarket.Algo.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {    
        services.AddDbContextPool<AlgoContext>((serviceProvider, options) =>
        {  
            options.UseNpgsql(configuration.GetValue<string>(KnownSettingsKeys.PostgresAlgoConnectionString)!);
        });

        services.AddPooledDbContextFactory<AlgoContext>(options =>
            options
                .UseNpgsql(configuration.GetValue<string>(KnownSettingsKeys.PostgresAlgoConnectionString)!)
                .EnableServiceProviderCaching(false), poolSize: 32);

        services.AddScoped<IStrategyExecuteResultRepository, StrategyExecuteResultRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();
    }

    public static void ConfigureStorageApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient(KnownHttpClients.FinMarketStorageServiceApiClient, client =>
        {
            string baseUrl = configuration.GetValue<string>(KnownSettingsKeys.FinMarketStorageServiceApiClientBaseAddress)!;
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddScoped<IStorageApiClient, StorageApiClient>();
    }

    public static async Task ApplyMigrations(this IHost host)
    {
        var scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
        await using var scope = scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AlgoContext>();
        await context.Database.MigrateAsync();
    }
}